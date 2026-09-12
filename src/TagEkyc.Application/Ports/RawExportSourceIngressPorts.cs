using TagEkyc.Contracts.RawExport;

namespace TagEkyc.Application.Ports;

public interface IRawExportSourceIngressBroker
{
    // A successful handoff means the broker-owned R1 transaction has committed.
    Task<RawExportBrokerAdmission> AdmitAsync(
        AuthenticatedClientContext caller,
        RawExportSourceIngressMetadata metadata,
        RawExportResolvedCaptureAcceptance acceptance,
        CancellationToken cancellationToken);
}

public interface IRawExportCaptureAcceptanceResolver
{
    Task<RawExportResolvedCaptureAcceptance?> ResolveExactAsync(
        AuthenticatedClientContext caller,
        RawExportSourceIngressMetadata metadata,
        CancellationToken cancellationToken);
}

public interface IRawExportSourceBodyPipeline
{
    Task<CaptureAgentFinalResult> ProcessAsync(
        RawExportSourceIngressMetadata metadata,
        RawExportR2Handoff handoff,
        Stream body,
        CancellationToken cancellationToken);
}

public interface IRawExportIngressCapacity
{
    IRawExportIngressCapacityLease? TryAcquire(string producerId, long plaintextBytes);
}

public interface IRawExportIngressCapacityLease : IDisposable
{
}

public interface ICaptureAgentConfigurationProvider
{
    Task<CaptureAgentConfigurationProjection?> GetSelfAsync(
        AuthenticatedClientContext caller,
        CancellationToken cancellationToken);

    Task<CaptureAgentConfigurationProjection?> GetCurrentAsync(
        AuthenticatedClientContext caller,
        string producerId,
        CancellationToken cancellationToken);
}
