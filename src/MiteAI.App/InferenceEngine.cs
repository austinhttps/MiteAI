using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LLama;
using LLama.Common;
using MiteAI.Core.Privacy;

namespace MiteAI.App;

public static class InferenceEngine
{
    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MiteAI");
    private static readonly string SettingsFile = Path.Combine(SettingsDir, "settings.json");

    private static string? _configuredModelPath;
    private static LLamaWeights? _weights;
    private static ModelParams? _parameters;
    private static readonly object InitLock = new();
    private static readonly PiiRedactionFilter PrivacyFilter = new();

    public static RedactionResult RedactPii(string? text) => PrivacyFilter.Redact(text);

    public static string? ResolveModelPath()
    {
        // 1. In-memory override
        if (!string.IsNullOrEmpty(_configuredModelPath) && File.Exists(_configuredModelPath))
        {
            return _configuredModelPath;
        }

        // 2. Persisted settings in %LOCALAPPDATA%\MiteAI\settings.json
        try
        {
            if (File.Exists(SettingsFile))
            {
                var json = File.ReadAllText(SettingsFile);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("ModelPath", out var prop))
                {
                    var savedPath = prop.GetString();
                    if (!string.IsNullOrWhiteSpace(savedPath) && File.Exists(savedPath))
                    {
                        _configuredModelPath = savedPath;
                        return savedPath;
                    }
                }
            }
        }
        catch
        {
        }

        // 3. %LOCALAPPDATA%\MiteAI\models\*.gguf
        var localModelsDir = Path.Combine(SettingsDir, "models");
        if (Directory.Exists(localModelsDir))
        {
            var ggufFiles = Directory.GetFiles(localModelsDir, "*.gguf");
            if (ggufFiles.Length > 0)
            {
                _configuredModelPath = ggufFiles[0];
                return ggufFiles[0];
            }
        }

        // 4. Default user home directory path (e.g. C:\Users\<User>\qwen2.5-coder-1.5b-instruct-q2_k.gguf)
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var defaultUserPath = Path.Combine(userProfile, "qwen2.5-coder-1.5b-instruct-q2_k.gguf");
        if (File.Exists(defaultUserPath))
        {
            _configuredModelPath = defaultUserPath;
            return defaultUserPath;
        }

        // 5. %USERPROFILE%\models\*.gguf
        var userModelsDir = Path.Combine(userProfile, "models");
        if (Directory.Exists(userModelsDir))
        {
            var userGgufFiles = Directory.GetFiles(userModelsDir, "*.gguf");
            if (userGgufFiles.Length > 0)
            {
                _configuredModelPath = userGgufFiles[0];
                return userGgufFiles[0];
            }
        }

        // 6. Application binary directory models\*.gguf
        var appModelsDir = Path.Combine(AppContext.BaseDirectory, "models");
        if (Directory.Exists(appModelsDir))
        {
            var appGgufFiles = Directory.GetFiles(appModelsDir, "*.gguf");
            if (appGgufFiles.Length > 0)
            {
                _configuredModelPath = appGgufFiles[0];
                return appGgufFiles[0];
            }
        }

        return null;
    }

    public static void SetModelPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            throw new FileNotFoundException("Specified GGUF model file not found.", path);
        }

        lock (InitLock)
        {
            _configuredModelPath = path;
            _weights?.Dispose();
            _weights = null;
            _parameters = null;
        }

        try
        {
            Directory.CreateDirectory(SettingsDir);
            var json = JsonSerializer.Serialize(new { ModelPath = path }, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFile, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to persist model path: {ex.Message}");
        }
    }

    public static string GetActiveModelName()
    {
        var path = ResolveModelPath();
        return string.IsNullOrEmpty(path) ? "No Model Found" : Path.GetFileName(path);
    }

    private static void EnsureModelLoaded()
    {
        if (_weights != null && _parameters != null) return;

        lock (InitLock)
        {
            if (_weights == null)
            {
                var resolvedPath = ResolveModelPath();
                if (string.IsNullOrWhiteSpace(resolvedPath) || !File.Exists(resolvedPath))
                {
                    throw new FileNotFoundException("No GGUF model file found. Please configure a model path.");
                }

                _parameters = new ModelParams(resolvedPath)
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

        var modelPath = ResolveModelPath();
        if (string.IsNullOrWhiteSpace(modelPath) || !File.Exists(modelPath))
        {
            yield return "[MiteAI Error] No GGUF model file found. Please select a .gguf model.";
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
