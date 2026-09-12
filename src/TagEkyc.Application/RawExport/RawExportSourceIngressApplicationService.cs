using TagEkyc.Application.Ports;
using TagEkyc.Contracts.RawExport;

namespace TagEkyc.Application.RawExport;

public sealed class RawExportSourceIngressApplicationService(
    IRawExportIngressCapacity capacity,
    ICaptureAgentConfigurationProvider configurations,
    IRawExportCaptureAcceptanceResolver acceptances,
    IRawExportSourceIngressBroker broker,
    IRawExportSourceBodyPipeline bodyPipeline)
{
    public async Task<CaptureAgentFinalResult> ExecuteAsync(
        AuthenticatedClientContext caller,
        RawExportSourceIngressMetadata metadata,
        Func<Stream> openBody,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(openBody);

        var acceptance = await acceptances.ResolveExactAsync(caller, metadata, cancellationToken).ConfigureAwait(false);
        if (acceptance is null)
            return new CaptureAgentFinalResult(RawExportSourceIngressCodes.BindingInvalid);
        var canonical = metadata with
        {
            ClientApplicationId = caller.ClientApplicationId,
            ProducerId = acceptance.ProducerId,
            CaptureAgentInstanceId = acceptance.CaptureAgentInstanceId,
        };

        var projection = await configurations.GetCurrentAsync(caller, canonical.ProducerId, cancellationToken).ConfigureAwait(false);
        var config = projection?.Configuration;
        var now = DateTimeOffset.UtcNow;
        DateTimeOffset projectedExpiry;
        try
        {
            projectedExpiry = canonical.PlaintextRetentionStartedAtUtc.AddSeconds(canonical.PlaintextRetentionBudgetSeconds);
        }
        catch (ArgumentOutOfRangeException)
        {
            return new CaptureAgentFinalResult(RawExportSourceIngressCodes.BindingInvalid);
        }

        if (config is null || !config.RawExportEnabled ||
            config.ConfigurationRevision != canonical.AgentConfigurationRevision ||
            config.PlaintextBudgetSeconds != canonical.PlaintextRetentionBudgetSeconds ||
            now < config.EffectiveAtUtc || now >= config.ExpiresAtUtc ||
            projectedExpiry != canonical.PlaintextRetentionExpiresAtUtc ||
            canonical.PlaintextRetentionStartedAtUtc > now ||
            canonical.PlaintextRetentionExpiresAtUtc <= now.AddMilliseconds(config.RawExportSourceClaimSafetyMarginMilliseconds))
        {
            return new CaptureAgentFinalResult(RawExportSourceIngressCodes.BindingInvalid);
        }

        using var lease = capacity.TryAcquire(canonical.ProducerId, canonical.ClaimedPlaintextLength);
        if (lease is null)
        {
            return new CaptureAgentFinalResult(RawExportSourceIngressCodes.CapacityUnavailable);
        }

        var admission = await broker.AdmitAsync(caller, canonical, acceptance, cancellationToken).ConfigureAwait(false);
        if (!admission.BodyRequired)
        {
            return admission.FinalResult
                ?? throw new InvalidOperationException("RAW_EXPORT_BROKER_RESULT_INVALID");
        }

        // This is deliberately the first invocation of openBody. Broker success above is
        // the committed R1 boundary; API parsing and admission never receive the Stream.
        var body = openBody();
        return await bodyPipeline.ProcessAsync(
            canonical,
            admission.Handoff!,
            body,
            cancellationToken).ConfigureAwait(false);
    }
}
