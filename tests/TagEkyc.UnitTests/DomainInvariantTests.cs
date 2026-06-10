using TagEkyc.Domain;

namespace TagEkyc.UnitTests;

public sealed class DomainInvariantTests
{
    [Fact]
    public void Transaction_bound_session_requires_binding_nonce_hash()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            VerificationSession.Create(
                Guid.NewGuid(),
                "patient_789",
                VerificationProfile.TransactionBoundEkycProfile,
                "SIGNING_AUTH",
                RequiredCheckPolicy.SignFlowS1RequiredChecks,
                DateTimeOffset.UtcNow.AddMinutes(15),
                DateTimeOffset.UtcNow,
                externalSessionId: "sf_session_123",
                externalTransactionId: "sf_tx_456"));

        Assert.Contains("binding nonce hash", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Transaction_bound_session_requires_external_transaction_id()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            VerificationSession.Create(
                Guid.NewGuid(),
                "patient_789",
                VerificationProfile.TransactionBoundEkycProfile,
                "SIGNING_AUTH",
                RequiredCheckPolicy.SignFlowS1RequiredChecks,
                DateTimeOffset.UtcNow.AddMinutes(15),
                DateTimeOffset.UtcNow,
                externalSessionId: "sf_session_123",
                bindingNonceHash: new HashRef("sha256:binding")));

        Assert.Contains("external transaction id", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Session_state_does_not_collapse_identity_failure_into_lifecycle_failed()
    {
        Assert.DoesNotContain("Failed", Enum.GetNames<VerificationSessionState>());
        Assert.Contains(VerificationResult.FailedIdentity, Enum.GetValues<VerificationResult>());
        Assert.Contains(VerificationResult.FailedCaptureQuality, Enum.GetValues<VerificationResult>());
    }

    [Fact]
    public void Signflow_s1_required_checks_exclude_optional_ocr_and_fingerprint()
    {
        Assert.True(RequiredCheckPolicy.SignFlowS1RequiredChecks.SetEquals(
            [
                RequiredCheckType.CaptureQuality,
                RequiredCheckType.DocumentNfc,
                RequiredCheckType.FaceMatch,
                RequiredCheckType.Liveness,
            ]));

        Assert.DoesNotContain(RequiredCheckType.DocumentOcr, RequiredCheckPolicy.SignFlowS1RequiredChecks);
        Assert.DoesNotContain(RequiredCheckType.Fingerprint, RequiredCheckPolicy.SignFlowS1RequiredChecks);
    }

    [Fact]
    public void Hash_ref_requires_sha256_prefix()
    {
        Assert.Throws<ArgumentException>(() => new HashRef("plain"));
        Assert.Equal("sha256:value", new HashRef("sha256:value").Value);
    }

    [Fact]
    public void Signature_placeholder_status_has_only_non_authoritative_value()
    {
        var value = Assert.Single(Enum.GetValues<SignaturePlaceholderStatus>());
        Assert.Equal(SignaturePlaceholderStatus.PlaceholderUnverified, value);
    }
}
