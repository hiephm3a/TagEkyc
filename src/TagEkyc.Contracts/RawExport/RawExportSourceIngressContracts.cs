using System.Text.Json.Serialization;

namespace TagEkyc.Contracts.RawExport;

public static class RawExportSourceIngressCodes
{
    public const string Available = "RAW_EXPORT_SOURCE_AVAILABLE";
    public const string AlreadyAvailable = "RAW_EXPORT_SOURCE_ALREADY_AVAILABLE";
    public const string EvaluationInProgress = "RAW_EXPORT_SOURCE_CLAIM_EVALUATION_IN_PROGRESS";
    public const string BindingInvalid = "RAW_EXPORT_SOURCE_BINDING_INVALID";
    public const string CapacityUnavailable = "RAW_EXPORT_SOURCE_CAPACITY_UNAVAILABLE";
    public const string TransportProtocolInvalid = "RAW_EXPORT_SOURCE_TRANSPORT_PROTOCOL_INVALID";
}

public sealed record RawExportSourceIngressMetadata(
    Guid ClientApplicationId,
    string ProducerId,
    string CaptureAgentInstanceId,
    long AgentConfigurationRevision,
    Guid VerificationSessionId,
    Guid CaptureArtifactId,
    int CaptureRevision,
    string RawClass,
    Guid IngressIdempotencyKey,
    string MediaType,
    long ClaimedPlaintextLength,
    string ClaimedPlaintextDigest,
    DateTimeOffset CapturedAtUtc,
    DateTimeOffset PlaintextRetentionStartedAtUtc,
    DateTimeOffset PlaintextRetentionExpiresAtUtc,
    long PlaintextRetentionBudgetSeconds);

public sealed record CaptureAgentFinalResult(
    string OutcomeCode,
    Guid? SourceArtifactId = null,
    string? CurrentSourceState = null,
    string? CurrentDisposition = null,
    DateTimeOffset? RetryNotBeforeUtc = null);

public sealed record CaptureAgentRawExportConfiguration(
    long ConfigurationRevision,
    DateTimeOffset EffectiveAtUtc,
    DateTimeOffset ExpiresAtUtc,
    bool RawExportEnabled,
    long PlaintextBudgetSeconds,
    int RawExportSourceClaimSafetyMarginMilliseconds);

public sealed record CaptureAgentConfigurationProjection(
    CaptureAgentRawExportConfiguration Configuration,
    string ETag);

public sealed record RawExportR2Handoff(
    Guid SourceArtifactId,
    Guid AttemptKeyReservationId,
    Guid AttemptId,
    long ExpectedEncryptionAttemptRevision,
    long ExpectedFence);

public sealed record RawExportResolvedCaptureAcceptance(
    Guid CaptureAcceptanceId,
    string SessionChallengeHash,
    string ProducerId,
    string CaptureAgentInstanceId);

public sealed record RawExportBrokerAdmission(
    CaptureAgentFinalResult? FinalResult,
    RawExportR2Handoff? Handoff)
{
    [JsonIgnore]
    public bool BodyRequired => Handoff is not null && FinalResult is null;
}
