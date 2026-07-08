namespace TagEkyc.Domain;

public sealed record VerificationSession
{
    private VerificationSession(
        Guid id,
        Guid clientApplicationId,
        string subjectRef,
        VerificationProfile profile,
        string purpose,
        IReadOnlySet<RequiredCheckType> requiredChecks,
        string? externalSessionId,
        string? clientReference,
        string? challenge,
        string requestId,
        string correlationId,
        VerificationSessionState state,
        VerificationResult result,
        AssuranceLevel assuranceLevel,
        Guid? finalDecisionId,
        Guid? evidencePackageId,
        HashRef? evidencePackageHash,
        HashRef? manifestHash,
        DataBoundaryMetadata metadata,
        DateTimeOffset expiresAt,
        DateTimeOffset createdAt,
        DateTimeOffset? completedAt)
    {
        Id = id;
        ClientApplicationId = clientApplicationId;
        SubjectRef = subjectRef;
        Profile = profile;
        Purpose = purpose;
        RequiredChecks = requiredChecks;
        ExternalSessionId = externalSessionId;
        ClientReference = clientReference;
        Challenge = challenge;
        RequestId = requestId;
        CorrelationId = correlationId;
        State = state;
        Result = result;
        AssuranceLevel = assuranceLevel;
        FinalDecisionId = finalDecisionId;
        EvidencePackageId = evidencePackageId;
        EvidencePackageHash = evidencePackageHash;
        ManifestHash = manifestHash;
        Metadata = metadata;
        ExpiresAt = expiresAt;
        CreatedAt = createdAt;
        CompletedAt = completedAt;
    }

    public Guid Id { get; }
    public Guid ClientApplicationId { get; }
    public string SubjectRef { get; }
    public VerificationProfile Profile { get; }
    public string Purpose { get; }
    public IReadOnlySet<RequiredCheckType> RequiredChecks { get; }
    public string? ExternalSessionId { get; }
    public string? ClientReference { get; }
    public string? Challenge { get; }
    public string RequestId { get; }
    public string CorrelationId { get; }
    public VerificationSessionState State { get; }
    public VerificationResult Result { get; }
    public AssuranceLevel AssuranceLevel { get; }
    public Guid? FinalDecisionId { get; }
    public Guid? EvidencePackageId { get; }
    public HashRef? EvidencePackageHash { get; }
    public HashRef? ManifestHash { get; }
    public DataBoundaryMetadata Metadata { get; }
    public DateTimeOffset ExpiresAt { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset? CompletedAt { get; }

    public static VerificationSession Create(
        Guid clientApplicationId,
        string subjectRef,
        VerificationProfile profile,
        string purpose,
        IEnumerable<RequiredCheckType> requiredChecks,
        DateTimeOffset expiresAt,
        DateTimeOffset createdAt,
        string? externalSessionId = null,
        string? clientReference = null,
        string? challenge = null,
        string? requestId = null,
        string? correlationId = null,
        DataBoundaryMetadata? metadata = null)
    {
        if (clientApplicationId == Guid.Empty)
        {
            throw new ArgumentException("Client application id is required.", nameof(clientApplicationId));
        }

        if (string.IsNullOrWhiteSpace(subjectRef))
        {
            throw new ArgumentException("Subject reference is required.", nameof(subjectRef));
        }

        if (string.IsNullOrWhiteSpace(purpose))
        {
            throw new ArgumentException("Purpose is required.", nameof(purpose));
        }

        var checkSet = requiredChecks.ToHashSet();
        if (checkSet.Count == 0)
        {
            throw new ArgumentException("At least one required check is required.", nameof(requiredChecks));
        }

        if (profile == VerificationProfile.ChallengeBoundEkycProfile)
        {
            if (string.IsNullOrEmpty(challenge))
            {
                throw new ArgumentException("Challenge-bound sessions require a challenge.", nameof(challenge));
            }
        }

        return new VerificationSession(
            Guid.NewGuid(),
            clientApplicationId,
            subjectRef,
            profile,
            purpose,
            checkSet,
            externalSessionId,
            clientReference,
            challenge,
            requestId ?? string.Empty,
            correlationId ?? string.Empty,
            VerificationSessionState.Created,
            VerificationResult.NotAvailable,
            AssuranceLevel.None,
            finalDecisionId: null,
            evidencePackageId: null,
            evidencePackageHash: null,
            manifestHash: null,
            metadata ?? DataBoundaryMetadata.LocalDevDefault(checkSet),
            expiresAt,
            createdAt,
            completedAt: null);
    }

    public VerificationSession WithState(VerificationSessionState state) =>
        new(
            Id,
            ClientApplicationId,
            SubjectRef,
            Profile,
            Purpose,
            RequiredChecks,
            ExternalSessionId,
            ClientReference,
            Challenge,
            RequestId,
            CorrelationId,
            state,
            Result,
            AssuranceLevel,
            FinalDecisionId,
            EvidencePackageId,
            EvidencePackageHash,
            ManifestHash,
            Metadata,
            ExpiresAt,
            CreatedAt,
            CompletedAt);

    public VerificationSession WithCancellation(string requestId, string correlationId) =>
        new(
            Id,
            ClientApplicationId,
            SubjectRef,
            Profile,
            Purpose,
            RequiredChecks,
            ExternalSessionId,
            ClientReference,
            Challenge,
            requestId,
            correlationId,
            VerificationSessionState.Cancelled,
            Result,
            AssuranceLevel,
            FinalDecisionId,
            EvidencePackageId,
            EvidencePackageHash,
            ManifestHash,
            Metadata,
            ExpiresAt,
            CreatedAt,
            CompletedAt);

    public VerificationSession WithCompletion(
        VerificationResult result,
        AssuranceLevel assuranceLevel,
        Guid finalDecisionId,
        Guid evidencePackageId,
        HashRef evidencePackageHash,
        HashRef manifestHash,
        string requestId,
        string correlationId,
        DateTimeOffset completedAt) =>
        new(
            Id,
            ClientApplicationId,
            SubjectRef,
            Profile,
            Purpose,
            RequiredChecks,
            ExternalSessionId,
            ClientReference,
            Challenge,
            requestId,
            correlationId,
            VerificationSessionState.Completed,
            result,
            assuranceLevel,
            finalDecisionId,
            evidencePackageId,
            evidencePackageHash,
            manifestHash,
            Metadata,
            ExpiresAt,
            CreatedAt,
            completedAt);
}
