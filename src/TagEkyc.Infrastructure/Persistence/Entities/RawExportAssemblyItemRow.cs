namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportAssemblyItemRow
{
    public Guid AssemblyId { get; set; }
    public int Ordinal { get; set; }
    public string RawClass { get; set; } = string.Empty;
    public Guid JobSourceBindingId { get; set; }
    public Guid SourceArtifactId { get; set; }
    public Guid CaptureArtifactId { get; set; }
    public int CaptureRevision { get; set; }
    public string MediaType { get; set; } = string.Empty;
    public long PlaintextLength { get; set; }
    public int ContentCommitmentSchemaVersion { get; set; }
    public string ContentCommitmentKeyId { get; set; } = string.Empty;
    public int ContentCommitmentKeyVersion { get; set; }
    public byte[] ContentCommitment { get; set; } = [];
}
