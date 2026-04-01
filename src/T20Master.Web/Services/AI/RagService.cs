using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using T20Master.Web.Data;
using T20Master.Web.Data.Entities;
using T20Master.Web.Data.Models;

namespace T20Master.Web.Services.AI;

public class RagService
{
    private readonly ApplicationDbContext _db;
    private readonly EmbeddingService _embeddingService;

    public RagService(ApplicationDbContext db, EmbeddingService embeddingService)
    {
        _db = db;
        _embeddingService = embeddingService;
    }

    public async Task<List<RuleChunk>> SearchRelevantChunksAsync(string query, int topK = 8)
    {
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query);

        return await _db.RuleChunks
            .OrderBy(r => r.Embedding!.CosineDistance(queryEmbedding))
            .Take(topK)
            .ToListAsync();
    }

    public async Task<string> BuildContextAsync(Guid campaignId, string query)
    {
        var chunks = await SearchRelevantChunksAsync(query);
        var characters = await _db.CharacterSheets
            .Where(c => c.CampaignId == campaignId)
            .ToListAsync();

        var context = "=== REGRAS DO TORMENTA 20 ===\n\n";

        foreach (var chunk in chunks)
        {
            context += $"[Pagina {chunk.SourcePage} | {chunk.Theme}]\n{chunk.Content}\n\n";
        }

        if (characters.Any())
        {
            context += "=== FICHAS DOS JOGADORES ===\n\n";

            foreach (var character in characters)
            {
                var classes = JsonSerializer.Deserialize<List<ClassEntry>>(character.Classes) ?? [];
                var classText = string.Join(", ", classes.Select(c => $"{c.ClassName} {c.Level}"));

                context += $"**{character.Name}** ({character.Race}, {classText}, Nivel {character.Level})\n";
                context += $"PV: {character.CurrentHP}/{character.MaxHP} | PM: {character.CurrentMP}/{character.MaxMP} | Defesa: {character.Defense}\n";
                context += $"For {character.Strength} | Des {character.Dexterity} | Con {character.Constitution} | ";
                context += $"Int {character.Intelligence} | Sab {character.Wisdom} | Car {character.Charisma}\n\n";
            }
        }

        return context;
    }
}
