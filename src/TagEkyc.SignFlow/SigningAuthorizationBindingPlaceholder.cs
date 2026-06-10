using TagEkyc.Contracts;

namespace TagEkyc.SignFlow;

public sealed record SigningAuthorizationBindingPlaceholder(
    string ExternalSessionId,
    string ExternalTransactionId,
    string BindingNonceHash,
    string EvidencePackageId,
    string EvidencePackageHash)
{
    public SessionStatusPlaceholder ToSessionStatus()
    {
        return new SessionStatusPlaceholder(
            ExternalSessionId,
            "TRANSACTION_BOUND_EKYC_PROFILE",
            "COMPLETED",
            "NOT_AVAILABLE");
    }
}
