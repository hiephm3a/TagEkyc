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
        string? externalTransactionId,
        HashRef? bindingNonceHash,
        string requestId,
        string correlationId,
        VerificationSessionState state,
        VerificationResult result,
        AssuranceLevel assuranceLevel,
        Guid? finalDecisionId,
        Guid? evidencePackageId,
        HashRef? evidencePackageHash,
        HashRef? manifestHash,
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
        ExternalTransactionId = externalTransactionId;
        BindingNonceHash = bindingNonceHash;
        RequestId = requestId;
        CorrelationId = correlationId;
        State = state;
        Result = result;
        AssuranceLevel = assuranceLevel;
        FinalDecisionId = finalDecisionId;
        EvidencePackageId = evidencePackageId;
        EvidencePackageHash = evidencePackageHash;
        ManifestHash = manifestHash;
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
    public string? ExternalTransactionId { get; }
    public HashRef? BindingNonceHash { get; }
    public string RequestId { get; }
    public string CorrelationId { get; }
    public VerificationSessionState State { get; }
    public VerificationResult Result { get; }
    public AssuranceLevel AssuranceLevel { get; }
    public Guid? FinalDecisionId { get; }
    public Guid? EvidencePackageId { get; }
    public HashRef? EvidencePackageHash { get; }
    public HashRef? ManifestHash { get; }
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
        string? externalTransactionId = null,
        HashRef? bindingNonceHash = null,
        string? requestId = null,
        string? correlationId = null)
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

        if (profile == VerificationProfile.TransactionBoundEkycProfile)
        {
            if (string.IsNullOrWhiteSpace(externalTransactionId))
            {
                throw new ArgumentException("Transaction-bound sessions require an external transaction id.", nameof(externalTransactionId));
            }

            if (bindingNonceHash is null)
            {
                throw new ArgumentException("Transaction-bound sessions require a binding nonce hash.", nameof(bindingNonceHash));
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
            externalTransactionId,
            bindingNonceHash,
            requestId ?? string.Empty,
            correlationId ?? string.Empty,
            VerificationSessionState.Created,
            VerificationResult.NotAvailable,
            AssuranceLevel.None,
            finalDecisionId: null,
            evidencePackageId: null,
            evidencePackageHash: null,
            manifestHash: null,
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
            ExternalTransactionId,
            BindingNonceHash,
            RequestId,
            CorrelationId,
            state,
            Result,
            AssuranceLevel,
            FinalDecisionId,
            EvidencePackageId,
            EvidencePackageHash,
            ManifestHash,
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
            ExternalTransactionId,
            BindingNonceHash,
            requestId,
            correlationId,
            VerificationSessionState.Completed,
            result,
            assuranceLevel,
            finalDecisionId,
            evidencePackageId,
            evidencePackageHash,
            manifestHash,
            ExpiresAt,
            CreatedAt,
            completedAt);
}
