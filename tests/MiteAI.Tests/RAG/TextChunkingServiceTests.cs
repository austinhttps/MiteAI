using System;
using System.Linq;
using MiteAI.Core.RAG;
using Xunit;

namespace MiteAI.Tests.RAG;

public class TextChunkingServiceTests
{
    [Fact]
    public void ChunkText_SplitsTextIntoOverlappingSegments()
    {
        var chunker = new TextChunkingService(chunkSize: 50, overlap: 10);
        var text = "The quick brown fox jumps over the lazy dog. The quick brown fox jumps over the lazy dog again.";

        var chunks = chunker.ChunkText(text, sourceDocumentId: "doc1").ToList();

        Assert.NotEmpty(chunks);
        Assert.All(chunks, c => Assert.Equal("doc1", c.DocumentId));
        Assert.All(chunks, c => Assert.True(c.Content.Length <= 50));
        Assert.Equal(0, chunks[0].Index);
        Assert.Equal(1, chunks[1].Index);
    }

    [Fact]
    public void ChunkText_HandlesShortTextWithoutSplitting()
    {
        var chunker = new TextChunkingService(chunkSize: 100, overlap: 20);
        var text = "Short note.";

        var chunks = chunker.ChunkText(text, sourceDocumentId: "doc2").ToList();

        Assert.Single(chunks);
        Assert.Equal("Short note.", chunks[0].Content);
        Assert.Equal(0, chunks[0].Index);
    }

    [Fact]
    public void ChunkText_HandlesNullOrEmptySafely()
    {
        var chunker = new TextChunkingService();

        var emptyChunks = chunker.ChunkText(string.Empty, "docEmpty").ToList();
        Assert.Empty(emptyChunks);

        var nullChunks = chunker.ChunkText(null!, "docNull").ToList();
        Assert.Empty(nullChunks);
    }

    [Fact]
    public void ChunkText_ThrowsIfOverlapGreaterThanOrEqualToChunkSize()
    {
        Assert.Throws<ArgumentException>(() => new TextChunkingService(chunkSize: 50, overlap: 50));
        Assert.Throws<ArgumentException>(() => new TextChunkingService(chunkSize: 50, overlap: 60));
    }
}
