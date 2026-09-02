using SmartDocs.Web.Interfaces;
using SmartDocs.Web.Models;

namespace SmartDocs.Web.Services;

// Isto trata do pipeline todo quando carrego um documento novo: extrai o texto,
// parte em pedaços com overlap, embebe cada pedaço, e indexa os vetores para
// depois conseguir pesquisar. Só depende de interfaces para a extração e os
// embeddings, por isso posso trocar isto por OCR ou por Azure OpenAI sem mexer
// nesta lógica.
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

    // Extraio, parto em pedaços, embebo e indexo o doc. Se não conseguir tirar
    // texto nenhum do PDF (o caso mais comum é ser uma digitalização/imagem sem
    // camada de texto), rebento logo aqui com um erro claro, antes de sequer
    // chamar o serviço de embeddings, assim evito que a API de embeddings
    // rebente com uma lista vazia e dê um erro confuso lá deles.
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

        // vectors[i] corresponde a chunks[i] porque o EmbedBatchAsync mantém a
        // ordem; guardo cada pedaço com o PublicId do documento (não o Id
        // numérico), porque assim continua a fazer sentido mesmo antes do EF
        // Core atribuir um Id à linha.
        for (int i = 0; i < chunks.Count; i++)
         _store.AddChunk(new IndexedChunk(doc.PublicId, chunks[i].Text, chunks[i].StartIndex, vectors[i]));

         _logger.LogInformation("Ingested document {DocumentId} with {ChunkCount} chunks", doc.Id, chunks.Count);
    }
}