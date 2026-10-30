using System.Threading.Tasks;
using MiteAI.Core.Inference;
using Xunit;

namespace MiteAI.Tests.Inference;

public class MockInferenceServiceTests
{
    [Fact]
    public async Task MockInference_ReturnsDeterministicResponseAndMetrics()
    {
        IModelInference service = new MockInferenceService();
        var request = new InferenceRequest("Summarize this text", MaxTokens: 50);

        var result = await service.GenerateAsync(request);

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.OutputText));
        Assert.True(result.TokensGenerated > 0);
        Assert.True(result.TimeToFirstTokenMs >= 0);

        var metrics = service.GetMetrics();
        Assert.True(metrics.IsDirectMlActive || metrics.IsMockActive);
        Assert.True(metrics.VramUsageBytes >= 0);
    }

    [Fact]
    public async Task MockInference_GeneratesDeterministicEmbeddings()
    {
        IModelInference service = new MockInferenceService();
        var text = "Zero trust privacy on Windows 11";

        var embedding1 = await service.GetEmbeddingsAsync(text);
        var embedding2 = await service.GetEmbeddingsAsync(text);

        Assert.NotNull(embedding1);
        Assert.Equal(384, embedding1.Length);
        Assert.Equal(embedding1, embedding2); // Deterministic
    }
}
