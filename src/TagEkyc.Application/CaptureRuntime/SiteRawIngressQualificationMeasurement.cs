using TagEkyc.Application.Ports;

namespace TagEkyc.Application.CaptureRuntime;

public enum SiteRawIngressQualificationRunMode
{
    FullBodyHeldCommit = 1,
    LostFinalNoRetry = 2
}

public sealed record SiteRawIngressQualificationRunBinding(
    Guid CredentialId,
    long CredentialGeneration,
    Guid IngressIdempotencyKey,
    string IngressMetadataSha256,
    string MediaType,
    long ContentLength,
    string PlaintextSha256);

public sealed record SiteRawIngressQualificationRunAccess(
    Guid ApiKeyId,
    string SiteId,
    string EndpointOrigin,
    string DeploymentRevision);

public sealed record SiteRawIngressQualificationSyntheticCredentialEnrollment(
    string SiteId,
    string EndpointOrigin,
    string DeploymentRevision,
    Guid CredentialId,
    long CredentialGeneration,
    DateTimeOffset ExpiresAtUtc,
    Guid EnrolledByApiKeyId);

public sealed record SiteRawIngressQualificationRunRegistration(
    Guid QualificationSuiteId,
    string SiteId,
    string EndpointOrigin,
    string DeploymentRevision,
    SiteRawIngressQualificationRunBinding Binding,
    SiteRawIngressQualificationRunMode Mode,
    DateTimeOffset ExpiresAtUtc,
    Guid RegisteredByApiKeyId);

public sealed record SiteRawIngressQualificationRunHandle(
    Guid QualificationRunId,
    Guid QualificationSuiteId,
    string SiteId,
    string EndpointOrigin,
    string DeploymentRevision,
    DateTimeOffset ExpiresAtUtc);

public sealed record SiteRawIngressQualificationRawPost(
    Guid QualificationRunId,
    SiteRawIngressQualificationRunMode Mode,
    bool IsActive);

public sealed record SiteRawIngressQualificationAgentObservation(
    int TransportEntryCount,
    long ContentBytesCopied,
    long BodyBytesSentWhileBrokerHeld,
    bool ObservedContinue,
    bool ContinueObservedBeforeBrokerCommit,
    bool ApplicationPrebufferObserved,
    bool FinalResponseObserved);

public sealed record SiteRawIngressQualificationRunReport(
    Guid QualificationRunId,
    Guid QualificationSuiteId,
    string SiteId,
    string EndpointOrigin,
    string DeploymentRevision,
    string State,
    SiteRawIngressQualificationRunMode Mode,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? ConsumedAtUtc,
    DateTimeOffset? BrokerHeldAtUtc,
    DateTimeOffset? BrokerCommittedAtUtc,
    int RawPostCount,
    int ServerApplicationBodyReadsWhileBrokerHeld,
    int ClientTransportEntryCount,
    long ContentBytesCopied,
    long AgentBodyBytesSentWhileBrokerHeld,
    bool ObservedContinue,
    bool ContinueObservedBeforeBrokerCommit,
    bool ApplicationPrebufferObserved,
    bool FinalResponseObserved,
    bool AgentObservationCompleted,
    bool ServerObservationCompleted,
    bool EvidenceComplete)
{
    public bool HiddenRetryObserved => RawPostCount != 1 || ClientTransportEntryCount != 1;
    public bool KestrelContinueRelayedAfterCommit =>
        EvidenceComplete && ObservedContinue && !ContinueObservedBeforeBrokerCommit &&
        BrokerCommittedAtUtc is not null && AgentBodyBytesSentWhileBrokerHeld == 0;
}

public interface ISiteRawIngressQualificationRunStore
{
    Task<bool> EnrollSyntheticCredentialAsync(
        SiteRawIngressQualificationSyntheticCredentialEnrollment enrollment,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<SiteRawIngressQualificationRunHandle?> RegisterAsync(
        SiteRawIngressQualificationRunRegistration registration,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<SiteRawIngressQualificationRawPost?> ObserveRawPostAsync(
        CaptureRuntimeSiteTransportQualificationSettings settings,
        SiteRawIngressQualificationRunBinding binding,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<bool> ConsumeAuthenticatedAsync(
        Guid qualificationRunId,
        SiteRawIngressQualificationRunBinding binding,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<bool> ReleaseBrokerAsync(Guid qualificationRunId, DateTimeOffset now,
        SiteRawIngressQualificationRunAccess access, CancellationToken cancellationToken);

    Task<bool> AcknowledgeBrokerCommitAsync(Guid qualificationRunId, DateTimeOffset now,
        SiteRawIngressQualificationRunAccess access, CancellationToken cancellationToken);

    Task<bool> RecordAgentObservationAsync(Guid qualificationRunId,
        SiteRawIngressQualificationAgentObservation observation, DateTimeOffset now,
        SiteRawIngressQualificationRunAccess access, CancellationToken cancellationToken);

    Task<bool> RecordServerBodyReadsAsync(Guid qualificationRunId, int readsWhileBrokerHeld,
        DateTimeOffset now, CancellationToken cancellationToken);

    Task<SiteRawIngressQualificationRunReport?> ReadAsync(Guid qualificationRunId,
        DateTimeOffset now, SiteRawIngressQualificationRunAccess access,
        CancellationToken cancellationToken);
}

// The broker service owns the B/R1 transaction and is the only process that can
// truthfully emit these two phase transitions.
public interface ISiteRawIngressQualificationBrokerObserver
{
    Task HoldBeforeCommitAsync(Guid qualificationRunId,
        CancellationToken cancellationToken);
    Task RecordCommittedAsync(Guid qualificationRunId,
        CancellationToken cancellationToken);
}

public interface ISiteRawIngressQualificationRequestMeasurement
{
    Guid? QualificationRunId { get; }
    bool BrokerCommitted { get; }
    int BodyReadsWhileBrokerHeld { get; }
    void Begin(Guid qualificationRunId);
    void MarkBrokerCommitted();
    void ObserveBodyRead();
}
