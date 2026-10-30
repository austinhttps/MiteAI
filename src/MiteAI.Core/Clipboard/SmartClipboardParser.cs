using System;
using System.IO;
using System.Text;
using System.Text.Json;
using MiteAI.Core.Privacy;

namespace MiteAI.Core.Clipboard;

public class SmartClipboardParser
{
    private readonly PiiRedactionFilter _redactionFilter;

    public SmartClipboardParser(PiiRedactionFilter redactionFilter)
    {
        _redactionFilter = redactionFilter ?? throw new ArgumentNullException(nameof(redactionFilter));
    }

    public ClipboardRecord ParseAndTransform(string rawText, string sourceApplication)
    {
        var safeInput = rawText ?? string.Empty;
        var redactionResult = _redactionFilter.Redact(safeInput);
        var sanitized = redactionResult.SanitizedText;

        var (format, formatted) = DetectAndFormat(sanitized);

        return new ClipboardRecord(
            Id: Guid.NewGuid().ToString("N"),
            OriginalContent: safeInput,
            FormattedContent: formatted,
            DetectedFormat: format,
            SourceApplication: sourceApplication ?? "Unknown",
            CapturedAtUtc: DateTime.UtcNow,
            RedactionCount: redactionResult.RedactionCount
        );
    }

    private static (ClipboardFormatType format, string formatted) DetectAndFormat(string text)
    {
        var trimmed = text.Trim();

        // 1. Check if JSON
        if ((trimmed.StartsWith('{') && trimmed.EndsWith('}')) || (trimmed.StartsWith('[') && trimmed.EndsWith(']')))
        {
            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                var prettyJson = JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                return (ClipboardFormatType.Json, prettyJson);
            }
            catch
            {
                // Fall through if not valid JSON
            }
        }

        // 2. Check if CSV (contains multiple lines with consistent comma delimiters)
        var lines = trimmed.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length >= 2 && lines[0].Contains(','))
        {
            var headerCols = lines[0].Split(',');
            if (headerCols.Length >= 2)
            {
                var sb = new StringBuilder();
                sb.AppendLine("| " + string.Join(" | ", Array.ConvertAll(headerCols, c => c.Trim())) + " |");
                sb.AppendLine("| " + string.Join(" | ", Array.ConvertAll(headerCols, _ => "---")) + " |");

                for (int i = 1; i < lines.Length; i++)
                {
                    var cols = lines[i].Split(',');
                    if (cols.Length == headerCols.Length)
                    {
                        sb.AppendLine("| " + string.Join(" | ", Array.ConvertAll(cols, c => c.Trim())) + " |");
                    }
                }
                return (ClipboardFormatType.Csv, sb.ToString().TrimEnd());
            }
        }

        return (ClipboardFormatType.PlainText, text);
    }
}
