using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MiteAI.Core.Privacy;

public record RedactionResult(
    string SanitizedText,
    int RedactionCount,
    IReadOnlySet<PiiMaskType> DetectedTypes
);

public class PiiRedactionFilter
{
    private static readonly Regex SsnRegex = new(
        @"\b\d{3}-\d{2}-\d{4}\b",
        RegexOptions.Compiled);

    private static readonly Regex CreditCardRegex = new(
        @"\b(?:4[0-9]{3}|5[1-5][0-9]{2}|6(?:011|5[0-9]{2})|3[47][0-9]{2})[- ]?[0-9]{4}[- ]?[0-9]{4}[- ]?[0-9]{4}\b",
        RegexOptions.Compiled);

    private static readonly Regex ApiKeyRegex = new(
        @"\b(?:sk-[a-zA-Z0-9_\-]{20,}|ghp_[a-zA-Z0-9]{20,}|AKIA[0-9A-Z]{16})\b",
        RegexOptions.Compiled);

    private static readonly Regex EmailRegex = new(
        @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b",
        RegexOptions.Compiled);

    public RedactionResult Redact(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return new RedactionResult(string.Empty, 0, new HashSet<PiiMaskType>());
        }

        int count = 0;
        var types = new HashSet<PiiMaskType>();

        string text = SsnRegex.Replace(input, m =>
        {
            count++;
            types.Add(PiiMaskType.Ssn);
            return "[REDACTED_SSN]";
        });

        text = CreditCardRegex.Replace(text, m =>
        {
            count++;
            types.Add(PiiMaskType.CreditCard);
            return "[REDACTED_CARD]";
        });

        text = ApiKeyRegex.Replace(text, m =>
        {
            count++;
            types.Add(PiiMaskType.ApiKey);
            return "[REDACTED_API_KEY]";
        });

        text = EmailRegex.Replace(text, m =>
        {
            count++;
            types.Add(PiiMaskType.Email);
            return "[REDACTED_EMAIL]";
        });

        return new RedactionResult(text, count, types);
    }
}
