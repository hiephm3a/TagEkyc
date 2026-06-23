namespace TagEkyc.Domain;

public sealed record EvidenceSignatureEnvelope(
    SignaturePlaceholderStatus SignatureStatus,
    string SignatureFormat,
    string SignatureScheme,
    string SignatureAlgorithm,
    string KeyId,
    DateTimeOffset SignedAt,
    string SignatureValue,
    string? PublicKeyJwk = null,
    string? PublicKeyFingerprint = null);
