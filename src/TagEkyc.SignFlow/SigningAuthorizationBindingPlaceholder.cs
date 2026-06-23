using TagEkyc.Contracts;

namespace TagEkyc.SignFlow;

public sealed record SigningAuthorizationBindingPlaceholder(
    string ExternalSessionId,
    string ClientReference,
    string Challenge,
    string EvidencePackageId,
    string EvidencePackageHash)
{
    public SessionStatusPlaceholder ToSessionStatus()
    {
        return new SessionStatusPlaceholder(
            ExternalSessionId,
            "CHALLENGE_BOUND_EKYC_PROFILE",
            "COMPLETED",
            "NOT_AVAILABLE");
    }
}
