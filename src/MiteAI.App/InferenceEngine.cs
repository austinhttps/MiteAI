using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LLama;
using LLama.Common;

namespace MiteAI.App;

public static class InferenceEngine
{
    private const string ModelPath = @"C:\Users\Austi\qwen2.5-coder-1.5b-instruct-q2_k.gguf";

    private static readonly Regex EmailRegex = new(
        @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Lazy<(ModelParams Parameters, LLamaWeights Weights, LLamaContext Context)> LazyModel =
        new(() =>
        {
            var parameters = new ModelParams(ModelPath)
            {
                ContextSize = 2048,
                GpuLayerCount = 0
            };
            var weights = LLamaWeights.LoadFromFile(parameters);
            var context = weights.CreateContext(parameters);
            return (parameters, weights, context);
        });

    public static async Task<string> ProcessTextAsync(string clipboardText)
    {
        if (string.IsNullOrEmpty(clipboardText))
        {
            return string.Empty;
        }

        // 1. Regex PII redaction on clipboardText
        string redactedText = EmailRegex.Replace(clipboardText, "[REDACTED]");

        if (!File.Exists(ModelPath))
        {
            return $"[MiteAI Error] Model file not found at: {ModelPath}";
        }

        try
        {
            var (parameters, weights, context) = LazyModel.Value;
            var executor = new StatelessExecutor(weights, parameters);

            var inferenceParams = new InferenceParams
            {
                MaxTokens = 256
            };

            var prompt = $"<|im_start|>system\nYou are MiteAI, a helpful desktop assistant.<|im_end|>\n<|im_start|>user\n{redactedText}<|im_end|>\n<|im_start|>assistant\n";

            var sb = new StringBuilder();
            await foreach (var token in executor.InferAsync(prompt, inferenceParams))
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
