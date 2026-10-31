using System;

namespace MiteAI.Core.RAG;

public record RagQueryResult(
    string ChunkId,
    string DocumentId,
    string Content,
    float RelevanceScore,
    DateTime TimestampUtc
);
