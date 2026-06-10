using TagEkyc.Application;
using TagEkyc.Contracts;
using TagEkyc.Domain;

namespace TagEkyc.Adapters;

public sealed class AdapterBoundaryPlaceholder
{
    public SessionStatusPlaceholder Describe(SessionBoundaryPlaceholder boundary)
    {
        return boundary.Describe(
            VerificationProfile.StandardEkycProfile,
            VerificationSessionState.InProgress,
            VerificationResult.NotAvailable);
    }
}
