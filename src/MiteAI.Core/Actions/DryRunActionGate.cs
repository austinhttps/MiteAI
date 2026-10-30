using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace MiteAI.Core.Actions;

public class DryRunActionGate
{
    private readonly int _maxBatchActionsThreshold;
    private int _actionCounter = 0;
    private bool _isRateLimitPaused = false;

    private readonly ConcurrentDictionary<string, DryRunPreview> _pendingActions = new();

    public bool IsRateLimitPaused => _isRateLimitPaused;

    public DryRunActionGate(int maxBatchActionsThreshold = 50)
    {
        _maxBatchActionsThreshold = maxBatchActionsThreshold;
    }

    public DryRunPreview PrepareDryRun(string script, ShellType shellType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(script);

        var risk = AssessRisk(script);

        if (_isRateLimitPaused && risk < RiskLevel.High)
        {
            risk = RiskLevel.High; // Elevate risk if batch boundary paused
        }

        var token = Guid.NewGuid().ToString("N");
        var preview = new DryRunPreview(
            ExecutionToken: token,
            Script: script.Trim(),
            Shell: shellType,
            Risk: risk,
            IsApproved: false,
            CreatedAtUtc: DateTime.UtcNow
        );

        _pendingActions[token] = preview;
        return preview;
    }

    public bool Approve(string token)
    {
        if (_pendingActions.TryGetValue(token, out var preview))
        {
            _pendingActions[token] = preview with { IsApproved = true };
            return true;
        }
        return false;
    }

    public ExecutionResult Execute(string token)
    {
        if (!_pendingActions.TryRemove(token, out var preview))
        {
            throw new InvalidOperationException($"Invalid or already executed token: {token}");
        }

        if (!preview.IsApproved)
        {
            throw new InvalidOperationException("Action rejected: Explicit dry-run operator approval is required before execution.");
        }

        _actionCounter++;
        if (_actionCounter >= _maxBatchActionsThreshold)
        {
            _isRateLimitPaused = true;
        }

        // Return deterministic sandboxed dry-run simulated result
        return new ExecutionResult(
            Success: true,
            Output: $"[DryRun Execution Simulated] Successfully validated {preview.Shell} script: '{preview.Script}'"
        );
    }

    public void ResetRateLimiter()
    {
        _actionCounter = 0;
        _isRateLimitPaused = false;
    }

    private static RiskLevel AssessRisk(string script)
    {
        // Critical patterns: recursive deletions, drive format, registry wipe
        if (Regex.IsMatch(script, @"(-Recurse|rmdir\s+/s|format\s+[a-z]:|diskpart|Remove-Item\s+-Recurse)", RegexOptions.IgnoreCase))
        {
            return RiskLevel.Critical;
        }

        // High patterns: deletion, termination, modification
        if (Regex.IsMatch(script, @"(Remove-Item|del\s+|del\b|Stop-Process|reg\s+delete|reg\s+add)", RegexOptions.IgnoreCase))
        {
            return RiskLevel.High;
        }

        // Medium patterns: state change / writes
        if (Regex.IsMatch(script, @"(Set-|New-Item|mkdir|copy|move|Out-File)", RegexOptions.IgnoreCase))
        {
            return RiskLevel.Medium;
        }

        return RiskLevel.Low;
    }
}
