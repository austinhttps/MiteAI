using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MiteAI.Core.Inference;

public class MockInferenceService : IModelInference
{
    private bool _isLoaded = true;

    public bool IsModelLoaded => _isLoaded;

    public Task<InferenceResult> GenerateAsync(InferenceRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string output = FormatOutput(request.Prompt);
        var tokens = output.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var result = new InferenceResult(
            OutputText: output,
            TokensGenerated: Math.Max(tokens, 12),
            TimeToFirstTokenMs: 8.5,
            TotalInferenceTimeMs: 42.0
        );

        return Task.FromResult(result);
    }

    public async IAsyncEnumerable<string> StreamGenerateAsync(
        InferenceRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string fullOutput = FormatOutput(request.Prompt);
        var words = fullOutput.Split(' ');

        for (int i = 0; i < words.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return (i == 0 ? "" : " ") + words[i];
            await Task.Yield();
        }
    }

    public Task<float[]> GetEmbeddingsAsync(string text, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Produce a deterministic 384-dimensional unit-norm embedding vector derived from text hash
        const int dimension = 384;
        var vector = new float[dimension];

        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(text ?? string.Empty));

        float sumSquares = 0f;
        for (int i = 0; i < dimension; i++)
        {
            byte b = hash[i % hash.Length];
            float val = ((float)b / 128.0f) - 1.0f + ((float)(i % 17) / 34.0f);
            vector[i] = val;
            sumSquares += val * val;
        }

        float norm = MathF.Sqrt(sumSquares);
        if (norm > 0)
        {
            for (int i = 0; i < dimension; i++)
            {
                vector[i] /= norm;
            }
        }

        return Task.FromResult(vector);
    }

    public InferenceMetrics GetMetrics()
    {
        return new InferenceMetrics(
            IsDirectMlActive: false,
            IsMockActive: true,
            VramUsageBytes: 128 * 1024 * 1024, // Simulated 128MB resident
            ActiveContextWindowTokens: 4096,
            TokensPerSecond: 85.0
        );
    }

    public Task UnloadModelAsync()
    {
        _isLoaded = false;
        return Task.CompletedTask;
    }

    private static string FormatOutput(string prompt)
    {
        return prompt switch
        {
            var p when p.Contains("Summarize", StringComparison.OrdinalIgnoreCase) =>
                "Summary: The input text conveys core local workflow operations executed securely on-device.",
            var p when p.Contains("Refactor", StringComparison.OrdinalIgnoreCase) =>
                "// Refactored on-device snippet\npublic void ProcessSafely() => Console.WriteLine(\"Executing locally.\");",
            _ => $"[Mock Mite Response] Completed local evaluation for prompt: {prompt}"
        };
    }
}
