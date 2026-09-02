namespace SmartDocs.Web.Services;

public record IndexedChunk(string DocumentId, string Text, int StartIndex, float[] Embedding);

// Índice de vetores bem básico: guardo tudo numa List e, para procurar, percorro
// tudo e calculo a similaridade de cosseno (força bruta). Serve bem para o que
// preciso agora (um utilizador, poucos documentos); numa app a sério isto seria
// substituído por uma base de dados vetorial (Azure AI Search, Cosmos DB vector
// search) mantendo a mesma forma da API. É singleton, por isso perco tudo se
// reiniciar a app, tenho de voltar a fazer ingest dos documentos.
public class InMemoryVectorStore
{
    private readonly List<IndexedChunk> _chunks = new();

    public void AddChunk(IndexedChunk chunk) => _chunks.Add(chunk);

    // Os topK pedaços mais parecidos, procurando em todos os documentos indexados.
    public IReadOnlyList<(IndexedChunk Chunk, float Score)> Search(float[] query, int topK = 4)
        => Rank(_chunks, query, topK);

    // Igual ao Search de cima, mas só dentro de um documento, para uma pergunta
    // sobre um PDF nunca ir buscar pedaços de outro.
    public IReadOnlyList<(IndexedChunk Chunk, float Score)> Search(float[] query, string documentId, int topK = 4)
        => Rank(_chunks.Where(c => c.DocumentId == documentId), query, topK);

    private static IReadOnlyList<(IndexedChunk Chunk, float Score)> Rank(IEnumerable<IndexedChunk> candidates, float[] query, int topK)
        => candidates
            .Select(c => (Chunk: c, Score: CosineSimilarity(query, c.Embedding)))
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .ToList();

    // Similaridade de cosseno: produto interno dos dois vetores a dividir pelo
    // produto das suas magnitudes. Vai de -1 (significados opostos) a 1 (iguais);
    // o 1e-8 é só para não dividir por zero se algum vetor for todo a zeros.
    private static float CosineSimilarity(float[] a, float[] b)
    {
        float dot = 0, normA = 0, normB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        return dot / (MathF.Sqrt(normA) * MathF.Sqrt(normB) + 1e-8f);
    }
}