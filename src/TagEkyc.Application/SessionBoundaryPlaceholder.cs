using TagEkyc.Contracts;
using TagEkyc.Domain;

namespace TagEkyc.Application;

public sealed class SessionBoundaryPlaceholder : IApplicationBoundary
{
    public SessionStatusPlaceholder Describe(VerificationProfile profile, VerificationSessionState state, VerificationResult result)
    {
        return new SessionStatusPlaceholder(
            "placeholder",
            profile.ToString(),
            state.ToString(),
            result.ToString());
    }
}
