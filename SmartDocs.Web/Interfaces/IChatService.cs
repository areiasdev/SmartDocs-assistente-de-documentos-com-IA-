namespace SmartDocs.Web.Services;

public interface IChatService
{
    Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken ct = default);
}