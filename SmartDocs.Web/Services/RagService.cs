using System.Runtime.CompilerServices;
using SmartDocs.Web.Interfaces;

namespace SmartDocs.Web.Services;

public record RagAnswer(string Text, IReadOnlyList<IndexedChunk> Sources);

public class RagService
{
    private readonly IEmbeddingService _embeddings;
    private readonly InMemoryVectorStore _store;
    private readonly IChatService _chat;

    public RagService(IEmbeddingService embeddingService, InMemoryVectorStore store, IChatService chat)
    {
        _embeddings= embeddingService;
        _store = store;
        _chat = chat;
    }

    public async Task<RagAnswer> AskAsync(string question, CancellationToken ct = default)
    {
        var queryVector = await _embeddings.EmbedAsync(question, ct);

        var hits = _store.Search(queryVector, topK: 4);
        var sources = hits.Select(h => h.Chunk).ToList();

        var context = string.Join("\n\n---\n\n", sources.Select(s => s.Text));
        var system = """
            És um assistente que responde APENAS com base no contexto fornecido.
            Se a resposta não estiver no contexto, diz claramente que não sabes.
            Responde em português, de forma concisa.
            """;
        var user = $"Contexto:\n{context}\n\nPergunta: {question}"; 

        var answer = await _chat.CompleteAsync(system, user, ct);
        return new RagAnswer(answer, sources);
    }

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