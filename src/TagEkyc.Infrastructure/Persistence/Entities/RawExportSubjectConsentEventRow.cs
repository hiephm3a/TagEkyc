namespace TagEkyc.Infrastructure.Persistence.Entities;

public sealed class RawExportSubjectConsentEventRow
{
    public Guid SubjectConsentRecordId { get; set; }

    public byte[] ConsentScopeHash { get; set; } = [];

    public Guid VerificationSessionId { get; set; }

    public string SubjectRef { get; set; } = string.Empty;

    public Guid PolicyId { get; set; }

    public int PolicyVersion { get; set; }

    public string PurposeCode { get; set; } = string.Empty;

    public Guid RecipientClientApplicationId { get; set; }

    public int Revision { get; set; }

    public string EventType { get; set; } = string.Empty;

    public int? TargetRevision { get; set; }

    public string? ConsentTextVersion { get; set; }

    public string? ConsentTextContentHash { get; set; }

    public string? ExternalConsentArtifactRef { get; set; }

    public string? DecisionRef { get; set; }

    public DateTimeOffset? ValidFromUtc { get; set; }

    public DateTimeOffset? ValidUntilUtc { get; set; }

    public DateTimeOffset? CapturedAtUtc { get; set; }

    public Guid? CapturedByPrincipalId { get; set; }

    public Guid? WithdrawnByPrincipalId { get; set; }

    public DateTimeOffset RecordedAtUtc { get; set; }
}
