namespace TagEkyc.Infrastructure.RawExport;

internal enum SourceCommitDisposition { Committed, ExistingMatch, NotFound, StateConflict, SourceRetentionNotAuthorized }
internal enum SourcePublishDisposition { Available, ExistingMatch, NotFound, StateConflict, SourceRetentionNotAuthorized }
internal enum SourceCleanupReadDisposition { ItemAvailable, NoPendingItem, NotFound, StateConflict }
internal enum SourceCleanupCompleteDisposition { Completed, ExistingMatch, NotFound, ResourceNotTerminal, StateConflict }
internal enum SourceCleanupFinalizeDisposition { Completed, ExistingMatch, CleanupPending, NotFound, StateConflict }

internal sealed record CommitStagedSourceCommand(
    Guid ActorPrincipalId, Guid AttemptId, long ExpectedReservationRevision,
    long ExpectedAttemptRevision, long ExpectedFence, long ExpectedObjectStateRevision);

internal sealed record PublishAvailableSourceCommand(
    Guid ActorPrincipalId, Guid SourcePublicationId,
    long ExpectedReservationRevision, long ExpectedFence);

internal sealed record ReadNextSourceCleanupItemCommand(
    Guid ActorPrincipalId, Guid SourcePublicationId, long ExpectedPublicationRevision);

internal sealed record CompleteSourceCleanupItemCommand(
    Guid ActorPrincipalId, Guid CleanupItemId, long ExpectedCleanupItemRevision);

internal sealed record FinalizeSourceCleanupCommand(
    Guid ActorPrincipalId, Guid SourcePublicationId, long ExpectedPublicationRevision);

internal sealed record SourceCommitResult(
    SourceCommitDisposition Disposition, Guid? SourcePublicationId, Guid? SourceArtifactId,
    Guid? AttemptId, Guid? ObjectCustodyId, byte[]? CommitEvidenceDigest,
    DateTimeOffset? CommittedAtUtc, long? ReservationRevision, long? Fence)
{
    public override string ToString() => $"SourceCommitResult:{Disposition}:<redacted>";
}

internal sealed record SourcePublishResult(
    SourcePublishDisposition Disposition, Guid? SourcePublicationId, Guid? SourceArtifactId,
    Guid? OpaqueCommittedLocatorId, byte[]? AvailableEvidenceDigest,
    long? ReservationRevision, long? Fence, DateTimeOffset? AvailableAtUtc,
    string? CleanupDisposition)
{
    public override string ToString() => $"SourcePublishResult:{Disposition}:<redacted>";
}

internal sealed record SourceCleanupReadResult(
    SourceCleanupReadDisposition Disposition, Guid? SourcePublicationId,
    long? PublicationRevision, Guid? CleanupItemId, string? ResourceKind,
    Guid? ResourceId, Guid? ResourceAttemptId, long? CleanupItemRevision,
    long? PlannedResourceRevision)
{
    public override string ToString() => $"SourceCleanupReadResult:{Disposition}:Kind={ResourceKind ?? "none"}:<redacted>";
}

internal sealed record SourceCleanupCompleteResult(
    SourceCleanupCompleteDisposition Disposition, Guid? SourcePublicationId,
    long? PublicationRevision, Guid? CleanupItemId, string? ResourceKind,
    Guid? ResourceId, string? CompletionDisposition, byte[]? CleanupEvidenceDigest,
    DateTimeOffset? CompletedAtUtc, long? CleanupItemRevision)
{
    public override string ToString() => $"SourceCleanupCompleteResult:{Disposition}:Kind={ResourceKind ?? "none"}:<redacted>";
}

internal sealed record SourceCleanupFinalizeResult(
    SourceCleanupFinalizeDisposition Disposition, Guid? SourcePublicationId,
    long? PublicationRevision, string? CleanupDisposition,
    byte[]? CleanupEvidenceDigest, DateTimeOffset? FinalizedAtUtc)
{
    public override string ToString() => $"SourceCleanupFinalizeResult:{Disposition}:<redacted>";
}

internal sealed record SourceCommitEvidenceInput(
    Guid SourceArtifactId, Guid AttemptId, Guid ObjectCustodyId, Guid AttemptKeyReservationId,
    int StagedCiphertextFingerprintSchemaVersion, byte[] StagedCiphertextFingerprint,
    int AuthoritySnapshotSchemaVersion, Guid AuthoritySnapshotId, long AuthorityRevision,
    Guid ConsentPolicyId, int ConsentPolicyVersion, DateTimeOffset CommittedAtUtc);

internal sealed record SourceAvailableEvidenceInput(
    Guid SourcePublicationId, Guid SourceArtifactId, Guid AttemptId, Guid ObjectCustodyId,
    Guid AttemptKeyReservationId, Guid OpaqueCommittedLocatorId, byte[] CommitEvidenceDigest,
    int AuthoritySnapshotSchemaVersion, Guid AuthoritySnapshotId, long AuthorityRevision,
    Guid ConsentPolicyId, int ConsentPolicyVersion, DateTimeOffset AvailableAtUtc);

internal sealed record SourceCleanupEvidenceInput(
    Guid SourcePublicationId, Guid SourceArtifactId, string CleanupDisposition,
    long CompletedCleanupItemCount, DateTimeOffset FinalizedAtUtc);

internal sealed record SourceCleanupItemEvidenceInput(
    Guid SourcePublicationId, Guid ResourceAttemptId, Guid ResourceId,
    string CompletionDisposition, byte[] UnderlyingTerminalEvidenceDigest,
    DateTimeOffset CompletedAtUtc);

internal sealed record SourceObjectLifecycleContext(
    Guid ObjectCustodyId, Guid AttemptId, Guid ProvisionalObjectIdentity,
    string ObjectKey, byte[] ObjectBindingDigest, string State, long StateRevision,
    Guid? PutOperationId, string? CleanupReasonCode, byte[]? CleanupEvidenceDigest,
    DateTimeOffset? CleanupRequestedAtUtc, byte[]? ProviderReceiptDigest);

internal sealed record SourceObjectReconcileContext(
    Guid ObjectCustodyId, Guid AttemptId, Guid AttemptKeyReservationId,
    Guid SourceArtifactId, Guid ProvisionalObjectIdentity, string ObjectKey,
    byte[] ObjectBindingDigest, string State, long StateRevision);

internal sealed record SourceKeyRecoveryContext(
    string PreparationDisposition, Guid AttemptId, byte[] AttemptKeyContextFingerprint,
    Guid? PreparationId, long PreparationFence, ProviderOperationToken? ProviderOperationToken,
    Guid? ProviderOperationId, string? ProviderOperationState,
    string? ProviderCleanupReference);
