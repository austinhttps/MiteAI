using System.Threading;
using System.Threading.Tasks;

namespace MiteAI.Core.Inference;

public interface IModelInference
{
    bool IsModelLoaded { get; }
    Task<InferenceResult> GenerateAsync(InferenceRequest request, CancellationToken cancellationToken = default);
    Task<float[]> GetEmbeddingsAsync(string text, CancellationToken cancellationToken = default);
    InferenceMetrics GetMetrics();
    Task UnloadModelAsync();
}
