using System;
using System.IO;
using System.Threading.Tasks;
using MiteAI.Core.Inference;
using MiteAI.Core.Privacy;
using MiteAI.Core.RAG;
using MiteAI.Core.Storage;
using Xunit;

namespace MiteAI.Tests.RAG;

public class LocalRagServiceTests : IDisposable
{
    private readonly LocalVectorDatabase _vectorDb;
    private readonly MockInferenceService _inference;
    private readonly PiiRedactionFilter _redactor;
    private readonly TextChunkingService _chunker;
    private readonly LocalRagService _ragService;

    public LocalRagServiceTests()
    {
        _vectorDb = new LocalVectorDatabase(":memory:");
        _vectorDb.InitializeAsync().GetAwaiter().GetResult();
        _inference = new MockInferenceService();
        _redactor = new PiiRedactionFilter();
        _chunker = new TextChunkingService(chunkSize: 150, overlap: 20);
        _ragService = new LocalRagService(_vectorDb, _inference, _redactor, _chunker);
    }

    [Fact]
    public async Task IngestTextAsync_SanitizesPiiChunksAndStoresInDatabase()
    {
        var text = "Project Mite uses DirectML on Windows 11. Developer secret key: sk-proj-1234567890abcdef12345678.";
        var documentId = "doc_secret";

        int chunksIngested = await _ragService.IngestTextAsync(text, documentId);

        Assert.True(chunksIngested >= 1);

        // Query vector database for DirectML
        var results = await _ragService.QueryAsync("Project Mite uses DirectML on Windows 11", topK: 3);

        Assert.NotEmpty(results);
        Assert.Equal(documentId, results[0].DocumentId);
        // Ensure PII was redacted before insertion
        Assert.Contains("[REDACTED_API_KEY]", results[0].Content);
        Assert.DoesNotContain("sk-proj-", results[0].Content);
    }

    [Fact]
    public async Task IngestFileAsync_ReadsAndIngestsLocalTextFile()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var content = "Local RAG pipeline executes offline without sending telemetry to external clouds.";
            await File.WriteAllTextAsync(tempFile, content);
            
            int chunks = await _ragService.IngestFileAsync(tempFile);
            Assert.True(chunks >= 1);

            var queryResults = await _ragService.QueryAsync(content, topK: 1);
            Assert.Single(queryResults);
            Assert.Contains("Local RAG pipeline executes offline", queryResults[0].Content);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    public void Dispose()
    {
        _vectorDb.Dispose();
    }
}
