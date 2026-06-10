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
            expiresAt,
            createdAt,
            completedAt: null);
    }
}
