using System.Text.Json;
using T20Master.Web.Data;
using T20Master.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace T20Master.Web.Services.AI;

public class RagIndexer
{
    private readonly ApplicationDbContext _db;
    private readonly PdfProcessor _pdfProcessor;
    private readonly EmbeddingService _embeddingService;
    private readonly ILogger<RagIndexer> _logger;

    private const string FailedChunksFile = "failed-chunks.json";

    public RagIndexer(
        ApplicationDbContext db,
        PdfProcessor pdfProcessor,
        EmbeddingService embeddingService,
        ILogger<RagIndexer> logger)
    {
        _db = db;
        _pdfProcessor = pdfProcessor;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task IndexPdfAsync(string pdfPath)
    {
        var existingCount = await _db.RuleChunks.CountAsync();
        if (existingCount > 0)
        {
            _logger.LogInformation("RuleChunks already indexed ({Count} chunks). Skipping. Use --clear-chunks first to re-index.", existingCount);
            return;
        }

        _logger.LogInformation("Extracting text from PDF: {Path}", pdfPath);
        var pages = _pdfProcessor.ExtractPages(pdfPath);
        _logger.LogInformation("Extracted {Count} pages", pages.Count);

        var pagesWithLowText = pages.Where(p => p.Text.Length < 100).ToList();
        if (pagesWithLowText.Any())
        {
            _logger.LogWarning("Pages with low text extraction ({Count}): {Pages}",
                pagesWithLowText.Count,
                string.Join(", ", pagesWithLowText.Select(p => p.PageNumber)));
        }

        _logger.LogInformation("Chunking text...");
        var chunks = _pdfProcessor.ChunkText(pages);
        _logger.LogInformation("Created {Count} chunks", chunks.Count);

        var failedChunks = await ProcessChunksAsync(chunks);

        var totalSaved = await _db.RuleChunks.CountAsync();
        _logger.LogInformation("=== INDEXING COMPLETE ===");
        _logger.LogInformation("Chunks saved: {Saved}", totalSaved);
        _logger.LogInformation("Chunks failed: {Failed}", failedChunks.Count);

        if (failedChunks.Count > 0)
        {
            SaveFailedChunks(failedChunks);
            _logger.LogWarning("Failed chunks saved to {File}. Use --retry-failed to retry.", FailedChunksFile);
            _logger.LogWarning("Failed pages: {Pages}", string.Join(", ", failedChunks.Select(c => c.SourcePage).Distinct().OrderBy(p => p)));
        }
    }

    public async Task RetryFailedAsync()
    {
        if (!File.Exists(FailedChunksFile))
        {
            _logger.LogInformation("No {File} found. Nothing to retry.", FailedChunksFile);
            return;
        }

        var json = await File.ReadAllTextAsync(FailedChunksFile);
        var chunks = JsonSerializer.Deserialize<List<TextChunk>>(json) ?? [];

        _logger.LogInformation("Retrying {Count} failed chunks...", chunks.Count);

        var failedAgain = await ProcessChunksAsync(chunks);

        var totalSaved = await _db.RuleChunks.CountAsync();
        _logger.LogInformation("=== RETRY COMPLETE ===");
        _logger.LogInformation("Total chunks in DB: {Total}", totalSaved);
        _logger.LogInformation("Still failed: {Failed}", failedAgain.Count);

        if (failedAgain.Count > 0)
        {
            SaveFailedChunks(failedAgain);
            _logger.LogWarning("Remaining failures saved to {File}. Run --retry-failed again later.", FailedChunksFile);
        }
        else
        {
            File.Delete(FailedChunksFile);
            _logger.LogInformation("All chunks processed successfully! {File} removed.", FailedChunksFile);
        }
    }

    private async Task<List<TextChunk>> ProcessChunksAsync(List<TextChunk> chunks)
    {
        var failedChunks = new List<TextChunk>();

        for (int i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            _logger.LogInformation("Chunk {Current}/{Total} ({Percent}%) page {Page} [{Theme}]",
                i + 1, chunks.Count, (i + 1) * 100 / chunks.Count, chunk.SourcePage, chunk.Theme);

            var embedding = await GenerateWithRetryAsync(chunk.Content, chunk.SourcePage);
            if (embedding == null)
            {
                failedChunks.Add(chunk);
                continue;
            }

            _db.RuleChunks.Add(new RuleChunk
            {
                Id = Guid.NewGuid(),
                Content = chunk.Content,
                Embedding = embedding,
                SourcePage = chunk.SourcePage,
                Section = chunk.Section,
                Chapter = chunk.Chapter,
                Theme = chunk.Theme
            });

            if ((i + 1) % 5 == 0)
                await _db.SaveChangesAsync();

            await Task.Delay(6000);
        }

        await _db.SaveChangesAsync();
        return failedChunks;
    }

    private void SaveFailedChunks(List<TextChunk> chunks)
    {
        var json = JsonSerializer.Serialize(chunks, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FailedChunksFile, json);
    }

    private async Task<Pgvector.Vector?> GenerateWithRetryAsync(string text, int page, int maxRetries = 3)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await _embeddingService.GenerateEmbeddingAsync(text);
            }
            catch (Exception ex)
            {
                var delay = attempt * 15000;
                _logger.LogWarning("Embedding failed for page {Page} (attempt {Attempt}/{Max}): {Error}. Retrying in {Delay}s...",
                    page, attempt, maxRetries, ex.Message, delay / 1000);
                await Task.Delay(delay);
            }
        }

        _logger.LogError("Embedding permanently failed for page {Page} after {Max} retries", page, maxRetries);
        return null;
    }
}
