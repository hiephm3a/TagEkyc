using TagEkyc.Contracts;
using TagEkyc.SignFlow;

namespace TagEkyc.ContractTests;

public sealed class ContractPlaceholderTests
{
    [Fact]
    public void Session_status_placeholder_uses_sanitized_shape()
    {
        var contract = new SessionStatusPlaceholder("vs_placeholder", "STANDARD_EKYC_PROFILE", "CREATED", "NOT_AVAILABLE");

        Assert.Equal("vs_placeholder", contract.VerificationSessionId);
        Assert.Equal("STANDARD_EKYC_PROFILE", contract.Profile);
    }

    [Fact]
    public void Signflow_placeholder_exposes_only_binding_fields()
    {
        var contract = new SigningAuthorizationBindingPlaceholder(
            "external-session",
            "external-transaction",
            "sha256:binding",
            "ep_placeholder",
            "sha256:evidence");

        Assert.Equal("sha256:binding", contract.BindingNonceHash);
        Assert.Equal("ep_placeholder", contract.EvidencePackageId);
    }
}
