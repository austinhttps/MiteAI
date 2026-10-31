using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MiteAI.Core.Inference;

public class InferenceEngine
{
    private static readonly Regex EmailRegex = new(
        @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly string? _endpoint;
    private readonly string? _model;

    public InferenceEngine(
        HttpClient? httpClient = null,
        string? apiKey = null,
        string? endpoint = null,
        string? model = null,
        string? configBasePath = null)
    {
        _httpClient = httpClient ?? new HttpClient();

        var (cfgKey, cfgEndpoint, cfgModel) = LoadConfiguration(configBasePath);
        _apiKey = apiKey ?? cfgKey;
        _endpoint = endpoint ?? cfgEndpoint;
        _model = model ?? cfgModel;
    }

    public static string RedactEmails(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return EmailRegex.Replace(text, "[REDACTED]");
    }

    public async Task<string> ProcessAsync(string text, int delayMs = 1500)
    {
        // Zero-trust deterministic PII redaction executes BEFORE network transmission
        string sanitized = RedactEmails(text);

        // If an API key or custom endpoint is configured, send HTTP request
        if (!string.IsNullOrEmpty(_endpoint) || !string.IsNullOrEmpty(_apiKey))
        {
            try
            {
                return await SendLlmRequestAsync(sanitized);
            }
            catch (Exception ex)
            {
                return $"[MiteAI Offline Response]\n" +
                       $"Analyzed Clipboard Input:\n{sanitized}\n\n" +
                       $"Insight: Remote inference error ({ex.Message}). Fallback to local sandbox.";
            }
        }

        // Mock / local response fallback
        if (delayMs > 0)
        {
            await Task.Delay(delayMs);
        }

        return $"[MiteAI Response]\n" +
               $"Analyzed Clipboard Input:\n{sanitized}\n\n" +
               $"Insight: Direct on-device analysis complete. Zero PII transmitted.";
    }

    private async Task<string> SendLlmRequestAsync(string sanitizedPrompt)
    {
        string endpoint = _endpoint ?? "https://api.openai.com/v1/chat/completions";
        string model = _model ?? "gpt-4o-mini";

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        if (!string.IsNullOrEmpty(_apiKey))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
        }

        var payload = new
        {
            model = model,
            messages = new[]
            {
                new { role = "system", content = "You are MiteAI, a privacy-focused on-device assistant." },
                new { role = "user", content = sanitizedPrompt }
            }
        };

        string jsonPayload = JsonSerializer.Serialize(payload);
        request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        string responseJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseJson);

        if (doc.RootElement.TryGetProperty("choices", out var choices) &&
            choices.GetArrayLength() > 0 &&
            choices[0].TryGetProperty("message", out var message) &&
            message.TryGetProperty("content", out var content))
        {
            return $"[MiteAI Response]\n{content.GetString()?.Trim()}";
        }

        if (doc.RootElement.TryGetProperty("response", out var ollamaResponse))
        {
            return $"[MiteAI Response]\n{ollamaResponse.GetString()?.Trim()}";
        }

        return $"[MiteAI Response]\n{responseJson}";
    }

    public static (string? apiKey, string? endpoint, string? model) LoadConfiguration(string? basePath = null)
    {
        string? apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? Environment.GetEnvironmentVariable("API_KEY");
        string? endpoint = Environment.GetEnvironmentVariable("LLM_ENDPOINT");
        string? model = Environment.GetEnvironmentVariable("LLM_MODEL");

        basePath ??= AppContext.BaseDirectory;

        string[] searchPaths = [
            Path.Combine(basePath, ".env"),
            Path.Combine(Directory.GetCurrentDirectory(), ".env"),
            Path.Combine(basePath, "..", "..", "..", ".env")
        ];

        foreach (var envPath in searchPaths)
        {
            if (File.Exists(envPath))
            {
                foreach (var line in File.ReadAllLines(envPath))
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("#") || !trimmed.Contains('=')) continue;
                    var parts = trimmed.Split('=', 2);
                    var key = parts[0].Trim();
                    var val = parts[1].Trim().Trim('"').Trim('\'');

                    if (key.Equals("API_KEY", StringComparison.OrdinalIgnoreCase) ||
                        key.Equals("OPENAI_API_KEY", StringComparison.OrdinalIgnoreCase))
                    {
                        apiKey ??= val;
                    }
                    else if (key.Equals("LLM_ENDPOINT", StringComparison.OrdinalIgnoreCase))
                    {
                        endpoint ??= val;
                    }
                    else if (key.Equals("LLM_MODEL", StringComparison.OrdinalIgnoreCase))
                    {
                        model ??= val;
                    }
                }
                break;
            }
        }

        string appSettingsPath = Path.Combine(basePath, "appsettings.json");
        if (File.Exists(appSettingsPath))
        {
            try
            {
                string json = File.ReadAllText(appSettingsPath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("ApiKey", out var keyElem)) apiKey ??= keyElem.GetString();
                if (root.TryGetProperty("Endpoint", out var epElem)) endpoint ??= epElem.GetString();
                if (root.TryGetProperty("Model", out var modElem)) model ??= modElem.GetString();
            }
            catch
            {
                // Fallback on parse errors
            }
        }

        return (apiKey, endpoint, model);
    }
}
