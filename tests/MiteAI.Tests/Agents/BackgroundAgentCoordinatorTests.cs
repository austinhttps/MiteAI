using System.Threading.Tasks;
using MiteAI.Core.Agents;
using Xunit;

namespace MiteAI.Tests.Agents;

public class BackgroundAgentCoordinatorTests
{
    [Fact]
    public async Task ExecuteAgentTask_CompletesStandardJob()
    {
        var coordinator = new BackgroundAgentCoordinator(batchModificationLimit: 10);
        var job = coordinator.CreateJob("Compile Research Notes", totalItems: 5);

        Assert.Equal(AgentJobStatus.Queued, job.Status);

        await coordinator.RunJobAsync(job.Id);

        Assert.Equal(AgentJobStatus.Completed, job.Status);
        Assert.Equal(5, job.ProcessedItems);
        Assert.Equal(100, job.ProgressPercentage);
    }

    [Fact]
    public async Task ExecuteAgentTask_PausesAtRateLimitThreshold()
    {
        var coordinator = new BackgroundAgentCoordinator(batchModificationLimit: 5);
        var job = coordinator.CreateJob("Sort Photo Library", totalItems: 20);

        await coordinator.RunJobAsync(job.Id);

        Assert.Equal(AgentJobStatus.PausedForPermission, job.Status);
        Assert.Equal(5, job.ProcessedItems);

        // Operator grants permission to continue next batch
        coordinator.AuthorizeBatchContinuation(job.Id);
        Assert.Equal(AgentJobStatus.Queued, job.Status);

        await coordinator.RunJobAsync(job.Id);
        Assert.Equal(10, job.ProcessedItems);
    }
}
