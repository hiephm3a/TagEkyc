namespace TagEkyc.Contracts.CaptureRuntime;

public sealed record AuthenticatedPlatformOperatorContext(
    Guid CredentialId,
    Guid PrincipalId,
    string CallerCategory,
    IReadOnlySet<string> Scopes,
    string KeyLookupPrefix,
    long CredentialRevision,
    DateTimeOffset AuthenticatedAtUtc);

public sealed record AuthenticatedCaptureRuntimeContext(
    Guid CaptureAgentId,
    Guid DeviceInstallationId,
    Guid CredentialId,
    long CredentialGeneration,
    byte[] PublicKeyThumbprint,
    Guid RolePolicyId,
    long RolePolicyRevision,
    long RuntimeRevision,
    long InstallationRevision,
    long CredentialRevision,
    DateTimeOffset SignedAtUtc,
    byte[] Nonce,
    byte[] SignedEnvelopeFingerprint);

public sealed record CaptureRuntimeSignedRequest(
    Guid CredentialId,
    long CredentialGeneration,
    DateTimeOffset SignedAtUtc,
    byte[] Nonce,
    byte[] Signature,
    string RequiredRole,
    ReadOnlyMemory<byte> ExactSignedPreimage);

public static class CaptureRuntimeErrorCodes
{
    public const string RequestInvalid = "REQUEST_INVALID";
    public const string AccessDenied = "ACCESS_DENIED";
    public const string ResourceNotAvailable = "RESOURCE_NOT_AVAILABLE";
    public const string Conflict = "CONFLICT";
    public const string ExistingMatchSecretUnavailable = "EXISTING_MATCH_SECRET_UNAVAILABLE";
    public const string NotReady = "NOT_READY";
}
