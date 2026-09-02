using System.Net.Http.Json;
using System.Text.Json.Serialization;
using SmartDocs.Web.Interfaces;

namespace SmartDocs.Web.Services;

public class OllamaEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _http;
    private readonly string _model;

    public OllamaEmbeddingService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _http.BaseAddress = new Uri(config["Ollama:BaseUrl"] ?? "http://localhost:11434");
        _model = config["Ollama:EmbeddingModel"] ?? "nomic-embed-text";
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
        => (await EmbedBatchAsync(new[] { text }, ct))[0];

    public async Task<IReadOnlyList<float[]>> EmbedBatchAsync(IEnumerable<string> texts, CancellationToken ct = default)
    {
        var request = new { model = _model, input = texts.ToArray() };
        var resp = await _http.PostAsJsonAsync("/api/embed", request, ct);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<OllamaEmbedResponse>(cancellationToken: ct);
        return result!.Embeddings;
    }

    private class OllamaEmbedResponse
    {
        [JsonPropertyName("embeddings")]
        public List<float[]> Embeddings { get; set; } = new();
    }
}