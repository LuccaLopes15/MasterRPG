using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using T20Master.Web.Data;
using T20Master.Web.Data.Entities;

namespace T20Master.Web.Services.AI;

public class GeminiChatService
{
    private readonly ApplicationDbContext _db;
    private readonly RagService _ragService;
    private readonly IConfiguration _config;
    private readonly ILogger<GeminiChatService> _logger;
    private readonly HttpClient _httpClient = new();

    private const string SystemPrompt = """
        Voce e um assistente especialista em Tormenta 20 (T20), o RPG brasileiro.
        Responda APENAS com base nas regras fornecidas no contexto.
        Cite a pagina e secao quando possivel.
        Se nao encontrar a informacao no contexto fornecido, diga que nao tem essa informacao disponivel.
        Use portugues brasileiro.
        Seja conciso mas completo nas respostas.
        """;

    public GeminiChatService(
        ApplicationDbContext db,
        RagService ragService,
        IConfiguration config,
        ILogger<GeminiChatService> logger)
    {
        _db = db;
        _ragService = ragService;
        _config = config;
        _logger = logger;
    }

    public async Task StreamResponseAsync(
        Guid campaignId,
        string userId,
        string question,
        Func<string, Task> onToken)
    {
        try
        {
            var context = await _ragService.BuildContextAsync(campaignId, question);
            var history = await GetOrCreateHistoryAsync(campaignId, userId);
            var messages = JsonSerializer.Deserialize<List<ChatMessage>>(history.Messages) ?? [];

            var apiKey = _config["Gemini:ApiKey"]!;
            var modelName = _config["Gemini:ChatModel"] ?? "gemini-2.0-flash";

            // Build history text
            var historyText = "";
            foreach (var msg in messages.TakeLast(10))
            {
                var role = msg.Role == "user" ? "Mestre" : "Assistente";
                historyText += $"{role}: {msg.Content}\n\n";
            }

            var fullPrompt = $"""
                {SystemPrompt}

                {context}

                {(string.IsNullOrEmpty(historyText) ? "" : $"=== HISTORICO RECENTE ===\n{historyText}")}

                Pergunta do mestre: {question}
                """;

            // Use streaming API
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:streamGenerateContent?alt=sse&key={apiKey}";

            var request = new
            {
                contents = new[]
                {
                    new { role = "user", parts = new[] { new { text = fullPrompt } } }
                }
            };

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(request)
            };

            var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);
            var fullResponse = "";

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Gemini API error: {Body}", errorBody);
                await onToken("Erro ao consultar a IA. Tente novamente.");
                return;
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (line == null) break;
                if (!line.StartsWith("data: ")) continue;

                var json = line["data: ".Length..];
                if (json == "[DONE]") break;

                try
                {
                    var chunk = JsonSerializer.Deserialize<StreamChunk>(json);
                    var text = chunk?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
                    if (!string.IsNullOrEmpty(text))
                    {
                        fullResponse += text;
                        await onToken(text);
                    }
                }
                catch { }
            }

            // Save to history
            messages.Add(new ChatMessage { Role = "user", Content = question, Timestamp = DateTime.UtcNow });
            messages.Add(new ChatMessage { Role = "assistant", Content = fullResponse, Timestamp = DateTime.UtcNow });

            if (messages.Count > 50)
                messages = messages.TakeLast(50).ToList();

            history.Messages = JsonSerializer.Serialize(messages);
            await _db.SaveChangesAsync();
        }
        catch (Exception ex) when (ex.Message.Contains("429") || ex.Message.Contains("RESOURCE_EXHAUSTED"))
        {
            await onToken("\n\nMuitas perguntas por minuto. Aguarde 30 segundos e tente novamente.");
            _logger.LogWarning("Gemini rate limit hit for campaign {CampaignId}", campaignId);
        }
        catch (TaskCanceledException)
        {
            await onToken("\n\nA resposta demorou muito. Tente novamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GeminiChatService for campaign {CampaignId}", campaignId);
            await onToken("\n\nOcorreu um erro ao consultar a IA. Tente novamente.");
        }
    }

    public async Task<List<ChatMessage>> GetHistoryAsync(Guid campaignId, string userId)
    {
        var history = await _db.ChatHistories
            .FirstOrDefaultAsync(h => h.CampaignId == campaignId && h.UserId == userId);

        if (history == null) return [];
        return JsonSerializer.Deserialize<List<ChatMessage>>(history.Messages) ?? [];
    }

    private async Task<ChatHistory> GetOrCreateHistoryAsync(Guid campaignId, string userId)
    {
        var history = await _db.ChatHistories
            .FirstOrDefaultAsync(h => h.CampaignId == campaignId && h.UserId == userId);

        if (history == null)
        {
            history = new ChatHistory
            {
                Id = Guid.NewGuid(),
                CampaignId = campaignId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                Messages = "[]"
            };
            _db.ChatHistories.Add(history);
            await _db.SaveChangesAsync();
        }

        return history;
    }

    // Deserialization types for Gemini streaming
    private class StreamChunk
    {
        [JsonPropertyName("candidates")]
        public List<Candidate>? Candidates { get; set; }
    }

    private class Candidate
    {
        [JsonPropertyName("content")]
        public ContentData? Content { get; set; }
    }

    private class ContentData
    {
        [JsonPropertyName("parts")]
        public List<Part>? Parts { get; set; }
    }

    private class Part
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}

public class ChatMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
