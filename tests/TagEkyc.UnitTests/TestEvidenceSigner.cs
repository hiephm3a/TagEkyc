using TagEkyc.Application.Ports;
using TagEkyc.Domain;

namespace TagEkyc.UnitTests;

internal sealed class TestEvidenceSigner : IEvidenceSigner
{
    public Task<EvidenceSignatureEnvelope> SignAsync(
        EvidenceSignatureRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new EvidenceSignatureEnvelope(
            SignaturePlaceholderStatus.Signed,
            EvidenceSignatureDefaults.FormatJws,
            EvidenceSignatureDefaults.SchemeJwsEs256V1,
            EvidenceSignatureDefaults.AlgorithmEs256,
            "unit-test-key",
            DateTimeOffset.UtcNow,
            $"unit-test-jws:{request.ManifestHash}"));

    public Task<EvidenceSignatureEnvelope> SignProofAsync(
        EvidenceProofSignatureRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new EvidenceSignatureEnvelope(
            SignaturePlaceholderStatus.Signed,
            EvidenceSignatureDefaults.FormatJws,
            EvidenceSignatureDefaults.SchemeJwsEs256V1,
            EvidenceSignatureDefaults.AlgorithmEs256,
            "unit-test-key",
            DateTimeOffset.UtcNow,
            $"unit-test-proof-jws:{request.SignedManifestHash}",
            """{"crv":"P-256","kty":"EC","x":"unit-test-x","y":"unit-test-y"}""",
            "sha256:unit-test-fingerprint"));
}
