using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
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

        string response = await engine.ProcessAsync(input, delayMs: 0);

        Assert.Contains("[MiteAI Response]", response);
        Assert.DoesNotContain("john.doe@company.com", response);
        Assert.Contains("[REDACTED]", response);
    }

    [Fact]
    public async Task ProcessAsync_RedactsPiiBeforeSendingOverNetwork()
    {
        string capturedPayload = string.Empty;

        var mockHandler = new TestHttpMessageHandler((request, cancellationToken) =>
        {
            capturedPayload = request.Content?.ReadAsStringAsync().Result ?? string.Empty;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"choices\": [{\"message\": {\"content\": \"Analyzed sanitized payload safely.\"}}]}",
                    System.Text.Encoding.UTF8,
                    "application/json")
            };
            return Task.FromResult(response);
        });

        var client = new HttpClient(mockHandler);
        var engine = new InferenceEngine(
            httpClient: client,
            apiKey: "test-sk-key-12345",
            endpoint: "https://api.openai.com/v1/chat/completions",
            model: "gpt-4o-mini");

        string rawInput = "Please audit user sensitive.contractor@secretorg.net right now.";
        string result = await engine.ProcessAsync(rawInput);

        // Verify the payload sent over the wire NEVER contained the raw email
        Assert.DoesNotContain("sensitive.contractor@secretorg.net", capturedPayload);
        Assert.Contains("[REDACTED]", capturedPayload);

        // Verify the mock response was parsed
        Assert.Contains("Analyzed sanitized payload safely.", result);
    }

    [Fact]
    public void LoadConfiguration_ReadsEnvFileCorrectly()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"MiteAI_Test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            string envPath = Path.Combine(tempDir, ".env");
            File.WriteAllLines(envPath, new[]
            {
                "# Comment line",
                "API_KEY=test-api-key-env-999",
                "LLM_ENDPOINT=http://localhost:11434/v1/chat/completions",
                "LLM_MODEL=llama3.2"
            });

            var (apiKey, endpoint, model) = InferenceEngine.LoadConfiguration(tempDir);

            Assert.Equal("test-api-key-env-999", apiKey);
            Assert.Equal("http://localhost:11434/v1/chat/completions", endpoint);
            Assert.Equal("llama3.2", model);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public TestHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _handler(request, cancellationToken);
        }
    }
}
