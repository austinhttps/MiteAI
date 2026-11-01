using System;
using System.IO;
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

        // 1. Redact emails from the input using Regex
        string redactedText = EmailRegex.Replace(clipboardText, "[REDACTED]");

        // 2. Read the AI_Endpoint and API_KEY from appsettings.json
        string endpoint = "https://api.openai.com/v1/chat/completions";
        string apiKey = string.Empty;

        string appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(appSettingsPath))
        {
            appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
        }

        if (File.Exists(appSettingsPath))
        {
            try
            {
                string json = await File.ReadAllTextAsync(appSettingsPath);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("AI_Endpoint", out var epElem) &&
                    epElem.GetString() is string ep && !string.IsNullOrWhiteSpace(ep))
                {
                    endpoint = ep;
                }

                if (doc.RootElement.TryGetProperty("API_KEY", out var keyElem) &&
                    keyElem.GetString() is string key && !string.IsNullOrWhiteSpace(key))
                {
                    apiKey = key;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to parse appsettings.json: {ex.Message}");
            }
        }

        // 3. Use HttpClient to send the redacted text to the endpoint as a JSON payload
        var payload = new
        {
            model = "gpt-4o-mini",
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

            // 4. Return the string content of the AI's response
            if (responseDoc.RootElement.TryGetProperty("choices", out var choices) &&
                choices.GetArrayLength() > 0 &&
                choices[0].TryGetProperty("message", out var message) &&
                message.TryGetProperty("content", out var content))
            {
                return content.GetString()?.Trim() ?? string.Empty;
            }

            return responseJson;
        }
        catch (Exception ex)
        {
            return $"Error during inference: {ex.Message}";
        }
    }
}
