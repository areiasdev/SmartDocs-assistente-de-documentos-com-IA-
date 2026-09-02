namespace SmartDocs.Web.Interfaces;

public interface IChatService
{
    Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken ct = default);

    IAsyncEnumerable<string> StreamAsync(string systemPrompt, string userMessage,CancellationToken ct = default);
}