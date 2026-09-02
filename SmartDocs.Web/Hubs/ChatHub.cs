using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.AspNetCore.SignalR;
using SmartDocs.Web.Services;

namespace SmartDocs.Web.Hubs;

public class ChatHub : Hub
{
    private readonly RagService _rag;
    private readonly ConversationService _conversations;
    public ChatHub(RagService rag, ConversationService conversations)
        => (_rag, _conversations) = (rag, conversations);

    public async IAsyncEnumerable<string> StreamAnswer(
    string documentId, string question, string userId, [EnumeratorCancellation] CancellationToken ct)
    {
        var convo = await _conversations.GetOrCreateAsync(userId, documentId, ct);
        var history = convo.Messages.OrderBy(m => m.CreatedAt)
            .Select(m => new ChatMessage(m.Role, m.Content)).ToList();

        await _conversations.AddMessageAsync(convo.Id, "user", question, ct);
        var full = new StringBuilder();
        await foreach (var token in _rag.StreamAnswerAsync(question, documentId, history, ct))
        {
            full.Append(token);
            yield return token;
        }
        await _conversations.AddMessageAsync(convo.Id, "assistant", full.ToString(), ct);
    }
}