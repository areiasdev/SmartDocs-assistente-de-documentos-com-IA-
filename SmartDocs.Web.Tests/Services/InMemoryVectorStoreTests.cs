using FluentAssertions;
using SmartDocs.Web.Services;
using Xunit;

namespace SmartDocs.Web.Tests.Services;

public class InMemoryVectorStoreTests
{
    [Fact]
    public void Search_ReturnsEmpty_WhenStoreHasNoChunks()
    {
        var store = new InMemoryVectorStore();

        var hits = store.Search(new float[] { 1f, 0f });

        hits.Should().BeEmpty();
    }

    [Fact]
    public void Search_RanksMostSimilarVectorFirst()
    {
        var store = new InMemoryVectorStore();
        var identical = new IndexedChunk("doc-1", "identical", 0, new float[] { 1f, 0f });
        var orthogonal = new IndexedChunk("doc-1", "orthogonal", 0, new float[] { 0f, 1f });
        var opposite = new IndexedChunk("doc-1", "opposite", 0, new float[] { -1f, 0f });
        store.AddChunk(orthogonal);
        store.AddChunk(opposite);
        store.AddChunk(identical);

        var hits = store.Search(new float[] { 1f, 0f }, topK: 3);

        hits.Should().HaveCount(3);
        hits[0].Chunk.Text.Should().Be("identical");
        hits[0].Score.Should().BeApproximately(1f, 0.0001f);
        hits[^1].Chunk.Text.Should().Be("opposite");
    }

    [Fact]
    public void Search_RespectsTopK()
    {
        var store = new InMemoryVectorStore();
        for (int i = 0; i < 10; i++)
            store.AddChunk(new IndexedChunk("doc-1", $"chunk-{i}", i, new float[] { 1f, i }));

        var hits = store.Search(new float[] { 1f, 0f }, topK: 2);

        hits.Should().HaveCount(2);
    }
}
