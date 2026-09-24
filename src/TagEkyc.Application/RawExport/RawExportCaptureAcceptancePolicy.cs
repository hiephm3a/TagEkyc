using TagEkyc.Contracts.TrustedAdapter;

namespace TagEkyc.Application.RawExport;

public sealed record RawExportCaptureAcceptancePolicy(
    Guid ClientApplicationId,
    string AcceptancePolicyId,
    int AcceptancePolicyVersion);

public interface IRawExportCaptureAcceptancePolicyProvider
{
    RawExportCaptureAcceptancePolicy? Find(Guid clientApplicationId);
}

public interface IRawExportCaptureAcceptanceWriter
{
    Task<RawCaptureAcceptanceDto?> BindAcceptedEvidenceAsync(
        Guid bindingId,
        Guid verificationSessionId,
        Guid evidenceResultId,
        RawExportCaptureAcceptancePolicy? configuredPolicy,
        CancellationToken cancellationToken);
}

// This signal is deliberately not a database exception: the transaction owner
// must roll B back, then the Application boundary maps the finite semantic
// conflict without disclosing SQL details.
public sealed class RawExportCaptureAcceptanceConflictException : Exception
{
    public RawExportCaptureAcceptanceConflictException() : base("RAW_EXPORT_CAPTURE_ACCEPTANCE_CONFLICT") { }
}
