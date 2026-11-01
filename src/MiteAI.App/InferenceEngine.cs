using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MiteAI.App;

public static class InferenceEngine
{
    private static readonly Regex EmailRegex = new(
        @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly HttpClient HttpClient = new();

    public static async Task<string> ProcessTextAsync(string clipboardText)
    {
        if (string.IsNullOrEmpty(clipboardText))
        {
            return string.Empty;
        }

        // Redact emails from the input using Regex
        string redactedText = EmailRegex.Replace(clipboardText, "[REDACTED]");

        // Local-First Graceful Degradation routing
        string? apiKey = Environment.GetEnvironmentVariable("MITEAI_API_KEY", EnvironmentVariableTarget.User);
        string endpoint;
        string model;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            endpoint = "http://localhost:11434/v1/chat/completions";
            apiKey = "local";
            model = "qwen2.5";
        }
        else
        {
            endpoint = "https://api.openai.com/v1/chat/completions";
            model = "gpt-4o-mini";
        }

        var payload = new
        {
            model = model,
            messages = new[]
            {
                new { role = "user", content = redactedText }
            }
        };

        string jsonPayload = JsonSerializer.Serialize(payload);
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }

        try
        {
            using var response = await HttpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            string responseJson = await response.Content.ReadAsStringAsync();
            using var responseDoc = JsonDocument.Parse(responseJson);

            if (responseDoc.RootElement.TryGetProperty("choices", out var choices) &&
                choices.GetArrayLength() > 0 &&
                choices[0].TryGetProperty("message", out var message) &&
                message.TryGetProperty("content", out var content))
            {
                return content.GetString()?.Trim() ?? string.Empty;
            }

            if (responseDoc.RootElement.TryGetProperty("response", out var ollamaResponse))
            {
                return ollamaResponse.GetString()?.Trim() ?? string.Empty;
            }

            return responseJson;
        }
        catch (Exception ex)
        {
            return $"Error during inference: {ex.Message}";
        }
    }
}
