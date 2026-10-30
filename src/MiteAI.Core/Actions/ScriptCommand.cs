using System;

namespace MiteAI.Core.Actions;

public record DryRunPreview(
    string ExecutionToken,
    string Script,
    ShellType Shell,
    RiskLevel Risk,
    bool IsApproved,
    DateTime CreatedAtUtc
);

public record ExecutionResult(
    bool Success,
    string Output,
    string? Error = null
);
