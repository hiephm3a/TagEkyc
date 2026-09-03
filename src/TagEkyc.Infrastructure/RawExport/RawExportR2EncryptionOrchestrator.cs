using TagEkyc.Contracts.RawExport;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed class RawExportR2EncryptionOrchestrator(
    RawExportR2Repository repository,
    IAttemptKeyReservationProvisioningOperation keyProvisioning,
    IAttemptAeadEncryptionOperation encryption,
    IContentCommitmentService contentCommitments,
    IProvisionalObjectWriter objectWriter)
{
    internal async Task<RawExportR2WriterResult> ExecuteAsync(
        RawExportR2EncryptionRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsValidRequest(request))
            return PreCustody(request.AttemptId, RawExportR2WriterDisposition.PreCustodyRejected);

        AttemptKeyProvisioningResult key;
        try
        {
            key = await keyProvisioning.ProvisionAsync(
                new(request.AttemptKeyReservationId, request.AttemptId, request.SourceArtifactId),
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return PreCustody(request.AttemptId, RawExportR2WriterDisposition.PreCustodyRetryable);
        }

        var partition = await PartitionKeyOutcomeAsync(request, key, cancellationToken)
            .ConfigureAwait(false);
        if (partition is not null)
            return partition;

        RawExportR2EncryptionContext? context;
        try
        {
            await repository.SetActorAsync(request.ActorPrincipalId, cancellationToken).ConfigureAwait(false);
            context = await repository.ReadEncryptionContextAsync(
                request.AttemptId,
                request.ExpectedEncryptionAttemptRevision,
                request.ExpectedFence,
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return PreCustody(request.AttemptId, RawExportR2WriterDisposition.PreCustodyRejected);
        }

        if (context is null || !ContextMatches(request, context) || !ProfileIsAdmitted(context))
            return PreCustody(request.AttemptId, RawExportR2WriterDisposition.PreCustodyRejected);

        ProvisionalObjectBeginResult begin;
        try
        {
            _ = RawExportR2FrameCodec.CiphertextLength(
                context.ClaimedPlaintextLength,
                CreateHeader(context, new byte[32]));
            begin = await repository.BeginAsync(
                request.AttemptId,
                request.ExpectedEncryptionAttemptRevision,
                request.ExpectedFence,
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return PreCustody(request.AttemptId, RawExportR2WriterDisposition.PreCustodyRejected);
        }

        if (begin.Mutation.OutcomeCode == "ExistingMatch")
            return ExistingObjectResult(request.AttemptId, begin.Mutation);
        if (begin.Mutation.OutcomeCode != "Created" || begin.Mutation.ObjectState != "Initiated")
            return ObjectResult(request.AttemptId, begin.Mutation, RawExportR2WriterDisposition.CustodyStateConflict);

        RawExportR2FramedCiphertextStream? framed = null;
        try
        {
            framed = new(
                request.PlaintextSource,
                context,
                begin.ObjectBindingDigest,
                encryption,
                contentCommitments);
        }
        catch
        {
            return await RecordNotArmedAsync(request.AttemptId, begin, cancellationToken)
                .ConfigureAwait(false);
        }

        await using (framed.ConfigureAwait(false))
        {
            var operationId = Guid.NewGuid();
            ProvisionalObjectMutationResult armed;
            try
            {
                armed = await repository.ArmAsync(
                    begin.Mutation.ObjectCustodyId,
                    begin.Mutation.StateRevision,
                    operationId,
                    cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                return await RecordNotArmedAsync(request.AttemptId, begin, CancellationToken.None)
                    .ConfigureAwait(false);
            }

            if (armed.OutcomeCode is not ("Armed" or "ExistingMatch") || armed.ObjectState != "PutInFlight")
                return ObjectResult(request.AttemptId, armed, RawExportR2WriterDisposition.CustodyStateConflict);

            ConditionalPutResult put;
            try
            {
                put = await objectWriter.PutIfAbsentAsync(
                    new(
                        new(begin.ProvisionalObjectIdentity, begin.ObjectKey, begin.ObjectBindingDigest),
                        operationId,
                        framed.ExpectedCiphertextLength),
                    framed,
                    cancellationToken).ConfigureAwait(false);
                if (put.Outcome == ConditionalPutOutcome.Created && !framed.Completed)
                    put = new(ConditionalPutOutcome.OutcomeUnknown, null, 0, null, null);
            }
            catch
            {
                put = new(ConditionalPutOutcome.OutcomeUnknown, null, 0, null, null);
            }

            ProvisionalObjectMutationResult recorded;
            try
            {
                recorded = await repository.RecordPutResultAsync(
                    begin.Mutation.ObjectCustodyId,
                    armed.StateRevision,
                    operationId,
                    put,
                    CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                return new(
                    request.AttemptId,
                    begin.Mutation.ObjectCustodyId,
                    "PutInFlight",
                    armed.StateRevision,
                    RawExportR2WriterDisposition.ReconciliationRequired);
            }

            return ObjectResult(
                request.AttemptId,
                recorded,
                recorded.ObjectState == "ObjectPresentPendingVerification"
                    ? RawExportR2WriterDisposition.PendingVerification
                    : RawExportR2WriterDisposition.ReconciliationRequired);
        }
    }

    private async Task<RawExportR2WriterResult?> PartitionKeyOutcomeAsync(
        RawExportR2EncryptionRequest request,
        AttemptKeyProvisioningResult result,
        CancellationToken cancellationToken)
    {
        if (result.AttemptKeyReservationId != request.AttemptKeyReservationId)
            return PreCustody(request.AttemptId, RawExportR2WriterDisposition.PreCustodyOperatorRequired);

        switch (result.Outcome)
        {
            case AttemptKeyProvisioningOutcome.Activated:
            case AttemptKeyProvisioningOutcome.ExistingMatch:
                return null;
            case AttemptKeyProvisioningOutcome.InProgress:
            case AttemptKeyProvisioningOutcome.ProviderUnavailable:
            case AttemptKeyProvisioningOutcome.ProviderOutcomeUnknown:
                return PreCustody(request.AttemptId, RawExportR2WriterDisposition.PreCustodyRetryable);
            case AttemptKeyProvisioningOutcome.Terminated:
                RawExportR2KeyInspection? inspection;
                try
                {
                    inspection = await repository.InspectKeyAsync(
                        request.AttemptKeyReservationId,
                        cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    inspection = null;
                }
                return PreCustody(
                    request.AttemptId,
                    inspection is not null
                    && inspection.AttemptId == request.AttemptId
                    && inspection.PreparationDisposition is "Revoked" or "ReservationAbandoned"
                        ? RawExportR2WriterDisposition.PreCustodyTerminalKey
                        : RawExportR2WriterDisposition.PreCustodyOperatorRequired);
            default:
                return PreCustody(request.AttemptId, RawExportR2WriterDisposition.PreCustodyOperatorRequired);
        }
    }

    private async Task<RawExportR2WriterResult> RecordNotArmedAsync(
        Guid attemptId,
        ProvisionalObjectBeginResult begin,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await repository.RecordNotArmedAsync(
                begin.Mutation.ObjectCustodyId,
                begin.Mutation.StateRevision,
                begin.ObjectBindingDigest,
                cancellationToken).ConfigureAwait(false);
            return ObjectResult(attemptId, result, RawExportR2WriterDisposition.NotArmed);
        }
        catch
        {
            return ObjectResult(
                attemptId,
                begin.Mutation,
                RawExportR2WriterDisposition.CustodyStateConflict);
        }
    }

    private static bool IsValidRequest(RawExportR2EncryptionRequest request) =>
        request.ActorPrincipalId != Guid.Empty
        && request.AttemptKeyReservationId != Guid.Empty
        && request.AttemptId != Guid.Empty
        && request.SourceArtifactId != Guid.Empty
        && request.ExpectedEncryptionAttemptRevision >= 1
        && request.ExpectedFence >= 1
        && request.PlaintextSource is { CanRead: true };

    private static bool ContextMatches(
        RawExportR2EncryptionRequest request,
        RawExportR2EncryptionContext context) =>
        context.AttemptId == request.AttemptId
        && context.SourceArtifactId == request.SourceArtifactId
        && context.AttemptKeyReservationId == request.AttemptKeyReservationId
        && context.EncryptionAttemptRevision == request.ExpectedEncryptionAttemptRevision
        && context.Fence == request.ExpectedFence;

    private static bool ProfileIsAdmitted(RawExportR2EncryptionContext context)
    {
        if (context.EncryptionSuiteId != RawExportR2FrameCodec.FixtureEncryptionSuiteId
            || context.EncryptionFramingVersion != RawExportR2FrameCodec.FixtureFramingVersion
            || context.ChunkSize != RawExportR2FrameCodec.FixtureChunkSize
            || context.NonceStrategyId != RawExportR2FrameCodec.FixtureNonceStrategyId
            || context.RawClass is not ("ChipDg2Portrait" or "LiveSelfieImage")
            || context.ClaimedPlaintextLength < 1)
            return false;

        try
        {
            return RawExportR2FrameCodec.CiphertextLength(
                context.ClaimedPlaintextLength,
                CreateHeader(context, new byte[32])) <= RawExportR2FrameCodec.MaximumCiphertextLength;
        }
        catch
        {
            return false;
        }
    }

    private static RawExportR2Header CreateHeader(
        RawExportR2EncryptionContext context,
        byte[] objectBindingDigest) =>
        new(
            context.AttemptId,
            context.AttemptKeyReservationId,
            context.SourceArtifactId,
            context.ProvisionalObjectIdentity,
            context.EncryptionAttemptFingerprint,
            objectBindingDigest,
            context.EncryptionSuiteId,
            context.EncryptionFramingVersion,
            context.ChunkSize,
            context.NonceStrategyId,
            context.FramingParametersDigest);

    private static RawExportR2WriterResult ExistingObjectResult(
        Guid attemptId,
        ProvisionalObjectMutationResult result) =>
        ObjectResult(
            attemptId,
            result,
            result.ObjectState switch
            {
                "ObjectPresentPendingVerification" or "VerifiedCompleted" =>
                    RawExportR2WriterDisposition.PendingVerification,
                "PutInFlight" or "PutOutcomeUnknown" =>
                    RawExportR2WriterDisposition.ReconciliationRequired,
                _ => RawExportR2WriterDisposition.CustodyStateConflict,
            });

    private static RawExportR2WriterResult ObjectResult(
        Guid attemptId,
        ProvisionalObjectMutationResult result,
        RawExportR2WriterDisposition disposition) =>
        new(attemptId, result.ObjectCustodyId, result.ObjectState, result.StateRevision, disposition);

    private static RawExportR2WriterResult PreCustody(
        Guid attemptId,
        RawExportR2WriterDisposition disposition) =>
        new(attemptId, null, null, null, disposition);
}
