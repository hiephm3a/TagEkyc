namespace TagEkyc.Infrastructure.CaptureRuntime;

public sealed class CaptureRuntimeVerifierPepperOptions
{
    public const string SectionName = "TagEkyc:CaptureRuntimeVerifierPeppers";

    public int CurrentVersion { get; set; }

    public List<CaptureRuntimeVerifierPepperVersionOptions> Versions { get; set; } = [];
}

public sealed class CaptureRuntimeVerifierPepperVersionOptions
{
    public int Version { get; set; }

    public string? SecretRef { get; set; }
}
