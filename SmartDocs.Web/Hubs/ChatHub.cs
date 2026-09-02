using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.AspNetCore.SignalR;
using SmartDocs.Web.Services;

namespace SmartDocs.Web.Hubs;

// Hub de SignalR para o chat com streaming. O Blazor Server já usa SignalR por
// baixo para o próprio circuito, mas fiz este hub à parte em /hubs/chat de
// propósito — mostra como se faz um método de streaming num hub, e o endpoint
// fica reutilizável por qualquer cliente SignalR, não só por esta UI.
//
// NOTA PARA MIM: este hub não tem [Authorize], e o userId vem como parâmetro do
// cliente em vez de vir do Context.User autenticado. Fiz assim porque a ligação
// SignalR que crio no Home.razor (uma ligação nova, de volta para o próprio
// servidor) não leva a cookie de autenticação, e resolver isso a sério dava
// muito mais trabalho do que o que precisava agora. Mas quer dizer que,
// tecnicamente, alguém podia mandar o userId de outra pessoa e mexer na
// conversa dela. Se isto for para produção a sério, tenho de voltar aqui:
// pôr [Authorize] no hub e tirar o userId do Context.User em vez de o receber.
public class ChatHub : Hub
{
    private readonly RagService _rag;
    private readonly ConversationService _conversations;
    public ChatHub(RagService rag, ConversationService conversations)
        => (_rag, _conversations) = (rag, conversations);

    // Vou buscar a conversa deste (userId, documentId), guardo a pergunta do
    // utilizador, mando a resposta em streaming token a token, e no fim guardo
    // a resposta completa do assistente.
    public async IAsyncEnumerable<string> StreamAnswer(
    string documentId, string question, string userId, [EnumeratorCancellation] CancellationToken ct)
    {
        var convo = await _conversations.GetOrCreateAsync(userId, documentId, ct);
        var history = convo.Messages.OrderBy(m => m.CreatedAt)
            .Select(m => new ChatMessage(m.Role, m.Content)).ToList();

        await _conversations.AddMessageAsync(convo.Id, "user", question, ct);

        // Vou juntando os tokens todos num StringBuilder para no fim guardar a
        // resposta inteira como uma mensagem só, mas continuo a devolver cada
        // token logo para o cliente ir mostrando o texto a aparecer aos poucos.
        var full = new StringBuilder();
        await foreach (var token in _rag.StreamAnswerAsync(question, documentId, history, ct))
        {
            full.Append(token);
            yield return token;
        }
        await _conversations.AddMessageAsync(convo.Id, "assistant", full.ToString(), ct);
    }
}