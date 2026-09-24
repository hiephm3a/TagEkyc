using TagEkyc.Application.CaptureRuntime;

namespace TagEkyc.Api;

public interface ISiteRawIngressTransportQualificationRuntimeGate
{
    CaptureRuntimeSiteTransportQualificationEvaluation Evaluate(DateTimeOffset now);
}

public sealed class SiteRawIngressTransportQualificationRuntimeGate(IServiceProvider services)
    : ISiteRawIngressTransportQualificationRuntimeGate
{
    public CaptureRuntimeSiteTransportQualificationEvaluation Evaluate(DateTimeOffset now) =>
        CaptureRuntimeSiteTransportQualificationPolicy.Evaluate(
            services.GetService<ICaptureRuntimeActivationEvidenceSealProvider>()?.Current,
            services.GetService<ICaptureRuntimeSiteTransportQualificationSettingsProvider>()?.Current,
            services.GetService<ICaptureRuntimeSiteTransportQualificationProvider>()?.Current, now);
}

// The request gate is authoritative. This monitor supplies advance operational
// notice so an expiring site record does not become a surprise outage.
public sealed class SiteRawIngressTransportQualificationMonitor(
    ISiteRawIngressTransportQualificationRuntimeGate gate,
    ILogger<SiteRawIngressTransportQualificationMonitor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        CaptureRuntimeSiteTransportQualificationState? last = null;
        DateTimeOffset? lastExpiry = null;
        while (!stoppingToken.IsCancellationRequested)
        {
            var result = gate.Evaluate(DateTimeOffset.UtcNow);
            if (result.State != last || result.ValidUntilUtc != lastExpiry)
            {
                if (result.State == CaptureRuntimeSiteTransportQualificationState.Expiring)
                    logger.LogWarning("{Code} validUntilUtc={ValidUntilUtc:O}", result.Code, result.ValidUntilUtc);
                else if (result.State is CaptureRuntimeSiteTransportQualificationState.Expired
                         or CaptureRuntimeSiteTransportQualificationState.Invalid
                         or CaptureRuntimeSiteTransportQualificationState.Missing)
                    logger.LogError("{Code} state={State} validUntilUtc={ValidUntilUtc:O}",
                        result.Code, result.State, result.ValidUntilUtc);
                last = result.State;
                lastExpiry = result.ValidUntilUtc;
            }
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
