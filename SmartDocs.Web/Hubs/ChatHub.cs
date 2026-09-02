using Microsoft.AspNetCore.SignalR;
using SmartDocs.Web.Services;

namespace SmartDocs.Web.Hubs;

public class ChatHub : Hub
{
    private readonly RagService _rag;
    public ChatHub(RagService rag) => _rag = rag;

    // método de streaming do Hub: devolve IAsyncEnumerable → o cliente consome como stream
    public IAsyncEnumerable<string> StreamAnswer(string question, CancellationToken ct)
        => _rag.StreamAnswerAsync(question, ct);
}