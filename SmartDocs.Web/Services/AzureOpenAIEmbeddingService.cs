using Azure;
using Azure.AI.OpenAI;
using OpenAI.Embeddings;

namespace SmartDocs.Web.Services;

public class AzureOpenAIEmbeddingService : IEmbeddingService
{
    private readonly EmbeddingClient _client;

    public AzureOpenAIEmbeddingService(IConfiguration configuration)
    {
        var endpoint = configuration["AzureOpenAI:Endpoint"]
            ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is not configured.");
        var apiKey = configuration["AzureOpenAI:ApiKey"]
            ?? throw new InvalidOperationException("AzureOpenAI:ApiKey is not configured.");
        var deploymentName = configuration["AzureOpenAI:EmbeddingDeployment"]
            ?? throw new InvalidOperationException("AzureOpenAI:EmbeddingDeployment is not configured.");

        var azure = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        _client = azure.GetEmbeddingClient(deploymentName);
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        var result = await _client.GenerateEmbeddingAsync(text, cancellationToken: ct);
        return result.Value.ToFloats().ToArray();
    }

    public async Task<IReadOnlyList<float[]>> EmbedBatchAsync(IEnumerable<string> texts, CancellationToken ct = default)
    {
        var result = await _client.GenerateEmbeddingsAsync(texts.ToList(), cancellationToken: ct);
        return result.Value.Select(e => e.ToFloats().ToArray()).ToList();
    }
}