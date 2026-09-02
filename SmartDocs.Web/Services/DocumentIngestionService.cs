using SmartDocs.Web.Interfaces;
using SmartDocs.Web.Models;

namespace SmartDocs.Web.Services;

public class DocumentIngestionService
{
    private readonly IPdfTextExtractor _extractor;
    private readonly IEmbeddingService _embeddings;
    private readonly InMemoryVectorStore _store;
    private readonly ILogger<DocumentIngestionService> _logger;

    public DocumentIngestionService(
        IPdfTextExtractor extractor,
        IEmbeddingService embeddings,
        InMemoryVectorStore store,
        ILogger<DocumentIngestionService> logger)
    {
        _extractor = extractor;
        _embeddings = embeddings;
        _store = store;
        _logger = logger;
    }

    public async Task IngestAsync (Document doc, CancellationToken ct = default)
    {
        var text = _extractor.ExtractText(doc.StoragePath);
        var chunks = TextChunker.Chunk(text);

        if (chunks.Count == 0)
        {
            _logger.LogWarning("No extractable text found in document {DocumentId} ({FileName})", doc.Id, doc.FileName);
            throw new InvalidOperationException(
                "O PDF não contém texto extraível (pode ser uma digitalização/imagem sem camada de texto).");
        }

        var vectors = await _embeddings.EmbedBatchAsync(chunks.Select(c => c.Text), ct);

        for (int i = 0; i < chunks.Count; i++)
         _store.AddChunk(new IndexedChunk(doc.PublicId, chunks[i].Text, chunks[i].StartIndex, vectors[i]));

         _logger.LogInformation("Ingested document {DocumentId} with {ChunkCount} chunks", doc.Id, chunks.Count);
    }
}