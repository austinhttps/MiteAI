using System;

namespace MiteAI.Core.Storage;

public record VectorRecord(
    string Id,
    string Content,
    string Source,
    float[] Vector,
    DateTime CreatedAtUtc
);

public record SearchResult(
    string Id,
    string Content,
    string Source,
    float SimilarityScore,
    DateTime CreatedAtUtc
);
