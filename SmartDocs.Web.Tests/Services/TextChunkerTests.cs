using FluentAssertions;
using SmartDocs.Web.Services;
using Xunit;

namespace SmartDocs.Web.Tests.Services;

public class TextChunkerTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Chunk_ReturnsEmpty_WhenTextIsNullOrWhitespace(string? text)
    {
        var chunks = TextChunker.Chunk(text!);

        chunks.Should().BeEmpty();
    }

    [Fact]
    public void Chunk_ReturnsSingleChunk_WhenTextFitsWithinMaxChars()
    {
        var chunks = TextChunker.Chunk("short text", maxChars: 2000, overlap: 200);

        chunks.Should().ContainSingle();
        chunks[0].Text.Should().Be("short text");
        chunks[0].StartIndex.Should().Be(0);
    }

    [Fact]
    public void Chunk_SplitsWithOverlap_WhenTextExceedsMaxChars()
    {
        var text = new string('a', 25);

        var chunks = TextChunker.Chunk(text, maxChars: 10, overlap: 3);

        chunks.Should().HaveCount(4);
        chunks[0].StartIndex.Should().Be(0);
        chunks[1].StartIndex.Should().Be(7);
        chunks[2].StartIndex.Should().Be(14);
        chunks[3].StartIndex.Should().Be(21);
    }

    [Fact]
    public void Chunk_PreservesExactSubstrings()
    {
        var text = "0123456789";

        var chunks = TextChunker.Chunk(text, maxChars: 4, overlap: 1);

        chunks[0].Text.Should().Be("0123");
        chunks[1].Text.Should().Be("3456");
        chunks[2].Text.Should().Be("6789");
    }
}
