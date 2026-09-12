namespace TagEkyc.Infrastructure.CaptureRuntime;

public sealed class CaptureRuntimeDatabaseOptions
{
    public const string SectionName = "TagEkyc:CaptureRuntimeDatabase";

    public string? OnlineConnectionString { get; set; }
    public string? OnlineConnectionStringSecretRef { get; set; }
    public string? OperatorConnectionString { get; set; }
    public string? OperatorConnectionStringSecretRef { get; set; }
}
