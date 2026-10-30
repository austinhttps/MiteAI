using MiteAI.Core.Privacy;
using Xunit;

namespace MiteAI.Tests.Privacy;

public class PiiRedactionFilterTests
{
    [Theory]
    [InlineData("My SSN is 123-45-6789 and my card is 4532-1122-3344-5566.", "My SSN is [REDACTED_SSN] and my card is [REDACTED_CARD].")]
    [InlineData("OpenAI Key: sk-proj-1234567890abcdef12345678 and GitHub ghp_1234567890abcdef12345678", "OpenAI Key: [REDACTED_API_KEY] and GitHub [REDACTED_API_KEY]")]
    [InlineData("Contact me at developer@example.com for assistance.", "Contact me at [REDACTED_EMAIL] for assistance.")]
    [InlineData("Safe regular text with 42 items and 3.14159 value.", "Safe regular text with 42 items and 3.14159 value.")]
    public void Redact_SanitizesSensitiveTokens(string input, string expected)
    {
        var filter = new PiiRedactionFilter();
        var result = filter.Redact(input);

        Assert.Equal(expected, result.SanitizedText);
    }

    [Fact]
    public void Redact_ReportsAccurateRedactionCounts()
    {
        var filter = new PiiRedactionFilter();
        var input = "SSN: 999-88-7777, Key: AKIAIOSFODNN7EXAMPLE, Card: 5412 7512 3412 3456";

        var result = filter.Redact(input);

        Assert.Equal(3, result.RedactionCount);
        Assert.Contains(PiiMaskType.Ssn, result.DetectedTypes);
        Assert.Contains(PiiMaskType.ApiKey, result.DetectedTypes);
        Assert.Contains(PiiMaskType.CreditCard, result.DetectedTypes);
    }

    [Fact]
    public void Redact_HandlesNullOrEmptyInputSafely()
    {
        var filter = new PiiRedactionFilter();

        var emptyResult = filter.Redact(string.Empty);
        Assert.Equal(string.Empty, emptyResult.SanitizedText);
        Assert.Equal(0, emptyResult.RedactionCount);

        var nullResult = filter.Redact(null!);
        Assert.Equal(string.Empty, nullResult.SanitizedText);
        Assert.Equal(0, nullResult.RedactionCount);
    }
}
