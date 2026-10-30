using System;
using MiteAI.Core.Hardware;
using Xunit;

namespace MiteAI.Tests.Hardware;

public class PowerAdaptiveManagerTests
{
    [Fact]
    public void PowerTransition_BatteryDegradesTierAndContext()
    {
        var manager = new PowerAdaptiveManager();
        Assert.Equal(ModelTier.HighPerformance, manager.CurrentTier);
        Assert.Equal(8192, manager.MaxContextWindow);

        manager.SetPowerSource(PowerSource.Battery);

        Assert.Equal(ModelTier.PowerSaver, manager.CurrentTier);
        Assert.Equal(2048, manager.MaxContextWindow);
        Assert.True(manager.AreBackgroundAgentsPaused);
    }

    [Fact]
    public void PowerTransition_AcRestoresHighPerformance()
    {
        var manager = new PowerAdaptiveManager();
        manager.SetPowerSource(PowerSource.Battery);
        manager.SetPowerSource(PowerSource.Ac);

        Assert.Equal(ModelTier.HighPerformance, manager.CurrentTier);
        Assert.Equal(8192, manager.MaxContextWindow);
        Assert.False(manager.AreBackgroundAgentsPaused);
    }

    [Fact]
    public void IdleTimeout_TriggersVramPagingRecommendation()
    {
        var manager = new PowerAdaptiveManager(idleTimeoutMinutes: 3);
        Assert.False(manager.IsVramPagingRecommended());

        // Simulate 4 minutes of inactivity
        manager.SimulateIdleTime(TimeSpan.FromMinutes(4));

        Assert.True(manager.IsVramPagingRecommended());

        // User action resets idle timer
        manager.RecordUserActivity();
        Assert.False(manager.IsVramPagingRecommended());
    }

    [Fact]
    public void ConstrainedMemory_ThrottlesContextWindowToPreventOom()
    {
        var manager = new PowerAdaptiveManager();
        
        // When system reports < 4096 MB free memory
        manager.UpdateAvailableMemory(2048);

        Assert.True(manager.MaxContextWindow <= 1024);
    }
}
