namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportAssemblyIdentityRow
{
    public Guid AssemblyId { get; set; }
    public Guid JobId { get; set; }
    public Guid AttemptId { get; set; }
    public long FencingToken { get; set; }
    public Guid PermitId { get; set; }
    public Guid VerificationSessionId { get; set; }
    public Guid ClientApplicationId { get; set; }
    public Guid RecipientClientApplicationId { get; set; }
    public Guid PolicyId { get; set; }
    public int PolicyVersion { get; set; }
    public string PurposeCode { get; set; } = string.Empty;
    public string ExportMode { get; set; } = string.Empty;
    public int ManifestVersion { get; set; }
    public int SubjectRefTokenSchemaVersion { get; set; }
    public string SubjectRefTokenKeyId { get; set; } = string.Empty;
    public int SubjectRefTokenKeyVersion { get; set; }
    public byte[] SubjectRefToken { get; set; } = [];
    public string AssemblyAuthenticationKeyId { get; set; } = string.Empty;
    public int AssemblyAuthenticationKeyVersion { get; set; }
    public byte[] AssemblyDigest { get; set; } = [];
    public byte[] ManifestDigest { get; set; } = [];
    public byte[] AssemblyAuthenticationValue { get; set; } = [];
    public byte[] AssemblyFingerprint { get; set; } = [];
    public long CompleteAssemblyLength { get; set; }
    public int ItemCount { get; set; }
    public Guid C2PreparationId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset JobExpiresAtUtc { get; set; }
    public DateTimeOffset SealedAtUtc { get; set; }
    public int SchemaVersion { get; set; }
}
