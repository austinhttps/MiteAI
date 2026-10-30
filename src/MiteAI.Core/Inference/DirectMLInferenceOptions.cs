namespace MiteAI.Core.Inference;

public class DirectMLInferenceOptions
{
    public int DeviceId { get; set; } = 0;
    public bool FallbackToCpu { get; set; } = true;
    public string? ModelPath { get; set; }
    public int MaxContextTokens { get; set; } = 4096;
    public bool EnableMemoryMapping { get; set; } = true;
}
