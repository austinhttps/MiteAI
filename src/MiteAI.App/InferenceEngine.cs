using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LLama;
using LLama.Common;
using MiteAI.Core.Privacy;

namespace MiteAI.App;

public static class InferenceEngine
{
    private const string ModelPath = @"C:\Users\Austi\qwen2.5-coder-1.5b-instruct-q2_k.gguf";
    private static LLamaWeights? _weights;
    private static ModelParams? _parameters;
    private static readonly object InitLock = new();
    private static readonly PiiRedactionFilter PrivacyFilter = new();

    public static RedactionResult RedactPii(string? text) => PrivacyFilter.Redact(text);

    private static void EnsureModelLoaded()
    {
        if (_weights != null && _parameters != null) return;

        lock (InitLock)
        {
            if (_weights == null)
            {
                _parameters = new ModelParams(ModelPath)
                {
                    ContextSize = 1024,
                    GpuLayerCount = 0
                };
                _weights = LLamaWeights.LoadFromFile(_parameters);
            }
        }
    }

    public static async IAsyncEnumerable<string> StreamTextAsync(
        string clipboardText,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clipboardText))
        {
            yield return "Error: Clipboard is empty or contains non-text data.";
            yield break;
        }

        if (!File.Exists(ModelPath))
        {
            yield return $"[MiteAI Error] Model file not found at: {ModelPath}";
            yield break;
        }

        // Full zero-trust deterministic PII redaction (SSNs, cards, API keys, emails)
        var redaction = PrivacyFilter.Redact(clipboardText);
        string redactedText = redaction.SanitizedText;

        EnsureModelLoaded();

        using var context = _weights!.CreateContext(_parameters!);
        var executor = new InteractiveExecutor(context);
        var inferenceParams = new InferenceParams { MaxTokens = 256 };

        string prompt = $"Process this text:\n{redactedText}\n\nResponse:";

        await foreach (var token in executor.InferAsync(prompt, inferenceParams, cancellationToken))
        {
            yield return token;
        }
    }

    public static async Task<string> ProcessTextAsync(string clipboardText)
    {
        try
        {
            var sb = new StringBuilder();
            await foreach (var token in StreamTextAsync(clipboardText))
            {
                sb.Append(token);
            }
            return sb.ToString().Trim();
        }
        catch (Exception ex)
        {
            return $"Error during local inference: {ex.Message}";
        }
    }
}
