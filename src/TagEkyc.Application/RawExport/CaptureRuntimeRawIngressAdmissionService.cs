using TagEkyc.Application.Ports;
using TagEkyc.Contracts.RawExport;

namespace TagEkyc.Application.RawExport;

public sealed class CaptureRuntimeRawIngressAdmissionService(
    IRawExportIngressCapacity capacity,
    IRawIngressMetadataBroker broker,
    ICaptureRuntimeRawIngressBodyPipeline bodyPipeline,
    long maximumPlaintextWindowBytesPerStream) : ICaptureRuntimeRawIngressAdmission
{
    public async ValueTask<CaptureRuntimeRawIngressAdmissionResult> AdmitAsync(
        CaptureRuntimeRawIngressAdmissionContext context,
        Stream body,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(body);
        if (maximumPlaintextWindowBytesPerStream <= 0 || context.ClaimedPlaintextLength <= 0)
            throw Invalid();

        // D2 is a closed capability set. Reject an unsupported raw class before
        // capacity, broker metadata, R1 creation, or any request-body read.
        if (context.RawClass is not ("ChipDg2Portrait" or "LiveSelfieImage"))
            return Outcome(CaptureRuntimeRawIngressOutcome.CapabilityUnavailable);

        var windowBytes = Math.Min(context.ClaimedPlaintextLength, maximumPlaintextWindowBytesPerStream);
        using var lease = capacity.TryAcquire(context.CaptureAgentId.ToString("N"), windowBytes);
        if (lease is null)
            return Outcome(CaptureRuntimeRawIngressOutcome.CapacityUnavailable);

        var admission = await broker.AdmitAsync(context, cancellationToken).ConfigureAwait(false);
        return admission switch
        {
            RawIngressBrokerResult.Final final => ProjectFinal(final),
            RawIngressBrokerResult.Handoff handoff when Valid(handoff.Value) =>
                await bodyPipeline.ProcessAsync(context, handoff.Value, body, cancellationToken).ConfigureAwait(false),
            _ => throw Invalid()
        };
    }

    private static CaptureRuntimeRawIngressAdmissionResult ProjectFinal(RawIngressBrokerResult.Final final)
    {
        ArgumentNullException.ThrowIfNull(final.Value);
        var value = final.Value;
        var outcome = value.OutcomeCode switch
        {
            RawExportSourceIngressCodes.BindingInvalid => CaptureRuntimeRawIngressOutcome.BindingInvalid,
            RawExportSourceIngressCodes.NotFoundOrNotAllowed => CaptureRuntimeRawIngressOutcome.NotFoundOrNotAllowed,
            RawExportSourceIngressCodes.TransportProtocolInvalid => CaptureRuntimeRawIngressOutcome.TransportProtocolInvalid,
            RawExportSourceIngressCodes.CapabilityUnavailable => CaptureRuntimeRawIngressOutcome.CapabilityUnavailable,
            RawExportSourceIngressCodes.ArtifactSizeLimitExceeded => CaptureRuntimeRawIngressOutcome.ArtifactSizeLimitExceeded,
            RawExportSourceIngressCodes.PlaintextRetentionInvalid => CaptureRuntimeRawIngressOutcome.PlaintextRetentionInvalid,
            RawExportSourceIngressCodes.CapacityUnavailable => CaptureRuntimeRawIngressOutcome.CapacityUnavailable,
            RawExportSourceIngressCodes.IdempotencyBusy => CaptureRuntimeRawIngressOutcome.IdempotencyBusy,
            RawExportSourceIngressCodes.EvaluationInProgress => CaptureRuntimeRawIngressOutcome.EvaluationInProgress,
            RawExportSourceIngressCodes.ClaimTokenInvalid => CaptureRuntimeRawIngressOutcome.ClaimTokenInvalid,
            RawExportSourceIngressCodes.ClaimRestartRequired => CaptureRuntimeRawIngressOutcome.ClaimRestartRequired,
            RawExportSourceIngressCodes.SourceRetentionNotAuthorized => CaptureRuntimeRawIngressOutcome.SourceRetentionNotAuthorized,
            RawExportSourceIngressCodes.HistoricCommitmentKeyUnavailable => CaptureRuntimeRawIngressOutcome.HistoricCommitmentKeyUnavailable,
            RawExportSourceIngressCodes.FingerprintConflict => CaptureRuntimeRawIngressOutcome.FingerprintConflict,
            RawExportSourceIngressCodes.ReservationBusy => CaptureRuntimeRawIngressOutcome.ReservationBusy,
            RawExportSourceIngressCodes.AlreadyAvailable => CaptureRuntimeRawIngressOutcome.AlreadyAvailable,
            RawExportSourceIngressCodes.ResumePending => CaptureRuntimeRawIngressOutcome.ResumePending,
            RawExportSourceIngressCodes.ContentCommitmentMismatch => CaptureRuntimeRawIngressOutcome.ContentCommitmentMismatch,
            RawExportSourceIngressCodes.RecaptureRequired => CaptureRuntimeRawIngressOutcome.RecaptureRequired,
            _ => throw Invalid()
        };

        var originValid = final.Origin switch
        {
            RawIngressBrokerFinalOrigin.PreAdmission => outcome is not
                (CaptureRuntimeRawIngressOutcome.AlreadyAvailable or CaptureRuntimeRawIngressOutcome.ResumePending
                    or CaptureRuntimeRawIngressOutcome.ContentCommitmentMismatch or CaptureRuntimeRawIngressOutcome.RecaptureRequired),
            RawIngressBrokerFinalOrigin.PublishedReplay => outcome == CaptureRuntimeRawIngressOutcome.AlreadyAvailable,
            RawIngressBrokerFinalOrigin.PreservedCiphertext => outcome == CaptureRuntimeRawIngressOutcome.ResumePending,
            RawIngressBrokerFinalOrigin.PersistedTerminal => outcome is
                CaptureRuntimeRawIngressOutcome.ArtifactSizeLimitExceeded
                or CaptureRuntimeRawIngressOutcome.ContentCommitmentMismatch
                or CaptureRuntimeRawIngressOutcome.RecaptureRequired,
            _ => false
        };
        if (!originValid) throw Invalid();

        if (outcome == CaptureRuntimeRawIngressOutcome.AlreadyAvailable)
        {
            if (value.SourceArtifactId is not { } id || id == Guid.Empty ||
                value.CurrentSourceState != "Available" || value.CurrentDisposition != "Available" ||
                value.RetryNotBeforeUtc is not null) throw Invalid();
        }
        else if (outcome == CaptureRuntimeRawIngressOutcome.EvaluationInProgress)
        {
            if (value.SourceArtifactId is not null || value.CurrentSourceState is not null ||
                value.CurrentDisposition is not null || value.RetryNotBeforeUtc is not { Offset: { } offset } ||
                offset != TimeSpan.Zero) throw Invalid();
        }
        else if (value.SourceArtifactId is not null || value.CurrentSourceState is not null ||
            value.CurrentDisposition is not null || value.RetryNotBeforeUtc is not null) throw Invalid();

        return new(outcome, value.SourceArtifactId, value.CurrentSourceState,
            value.CurrentDisposition, value.RetryNotBeforeUtc);
    }

    private static bool Valid(RawIngressBrokerHandoff handoff) =>
        handoff.SourceArtifactId != Guid.Empty && handoff.AttemptKeyReservationId != Guid.Empty &&
        handoff.AttemptId != Guid.Empty && handoff.ExpectedEncryptionAttemptRevision > 0 &&
        handoff.ExpectedFence > 0 && handoff.CustodyActorPrincipalId != Guid.Empty &&
        handoff.ClientApplicationId != Guid.Empty && handoff.BindingId != Guid.Empty &&
        handoff.RetentionAuthorityId != Guid.Empty && handoff.RetentionAuthorityRevision > 0 &&
        handoff.ExecutionExpiresAtUtc.Offset == TimeSpan.Zero;

    private static CaptureRuntimeRawIngressAdmissionResult Outcome(CaptureRuntimeRawIngressOutcome outcome) =>
        new(outcome, null, null, null, null);

    private static InvalidOperationException Invalid() => new("RAW_INGRESS_ADMISSION_RESULT_INVALID");
}
