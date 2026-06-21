using System.Reflection;
using TagEkyc.Contracts.Common;
using TagEkyc.Contracts.InternalAudit.Manifest;
using TagEkyc.Domain;
using TagEkyc.Infrastructure.Persistence.Entities;

namespace TagEkyc.Infrastructure.Persistence;

internal static class DomainRowMapper
{
    private static readonly ConstructorInfo SessionCtor =
        typeof(VerificationSession).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(ctor => ctor.GetParameters().Length == 22);

    public static VerificationSessionRow ToRow(VerificationSession session) =>
        new()
        {
            Id = session.Id,
            ClientApplicationId = session.ClientApplicationId,
            SubjectRef = session.SubjectRef,
            Profile = session.Profile.ToString(),
            Purpose = session.Purpose,
            RequiredChecksJson = PersistenceJson.Serialize(session.RequiredChecks.Select(check => check.ToString()).ToArray()),
            ExternalSessionId = session.ExternalSessionId,
            ExternalTransactionId = session.ExternalTransactionId,
            BindingNonceHash = session.BindingNonceHash?.ToString(),
            RequestId = session.RequestId,
            CorrelationId = session.CorrelationId,
            State = session.State.ToString(),
            Result = session.Result.ToString(),
            AssuranceLevel = session.AssuranceLevel.ToString(),
            FinalDecisionId = session.FinalDecisionId,
            EvidencePackageId = session.EvidencePackageId,
            EvidencePackageHash = session.EvidencePackageHash?.ToString(),
            ManifestHash = session.ManifestHash?.ToString(),
            PolicySnapshotId = session.Metadata.PolicySnapshotId.Value,
            RetentionClass = session.Metadata.RetentionClass.ToString(),
            DeletionEligibility = session.Metadata.DeletionEligibility.ToString(),
            LegalHoldStatus = session.Metadata.LegalHoldStatus.ToString(),
            PurgeBlockReason = session.Metadata.PurgeBlockReason.ToString(),
            AccessAuditRequired = session.Metadata.AccessAuditRequired,
            ActorId = session.Metadata.Actor?.ActorId,
            ActorCategory = session.Metadata.Actor?.ActorCategory,
            ExpiresAt = session.ExpiresAt,
            CreatedAt = session.CreatedAt,
            CompletedAt = session.CompletedAt,
        };

    public static VerificationSession ToDomain(VerificationSessionRow row)
    {
        var requiredChecks = PersistenceJson.Deserialize<string[]>(row.RequiredChecksJson)
            .Select(Parse<RequiredCheckType>)
            .ToHashSet();
        var policySnapshotId = new PolicySnapshotId(row.PolicySnapshotId);
        var actor = row.ActorId is not null && row.ActorCategory is not null
            ? new ActorReference(row.ActorId, row.ActorCategory)
            : null;
        var metadata = new DataBoundaryMetadata(
            policySnapshotId,
            new DecisionReproducibilityBoundary(policySnapshotId, new RequiredCheckSetPolicyIdentity(policySnapshotId, requiredChecks)),
            Parse<RetentionClass>(row.RetentionClass),
            Parse<DeletionEligibility>(row.DeletionEligibility),
            Parse<LegalHoldStatus>(row.LegalHoldStatus),
            Parse<PurgeBlockReason>(row.PurgeBlockReason),
            row.AccessAuditRequired,
            actor);

        return (VerificationSession)SessionCtor.Invoke([
            row.Id,
            row.ClientApplicationId,
            row.SubjectRef,
            Parse<VerificationProfile>(row.Profile),
            row.Purpose,
            requiredChecks,
            row.ExternalSessionId,
            row.ExternalTransactionId,
            ToHash(row.BindingNonceHash),
            row.RequestId,
            row.CorrelationId,
            Parse<VerificationSessionState>(row.State),
            Parse<VerificationResult>(row.Result),
            Parse<AssuranceLevel>(row.AssuranceLevel),
            row.FinalDecisionId,
            row.EvidencePackageId,
            ToHash(row.EvidencePackageHash),
            ToHash(row.ManifestHash),
            metadata,
            row.ExpiresAt,
            row.CreatedAt,
            row.CompletedAt,
        ]);
    }

    public static CaptureArtifactRow ToRow(CaptureArtifact artifact) =>
        new()
        {
            Id = artifact.Id,
            VerificationSessionId = artifact.VerificationSessionId,
            ArtifactType = artifact.ArtifactType.ToString(),
            CaptureSource = artifact.CaptureSource.ToString(),
            CaptureAgentId = artifact.CaptureAgentId,
            DeviceId = artifact.DeviceId,
            VaultRef = artifact.VaultRef?.ToString(),
            ArtifactHash = artifact.ArtifactHash?.ToString(),
            MetadataHash = artifact.MetadataHash?.ToString(),
            QualityState = artifact.QualityState.ToString(),
            RetryReasonCode = artifact.RetryReasonCode,
            RequestId = artifact.RequestId,
            CorrelationId = artifact.CorrelationId,
            CreatedAt = artifact.CreatedAt,
            ExpiresAt = artifact.ExpiresAt,
        };

    public static CaptureArtifact ToDomain(CaptureArtifactRow row) =>
        new(
            row.Id,
            row.VerificationSessionId,
            Parse<CaptureArtifactType>(row.ArtifactType),
            Parse<CaptureSource>(row.CaptureSource),
            row.CaptureAgentId,
            row.DeviceId,
            row.VaultRef is null ? null : new VaultRef(row.VaultRef),
            ToHash(row.ArtifactHash),
            ToHash(row.MetadataHash),
            Parse<CaptureArtifactQualityState>(row.QualityState),
            row.RetryReasonCode,
            row.RequestId,
            row.CorrelationId,
            row.CreatedAt,
            row.ExpiresAt);

    public static EvidenceResultRow ToRow(EvidenceResult evidence) =>
        new()
        {
            Id = evidence.Id,
            VerificationSessionId = evidence.VerificationSessionId,
            VerificationCheckId = evidence.VerificationCheckId,
            ResultType = evidence.ResultType.ToString(),
            InputCaptureArtifactIdsJson = PersistenceJson.Serialize(evidence.InputCaptureArtifactIds.Select(id => id.ToString("N")).ToArray()),
            Result = evidence.Result.ToString(),
            Confidence = evidence.Confidence,
            ReasonCodesJson = PersistenceJson.Serialize(evidence.ReasonCodes),
            RetryReasonCode = evidence.RetryReasonCode,
            SanitizedSummaryRef = evidence.SanitizedSummaryRef,
            PayloadHash = evidence.PayloadHash?.ToString(),
            PayloadSignatureStatus = evidence.PayloadSignatureStatus.ToString(),
            EngineName = evidence.EngineName,
            EngineVersion = evidence.EngineVersion,
            RequestId = evidence.RequestId,
            CorrelationId = evidence.CorrelationId,
            CreatedAt = evidence.CreatedAt,
        };

    public static EvidenceResult ToDomain(EvidenceResultRow row) =>
        new(
            row.Id,
            row.VerificationSessionId,
            row.VerificationCheckId,
            Parse<EvidenceResultType>(row.ResultType),
            PersistenceJson.Deserialize<string[]>(row.InputCaptureArtifactIdsJson).Select(Guid.Parse).ToArray(),
            Parse<VerificationResult>(row.Result),
            row.Confidence,
            PersistenceJson.Deserialize<string[]>(row.ReasonCodesJson),
            row.RetryReasonCode,
            row.SanitizedSummaryRef,
            ToHash(row.PayloadHash),
            Parse<SignaturePlaceholderStatus>(row.PayloadSignatureStatus),
            row.EngineName,
            row.EngineVersion,
            row.RequestId,
            row.CorrelationId,
            row.CreatedAt);

    public static VerificationDecisionRow ToRow(VerificationDecision decision) =>
        new()
        {
            Id = decision.Id,
            VerificationSessionId = decision.VerificationSessionId,
            Result = decision.Result.ToString(),
            AssuranceLevel = decision.AssuranceLevel.ToString(),
            RiskScore = decision.RiskScore,
            FailedChecksJson = PersistenceJson.Serialize(decision.FailedChecks.Select(check => check.ToString()).ToArray()),
            CompletedChecksJson = PersistenceJson.Serialize(decision.CompletedChecks.Select(check => check.ToString()).ToArray()),
            DecisionReasonCodesJson = PersistenceJson.Serialize(decision.DecisionReasonCodes),
            RetryReasonCodesJson = PersistenceJson.Serialize(decision.RetryReasonCodes),
            CreatedAt = decision.CreatedAt,
        };

    public static VerificationDecision ToDomain(VerificationDecisionRow row) =>
        new(
            row.Id,
            row.VerificationSessionId,
            Parse<VerificationResult>(row.Result),
            Parse<AssuranceLevel>(row.AssuranceLevel),
            row.RiskScore,
            PersistenceJson.Deserialize<string[]>(row.FailedChecksJson).Select(Parse<RequiredCheckType>).ToArray(),
            PersistenceJson.Deserialize<string[]>(row.CompletedChecksJson).Select(Parse<RequiredCheckType>).ToArray(),
            PersistenceJson.Deserialize<string[]>(row.DecisionReasonCodesJson),
            PersistenceJson.Deserialize<string[]>(row.RetryReasonCodesJson),
            row.CreatedAt);

    public static EvidencePackageRow ToRow(EvidencePackage package) =>
        new()
        {
            Id = package.Id,
            VerificationSessionId = package.VerificationSessionId,
            PackageVersion = package.PackageVersion,
            ManifestHash = package.ManifestHash.ToString(),
            EvidenceRefsJson = PersistenceJson.Serialize(package.EvidenceRefs),
            AuditEventRefsJson = PersistenceJson.Serialize(package.AuditEventRefs),
            ResultRef = package.ResultRef,
            PackageHash = package.PackageHash.ToString(),
            EvidencePackageSignatureStatus = package.EvidencePackageSignatureStatus.ToString(),
            CreatedAt = package.CreatedAt,
        };

    public static EvidencePackage ToDomain(EvidencePackageRow row) =>
        new(
            row.Id,
            row.VerificationSessionId,
            row.PackageVersion,
            new HashRef(row.ManifestHash),
            PersistenceJson.Deserialize<string[]>(row.EvidenceRefsJson),
            PersistenceJson.Deserialize<string[]>(row.AuditEventRefsJson),
            row.ResultRef,
            new HashRef(row.PackageHash),
            Parse<SignaturePlaceholderStatus>(row.EvidencePackageSignatureStatus),
            row.CreatedAt);

    public static EvidenceManifestRow ToRow(EvidenceManifestDto manifest) =>
        new()
        {
            EvidencePackageId = Guid.Parse(manifest.EvidencePackageId),
            SessionGuid = Guid.Parse(manifest.VerificationSessionId),
            VerificationSessionId = manifest.VerificationSessionId,
            PackageVersion = manifest.PackageVersion,
            ManifestHash = manifest.ManifestHash,
            PackageHash = manifest.PackageHash,
            EvidenceRefsJson = PersistenceJson.Serialize(manifest.EvidenceRefs),
            AuditEventRefsJson = PersistenceJson.Serialize(manifest.AuditEventRefs),
            ResultRef = Guid.Parse(manifest.ResultRef),
            EvidencePackageSignatureStatus = manifest.EvidencePackageSignatureStatus.ToString(),
            CreatedAt = manifest.CreatedAt,
        };

    public static EvidenceManifestDto ToDomain(EvidenceManifestRow row) =>
        new(
            row.EvidencePackageId.ToString("N"),
            row.VerificationSessionId,
            row.PackageVersion,
            row.ManifestHash,
            row.PackageHash,
            PersistenceJson.Deserialize<ManifestEvidenceRefDto[]>(row.EvidenceRefsJson),
            PersistenceJson.Deserialize<ManifestAuditRefDto[]>(row.AuditEventRefsJson),
            row.ResultRef.ToString("N"),
            Parse<SignaturePlaceholderStatusDto>(row.EvidencePackageSignatureStatus),
            row.CreatedAt);

    public static AuditEventRow ToRow(AuditEvent auditEvent) =>
        new()
        {
            Id = auditEvent.Id,
            ClientApplicationId = auditEvent.ClientApplicationId,
            VerificationSessionId = auditEvent.VerificationSessionId,
            ActorType = auditEvent.ActorType,
            ActorId = auditEvent.ActorId,
            EventType = auditEvent.EventType,
            EventPayloadHash = auditEvent.EventPayloadHash.ToString(),
            EventPayloadRef = auditEvent.EventPayloadRef,
            RequestId = auditEvent.RequestId,
            CorrelationId = auditEvent.CorrelationId,
            OccurredAt = auditEvent.OccurredAt,
        };

    public static AuditEvent ToDomain(AuditEventRow row) =>
        new(
            row.Id,
            row.ClientApplicationId,
            row.VerificationSessionId,
            row.ActorType,
            row.ActorId,
            row.EventType,
            new HashRef(row.EventPayloadHash),
            row.EventPayloadRef,
            row.RequestId,
            row.CorrelationId,
            row.OccurredAt);

    private static HashRef? ToHash(string? value) => value is null ? null : new HashRef(value);

    private static TEnum Parse<TEnum>(string value)
        where TEnum : struct, Enum =>
        Enum.Parse<TEnum>(value, ignoreCase: false);
}
