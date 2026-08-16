using System.Security.Cryptography;
using TagEkyc.Application.Ports;
using TagEkyc.Contracts.RawExport;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed class RawExportAssemblySourceReadException(RawExportAssemblySourceDisposition disposition)
    : IOException($"RAW_EXPORT_ASSEMBLY_SOURCE_{disposition.ToString().ToUpperInvariant()}")
{
    internal RawExportAssemblySourceDisposition Disposition { get; } = disposition;
}

internal sealed class RawExportAssemblyOrchestrator(
    RawExportAssemblyRepository repository,
    RawExportAssemblySourceResolver sourceResolver,
    RawExportAssemblyAuthenticationService authentication,
    IC2AssemblyPreparationProvider c2) : IRawExportAssemblyOrchestrator
{
    public async Task<RawExportAssemblyExecutionResult> ExecuteAsync(
        RawExportAssemblyExecutionRequest request,
        CancellationToken cancellationToken)
    {
        if (request.JobId == Guid.Empty || request.AttemptId == Guid.Empty
            || request.ActorPrincipalId == Guid.Empty || request.ExpectedJobRevision < 1 || request.ExpectedFence < 1)
            return Failure(RawExportAssemblyExecutionOutcome.NotFoundOrNotAllowed);

        var committedRecovery = await TryRecoverCommittedAsync(request, cancellationToken).ConfigureAwait(false);
        if (committedRecovery is not null) return committedRecovery;

        var freeze = await repository.FreezeAsync(request, cancellationToken).ConfigureAwait(false);
        if (freeze.Outcome is not ("Frozen" or "ExistingMatch")) return Failure(Map(freeze.Outcome));
        var job = await repository.ReadJobAsync(request, checked((int)(freeze.RowRevision ?? 0)), cancellationToken).ConfigureAwait(false);
        if (job is null || job.Bindings.Count == 0 || job.Bindings.Count != freeze.RowRevision)
            return Failure(RawExportAssemblyExecutionOutcome.SourceUnavailable);

        var resolved = new List<ResolvedAssemblySource>(job.Bindings.Count);
        foreach (var binding in job.Bindings)
        {
            var source = await sourceResolver.ResolveAsync(request, binding.Ordinal, cancellationToken).ConfigureAwait(false);
            if (source is null) return Failure(RawExportAssemblyExecutionOutcome.SourceUnavailable);
            resolved.Add(source);
        }

        var assemblyId = RawExportAssemblyCodec.AssemblyId(request.JobId);
        var header = new RawExportAssemblyHeader(
            assemblyId, job.ClientApplicationId, job.CreatedAtUtc, job.ExportMode, job.JobId, 1,
            job.PermitId, job.PolicyId, job.PolicyVersion, job.PurposeCode,
            job.RecipientClientApplicationId, job.Bindings[0].SubjectRefToken.ToArray(),
            job.VerificationSessionId, resolved.Select(source => source.Descriptor).ToArray());
        var passItems = resolved.Select(Adapt).ToArray();
        byte[]? assemblyDigest = null;
        byte[]? manifestDigest = null;
        byte[]? authenticationValue = null;
        byte[]? assemblyFingerprint = null;
        byte[]? preparationFingerprint = null;
        try
        {
            assemblyDigest = await RawExportAssemblyCodec.ComputeAssemblyDigestAsync(
                header, passItems, cancellationToken).ConfigureAwait(false);
            manifestDigest = RawExportAssemblyCodec.ManifestDigest(
                header, request.AttemptId, request.ExpectedFence, job.JobExpiresAtUtc,
                job.Bindings[0].SubjectRefTokenSchemaVersion,
                job.Bindings[0].SubjectRefTokenKeyId,
                job.Bindings[0].SubjectRefTokenKeyVersion,
                authentication.KeyId,
                authentication.KeyVersion,
                assemblyDigest);
            authenticationValue = await authentication.AuthenticateAsync(manifestDigest, cancellationToken).ConfigureAwait(false);
            assemblyFingerprint = RawExportAssemblyCodec.AssemblyFingerprint(
                assemblyId, request.JobId, request.AttemptId, request.ExpectedFence,
                manifestDigest, authentication.KeyId, authentication.KeyVersion, authenticationValue);
            var preparationId = RawExportAssemblyCodec.C2PreparationId(assemblyId, assemblyFingerprint);
            var headerBytes = RawExportAssemblyCodec.SerializeHeader(header);
            var completeLength = RawExportAssemblyCodec.CompleteLength(headerBytes.Length, header.Items);
            CryptographicOperations.ZeroMemory(headerBytes);
            preparationFingerprint = RawExportAssemblyCodec.PreparationFingerprint(
                preparationId, assemblyId, assemblyFingerprint, manifestDigest, assemblyDigest, completeLength);
            var derivation = new RawExportAssemblyDerivation(
                assemblyId, [], completeLength, assemblyDigest, manifestDigest,
                authenticationValue, assemblyFingerprint, preparationId, preparationFingerprint);

            var registered = await repository.RegisterPreparingAsync(
                preparationId, assemblyId, request, assemblyFingerprint, preparationFingerprint, cancellationToken).ConfigureAwait(false);
            if (registered.Outcome is not ("Created" or "ExistingMatch") || registered.RowRevision is null)
                return Failure(Map(registered.Outcome), assemblyId, preparationId);

            // The second database barrier is deliberately after pass 1 and immediately before provider I/O.
            var secondBarrier = await repository.FreezeAsync(request, cancellationToken).ConfigureAwait(false);
            if (secondBarrier.Outcome is not ("Frozen" or "ExistingMatch"))
                return Failure(Map(secondBarrier.Outcome), assemblyId, preparationId);

            var providerRequest = new C2AssemblyPreparationRequest(
                preparationId, assemblyId, assemblyFingerprint, manifestDigest,
                assemblyDigest, authenticationValue, completeLength);
            var prepared = await PrepareOrRecoverAsync(
                c2,
                providerRequest,
                registered.Outcome == "ExistingMatch",
                (destination, token) => RawExportAssemblyCodec.WriteAssemblyAsync(header, passItems, destination, token),
                cancellationToken).ConfigureAwait(false);
            if (prepared.Outcome is C2AssemblyPrepareOutcome.Unavailable)
                return Failure(RawExportAssemblyExecutionOutcome.ProviderUnavailable, assemblyId, preparationId);
            if (prepared.Outcome is C2AssemblyPrepareOutcome.OutcomeUnknown)
                return Failure(RawExportAssemblyExecutionOutcome.ProviderOutcomeUnknown, assemblyId, preparationId);
            if (prepared.Outcome is C2AssemblyPrepareOutcome.Conflict || prepared.ProviderReceiptDigest is not { Length: 32 })
                return Failure(RawExportAssemblyExecutionOutcome.PreparationConflict, assemblyId, preparationId);

            var pending = await repository.RecordPendingAsync(
                preparationId, registered.RowRevision.Value, prepared.ProviderReceiptDigest, cancellationToken).ConfigureAwait(false);
            if (pending.Outcome is not ("Pending" or "ExistingMatch") || pending.RowRevision is null)
                return Failure(Map(pending.Outcome), assemblyId, preparationId);

            var sealedResult = await repository.SealAsync(
                preparationId, request, pending.RowRevision.Value, derivation,
                authentication.KeyId, authentication.KeyVersion, header.Items, cancellationToken).ConfigureAwait(false);
            if (sealedResult.Outcome is not ("Sealed" or "ExistingMatch") || sealedResult.PreparationRevision is null)
                return Failure(Map(sealedResult.Outcome), assemblyId, preparationId);

            var finalized = await FinalizeOrRecoverAsync(
                c2, preparationId, assemblyFingerprint, cancellationToken).ConfigureAwait(false);
            if (finalized.Outcome is C2AssemblyFinalizeOutcome.Unavailable)
                return Failure(RawExportAssemblyExecutionOutcome.ProviderUnavailable, assemblyId, preparationId);
            if (finalized.Outcome is C2AssemblyFinalizeOutcome.OutcomeUnknown)
                return Failure(RawExportAssemblyExecutionOutcome.ProviderOutcomeUnknown, assemblyId, preparationId);
            if (finalized.Outcome is C2AssemblyFinalizeOutcome.Conflict)
                return Failure(RawExportAssemblyExecutionOutcome.PreparationConflict, assemblyId, preparationId);

            var recorded = await repository.RecordFinalizedAsync(
                preparationId, sealedResult.PreparationRevision.Value, assemblyFingerprint, cancellationToken).ConfigureAwait(false);
            if (recorded.Outcome is not ("Finalized" or "ExistingMatch"))
                return Failure(Map(recorded.Outcome), assemblyId, preparationId);
            return new(
                sealedResult.Outcome == "ExistingMatch" ? RawExportAssemblyExecutionOutcome.ExistingMatch : RawExportAssemblyExecutionOutcome.Sealed,
                assemblyId, preparationId, sealedResult.JobRevision, recorded.RowRevision);
        }
        catch (RawExportAssemblySourceReadException)
        {
            return Failure(RawExportAssemblyExecutionOutcome.VerificationIndeterminate, assemblyId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Failure(RawExportAssemblyExecutionOutcome.ProviderOutcomeUnknown, assemblyId);
        }
        finally
        {
            if (assemblyDigest is not null) CryptographicOperations.ZeroMemory(assemblyDigest);
            if (manifestDigest is not null) CryptographicOperations.ZeroMemory(manifestDigest);
            if (authenticationValue is not null) CryptographicOperations.ZeroMemory(authenticationValue);
            if (assemblyFingerprint is not null) CryptographicOperations.ZeroMemory(assemblyFingerprint);
            if (preparationFingerprint is not null) CryptographicOperations.ZeroMemory(preparationFingerprint);
        }
    }

    private async Task<RawExportAssemblyExecutionResult?> TryRecoverCommittedAsync(
        RawExportAssemblyExecutionRequest request,
        CancellationToken cancellationToken)
    {
        var committed = await repository.ReadCommittedRecoveryContextAsync(request, cancellationToken).ConfigureAwait(false);
        if (committed is null) return null;
        if (committed.Disposition == "Finalized")
            return new(
                RawExportAssemblyExecutionOutcome.ExistingMatch,
                committed.AssemblyId,
                committed.C2PreparationId,
                committed.JobRevision,
                committed.RowRevision);
        if (committed.Disposition != "SealCommitted")
            return Failure(RawExportAssemblyExecutionOutcome.PreparationConflict, committed.AssemblyId, committed.C2PreparationId);

        var finalized = await FinalizeOrRecoverAsync(
            c2,
            committed.C2PreparationId,
            committed.AssemblyFingerprint,
            cancellationToken).ConfigureAwait(false);
        if (finalized.Outcome is C2AssemblyFinalizeOutcome.Unavailable)
            return Failure(RawExportAssemblyExecutionOutcome.ProviderUnavailable, committed.AssemblyId, committed.C2PreparationId);
        if (finalized.Outcome is C2AssemblyFinalizeOutcome.OutcomeUnknown)
            return Failure(RawExportAssemblyExecutionOutcome.ProviderOutcomeUnknown, committed.AssemblyId, committed.C2PreparationId);
        if (finalized.Outcome is C2AssemblyFinalizeOutcome.Conflict)
            return Failure(RawExportAssemblyExecutionOutcome.PreparationConflict, committed.AssemblyId, committed.C2PreparationId);

        var recorded = await repository.RecordFinalizedAsync(
            committed.C2PreparationId,
            committed.RowRevision,
            committed.AssemblyFingerprint,
            cancellationToken).ConfigureAwait(false);
        if (recorded.Outcome is not ("Finalized" or "ExistingMatch") || recorded.RowRevision is null)
            return Failure(Map(recorded.Outcome), committed.AssemblyId, committed.C2PreparationId);
        return new(
            RawExportAssemblyExecutionOutcome.ExistingMatch,
            committed.AssemblyId,
            committed.C2PreparationId,
            committed.JobRevision,
            recorded.RowRevision);
    }

    internal static async Task<C2AssemblyPrepareResult> PrepareOrRecoverAsync(
        IC2AssemblyPreparationProvider c2,
        C2AssemblyPreparationRequest request,
        bool durablePreparingAlreadyExists,
        Func<Stream, CancellationToken, Task> boundedAssemblyWriter,
        CancellationToken cancellationToken)
    {
        if (durablePreparingAlreadyExists)
        {
            var existing = await c2.GetPreparationAsync(request.C2PreparationId, cancellationToken).ConfigureAwait(false);
            if (ExactInspection(existing, request.AssemblyFingerprint, requireFinalized: false))
                return new(C2AssemblyPrepareOutcome.ExistingMatch, existing.ProviderReceiptDigest!.ToArray());
            if (existing.Outcome is not (C2AssemblyInspectionOutcome.Missing or C2AssemblyInspectionOutcome.Preparing))
                return new(MapPrepareInspection(existing), null);
        }

        var prepared = await c2.PrepareAsync(
            request, boundedAssemblyWriter, cancellationToken).ConfigureAwait(false);
        if (prepared.Outcome is not C2AssemblyPrepareOutcome.OutcomeUnknown)
            return prepared;

        var inspection = await c2.GetPreparationAsync(request.C2PreparationId, cancellationToken).ConfigureAwait(false);
        return ExactInspection(inspection, request.AssemblyFingerprint, requireFinalized: false)
            ? new(C2AssemblyPrepareOutcome.ExistingMatch, inspection.ProviderReceiptDigest!.ToArray())
            : new(MapPrepareInspection(inspection), null);
    }

    internal static async Task<C2AssemblyFinalizeResult> FinalizeOrRecoverAsync(
        IC2AssemblyPreparationProvider c2,
        Guid preparationId,
        byte[] assemblyFingerprint,
        CancellationToken cancellationToken)
    {
        var finalized = await c2.FinalizeAsync(
            preparationId, assemblyFingerprint, cancellationToken).ConfigureAwait(false);
        if (finalized.Outcome is not C2AssemblyFinalizeOutcome.OutcomeUnknown)
            return finalized;
        var inspection = await c2.GetPreparationAsync(preparationId, cancellationToken).ConfigureAwait(false);
        return ExactInspection(inspection, assemblyFingerprint, requireFinalized: true)
            ? new(C2AssemblyFinalizeOutcome.ExistingMatch)
            : new(MapFinalizeInspection(inspection));
    }

    internal static async Task<C2AssemblyAbortResult> CompleteAuthorizedAbortAsync(
        RawExportAssemblyRepository repository,
        IC2AssemblyPreparationProvider c2,
        Guid preparationId,
        byte[] abortAuthorizationDigest,
        CancellationToken cancellationToken)
    {
        var recovery = await repository.ReadRecoveryContextAsync(
            preparationId, cancellationToken).ConfigureAwait(false);
        if (recovery is null || recovery.Disposition != "AbortAuthorized"
            || recovery.AbortAuthorizationDigest is not { Length: 32 } durableDigest
            || abortAuthorizationDigest.Length != durableDigest.Length
            || !CryptographicOperations.FixedTimeEquals(abortAuthorizationDigest, durableDigest))
            return new(C2AssemblyAbortOutcome.Conflict);

        var aborted = await c2.AbortAsync(
            preparationId, abortAuthorizationDigest, cancellationToken).ConfigureAwait(false);
        if (aborted.Outcome is not (C2AssemblyAbortOutcome.Aborted or C2AssemblyAbortOutcome.ExistingMatch))
            return aborted;
        var recorded = await repository.RecordAbortedAsync(
            preparationId, recovery.RowRevision, abortAuthorizationDigest, cancellationToken).ConfigureAwait(false);
        return recorded.Outcome is "Aborted" or "ExistingMatch"
            ? aborted
            : new(C2AssemblyAbortOutcome.Conflict);
    }

    private static bool ExactInspection(
        C2AssemblyInspection inspection,
        ReadOnlySpan<byte> expectedFingerprint,
        bool requireFinalized) =>
        (!requireFinalized && inspection.Outcome is C2AssemblyInspectionOutcome.Prepared or C2AssemblyInspectionOutcome.Finalized
            || requireFinalized && inspection.Outcome is C2AssemblyInspectionOutcome.Finalized)
        && inspection.AssemblyFingerprint is { Length: 32 } fingerprint
        && fingerprint.Length == expectedFingerprint.Length
        && CryptographicOperations.FixedTimeEquals(fingerprint, expectedFingerprint)
        && inspection.ProviderReceiptDigest is { Length: 32 };

    private static C2AssemblyPrepareOutcome MapPrepareInspection(C2AssemblyInspection inspection) => inspection.Outcome switch
    {
        C2AssemblyInspectionOutcome.Unavailable => C2AssemblyPrepareOutcome.Unavailable,
        C2AssemblyInspectionOutcome.OutcomeUnknown or C2AssemblyInspectionOutcome.Preparing
            or C2AssemblyInspectionOutcome.Missing => C2AssemblyPrepareOutcome.OutcomeUnknown,
        _ => C2AssemblyPrepareOutcome.Conflict,
    };

    private static C2AssemblyFinalizeOutcome MapFinalizeInspection(C2AssemblyInspection inspection) => inspection.Outcome switch
    {
        C2AssemblyInspectionOutcome.Unavailable => C2AssemblyFinalizeOutcome.Unavailable,
        C2AssemblyInspectionOutcome.OutcomeUnknown or C2AssemblyInspectionOutcome.Preparing
            or C2AssemblyInspectionOutcome.Missing or C2AssemblyInspectionOutcome.Prepared
            => C2AssemblyFinalizeOutcome.OutcomeUnknown,
        _ => C2AssemblyFinalizeOutcome.Conflict,
    };

    private static RawExportAssemblyPlaintextItem Adapt(ResolvedAssemblySource source) =>
        new(source.Descriptor, async (consumer, token) =>
        {
            var disposition = await source.ReadVerifiedChunksAsync(consumer, token).ConfigureAwait(false);
            if (disposition != RawExportAssemblySourceDisposition.Verified)
                throw new RawExportAssemblySourceReadException(disposition);
        });

    private static RawExportAssemblyExecutionOutcome Map(string outcome) => outcome switch
    {
        "NotFoundOrNotAllowed" => RawExportAssemblyExecutionOutcome.NotFoundOrNotAllowed,
        "AuthorityInvalid" => RawExportAssemblyExecutionOutcome.AuthorityInvalid,
        "SourceUnavailable" => RawExportAssemblyExecutionOutcome.SourceUnavailable,
        "BindingConflict" => RawExportAssemblyExecutionOutcome.BindingConflict,
        "AssemblyConflict" => RawExportAssemblyExecutionOutcome.AssemblyConflict,
        "PreparationConflict" => RawExportAssemblyExecutionOutcome.PreparationConflict,
        "LeaseLost" => RawExportAssemblyExecutionOutcome.LeaseLost,
        "Expired" => RawExportAssemblyExecutionOutcome.Expired,
        _ => RawExportAssemblyExecutionOutcome.StateConflict,
    };

    private static RawExportAssemblyExecutionResult Failure(
        RawExportAssemblyExecutionOutcome outcome,
        Guid? assemblyId = null,
        Guid? preparationId = null) =>
        new(outcome, assemblyId, preparationId, null, null);
}
