using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MiteAI.Core.Inference;
using MiteAI.Core.Privacy;
using MiteAI.Core.Storage;

namespace MiteAI.Core.RAG;

public class LocalRagService
{
    private readonly LocalVectorDatabase _vectorDatabase;
    private readonly IModelInference _inferenceEngine;
    private readonly PiiRedactionFilter _redactionFilter;
    private readonly TextChunkingService _chunker;

    public LocalRagService(
        LocalVectorDatabase vectorDatabase,
        IModelInference inferenceEngine,
        PiiRedactionFilter redactionFilter,
        TextChunkingService? chunker = null)
    {
        _vectorDatabase = vectorDatabase ?? throw new ArgumentNullException(nameof(vectorDatabase));
        _inferenceEngine = inferenceEngine ?? throw new ArgumentNullException(nameof(inferenceEngine));
        _redactionFilter = redactionFilter ?? throw new ArgumentNullException(nameof(redactionFilter));
        _chunker = chunker ?? new TextChunkingService();
    }

    public async Task<int> IngestFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Document file not found: {filePath}", filePath);
        }

        var content = await File.ReadAllTextAsync(filePath, cancellationToken);
        var documentId = Path.GetFileName(filePath);
        return await IngestTextAsync(content, documentId, cancellationToken);
    }

    public async Task<int> IngestTextAsync(string rawText, string documentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return 0;
        }

        // 1. Enforce zero-trust client-side PII redaction prior to ingestion
        var sanitizedText = _redactionFilter.Redact(rawText).SanitizedText;

        // 2. Split into overlapping chunks
        var chunks = _chunker.ChunkText(sanitizedText, documentId);

        int count = 0;
        foreach (var chunk in chunks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 3. Generate embedding vector
            var embedding = await _inferenceEngine.GetEmbeddingsAsync(chunk.Content, cancellationToken);

            var chunkRecordId = $"{documentId}_{chunk.Index}";
            var vectorRecord = new VectorRecord(
                Id: chunkRecordId,
                Content: chunk.Content,
                Source: documentId,
                Vector: embedding,
                CreatedAtUtc: DateTime.UtcNow
            );

            // 4. Store in local SQLite
            await _vectorDatabase.InsertAsync(vectorRecord);
            count++;
        }

        return count;
    }

    public async Task<List<RagQueryResult>> QueryAsync(string query, int topK = 5, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new List<RagQueryResult>();
        }

        // 1. Generate query embedding
        var queryEmbedding = await _inferenceEngine.GetEmbeddingsAsync(query, cancellationToken);

        // 2. Search local SQLite vector database
        var searchResults = await _vectorDatabase.SearchSimilarAsync(queryEmbedding, topK);

        var ragResults = new List<RagQueryResult>();
        foreach (var res in searchResults)
        {
            ragResults.Add(new RagQueryResult(
                ChunkId: res.Id,
                DocumentId: res.Source,
                Content: res.Content,
                RelevanceScore: res.SimilarityScore,
                TimestampUtc: res.CreatedAtUtc
            ));
        }

        return ragResults;
    }
}
