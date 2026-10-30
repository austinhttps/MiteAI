namespace MiteAI.Core.Hardware;

public enum ModelTier
{
    HighPerformance, // e.g. 3B parameter 8-bit model on dGPU/DirectML
    Balanced,        // e.g. 2B model
    PowerSaver       // e.g. 1.5B 4-bit quantized model for battery operation
}

public enum PowerSource
{
    Ac,
    Battery
}
