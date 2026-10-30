using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace MiteAI.Core.Agents;

public class BackgroundAgentCoordinator
{
    private readonly int _batchModificationLimit;
    private readonly ConcurrentDictionary<string, AgentJob> _jobs = new();

    public BackgroundAgentCoordinator(int batchModificationLimit = 50)
    {
        _batchModificationLimit = batchModificationLimit;
    }

    public AgentJob CreateJob(string title, int totalItems)
    {
        var job = new AgentJob(title, totalItems);
        _jobs[job.Id] = job;
        return job;
    }

    public Task RunJobAsync(string jobId, CancellationToken cancellationToken = default)
    {
        if (!_jobs.TryGetValue(jobId, out var job))
        {
            throw new ArgumentException($"Job not found: {jobId}");
        }

        job.Status = AgentJobStatus.Running;
        int batchProcessed = 0;

        while (job.ProcessedItems < job.TotalItems)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (batchProcessed >= _batchModificationLimit)
            {
                job.Status = AgentJobStatus.PausedForPermission;
                return Task.CompletedTask;
            }

            job.ProcessedItems++;
            batchProcessed++;
        }

        job.Status = AgentJobStatus.Completed;
        return Task.CompletedTask;
    }

    public void AuthorizeBatchContinuation(string jobId)
    {
        if (_jobs.TryGetValue(jobId, out var job) && job.Status == AgentJobStatus.PausedForPermission)
        {
            job.Status = AgentJobStatus.Queued;
        }
    }
}
