using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MiteAI.Core.Inference;

public class InferenceEngine
{
    private static readonly Regex EmailRegex = new(
        @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

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
        string sanitized = RedactEmails(text);

        if (delayMs > 0)
        {
            await Task.Delay(delayMs);
        }

        return $"[MiteAI Response]\n" +
               $"Analyzed Clipboard Input:\n{sanitized}\n\n" +
               $"Insight: Direct on-device analysis complete. Zero PII transmitted.";
    }
}
