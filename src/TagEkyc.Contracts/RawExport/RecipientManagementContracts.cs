namespace TagEkyc.Contracts.RawExport;

public sealed record EnrollManagedRecipientRequest(
    Guid RecipientClientApplicationId,
    Guid PrincipalId);

public sealed record IssueManagedRecipientCredentialRequest(
    Guid RecipientClientApplicationId,
    DateTimeOffset? ExpiresAtUtc);

public sealed record ReplaceManagedRecipientCredentialRequest(
    Guid RecipientClientApplicationId,
    Guid CurrentApiKeyId,
    long CurrentCredentialRevision,
    DateTimeOffset? ExpiresAtUtc,
    string Reason);

public sealed record RevokeManagedRecipientCredentialRequest(
    Guid RecipientClientApplicationId,
    Guid ApiKeyId,
    long ExpectedRevision,
    string Reason);

public sealed record EnrollRecipientPublicKeyRequest(
    Guid RecipientClientApplicationId,
    string RecipientKeyId,
    int RecipientKeyVersion,
    string PublicKeyAlgorithm,
    string PublicKeySpkiBase64,
    string PublicKeyFingerprintHex,
    DateTimeOffset ValidFromUtc,
    DateTimeOffset ValidUntilUtc);

public sealed record RotateRecipientPublicKeyRequest(
    Guid RecipientClientApplicationId,
    string RecipientKeyId,
    int CurrentKeyVersion,
    long CurrentKeyRevision,
    int NewKeyVersion,
    string PublicKeyAlgorithm,
    string PublicKeySpkiBase64,
    string PublicKeyFingerprintHex,
    DateTimeOffset ValidFromUtc,
    DateTimeOffset ValidUntilUtc,
    string Reason);

public sealed record RevokeRecipientPublicKeyRequest(
    Guid RecipientClientApplicationId,
    string RecipientKeyId,
    int RecipientKeyVersion,
    long ExpectedRevision,
    string Reason);

public sealed record ManagedRecipientIdentityDto(
    Guid RecipientClientApplicationId,
    Guid PrincipalId,
    string State,
    long Revision);

public sealed record ManagedCredentialDto(
    Guid ApiKeyId,
    Guid RecipientClientApplicationId,
    Guid PrincipalId,
    int CredentialVersion,
    string State,
    long Revision,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    string? PresentedKey);

public sealed record RecipientPublicKeyDto(
    Guid RecipientClientApplicationId,
    string RecipientKeyId,
    int RecipientKeyVersion,
    string PublicKeyAlgorithm,
    string PublicKeyFingerprintHex,
    string State,
    long Revision,
    DateTimeOffset ValidFromUtc,
    DateTimeOffset ValidUntilUtc,
    DateTimeOffset RegisteredAtUtc,
    DateTimeOffset? RevokedAtUtc);

public sealed record RecipientPublicKeyOperationResultDto(
    RecipientPublicKeyDto Key,
    string? Warning,
    int? AuthorizedDeliveryCount,
    int? StreamingDeliveryCount,
    int? InterruptedDeliveryCount);

public sealed record ManagedRecipientReadinessDto(
    Guid RecipientClientApplicationId,
    bool Ready,
    IReadOnlyList<string> Codes,
    int AuthorizedDeliveryCount,
    int StreamingDeliveryCount,
    int InterruptedDeliveryCount);

public static class RecipientManagementErrorCodes
{
    public const string Forbidden = "RAW_EXPORT_RECIPIENT_MANAGEMENT_FORBIDDEN";
    public const string RequestInvalid = "RAW_EXPORT_RECIPIENT_MANAGEMENT_REQUEST_INVALID";
    public const string IdempotencyConflict = "RAW_EXPORT_RECIPIENT_MANAGEMENT_IDEMPOTENCY_CONFLICT";
    public const string TargetNotFound = "RAW_EXPORT_RECIPIENT_MANAGEMENT_TARGET_NOT_FOUND";
    public const string PrincipalConflict = "RAW_EXPORT_RECIPIENT_MANAGEMENT_PRINCIPAL_CONFLICT";
    public const string CredentialConflict = "RAW_EXPORT_RECIPIENT_MANAGEMENT_CREDENTIAL_CONFLICT";
    public const string KeyProfileInvalid = "RAW_EXPORT_RECIPIENT_MANAGEMENT_KEY_PROFILE_INVALID";
    public const string KeyConflict = "RAW_EXPORT_RECIPIENT_MANAGEMENT_KEY_CONFLICT";
    public const string DeliveryDrainRequired = "RAW_EXPORT_RECIPIENT_MANAGEMENT_DELIVERY_DRAIN_REQUIRED";
    public const string Unavailable = "RAW_EXPORT_RECIPIENT_MANAGEMENT_UNAVAILABLE";
}
