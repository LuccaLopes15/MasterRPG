using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Pgvector;

namespace T20Master.Web.Services.AI;

public class EmbeddingService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmbeddingService> _logger;
    private readonly HttpClient _httpClient = new();

    public EmbeddingService(IConfiguration config, ILogger<EmbeddingService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<Vector> GenerateEmbeddingAsync(string text)
    {
        var apiKey = _config["Gemini:ApiKey"]!;
        var model = _config["Gemini:EmbeddingModel"] ?? "gemini-embedding-001";

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:embedContent?key={apiKey}";

        var request = new
        {
            model = $"models/{model}",
            content = new
            {
                parts = new[] { new { text } }
            }
        };

        var response = await _httpClient.PostAsJsonAsync(url, request);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Embedding API error: {Body}", body);
            throw new InvalidOperationException($"Embedding API failed: {body}");
        }

        var result = JsonSerializer.Deserialize<EmbeddingResponse>(body);
        var values = result?.Embedding?.Values
            ?? throw new InvalidOperationException("Embedding response was null");

        return new Vector(values);
    }

    private class EmbeddingResponse
    {
        [JsonPropertyName("embedding")]
        public EmbeddingData? Embedding { get; set; }
    }

    private class EmbeddingData
    {
        [JsonPropertyName("values")]
        public float[]? Values { get; set; }
    }
}
