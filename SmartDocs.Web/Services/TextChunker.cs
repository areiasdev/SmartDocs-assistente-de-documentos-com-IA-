namespace SmartDocs.Web.Services;

public record TextChunk(string Text, int StartIndex);

public static class TextChunker
{
    public static IReadOnlyList<TextChunk> Chunk(string text, int maxChars = 2000, int overlap = 200)
    {
        // ~2000 caracteres ≈ ~500 tokens. overlap evita cortar ideias a meio.
        var chunks = new List<TextChunk>();
        if (string.IsNullOrWhiteSpace(text)) return chunks;
        
        int start = 0;
        while (start < text.Length)
        {
            int length = Math.Min(maxChars, text.Length - start);
            chunks.Add(new TextChunk(text.Substring(start, length), start));
            start += maxChars - overlap;
        }

        return chunks;
    }
}