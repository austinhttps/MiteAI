using System;
using System.Collections.Generic;
using System.IO;

namespace MiteAI.Core.Privacy;

public class ContextBlindspotService : IContextBlindspotService
{
    private readonly HashSet<string> _excludedProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "1password.exe",
        "1password",
        "bitwarden.exe",
        "bitwarden",
        "keepass.exe",
        "keepass",
        "lastpass.exe",
        "lastpass",
        "msauthenticator.exe",
        "authy.exe"
    };

    private readonly HashSet<string> _excludedTitleKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "banking",
        "chase online",
        "wells fargo",
        "bank of america",
        "fidelity investments",
        "paypal checkout",
        "mastercard",
        "visa secure"
    };

    public bool IsExcluded(string? windowTitle, string? processName)
    {
        if (!string.IsNullOrWhiteSpace(processName))
        {
            var fileName = Path.GetFileName(processName);
            if (_excludedProcesses.Contains(fileName) || _excludedProcesses.Contains(Path.GetFileNameWithoutExtension(fileName)))
            {
                return true;
            }
        }

        if (!string.IsNullOrWhiteSpace(windowTitle))
        {
            foreach (var keyword in _excludedTitleKeywords)
            {
                if (windowTitle.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void AddProcessExclusion(string processName)
    {
        if (!string.IsNullOrWhiteSpace(processName))
        {
            _excludedProcesses.Add(Path.GetFileName(processName));
        }
    }

    public void AddTitleKeywordExclusion(string keyword)
    {
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            _excludedTitleKeywords.Add(keyword.Trim());
        }
    }

    public void RemoveProcessExclusion(string processName)
    {
        if (!string.IsNullOrWhiteSpace(processName))
        {
            _excludedProcesses.Remove(Path.GetFileName(processName));
        }
    }
}
