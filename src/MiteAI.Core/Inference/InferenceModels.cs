using System;

namespace MiteAI.Core.Inference;

public record InferenceRequest(
    string Prompt,
    int MaxTokens = 128,
    float Temperature = 0.7f,
    string? SystemPrompt = null
);

public record InferenceResult(
    string OutputText,
    int TokensGenerated,
    double TimeToFirstTokenMs,
    double TotalInferenceTimeMs
);

public record InferenceMetrics(
    bool IsDirectMlActive,
    bool IsMockActive,
    long VramUsageBytes,
    int ActiveContextWindowTokens,
    double TokensPerSecond
);
