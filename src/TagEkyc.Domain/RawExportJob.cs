namespace TagEkyc.Domain;

public enum RawExportJobState
{
    Claimed = 0,
    Assembling = 1,
    AssemblySealed = 2,
    Protecting = 3,
    PackageSealed = 4,
    ReadyForDelivery = 5,
    DeliveryInProgress = 6,
    DeliveryOutcomeUnknown = 7,
    Delivered = 8,
    ReconciliationExpired = 9,
    TerminalFailed = 10,
    Cancelled = 11,
    Expired = 12,
}

public enum RawExportJobAttemptPhase { Assembling = 0 }

public enum RawExportJobEventType
{
    JobBound = 0,
    LeaseAcquired = 1,
    LeaseRenewed = 2,
    AttemptFailedRetryable = 3,
    LeaseAcquiredAfterRetryableFailure = 4,
    LeaseReclaimed = 5,
    JobTerminalFailed = 6,
    JobCancelled = 7,
    JobExpired = 8,
}

public enum RawExportJobAttemptFailureCode { ATTEMPT_EXECUTION_FAILED_RETRYABLE = 0 }

public enum RawExportJobTerminalReasonCode
{
    AUTHORITY_REVALIDATION_FAILED = 0,
    ATTEMPT_EXECUTION_FAILED_NON_RETRYABLE = 1,
    JOB_GRAPH_INVARIANT_FAILURE = 2,
    MODE_RETRY_NOT_AUTHORIZED = 3,
    PERMIT_OR_JOB_EXPIRED = 4,
    REQUEST_CANCELLED = 5,
}

public enum RawExportJobBindStatus { NewJob = 0, ExistingMatch = 1, Terminal = 2 }
public enum RawExportJobReadStatus { Found = 0, NotFound = 1 }
public enum RawExportJobLeaseStatus
{
    Acquired = 0,
    AcquiredAfterRetryableFailure = 1,
    Reclaimed = 2,
    NotFound = 3,
    AlreadyTerminal = 4,
    TerminalFailed = 5,
    Expired = 6,
}
public enum RawExportJobRenewStatus { Renewed = 0 }
public enum RawExportJobAttemptFailureStatus { Recorded = 0, TerminalFailed = 1 }
public enum RawExportJobTerminalizeStatus { Terminalized = 0, AlreadyTerminal = 1, Expired = 2 }

public sealed record BindRawExportJobCommand(
    AuthenticatedRawExportActor Actor,
    Guid PermitId,
    string IdempotencyKey);

public sealed record ReadRawExportJobCommand(AuthenticatedRawExportActor Actor, Guid JobId);

public sealed record AcquireOrReclaimRawExportJobLeaseCommand(
    AuthenticatedRawExportActor Actor,
    Guid JobId,
    long ExpectedRevision,
    long ExpectedFencingToken,
    Guid LeaseOwnerId);

public sealed record RenewRawExportJobLeaseCommand(
    AuthenticatedRawExportActor Actor,
    Guid JobId,
    long ExpectedRevision,
    long ExpectedFencingToken,
    Guid AttemptId,
    Guid LeaseOwnerId);

public sealed record RecordRawExportJobAttemptFailureCommand(
    AuthenticatedRawExportActor Actor,
    Guid JobId,
    long ExpectedRevision,
    long ExpectedFencingToken,
    Guid AttemptId,
    Guid LeaseOwnerId,
    RawExportJobAttemptFailureCode FailureCode);

public sealed record TerminalizeRawExportJobCommand(
    AuthenticatedRawExportActor Actor,
    Guid JobId,
    long ExpectedRevision,
    long ExpectedFencingToken,
    Guid? AttemptId,
    Guid? LeaseOwnerId,
    RawExportJobState TerminalState,
    RawExportJobTerminalReasonCode ReasonCode);

public sealed record RawExportJobIdentityView(
    Guid JobId,
    Guid PermitId,
    Guid AuthorizationDecisionId,
    Guid PrincipalId,
    Guid ClientApplicationId,
    Guid CreatedByApiKeyId,
    Guid VerificationSessionId,
    string SubjectRef,
    Guid PolicyId,
    int PolicyVersion,
    string PurposeCode,
    Guid RecipientClientApplicationId,
    RawExportMode ExportMode,
    DateTimeOffset PermitExpiresAt,
    DateTimeOffset JobExpiresAt,
    int SchemaVersion,
    DateTimeOffset CreatedAt);

public sealed record RawExportJobClassView(int Ordinal, RawExportRawClass RawClass);

public sealed record RawExportJobOperationalHeadView(
    RawExportJobState CurrentState,
    long Revision,
    Guid? CurrentAttemptId,
    Guid? LeaseOwnerId,
    DateTimeOffset? LeaseExpiresAt,
    long FencingToken);

public sealed record RawExportJobTransitionSummary(
    RawExportJobEventType LatestEventType,
    string? LatestFailureCode,
    DateTimeOffset LatestOccurredAt);

public sealed record RawExportJobView(
    RawExportJobIdentityView Identity,
    IReadOnlyList<RawExportJobClassView> Classes,
    RawExportJobOperationalHeadView Head,
    RawExportJobTransitionSummary LatestTransition);

public sealed record RawExportJobBindResult(
    RawExportJobBindStatus Status,
    Guid JobId,
    RawExportJobTerminalizeResult? TerminalResult);

public sealed record RawExportJobReadResult(RawExportJobReadStatus Status, RawExportJobView? Job);

public sealed record RawExportJobLeaseResult(
    RawExportJobLeaseStatus Status,
    Guid? AttemptId,
    RawExportJobState? State,
    long? Revision,
    long? FencingToken,
    DateTimeOffset? LeaseExpiresAt,
    RawExportJobTerminalReasonCode? TerminalReason,
    string? StableCode);

public sealed record RawExportJobRenewResult(
    RawExportJobRenewStatus Status,
    long Revision,
    long FencingToken,
    DateTimeOffset LeaseExpiresAt);

public sealed record RawExportJobAttemptFailureResult(
    RawExportJobAttemptFailureStatus Status,
    RawExportJobState State,
    long Revision,
    long FencingToken,
    RawExportJobTerminalReasonCode? TerminalReason,
    string? StableCode);

public sealed record RawExportJobTerminalizeResult(
    RawExportJobTerminalizeStatus Status,
    RawExportJobState State,
    long Revision,
    long FencingToken,
    RawExportJobTerminalReasonCode TerminalReason,
    string? StableCode);

public sealed class RawExportJobException(string code) : InvalidOperationException(code)
{
    public string Code { get; } = code;
}
