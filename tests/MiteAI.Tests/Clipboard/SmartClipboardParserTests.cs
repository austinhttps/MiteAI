using MiteAI.Core.Clipboard;
using MiteAI.Core.Privacy;
using Xunit;

namespace MiteAI.Tests.Clipboard;

public class SmartClipboardParserTests
{
    [Fact]
    public void ParseAndTransform_FormatsIndentedJsonAndRedactsPii()
    {
        var redactor = new PiiRedactionFilter();
        var parser = new SmartClipboardParser(redactor);
        var rawJson = "{\"token\":\"sk-proj-1234567890abcdef12345678\",\"user\":\"developer@test.com\",\"active\":true}";

        var record = parser.ParseAndTransform(rawJson, "WindowsTerminal.exe");

        Assert.Equal(ClipboardFormatType.Json, record.DetectedFormat);
        Assert.Contains("[REDACTED_API_KEY]", record.FormattedContent);
        Assert.Contains("[REDACTED_EMAIL]", record.FormattedContent);
        Assert.Contains("\n", record.FormattedContent); // Formatted with indent
    }

    [Fact]
    public void ParseAndTransform_ConvertsCsvToMarkdownTable()
    {
        var redactor = new PiiRedactionFilter();
        var parser = new SmartClipboardParser(redactor);
        var csv = "Name,Age,Role\nAlice,30,Engineer\nBob,25,Designer";

        var record = parser.ParseAndTransform(csv, "excel.exe");

        Assert.Equal(ClipboardFormatType.Csv, record.DetectedFormat);
        Assert.Contains("| Name | Age | Role |", record.FormattedContent);
        Assert.Contains("| --- | --- | --- |", record.FormattedContent);
        Assert.Contains("| Alice | 30 | Engineer |", record.FormattedContent);
    }

    [Fact]
    public void ParseAndTransform_TreatsPlainTextNormally()
    {
        var redactor = new PiiRedactionFilter();
        var parser = new SmartClipboardParser(redactor);
        var text = "Simple note without specific structured markup.";

        var record = parser.ParseAndTransform(text, "notepad.exe");

        Assert.Equal(ClipboardFormatType.PlainText, record.DetectedFormat);
        Assert.Equal(text, record.FormattedContent);
    }
}
