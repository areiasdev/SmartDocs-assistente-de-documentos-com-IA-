using System.Runtime.CompilerServices;
using SmartDocs.Web.Interfaces;

namespace SmartDocs.Web.Services;

// Isto é o coração do RAG: pego na pergunta, embebo-a, vou buscar os pedaços mais
// parecidos do documento, meto tudo isso num prompt e mando para o LLM, com
// streaming. Só depende de interfaces (IEmbeddingService, IChatService), se um
// dia trocar o Ollama pelo Azure OpenAI, mudo só o registo no DI, esta classe
// nem dá por isso.
public class RagService
{
    private readonly IEmbeddingService _embeddings;
    private readonly InMemoryVectorStore _store;
    private readonly IChatService _chat;

    public RagService(IEmbeddingService embeddingService, InMemoryVectorStore store, IChatService chat)
    {
        _embeddings = embeddingService;
        _store = store;
        _chat = chat;
    }

    // Respondo à pergunta usando só os pedaços deste documento (documentId), e vou
    // devolvendo a resposta aos poucos (streaming). O history são as mensagens
    // anteriores desta conversa, para o modelo ter memória de curto prazo.
    public async IAsyncEnumerable<string> StreamAnswerAsync(
        string question, string documentId, IReadOnlyList<ChatMessage> history,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var queryVector = await _embeddings.EmbedAsync(question, ct);
        var hits = _store.Search(queryVector, documentId, topK: 4);
        var context = string.Join("\n\n---\n\n", hits.Select(h => h.Chunk.Text));

        var messages = new List<ChatMessage>
        {
            new("system",
                "Responde com base no contexto e no histórico da conversa. " +
                "Se a resposta não estiver no contexto, diz que não sabes.\n\nContexto:\n" + context)
        };
        messages.AddRange(history);              // turnos anteriores → a "memória"
        messages.Add(new("user", question));     // nova pergunta

        await foreach (var token in _chat.StreamAsync(messages, ct))
            yield return token;
    }
}