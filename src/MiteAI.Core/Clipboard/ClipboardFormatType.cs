using System;

namespace MiteAI.Core.Clipboard;

public enum ClipboardFormatType
{
    PlainText,
    Json,
    Csv,
    Code
}

public record ClipboardRecord(
    string Id,
    string OriginalContent,
    string FormattedContent,
    ClipboardFormatType DetectedFormat,
    string SourceApplication,
    DateTime CapturedAtUtc,
    int RedactionCount
);
