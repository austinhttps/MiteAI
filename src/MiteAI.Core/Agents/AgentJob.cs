using System;

namespace MiteAI.Core.Agents;

public enum AgentJobStatus
{
    Queued,
    Running,
    PausedForPermission,
    Completed,
    Failed
}

public class AgentJob
{
    public string Id { get; } = Guid.NewGuid().ToString("N");
    public string Title { get; }
    public int TotalItems { get; }
    public int ProcessedItems { get; set; } = 0;
    public AgentJobStatus Status { get; set; } = AgentJobStatus.Queued;
    public DateTime CreatedAtUtc { get; } = DateTime.UtcNow;

    public int ProgressPercentage => TotalItems > 0 ? (ProcessedItems * 100) / TotalItems : 0;

    public AgentJob(string title, int totalItems)
    {
        Title = title;
        TotalItems = totalItems;
    }
}
