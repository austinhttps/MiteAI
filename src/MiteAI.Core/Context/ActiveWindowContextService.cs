using System;
using MiteAI.Core.Privacy;

namespace MiteAI.Core.Context;

public class ActiveWindowContextService
{
    private readonly IContextBlindspotService _blindspotService;

    public ActiveWindowContextService(IContextBlindspotService blindspotService)
    {
        _blindspotService = blindspotService ?? throw new ArgumentNullException(nameof(blindspotService));
    }

    public ActiveWindowContext CreateContext(string windowTitle, string processName, string? selectedText)
    {
        bool isSuppressed = _blindspotService.IsExcluded(windowTitle, processName);

        if (isSuppressed)
        {
            return new ActiveWindowContext(
                WindowTitle: "[Protected Window]",
                ProcessName: processName,
                SelectedText: null,
                IsSuppressed: true,
                StatusSummary: "[Context Suppressed - Sensitive Application Active]"
            );
        }

        return new ActiveWindowContext(
            WindowTitle: windowTitle ?? string.Empty,
            ProcessName: processName ?? string.Empty,
            SelectedText: selectedText,
            IsSuppressed: false,
            StatusSummary: $"Active: {processName}"
        );
    }
}
