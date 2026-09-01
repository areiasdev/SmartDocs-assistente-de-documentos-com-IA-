namespace SmartDocs.Web.Services;

public record RagAnswer(string Text, IReadOnlyList<IndexedChunk> Sources);

public class RagService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly InMemoryVectorStore _store;
    private readonly IChatService _chat;

    public RagService(IEmbeddingService embeddingService, InMemoryVectorStore store, IChatService chat)
    {
        _embeddingService = embeddingService;
        _store = store;
        _chat = chat;
    }

    public async Task<RagAnswer> AskAsync(string question, CancellationToken ct = default)
    {
        var queryVector = await _embeddingService.EmbedAsync(question, ct);

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
}