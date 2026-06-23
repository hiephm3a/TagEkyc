using TagEkyc.Domain;

namespace TagEkyc.Application.Ports;

public static class EvidenceSignatureDefaults
{
    public const string PurposeEvidencePackageManifest = "EvidencePackageManifestSignature";
    public const string PurposeNeutralEkycProof = "NeutralEkycProofSignature";
    public const string ProofVersionNeutralV1 = "neutral-proof-v1";
    public const string FormatJws = "JWS";
    public const string SchemeJwsEs256V1 = "jws-es256-v1";
    public const string AlgorithmEs256 = "ES256";
    public const string ResultHashAlgorithmSha256 = "sha256";
    public const string ResultHashCanonicalizationSchemeJcsV1 = "rfc8785-jcs-v1";
    public const string ResultHashLabel = "tip-67b-neutral-proof-result";
}

public sealed record EvidenceSignatureRequest(
    string PackageId,
    string ManifestHash,
    string PackageVersion,
    string CanonicalizationScheme,
    string HashAlgorithm,
    string Purpose);

public sealed record EvidenceProofEngineRef(
    string EvidenceResultType,
    string EvidenceResultId,
    string EngineName,
    string EngineVersion,
    string CheckType);

public sealed record EvidenceProofSignatureRequest(
    string PackageId,
    string PackageVersion,
    string CanonicalizationScheme,
    string HashAlgorithm,
    string Purpose,
    string SessionId,
    string IdentityRef,
    string Result,
    string AssuranceLevel,
    IReadOnlyList<string> RequiredChecks,
    IReadOnlyList<string> CompletedChecks,
    IReadOnlyList<EvidenceProofEngineRef> EvidenceEngines,
    string Challenge,
    string SignedManifestHash);

public interface IEvidenceSigner
{
    Task<EvidenceSignatureEnvelope> SignAsync(
        EvidenceSignatureRequest request,
        CancellationToken cancellationToken = default);

    Task<EvidenceSignatureEnvelope> SignProofAsync(
        EvidenceProofSignatureRequest request,
        CancellationToken cancellationToken = default);
}
