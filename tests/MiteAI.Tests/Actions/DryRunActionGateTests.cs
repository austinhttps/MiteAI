using System;
using MiteAI.Core.Actions;
using Xunit;

namespace MiteAI.Tests.Actions;

public class DryRunActionGateTests
{
    [Fact]
    public void PrepareDryRun_IdentifiesCriticalRiskForDestructiveCommands()
    {
        var gate = new DryRunActionGate();
        var preview = gate.PrepareDryRun("Remove-Item -Recurse -Force C:\\Windows\\Temp", ShellType.PowerShell);

        Assert.Equal(RiskLevel.Critical, preview.Risk);
        Assert.False(preview.IsApproved);
        Assert.NotNull(preview.ExecutionToken);
        Assert.Contains("Remove-Item", preview.Script);
    }

    [Fact]
    public void PrepareDryRun_IdentifiesLowRiskForReadonlyCommands()
    {
        var gate = new DryRunActionGate();
        var preview = gate.PrepareDryRun("Get-Process | Where-Object WorkingSet -gt 100MB", ShellType.PowerShell);

        Assert.Equal(RiskLevel.Low, preview.Risk);
        Assert.False(preview.IsApproved);
    }

    [Fact]
    public void Execute_ThrowsWhenNotExplicitlyApproved()
    {
        var gate = new DryRunActionGate();
        var preview = gate.PrepareDryRun("Stop-Process -Name notepad", ShellType.PowerShell);

        Assert.Throws<InvalidOperationException>(() => gate.Execute(preview.ExecutionToken));
    }

    [Fact]
    public void ApproveAndExecute_SucceedsAndInvalidatesToken()
    {
        var gate = new DryRunActionGate();
        var preview = gate.PrepareDryRun("Get-Service", ShellType.PowerShell);

        bool approved = gate.Approve(preview.ExecutionToken);
        Assert.True(approved);

        var result = gate.Execute(preview.ExecutionToken);
        Assert.True(result.Success);

        // Replay prevention: same token cannot be executed twice
        Assert.Throws<InvalidOperationException>(() => gate.Execute(preview.ExecutionToken));
    }

    [Fact]
    public void RateLimiter_HaltsWhenThresholdExceeded()
    {
        var gate = new DryRunActionGate(maxBatchActionsThreshold: 5);
        for (int i = 0; i < 5; i++)
        {
            var p = gate.PrepareDryRun($"echo Item {i}", ShellType.Cmd);
            gate.Approve(p.ExecutionToken);
            gate.Execute(p.ExecutionToken);
        }

        Assert.True(gate.IsRateLimitPaused);

        // Next action should require explicit resumption
        var next = gate.PrepareDryRun("echo Over limit", ShellType.Cmd);
        Assert.Equal(RiskLevel.High, next.Risk);

        gate.ResetRateLimiter();
        Assert.False(gate.IsRateLimitPaused);
    }
}
