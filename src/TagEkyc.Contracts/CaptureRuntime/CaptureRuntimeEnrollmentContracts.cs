namespace TagEkyc.Contracts.CaptureRuntime;

public sealed record CaptureRuntimeEnrollmentRedeemRequest(
    Guid BootstrapIssuanceId,
    string BootstrapSecret,
    Guid CandidateKeyId,
    string PublicVerifierSpki,
    string PublicKeyThumbprint,
    DateTimeOffset SignedAtUtc,
    string Nonce,
    string CandidateProof);

public sealed record CaptureRuntimeEnrollmentResponse(
    Guid CaptureAgentId,
    Guid DeviceInstallationId,
    Guid CredentialId,
    long Generation,
    long RuntimeRevision,
    long InstallationRevision,
    long CredentialRevision);
