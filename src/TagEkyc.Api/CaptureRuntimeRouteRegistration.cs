using TagEkyc.Application.CaptureRuntime;

namespace TagEkyc.Api;

public static class CaptureRuntimeRouteRegistration
{
    // Consumes the one pre-listener selection; never probes or changes durable state.
    public static IEndpointRouteBuilder MapCaptureRuntimeSelectedEndpoints(
        this IEndpointRouteBuilder endpoints, CaptureRuntimeRouteSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        if (selection.Revision <= 0 || !Enum.IsDefined(selection.State))
            throw new InvalidOperationException("CAPTURE_RUNTIME_STARTUP_NOT_READY");
        endpoints.MapVerificationSessionEndpoints(selection.State == CaptureRuntimeRouteState.Prepared);
        endpoints.MapCaptureRuntimeManagementEndpoints();
        endpoints.MapCaptureRuntimeRotationEndpoints();
        if (selection.State == CaptureRuntimeRouteState.Prepared)
        {
            endpoints.MapRawExportSourceIngressEndpoints();
            endpoints.MapCaptureAgentConfigurationEndpoints();
        }
        else
        {
            endpoints.MapCaptureRuntimeExecutionEndpoints();
            endpoints.MapCaptureRuntimeRawIngressEndpoints();
        }
        return endpoints;
    }
}
