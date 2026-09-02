using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using SmartDocs.Web.Interfaces;

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
            ptions = new { temperature = 0.1 },
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

    public async IAsyncEnumerable<string> StreamAsync(
        IEnumerable<ChatMessage> messages,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var request = new
        {
            model = _model,
            stream = true,
            options = new { temperature = 0.1 },
            messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToArray()
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/chat")
        {
            Content = JsonContent.Create(request)
        };
        // ResponseHeadersRead = não esperar pelo corpo todo; começar a ler já
        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (true)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null) break;
            if (string.IsNullOrWhiteSpace(line)) continue;

            var chunk = JsonSerializer.Deserialize<OllamaChatResponse>(line);
            if (!string.IsNullOrEmpty(chunk?.Message?.Content))
                yield return chunk!.Message!.Content;

            if (chunk?.Done == true) break;
        }
    }

    private class OllamaChatResponse
    {
        [JsonPropertyName("message")] public OllamaMessage Message { get; set; } = new();
        [JsonPropertyName("done")] public bool Done { get; set; }
    }
    private class OllamaMessage
    {
        [JsonPropertyName("content")] public string Content { get; set; } = "";
    }
}