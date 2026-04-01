using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace T20Master.Web.Services.AI;

public class PdfProcessor
{
    public List<PageText> ExtractPages(string pdfPath)
    {
        var pages = new List<PageText>();

        using var document = PdfDocument.Open(pdfPath);
        foreach (var page in document.GetPages())
        {
            var words = page.GetWords().ToList();
            if (words.Count == 0) continue;

            var pageWidth = page.Width;
            var midPoint = pageWidth / 2;

            // Separate into left and right columns
            var leftWords = words.Where(w => w.BoundingBox.Left < midPoint).ToList();
            var rightWords = words.Where(w => w.BoundingBox.Left >= midPoint).ToList();

            var leftText = BuildTextFromWords(leftWords);
            var rightText = BuildTextFromWords(rightWords);

            var fullText = leftText;
            if (!string.IsNullOrWhiteSpace(rightText))
            {
                fullText += "\n" + rightText;
            }

            if (!string.IsNullOrWhiteSpace(fullText))
            {
                pages.Add(new PageText
                {
                    PageNumber = page.Number,
                    Text = fullText.Trim()
                });
            }
        }

        return pages;
    }

    private static string BuildTextFromWords(List<Word> words)
    {
        if (words.Count == 0) return string.Empty;

        // Sort by Y descending (top to bottom), then X ascending (left to right)
        var sorted = words.OrderByDescending(w => w.BoundingBox.Top)
                          .ThenBy(w => w.BoundingBox.Left)
                          .ToList();

        var lines = new List<string>();
        var currentLine = new List<string>();
        double? lastY = null;

        foreach (var word in sorted)
        {
            var y = Math.Round(word.BoundingBox.Top, 1);

            if (lastY.HasValue && Math.Abs(y - lastY.Value) > 3)
            {
                if (currentLine.Count > 0)
                {
                    lines.Add(string.Join(" ", currentLine));
                    currentLine.Clear();
                }
            }

            currentLine.Add(word.Text);
            lastY = y;
        }

        if (currentLine.Count > 0)
            lines.Add(string.Join(" ", currentLine));

        return string.Join("\n", lines);
    }

    public List<TextChunk> ChunkText(List<PageText> pages, int targetChunkSize = 2500, int overlap = 200)
    {
        var chunks = new List<TextChunk>();
        var buffer = "";
        var currentPage = 0;
        var startPage = 0;

        foreach (var page in pages)
        {
            if (buffer.Length == 0) startPage = page.PageNumber;
            buffer += "\n" + page.Text;
            currentPage = page.PageNumber;

            while (buffer.Length >= targetChunkSize)
            {
                var cutPoint = FindCutPoint(buffer, targetChunkSize);
                var chunkText = buffer[..cutPoint].Trim();

                if (chunkText.Length > 50)
                {
                    chunks.Add(new TextChunk
                    {
                        Content = chunkText,
                        SourcePage = startPage,
                        Chapter = DetectChapter(chunkText),
                        Section = DetectSection(chunkText),
                        Theme = ClassifyTheme(chunkText)
                    });
                }

                var overlapStart = Math.Max(0, cutPoint - overlap);
                buffer = buffer[overlapStart..];
                startPage = currentPage;
            }
        }

        // Remaining buffer
        if (buffer.Trim().Length > 50)
        {
            chunks.Add(new TextChunk
            {
                Content = buffer.Trim(),
                SourcePage = startPage,
                Chapter = DetectChapter(buffer),
                Section = DetectSection(buffer),
                Theme = ClassifyTheme(buffer)
            });
        }

        return chunks;
    }

    private static int FindCutPoint(string text, int target)
    {
        // Try to cut at paragraph break
        var paragraphBreak = text.LastIndexOf("\n\n", target, StringComparison.Ordinal);
        if (paragraphBreak > target / 2) return paragraphBreak;

        // Try to cut at line break
        var lineBreak = text.LastIndexOf('\n', target);
        if (lineBreak > target / 2) return lineBreak;

        // Try to cut at period
        var period = text.LastIndexOf('.', target);
        if (period > target / 2) return period + 1;

        return target;
    }

    private static string DetectChapter(string text)
    {
        var firstLine = text.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        var upper = firstLine.ToUpperInvariant();

        if (upper.Contains("CAPITULO") || upper.Contains("CAP."))
            return firstLine.Trim();

        return "Geral";
    }

    private static string DetectSection(string text)
    {
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines.Take(3))
        {
            if (line.Length < 60 && line == line.ToUpperInvariant() && line.Length > 3)
                return line.Trim();
        }
        return "Geral";
    }

    public static string ClassifyTheme(string text)
    {
        var lower = text.ToLowerInvariant();

        if (ContainsAny(lower, "ataque", "dano", "rodada", "iniciativa", "acerto", "combate", "turno"))
            return "combate";
        if (ContainsAny(lower, "magia", "arcana", "divina", "circulo", "mana", "conjuracao", "conjurar"))
            return "magia";
        if (ContainsAny(lower, "arcanista", "barbaro", "bardo", "bucaneiro", "cacador", "cavaleiro",
            "clerigo", "druida", "guerreiro", "inventor", "ladino", "lutador", "nobre", "paladino"))
            return "classes";
        if (ContainsAny(lower, "humano", "anao", "dahllan", "elfo", "goblin", "lefou",
            "minotauro", "qareen", "golem", "hynne", "kliren", "medusa", "osteon", "sulfure", "trog"))
            return "racas";
        if (ContainsAny(lower, "pericia", "teste de", "treinada", "acrobacia", "atletismo", "furtividade"))
            return "pericias";
        if (ContainsAny(lower, "arma", "armadura", "escudo", "equipamento", "item magico"))
            return "equipamento";
        if (ContainsAny(lower, "condicao", "veneno", "doenca", "agarrado", "caido", "cego"))
            return "condicoes";

        return "geral";
    }

    private static bool ContainsAny(string text, params string[] terms)
        => terms.Any(text.Contains);
}

public class PageText
{
    public int PageNumber { get; set; }
    public string Text { get; set; } = string.Empty;
}

public class TextChunk
{
    public string Content { get; set; } = string.Empty;
    public int SourcePage { get; set; }
    public string Chapter { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string Theme { get; set; } = string.Empty;
}
