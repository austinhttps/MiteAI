using System;

namespace MiteAI.Core.Hardware;

public class PowerAdaptiveManager
{
    private readonly TimeSpan _idleTimeout;
    private DateTime _lastActivityUtc;
    private PowerSource _powerSource = PowerSource.Ac;
    private long _availableMemoryMb = 16384;

    public ModelTier CurrentTier { get; private set; } = ModelTier.HighPerformance;
    public int MaxContextWindow { get; private set; } = 8192;
    public bool AreBackgroundAgentsPaused { get; private set; } = false;

    public PowerAdaptiveManager(int idleTimeoutMinutes = 3)
    {
        _idleTimeout = TimeSpan.FromMinutes(idleTimeoutMinutes);
        _lastActivityUtc = DateTime.UtcNow;
    }

    public void SetPowerSource(PowerSource source)
    {
        _powerSource = source;
        RecalculateOperationalTier();
    }

    public void UpdateAvailableMemory(long freeMemoryMb)
    {
        _availableMemoryMb = freeMemoryMb;
        RecalculateOperationalTier();
    }

    public void RecordUserActivity()
    {
        _lastActivityUtc = DateTime.UtcNow;
    }

    public void SimulateIdleTime(TimeSpan elapsed)
    {
        _lastActivityUtc = DateTime.UtcNow - elapsed;
    }

    public bool IsVramPagingRecommended()
    {
        return (DateTime.UtcNow - _lastActivityUtc) >= _idleTimeout;
    }

    private void RecalculateOperationalTier()
    {
        if (_powerSource == PowerSource.Battery)
        {
            CurrentTier = ModelTier.PowerSaver;
            MaxContextWindow = 2048;
            AreBackgroundAgentsPaused = true;
        }
        else
        {
            CurrentTier = ModelTier.HighPerformance;
            MaxContextWindow = 8192;
            AreBackgroundAgentsPaused = false;
        }

        // Apply OOM safety constraint if system memory is severely constrained
        if (_availableMemoryMb < 4096)
        {
            MaxContextWindow = Math.Min(MaxContextWindow, 1024);
        }
    }
}
