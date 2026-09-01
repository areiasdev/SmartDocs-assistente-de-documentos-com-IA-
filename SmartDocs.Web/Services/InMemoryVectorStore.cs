namespace SmartDocs.Web.Services;

public record IndexedChunk(string DocumentId, string Text, int StartIndex, float[] Embedding);

public class InMemoryVectorStore 
{
    private readonly List<IndexedChunk> _chunks = new();

    public void AddChunk(IndexedChunk chunk) => _chunks.Add(chunk);
    public IReadOnlyList<(IndexedChunk Chunk, float Score)> Search(float[] query, int topK = 4)
    => _chunks
        .Select(c => (Chunk: c, Score: CosineSimilarity(query, c.Embedding)))
        .OrderByDescending(x => x.Score)
        .Take(topK)
        .ToList();
    
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