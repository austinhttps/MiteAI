namespace MiteAI.Core.Context;

public record ActiveWindowContext(
    string WindowTitle,
    string ProcessName,
    string? SelectedText,
    bool IsSuppressed,
    string StatusSummary
);
