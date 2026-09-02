using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SmartDocs.Web.Interfaces;
using SmartDocs.Web.Models;
using SmartDocs.Web.Services;
using Xunit;

namespace SmartDocs.Web.Tests.Services;

public class DocumentIngestionServiceTests
{
    private static DocumentIngestionService CreateSut(
        Mock<IPdfTextExtractor> extractor,
        Mock<IEmbeddingService> embeddings,
        InMemoryVectorStore store)
        => new(extractor.Object, embeddings.Object, store, NullLogger<DocumentIngestionService>.Instance);

    [Fact]
    public async Task IngestAsync_ThrowsAndSkipsEmbedding_WhenDocumentHasNoExtractableText()
    {
        var extractor = new Mock<IPdfTextExtractor>();
        extractor.Setup(x => x.ExtractText(It.IsAny<string>())).Returns("   ");
        var embeddings = new Mock<IEmbeddingService>();
        var store = new InMemoryVectorStore();
        var sut = CreateSut(extractor, embeddings, store);
        var doc = new Document { FileName = "scan.pdf", StoragePath = "scan.pdf" };

        var act = () => sut.IngestAsync(doc);

        await act.Should().ThrowAsync<InvalidOperationException>();
        embeddings.Verify(
            x => x.EmbedBatchAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task IngestAsync_IndexesExtractedChunks_WhenTextIsExtractable()
    {
        var extractor = new Mock<IPdfTextExtractor>();
        extractor.Setup(x => x.ExtractText(It.IsAny<string>())).Returns("hello world");
        var embeddings = new Mock<IEmbeddingService>();
        var vector = new float[] { 1f, 0f };
        embeddings
            .Setup(x => x.EmbedBatchAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<float[]> { vector });
        var store = new InMemoryVectorStore();
        var sut = CreateSut(extractor, embeddings, store);
        var doc = new Document { FileName = "doc.pdf", StoragePath = "doc.pdf" };

        await sut.IngestAsync(doc);

        var hits = store.Search(vector, topK: 1);
        hits.Should().ContainSingle();
        hits[0].Chunk.DocumentId.Should().Be(doc.PublicId);
        hits[0].Chunk.Text.Should().Be("hello world");
    }
}
