using TagEkyc.Domain;

namespace TagEkyc.Application.Ports;

public static class EvidenceSignatureDefaults
{
    public const string PurposeEvidencePackageManifest = "EvidencePackageManifestSignature";
    public const string FormatJws = "JWS";
    public const string SchemeJwsEs256V1 = "jws-es256-v1";
    public const string AlgorithmEs256 = "ES256";
}

public sealed record EvidenceSignatureRequest(
    string PackageId,
    string ManifestHash,
    string PackageVersion,
    string CanonicalizationScheme,
    string HashAlgorithm,
    string Purpose);

public interface IEvidenceSigner
{
    Task<EvidenceSignatureEnvelope> SignAsync(
        EvidenceSignatureRequest request,
        CancellationToken cancellationToken = default);
}
