using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace SmartDocs.Web.Services;

public class OllamaChatService : IChatService
{
    private readonly HttpClient _http;
    private readonly string _model;

    public OllamaChatService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _http.BaseAddress = new Uri(config["Ollama:BaseUrl"] ?? "http://localhost:11434");
        _model = config["Ollama:ChatModel"] ?? "llama3.2:3b";
        _http.Timeout = TimeSpan.FromMinutes(5);   //modelos locais costumam ser lentos
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken ct = default)
    {
        var request = new
        {
            model = _model,
            stream = false,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user",   content = userMessage }
            }
        };
        var resp = await _http.PostAsJsonAsync("/api/chat", request, ct);
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken: ct);
        return result!.Message.Content;
    }

    private class OllamaChatResponse
    {
        [JsonPropertyName("message")] public OllamaMessage Message { get; set; } = new();
    }
    private class OllamaMessage
    {
        [JsonPropertyName("content")] public string Content { get; set; } = "";
    }
}