using TagEkyc.Application.LocalDev;
using TagEkyc.Domain;

namespace TagEkyc.UnitTests;

public sealed class Tip11DataBoundaryMetadataTests
{
    [Fact]
    public void Localdev_metadata_defaults_are_safe_and_deterministic()
    {
        var metadata = DataBoundaryMetadata.LocalDevDefault(RequiredCheckPolicy.SignFlowS1RequiredChecks);

        Assert.Equal("LOCALDEV-S1-POLICY-V1", PolicySnapshotId.LocalDevS1Value);
        Assert.Equal(PolicySnapshotId.LocalDevS1Value, metadata.PolicySnapshotId.Value);
        Assert.Equal(PolicySnapshotId.LocalDevS1, metadata.DecisionBoundary.PolicySnapshotId);
        Assert.Equal(RequiredCheckPolicy.SignFlowS1RequiredChecks, metadata.DecisionBoundary.RequiredCheckSetPolicy.RequiredChecks);
        Assert.Equal(RetentionClass.LocalDevEphemeral, metadata.RetentionClass);
        Assert.Equal(DeletionEligibility.NotEvaluated, metadata.DeletionEligibility);
        Assert.Equal(LegalHoldStatus.None, metadata.LegalHoldStatus);
        Assert.Equal(PurgeBlockReason.None, metadata.PurgeBlockReason);
        Assert.False(metadata.AccessAuditRequired);
        Assert.Null(metadata.Actor);
    }

    [Fact]
    public void Purge_block_reason_is_enum_code_only()
    {
        Assert.True(typeof(PurgeBlockReason).IsEnum);
        Assert.DoesNotContain(
            typeof(PurgeBlockReason).GetFields().Where(field => field.IsStatic),
            field => field.FieldType == typeof(string));
    }

    [Fact]
    public void Localdev_policy_source_exposes_internal_snapshot_identity_only()
    {
        var policies = new LocalDevRuntimePolicySource().Policies;

        Assert.NotEmpty(policies);
        Assert.All(policies, policy => Assert.Equal(PolicySnapshotId.LocalDevS1, policy.PolicySnapshotId));
    }

    [Fact]
    public void Verification_session_attaches_and_preserves_internal_metadata()
    {
        var session = VerificationSession.Create(
            LocalDevRuntimePolicySource.BusinessClientId,
            "subject-ref",
            VerificationProfile.StandardEkycProfile,
            "PATIENT_REGISTRATION",
            [RequiredCheckType.CaptureQuality],
            DateTimeOffset.UtcNow.AddMinutes(30),
            DateTimeOffset.UtcNow);

        Assert.Equal(PolicySnapshotId.LocalDevS1, session.Metadata.PolicySnapshotId);

        var inProgress = session.WithState(VerificationSessionState.InProgress);
        var completed = inProgress.WithCompletion(
            VerificationResult.Passed,
            AssuranceLevel.Medium,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new HashRef("sha256:package"),
            new HashRef("sha256:manifest"),
            "req-tip11",
            "corr-tip11",
            DateTimeOffset.UtcNow);

        Assert.Equal(session.Metadata, inProgress.Metadata);
        Assert.Equal(session.Metadata, completed.Metadata);
        Assert.Equal(VerificationSessionState.Completed, completed.State);
    }

    [Fact]
    public void Retention_and_legal_hold_metadata_do_not_enforce_completion_behavior()
    {
        var requiredChecks = new HashSet<RequiredCheckType> { RequiredCheckType.CaptureQuality };
        var policySnapshotId = new PolicySnapshotId("TEST-POLICY");
        var metadata = new DataBoundaryMetadata(
            policySnapshotId,
            new DecisionReproducibilityBoundary(
                policySnapshotId,
                new RequiredCheckSetPolicyIdentity(policySnapshotId, requiredChecks)),
            RetentionClass.RegulatedEvidence,
            DeletionEligibility.Blocked,
            LegalHoldStatus.Active,
            PurgeBlockReason.LegalHold,
            accessAuditRequired: true,
            new ActorReference("actor-1", "BusinessConsumer"));
        var session = VerificationSession.Create(
            LocalDevRuntimePolicySource.BusinessClientId,
            "subject-ref",
            VerificationProfile.StandardEkycProfile,
            "PATIENT_REGISTRATION",
            requiredChecks,
            DateTimeOffset.UtcNow.AddMinutes(30),
            DateTimeOffset.UtcNow,
            metadata: metadata);

        var completed = session.WithCompletion(
            VerificationResult.Passed,
            AssuranceLevel.Medium,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new HashRef("sha256:package"),
            new HashRef("sha256:manifest"),
            "req-tip11",
            "corr-tip11",
            DateTimeOffset.UtcNow);

        Assert.Equal(VerificationSessionState.Completed, completed.State);
        Assert.Equal(DeletionEligibility.Blocked, completed.Metadata.DeletionEligibility);
        Assert.Equal(LegalHoldStatus.Active, completed.Metadata.LegalHoldStatus);
        Assert.True(completed.Metadata.AccessAuditRequired);
    }
}
