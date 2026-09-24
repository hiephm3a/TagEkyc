using TagEkyc.Contracts.RawExport;

namespace TagEkyc.Application.Ports;

public interface IRawIngressMetadataBroker
{
    Task<RawIngressBrokerResult> AdmitAsync(
        CaptureRuntimeRawIngressAdmissionContext context, CancellationToken cancellationToken);
}

public interface ICaptureRuntimeRawIngressBodyPipeline
{
    Task<CaptureRuntimeRawIngressAdmissionResult> ProcessAsync(
        CaptureRuntimeRawIngressAdmissionContext context,
        RawIngressBrokerHandoff handoff,
        Stream body,
        CancellationToken cancellationToken);
}
