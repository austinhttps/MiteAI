using System.Threading.Tasks;
using MiteAI.Core.Inference;
using Xunit;

namespace MiteAI.Tests.Inference;

public class InferenceEngineTests
{
    [Theory]
    [InlineData("Contact developer@example.com for support.", "Contact [REDACTED] for support.")]
    [InlineData("Send copies to alice@work.corp and bob.smith+test@domain.co.uk please.", "Send copies to [REDACTED] and [REDACTED] please.")]
    [InlineData("No email addresses here, just numbers 12345 and text.", "No email addresses here, just numbers 12345 and text.")]
    public void RedactEmails_RedactsEmailPatternsCorrectly(string input, string expected)
    {
        string result = InferenceEngine.RedactEmails(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void RedactEmails_HandlesNullAndEmptySafely()
    {
        Assert.Equal(string.Empty, InferenceEngine.RedactEmails(string.Empty));
        Assert.Equal(string.Empty, InferenceEngine.RedactEmails(null!));
    }

    [Fact]
    public async Task ProcessAsync_RedactsPiiAndReturnsFormattedResponse()
    {
        var engine = new InferenceEngine();
        string input = "User inquiry from john.doe@company.com with urgent request.";

        // Use 0 delay in unit tests for speed
        string response = await engine.ProcessAsync(input, delayMs: 0);

        Assert.Contains("[MiteAI Response]", response);
        Assert.DoesNotContain("john.doe@company.com", response);
        Assert.Contains("[REDACTED]", response);
    }
}
