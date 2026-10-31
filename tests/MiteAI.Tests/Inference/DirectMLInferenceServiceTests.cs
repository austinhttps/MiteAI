using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MiteAI.Core.Inference;
using Xunit;

namespace MiteAI.Tests.Inference;

public class DirectMLInferenceServiceTests
{
    [Fact]
    public void Constructor_FailsFastWhenModelFileNotFoundAndNoFallback()
    {
        var options = new DirectMLInferenceOptions
        {
            ModelPath = "C:\\non_existent_weights.onnx",
            FallbackToCpu = false
        };

        Assert.Throws<FileNotFoundException>(() => new DirectMLInferenceService(options));
    }

    [Fact]
    public void Constructor_GracefullyHandlesMissingModelWhenFallbackEnabled()
    {
        var options = new DirectMLInferenceOptions
        {
            ModelPath = "C:\\non_existent_weights.onnx",
            FallbackToCpu = true
        };

        var service = new DirectMLInferenceService(options);
        Assert.False(service.IsModelLoaded);
        Assert.Contains("missing", service.StatusDescription, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StreamGenerateAsync_YieldsTokensProgressively()
    {
        var options = new DirectMLInferenceOptions
        {
            ModelPath = null, // Mock fallback mode
            FallbackToCpu = true
        };

        var service = new DirectMLInferenceService(options);
        var request = new InferenceRequest("Analyze local execution performance");

        var tokens = new List<string>();
        await foreach (var token in service.StreamGenerateAsync(request))
        {
            tokens.Add(token);
        }

        Assert.NotEmpty(tokens);
        var fullText = string.Join("", tokens);
        Assert.Contains("local", fullText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HandleGpuVramExhaustion_FallsBackToCpuGracefully()
    {
        var options = new DirectMLInferenceOptions
        {
            DeviceId = 0,
            FallbackToCpu = true
        };

        var service = new DirectMLInferenceService(options);
        // Simulate DirectML OutOfMemory / VRAM exhaustion event
        service.SimulateDirectMlVramExhaustion();

        Assert.True(service.IsCpuFallbackActive);
        Assert.Contains("GPU out of memory – falling back to CPU", service.StatusDescription);

        var result = await service.GenerateAsync(new InferenceRequest("Test query"));
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.OutputText));
    }
}
