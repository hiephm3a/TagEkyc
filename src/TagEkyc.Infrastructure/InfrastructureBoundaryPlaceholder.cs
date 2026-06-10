using TagEkyc.Application;
using TagEkyc.Contracts;
using TagEkyc.Domain;

namespace TagEkyc.Infrastructure;

public sealed class InfrastructureBoundaryPlaceholder
{
    public SessionStatusPlaceholder Describe(SessionBoundaryPlaceholder boundary)
    {
        return boundary.Describe(
            VerificationProfile.StandardEkycProfile,
            VerificationSessionState.Created,
            VerificationResult.NotAvailable);
    }
}
