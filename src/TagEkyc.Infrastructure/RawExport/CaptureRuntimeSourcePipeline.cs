using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NpgsqlTypes;
using System.Security.Cryptography;
using TagEkyc.Application.Ports;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.Infrastructure.RawExport;

internal enum RetainedPublicationStep { None, Stage, Commit, Publish }
internal sealed record RetainedPublicationProgress(RetainedPublicationStep Step,
    RawSourceRetentionContinuation? Snapshot, bool RetentionDenied = false);
internal enum RetainedContinuationStep
{
    None, Publication, ProveNoProviderStart, RecordExpiredIntent,
    ReconcileIncompleteObject, VerifyIncompleteObject, RecordVerificationFailureIntent,
    MarkTerminalObjectCleanup, CleanupTerminalObject, ConfirmTerminalObjectAbsence,
    RequestTerminalKeyAbandon, ResolveTerminalKeyProvider,
    CleanupTerminalKeyProvider, FinalizeTerminalKeyAbandon,
    SettleTerminalResources, FinalizeIntent,
    ReconcileCleanupResource, SettleCleanupResource, FinalizeCleanup
}
internal sealed record RetainedContinuationProgress(RetainedContinuationStep Step, RawSourceRetentionContinuation? Snapshot);

// Bodyless continuation of already verified ciphertext. R2/verification and
// terminal/resource recovery are separate selections, not a second writer.
// This collaborator is not an Activated-host readiness certificate.
internal sealed class CaptureRuntimeSourcePipeline
{
    private readonly CaptureRuntimeCustodyProviderScopes scopes;
    private readonly IKekProvisioningRecoveryOperation? keyRecovery;
    private readonly Func<CancellationToken,Task> objectAbsenceQuiescence;
    private readonly IKekOperationProvider? verificationKeyProvider;
    private readonly IContentCommitmentService? contentCommitments;
    private readonly DurableKeyCustodyOptions? keyCustodyOptions;

    internal CaptureRuntimeSourcePipeline(CaptureRuntimeCustodyProviderScopes scopes)
        : this(scopes, null, null) { }

    internal CaptureRuntimeSourcePipeline(CaptureRuntimeCustodyProviderScopes scopes,
        IKekProvisioningRecoveryOperation? keyRecovery)
        : this(scopes, keyRecovery, null) { }

    internal CaptureRuntimeSourcePipeline(CaptureRuntimeCustodyProviderScopes scopes,
        IKekProvisioningRecoveryOperation? keyRecovery,
        Func<CancellationToken,Task>? objectAbsenceQuiescence)
        : this(scopes, keyRecovery, objectAbsenceQuiescence, null, null, null) { }

    internal CaptureRuntimeSourcePipeline(CaptureRuntimeCustodyProviderScopes scopes,
        IKekProvisioningRecoveryOperation? keyRecovery,
        Func<CancellationToken,Task>? objectAbsenceQuiescence,
        IKekOperationProvider? verificationKeyProvider,
        IContentCommitmentService? contentCommitments,
        DurableKeyCustodyOptions? keyCustodyOptions)
    {
        this.scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        this.keyRecovery = keyRecovery;
        this.objectAbsenceQuiescence = objectAbsenceQuiescence
            ?? (token => Task.Delay(TimeSpan.FromMilliseconds(25), token));
        if ((verificationKeyProvider is null) != (contentCommitments is null)
            || (verificationKeyProvider is null) != (keyCustodyOptions is null))
            throw new ArgumentException("RAW_INGRESS_R2_VERIFICATION_COMPOSITION_INCOMPLETE");
        this.verificationKeyProvider = verificationKeyProvider;
        this.contentCommitments = contentCommitments;
        this.keyCustodyOptions = keyCustodyOptions;
    }

    internal async Task<RetainedContinuationProgress> AdvanceAsync(Guid source, int timeoutMilliseconds, CancellationToken ct)
    {
        if (source == Guid.Empty) throw Invalid();
        await using var scope = await scopes.OpenReconcilerAsync(ct);
        var reader = Reader(scope);
        var before = await reader.ReadAsync(source, ct);
        if (before is null) return new(RetainedContinuationStep.None, null);
        if (SelectPublicationStep(before) != RetainedPublicationStep.None)
        {
            var publication = await AdvancePublicationAsync(source, scope, reader, before, ct);
            return new(RetainedContinuationStep.Publication, publication.Snapshot);
        }
        if (before.CustodyState == "Available" && before.PublicationState == "Available"
            && before.CleanupDisposition == "Pending" && before.SourcePublicationId is not null
            && before.PublicationRevision is not null)
            return await AdvanceCleanupAsync(scope, reader, before, ct).ConfigureAwait(false);
        if (before.CustodyState != "Reserved" || before.SourcePublicationId is not null
            || before.R2TerminalOutcomeCode is not null) return new(RetainedContinuationStep.None, before);

        var recorder = new RawExportR2TerminalIntentRecorder(scope.Services.GetRequiredService<NpgsqlDataSource>(),
            before.CustodyPrincipalId, timeoutMilliseconds, ct);
        RetainedContinuationStep step;
        var acknowledged = false;
        DateTimeOffset? terminatedAt = null;
        if (before.R2TerminalIntentCode is not null)
        {
            // An armed PUT can remain outcome-unknown after a deterministic
            // input failure. Reconcile its exact object before terminal cleanup.
            if (before.ObjectState is "PutInFlight" or "PutOutcomeUnknown")
            {
                (step, acknowledged) = await AdvanceIncompleteObjectAsync(scope, before, ct)
                    .ConfigureAwait(false);
            }
            else if (!ObjectSettledForTerminal(before))
            {
                (step, acknowledged) = await AdvanceTerminalObjectAsync(scope, before, ct);
            }
            else
            {
            var key = await ReadKeyAsync(scope, before, ct);
            // A terminal intent with an established-but-active key first enters
            // the lifecycle owner. That transaction commits before TI02 is
            // attempted by a later worker pass. No provider call is inferred
            // from a key row and no object operation is authorized here.
            if (ObjectSettledForTerminal(before)
                && key?.PreparationDisposition == "Active")
            {
                acknowledged = await RevokeTerminalKeyAsync(before, ct);
                // A concurrent owner may have advanced the key between the
                // read and locked revoke. Report no selected mutation unless
                // this call observed the exact terminal result.
                step = acknowledged ? RetainedContinuationStep.SettleTerminalResources
                    : RetainedContinuationStep.None;
            }
            else if (ObjectSettledForTerminal(before)
                && key is not null
                && key.PreparationDisposition is "PreparingLive" or "PreparingExpiredAwaitingResolution"
                    or "ProviderOutcomeUnknown" or "ProviderCleanupRequired")
            {
                acknowledged = await RequestTerminalKeyAbandonAsync(before, ct);
                step = acknowledged ? RetainedContinuationStep.RequestTerminalKeyAbandon
                    : RetainedContinuationStep.None;
            }
            else if (ObjectSettledForTerminal(before)
                && key?.PreparationDisposition == "AbandonRequested"
                && key.ProviderOperationState is "CleanedUp" or "AbsenceProven")
            {
                acknowledged = await FinalizeTerminalKeyAbandonAsync(before, ct);
                step = acknowledged ? RetainedContinuationStep.FinalizeTerminalKeyAbandon
                    : RetainedContinuationStep.None;
            }
            else if (ObjectSettledForTerminal(before)
                && key?.PreparationDisposition == "AbandonRequested"
                && key.ProviderOperationState == "CleanupRequired"
                && keyRecovery is not null)
            {
                acknowledged = await CleanupTerminalKeyProviderAsync(scope, before, key, keyRecovery, ct);
                step = acknowledged ? RetainedContinuationStep.CleanupTerminalKeyProvider
                    : RetainedContinuationStep.None;
            }
            else if (ObjectSettledForTerminal(before)
                && key?.PreparationDisposition == "AbandonRequested"
                && key.ProviderOperationState is "Issued" or "ResultObserved"
                && keyRecovery is not null)
            {
                acknowledged = await ResolveTerminalKeyProviderAsync(scope, before, key, keyRecovery, ct);
                step = acknowledged ? RetainedContinuationStep.ResolveTerminalKeyProvider
                    : RetainedContinuationStep.None;
            }
            else
            {
                step = RetainedContinuationStep.FinalizeIntent;
                // TI02 itself proves exact key/object settlement. CleanupPending
                // is not permission to fabricate or repeat a provider side effect.
                var result = await recorder.FinalizeAsync(source, before.AttemptId, before.EncryptionAttemptRevision,
                    before.Fence, before.R2TerminalIntentCode, ct);
                if (result.Outcome == RetainedTerminalFinalizeOutcome.NotFound) throw Invalid();
                acknowledged = result.Outcome is RetainedTerminalFinalizeOutcome.Finalized or RetainedTerminalFinalizeOutcome.ExistingMatch;
            }
            }
        }
        else if (before.ObjectState is "PutInFlight" or "PutOutcomeUnknown")
        {
            (step, acknowledged) = await AdvanceIncompleteObjectAsync(scope, before, ct).ConfigureAwait(false);
        }
        else if (before.ObjectState == "ObjectPresentPendingVerification"
            && verificationKeyProvider is not null
            && contentCommitments is not null
            && keyCustodyOptions is not null)
        {
            (step, acknowledged) = await AdvanceVerificationAsync(scope, recorder, before, ct)
                .ConfigureAwait(false);
        }
        else if (before.R2TerminationDisposition == "TerminatedBeforeStart" && before.ObjectCustodyId is null)
        {
            step = RetainedContinuationStep.RecordExpiredIntent;
            // No application-clock inference of H. TI01's locked clock and
            // immutable horizon decide whether this O20 intent is legal yet.
            var result = await recorder.RecordAsync(source, before.AttemptId, before.EncryptionAttemptRevision,
                before.Fence, "TerminatedBeforeStart", "RECAPTURE_REQUIRED", ct);
            if (result == RawExportR2TerminalIntentOutcome.NotFound) throw Invalid();
            acknowledged = result is RawExportR2TerminalIntentOutcome.Recorded or RawExportR2TerminalIntentOutcome.ExistingMatch;
        }
        else if (before.R2TerminationDisposition is null && before.ObjectCustodyId is null)
        {
            step = RetainedContinuationStep.ProveNoProviderStart;
            // A missing CP08 object is only a candidate. NPS01 proves all four
            // absence predicates; LeaseLive/ProviderEvidencePresent defer.
            var result = await recorder.TerminateBeforeProviderStartAsync(source, before.AttemptId,
                before.EncryptionAttemptRevision, before.Fence, ct);
            if (result.Outcome == RetainedNoStartOutcome.NotFound) throw Invalid();
            acknowledged = result.Outcome is RetainedNoStartOutcome.TerminatedBeforeStart or RetainedNoStartOutcome.ExistingMatch;
            terminatedAt = result.TerminatedAtUtc;
        }
        else return new(RetainedContinuationStep.None, before);

        var after = await reader.ReadAsync(source, ct);
        if (after is null || !SameLineage(before, after)) throw Invalid();
        if (acknowledged && (after.AttemptId != before.AttemptId || after.AttemptKeyReservationId != before.AttemptKeyReservationId
            || after.EncryptionAttemptRevision != before.EncryptionAttemptRevision || after.Fence != before.Fence
            || after.ReservationRevision != before.ReservationRevision
            || (step == RetainedContinuationStep.ProveNoProviderStart
                && (after.R2TerminationDisposition != "TerminatedBeforeStart" || after.R2TerminatedAtUtc != terminatedAt))
            || (step == RetainedContinuationStep.RecordExpiredIntent
                && (after.R2TerminalIntentCode != "RECAPTURE_REQUIRED" || after.R2TerminalIntentDisposition != "TerminatedBeforeStart"
                    || after.R2TerminatedAtUtc != before.R2TerminatedAtUtc))
            || (step == RetainedContinuationStep.ReconcileIncompleteObject
                && (after.R2TerminalIntentCode != before.R2TerminalIntentCode
                    || after.R2TerminalIntentDisposition != before.R2TerminalIntentDisposition
                    || after.R2TerminalIntentAtUtc != before.R2TerminalIntentAtUtc
                    || after.R2TerminationDisposition != before.R2TerminationDisposition
                    || after.R2TerminalOutcomeCode != before.R2TerminalOutcomeCode
                    || after.ObjectCustodyId != before.ObjectCustodyId
                    || after.ObjectStateRevision != before.ObjectStateRevision + 1
                    || after.ObjectState is not ("ObjectPresentPendingVerification" or "NoObjectEstablished" or "ObjectConflict")))
            || (step == RetainedContinuationStep.VerifyIncompleteObject
                && (after.ObjectCustodyId != before.ObjectCustodyId
                    || after.ObjectState != "VerifiedCompleted"
                    || after.ObjectStateRevision != before.ObjectStateRevision + 1
                    || after.R2TerminalIntentCode is not null
                    || after.R2TerminalOutcomeCode is not null))
            || (step == RetainedContinuationStep.RecordVerificationFailureIntent
                && (after.ObjectCustodyId != before.ObjectCustodyId
                    || after.ObjectState != "CleanupPending"
                    || after.ObjectStateRevision != before.ObjectStateRevision + 1
                    || after.R2TerminalIntentCode != "RECAPTURE_REQUIRED"
                    || after.R2TerminalIntentDisposition != "Terminated"
                    || after.R2TerminalOutcomeCode is not null))
            || (step == RetainedContinuationStep.SettleTerminalResources
                && (after.R2TerminalIntentCode != before.R2TerminalIntentCode
                    || after.R2TerminalIntentDisposition != before.R2TerminalIntentDisposition
                    || after.R2TerminalIntentAtUtc != before.R2TerminalIntentAtUtc
                    || after.R2TerminalOutcomeCode is not null
                    || after.ObjectCustodyId != before.ObjectCustodyId
                    || after.ObjectState != before.ObjectState
                    || after.ObjectStateRevision != before.ObjectStateRevision))
            || (step is RetainedContinuationStep.RequestTerminalKeyAbandon
                    or RetainedContinuationStep.ResolveTerminalKeyProvider
                    or RetainedContinuationStep.CleanupTerminalKeyProvider
                    or RetainedContinuationStep.FinalizeTerminalKeyAbandon
                && (after.R2TerminalIntentCode != before.R2TerminalIntentCode
                    || after.R2TerminalIntentDisposition != before.R2TerminalIntentDisposition
                    || after.R2TerminalIntentAtUtc != before.R2TerminalIntentAtUtc
                    || after.R2TerminalOutcomeCode is not null
                    || after.ObjectCustodyId != before.ObjectCustodyId
                    || after.ObjectState != before.ObjectState
                    || after.ObjectStateRevision != before.ObjectStateRevision))
            || (step == RetainedContinuationStep.MarkTerminalObjectCleanup
                && (after.R2TerminalIntentCode != before.R2TerminalIntentCode
                    || after.R2TerminalIntentDisposition != before.R2TerminalIntentDisposition
                    || after.R2TerminalIntentAtUtc != before.R2TerminalIntentAtUtc
                    || after.R2TerminalOutcomeCode is not null
                    || after.ObjectCustodyId != before.ObjectCustodyId
                    || after.ObjectState != "CleanupPending"
                    || after.ObjectStateRevision != before.ObjectStateRevision + 1))
            || (step is RetainedContinuationStep.CleanupTerminalObject
                    or RetainedContinuationStep.ConfirmTerminalObjectAbsence
                && (after.R2TerminalIntentCode != before.R2TerminalIntentCode
                    || after.R2TerminalIntentDisposition != before.R2TerminalIntentDisposition
                    || after.R2TerminalIntentAtUtc != before.R2TerminalIntentAtUtc
                    || after.R2TerminalOutcomeCode is not null
                    || after.ObjectCustodyId != before.ObjectCustodyId
                    || after.ObjectState != "Deleted"
                    || after.ObjectStateRevision != before.ObjectStateRevision + 1))
            || (step == RetainedContinuationStep.FinalizeIntent && after.R2TerminalOutcomeCode != before.R2TerminalIntentCode)))
            throw Invalid();
        return new(step, after);
    }

    private async Task<RetainedContinuationProgress> AdvanceCleanupAsync(
        CaptureRuntimeCustodyProviderScopes.RoleScope reconcilerScope,
        RawSourceRetentionContinuationRepository reader,
        RawSourceRetentionContinuation before,
        CancellationToken ct)
    {
        var command = new ReadNextSourceCleanupItemCommand(before.CustodyPrincipalId,
            before.SourcePublicationId!.Value, before.PublicationRevision!.Value);
        var reconciliation = new RawExportSourceCleanupReconciliationService(
            reconcilerScope.Services.GetRequiredService<TagEkycDbContext>());
        var item = await reconciliation.ReadNextAsync(command, ct).ConfigureAwait(false);
        ValidateCleanupRead(item, before);

        RetainedContinuationStep step;
        if (item.Disposition == SourceCleanupReadDisposition.NoPendingItem)
        {
            await using var lifecycleScope = await scopes.OpenLifecycleAsync(ct).ConfigureAwait(false);
            var result = await new RawExportSourceCleanupService(
                lifecycleScope.Services.GetRequiredService<TagEkycDbContext>()).FinalizeAsync(new(
                    before.CustodyPrincipalId, before.SourcePublicationId.Value,
                    before.PublicationRevision.Value), ct).ConfigureAwait(false);
            ValidateCleanupFinalize(result, before);
            step = result.Disposition is SourceCleanupFinalizeDisposition.Completed
                    or SourceCleanupFinalizeDisposition.ExistingMatch
                ? RetainedContinuationStep.FinalizeCleanup
                : RetainedContinuationStep.None;
        }
        else if (item.Disposition == SourceCleanupReadDisposition.ItemAvailable)
        {
            step = await AdvanceCleanupItemAsync(reconcilerScope, reconciliation, command,
                item, before, ct).ConfigureAwait(false);
        }
        else
        {
            step = RetainedContinuationStep.None;
        }

        var after = await reader.ReadAsync(before.SourceArtifactId, ct).ConfigureAwait(false);
        if (after is null || !SameLineage(before, after)
            || after.CustodyState != "Available" || after.PublicationState != "Available"
            || after.SourcePublicationId != before.SourcePublicationId)
            throw Invalid();
        if (step == RetainedContinuationStep.FinalizeCleanup
            && (after.CleanupDisposition != "Completed" || after.PublicationRevision != 3))
            throw Invalid();
        if (step is RetainedContinuationStep.ReconcileCleanupResource
                or RetainedContinuationStep.SettleCleanupResource
            && after.CleanupDisposition != "Pending")
            throw Invalid();
        return new(step, after);
    }

    private async Task<RetainedContinuationStep> AdvanceCleanupItemAsync(
        CaptureRuntimeCustodyProviderScopes.RoleScope reconcilerScope,
        RawExportSourceCleanupReconciliationService reconciliation,
        ReadNextSourceCleanupItemCommand command,
        SourceCleanupReadResult item,
        RawSourceRetentionContinuation before,
        CancellationToken ct)
    {
        if (item.ResourceKind == "ProvisionalObject")
        {
            var context = await ReadCleanupObjectAsync(reconcilerScope, before.CustodyPrincipalId,
                item.ResourceId!.Value, item.ResourceAttemptId!.Value, ct).ConfigureAwait(false);
            if (context.State == "CleanupPending")
            {
                var result = await reconciliation.ReconcileObjectAsync(command,
                    reconcilerScope.Services.GetRequiredService<IProvisionalObjectReconciler>(), ct)
                    .ConfigureAwait(false);
                ValidateCleanupComplete(result, before, item, allowPending: true);
                return RetainedContinuationStep.ReconcileCleanupResource;
            }
            return await SettleCleanupItemAsync(command, before, item, ct).ConfigureAwait(false);
        }

        if (item.ResourceKind == "AttemptKeyReservation")
        {
            var context = await ReadCleanupKeyAsync(reconcilerScope, before.CustodyPrincipalId,
                item.ResourceId!.Value, item.ResourceAttemptId!.Value, ct).ConfigureAwait(false);
            if (context.PreparationDisposition == "AbandonRequested"
                && context.ProviderOperationState is not ("CleanedUp" or "AbsenceProven")
                && keyRecovery is not null)
            {
                var result = await reconciliation.ReconcileKeyAsync(command, keyRecovery, ct)
                    .ConfigureAwait(false);
                ValidateCleanupComplete(result, before, item, allowPending: true);
                return RetainedContinuationStep.ReconcileCleanupResource;
            }
            return await SettleCleanupItemAsync(command, before, item, ct).ConfigureAwait(false);
        }

        throw Invalid();
    }

    private async Task<RetainedContinuationStep> SettleCleanupItemAsync(
        ReadNextSourceCleanupItemCommand command,
        RawSourceRetentionContinuation before,
        SourceCleanupReadResult item,
        CancellationToken ct)
    {
        await using var lifecycleScope = await scopes.OpenLifecycleAsync(ct).ConfigureAwait(false);
        var service = new RawExportSourceCleanupService(
            lifecycleScope.Services.GetRequiredService<TagEkycDbContext>());
        var result = await service.SettleNextAsync(command,
            lifecycleScope.Services.GetRequiredService<IProvisionalObjectLifecycle>(), ct)
            .ConfigureAwait(false);
        ValidateCleanupComplete(result, before, item, allowPending: true);
        return result.Disposition is SourceCleanupCompleteDisposition.Completed
                or SourceCleanupCompleteDisposition.ExistingMatch
                or SourceCleanupCompleteDisposition.ResourceNotTerminal
            ? RetainedContinuationStep.SettleCleanupResource
            : RetainedContinuationStep.None;
    }

    private static async Task<SourceObjectReconcileContext> ReadCleanupObjectAsync(
        CaptureRuntimeCustodyProviderScopes.RoleScope scope, Guid actor, Guid resource,
        Guid attempt, CancellationToken ct)
    {
        var db = scope.Services.GetRequiredService<TagEkycDbContext>();
        await using var tx = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        try
        {
            var repository = new RawExportSourceFinalizationRepository(db);
            await repository.SetActorLocalAsync(actor, ct).ConfigureAwait(false);
            var current = await repository.ReadObjectReconcileContextAsync(resource, ct).ConfigureAwait(false);
            if (current is null || current.ObjectCustodyId != resource || current.AttemptId != attempt
                || current.ObjectBindingDigest is not { Length: 32 }) throw Invalid();
            await tx.CommitAsync(ct).ConfigureAwait(false);
            return current;
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    private static async Task<SourceKeyRecoveryContext> ReadCleanupKeyAsync(
        CaptureRuntimeCustodyProviderScopes.RoleScope scope, Guid actor, Guid resource,
        Guid attempt, CancellationToken ct)
    {
        var db = scope.Services.GetRequiredService<TagEkycDbContext>();
        await using var tx = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        try
        {
            var repository = new RawExportSourceFinalizationRepository(db);
            await repository.SetActorLocalAsync(actor, ct).ConfigureAwait(false);
            var current = await repository.ReadKeyRecoveryContextAsync(resource, ct).ConfigureAwait(false);
            if (current is null || current.AttemptId != attempt
                || current.AttemptKeyContextFingerprint is not { Length: 32 }) throw Invalid();
            await tx.CommitAsync(ct).ConfigureAwait(false);
            return current;
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    private static void ValidateCleanupRead(SourceCleanupReadResult result,
        RawSourceRetentionContinuation before)
    {
        var itemShape = result.CleanupItemId is not null && result.CleanupItemId != Guid.Empty
            && result.ResourceKind is "ProvisionalObject" or "AttemptKeyReservation"
            && result.ResourceId is not null && result.ResourceId != Guid.Empty
            && result.ResourceAttemptId is not null && result.ResourceAttemptId != Guid.Empty
            && result.CleanupItemRevision is > 0 && result.PlannedResourceRevision is > 0;
        if (result.Disposition == SourceCleanupReadDisposition.ItemAvailable)
        {
            if (result.SourcePublicationId != before.SourcePublicationId
                || result.PublicationRevision != before.PublicationRevision || !itemShape) throw Invalid();
            return;
        }
        if (result.Disposition == SourceCleanupReadDisposition.NoPendingItem)
        {
            if (result.SourcePublicationId != before.SourcePublicationId
                || result.PublicationRevision != before.PublicationRevision
                || result.CleanupItemId is not null || result.ResourceKind is not null
                || result.ResourceId is not null || result.ResourceAttemptId is not null
                || result.CleanupItemRevision is not null || result.PlannedResourceRevision is not null)
                throw Invalid();
            return;
        }
        if (result.SourcePublicationId is not null || result.PublicationRevision is not null
            || result.CleanupItemId is not null || result.ResourceKind is not null
            || result.ResourceId is not null || result.ResourceAttemptId is not null
            || result.CleanupItemRevision is not null || result.PlannedResourceRevision is not null)
            throw Invalid();
    }

    private static void ValidateCleanupComplete(SourceCleanupCompleteResult result,
        RawSourceRetentionContinuation before, SourceCleanupReadResult item, bool allowPending)
    {
        if (result.Disposition is SourceCleanupCompleteDisposition.Completed
                or SourceCleanupCompleteDisposition.ExistingMatch)
        {
            if (result.SourcePublicationId != before.SourcePublicationId
                || result.PublicationRevision != before.PublicationRevision
                || result.CleanupItemId != item.CleanupItemId || result.ResourceKind != item.ResourceKind
                || result.ResourceId != item.ResourceId || result.CompletionDisposition is not ("Deleted" or "Quarantined" or "Revoked" or "ReservationAbandoned")
                || result.CleanupEvidenceDigest is not { Length: 32 }
                || result.CompletedAtUtc is null || result.CleanupItemRevision is not 2)
                throw Invalid();
            return;
        }
        if (allowPending && result.Disposition == SourceCleanupCompleteDisposition.ResourceNotTerminal)
        {
            var empty = result.SourcePublicationId is null && result.PublicationRevision is null
                && result.CleanupItemId is null && result.ResourceKind is null && result.ResourceId is null
                && result.CompletionDisposition is null && result.CleanupEvidenceDigest is null
                && result.CompletedAtUtc is null && result.CleanupItemRevision is null;
            var pending = result.SourcePublicationId == before.SourcePublicationId
                && result.PublicationRevision == before.PublicationRevision
                && result.CleanupItemId == item.CleanupItemId && result.ResourceKind == item.ResourceKind
                && result.ResourceId == item.ResourceId && result.CompletionDisposition is null
                && result.CleanupEvidenceDigest is null && result.CompletedAtUtc is null
                && result.CleanupItemRevision == item.CleanupItemRevision;
            if (!empty && !pending) throw Invalid();
            return;
        }
        if (result.SourcePublicationId is not null || result.PublicationRevision is not null
            || result.CleanupItemId is not null || result.ResourceKind is not null
            || result.ResourceId is not null || result.CompletionDisposition is not null
            || result.CleanupEvidenceDigest is not null || result.CompletedAtUtc is not null
            || result.CleanupItemRevision is not null)
            throw Invalid();
    }

    private static void ValidateCleanupFinalize(SourceCleanupFinalizeResult result,
        RawSourceRetentionContinuation before)
    {
        if (result.Disposition is SourceCleanupFinalizeDisposition.Completed
                or SourceCleanupFinalizeDisposition.ExistingMatch)
        {
            if (result.SourcePublicationId != before.SourcePublicationId
                || result.PublicationRevision is not (2 or 3)
                || result.CleanupDisposition != "Completed"
                || result.CleanupEvidenceDigest is not { Length: 32 } || result.FinalizedAtUtc is null)
                throw Invalid();
            return;
        }
        if (result.Disposition == SourceCleanupFinalizeDisposition.CleanupPending)
        {
            if (result.SourcePublicationId != before.SourcePublicationId
                || result.PublicationRevision != before.PublicationRevision
                || result.CleanupDisposition != "Pending"
                || result.CleanupEvidenceDigest is not null || result.FinalizedAtUtc is not null)
                throw Invalid();
            return;
        }
        if (result.SourcePublicationId is not null || result.PublicationRevision is not null
            || result.CleanupDisposition is not null || result.CleanupEvidenceDigest is not null
            || result.FinalizedAtUtc is not null)
            throw Invalid();
    }

    private async Task<(RetainedContinuationStep Step, bool Acknowledged)> AdvanceVerificationAsync(
        CaptureRuntimeCustodyProviderScopes.RoleScope scope,
        RawExportR2TerminalIntentRecorder recorder,
        RawSourceRetentionContinuation before,
        CancellationToken ct)
    {
        var db = scope.Services.GetRequiredService<TagEkycDbContext>();
        var verifier = new RawExportR2CompletionVerifier(new RawExportR2Repository(db),
            scope.Services.GetRequiredService<IProvisionalObjectReconciler>(),
            new AttemptAeadVerificationOperationService(db, verificationKeyProvider!, keyCustodyOptions!),
            contentCommitments!);
        var result = await verifier.ExecuteAsync(new(before.CustodyPrincipalId, before.AttemptId,
            before.EncryptionAttemptRevision, before.Fence, before.ObjectCustodyId!.Value), ct)
            .ConfigureAwait(false);
        if (result.Disposition == RawExportR2VerificationDisposition.Verified)
            return (RetainedContinuationStep.VerifyIncompleteObject, true);
        if (result.Disposition == RawExportR2VerificationDisposition.VerificationIndeterminateRetry)
            return (RetainedContinuationStep.None, false);

        var intent = await recorder.RecordAsync(before.SourceArtifactId, before.AttemptId,
            before.EncryptionAttemptRevision, before.Fence, "Terminated", "RECAPTURE_REQUIRED", ct)
            .ConfigureAwait(false);
        if (intent == RawExportR2TerminalIntentOutcome.NotFound) throw Invalid();
        return (RetainedContinuationStep.RecordVerificationFailureIntent,
            intent is RawExportR2TerminalIntentOutcome.Recorded or RawExportR2TerminalIntentOutcome.ExistingMatch);
    }

    private static bool NoObjectEstablished(RawSourceRetentionContinuation row) =>
        row.ObjectCustodyId is null
            ? row.ObjectState is null && row.ObjectStateRevision is null
            : row.ObjectState == "NoObjectEstablished" && row.ObjectStateRevision is >= 1;

    private static bool ObjectSettledForTerminal(RawSourceRetentionContinuation row) =>
        NoObjectEstablished(row) || row.ObjectState is "Deleted" or "Quarantined";

    private async Task<(RetainedContinuationStep Step, bool Acknowledged)> AdvanceIncompleteObjectAsync(
        CaptureRuntimeCustodyProviderScopes.RoleScope reconcilerScope,
        RawSourceRetentionContinuation before,
        CancellationToken ct)
    {
        var expected = await ReadR2ObjectAsync(reconcilerScope, before, ct).ConfigureAwait(false);
        if (expected.PutOperationId is null) throw Invalid();
        var reconciler = reconcilerScope.Services.GetRequiredService<IProvisionalObjectReconciler>();
        var locator = new ExactObjectLocator(expected.ProvisionalObjectIdentity,
            expected.ObjectKey, expected.ObjectBindingDigest);
        var inspection = await reconciler.InspectExactAsync(locator, ct).ConfigureAwait(false);
        string resolution;
        long? length = null;
        byte[]? digest = null;
        DateTimeOffset first;
        DateTimeOffset? second = null;

        switch (inspection.Outcome)
        {
            case ExactObjectInspectionOutcome.Present:
                first = CanonicalProviderObservationTime(DateTimeOffset.UtcNow);
                var exactBinding = inspection.StatusCode == 200
                    && Fixed(inspection.ObjectBindingDigest, expected.ObjectBindingDigest)
                    && Fixed(inspection.PutOperationDigest,
                        ProvisionalObjectDigests.PutOperation(expected.PutOperationId.Value));
                if (!exactBinding)
                {
                    resolution = "RecoveredMismatch";
                    break;
                }
                try
                {
                    await using var exact = await reconciler.OpenExactReadAsync(locator, ct).ConfigureAwait(false);
                    (length, digest) = await HashExactCiphertextAsync(exact, ct).ConfigureAwait(false);
                }
                catch (ProvisionalObjectReadException)
                {
                    return (RetainedContinuationStep.None, false);
                }
                resolution = "RecoveredPresent";
                break;
            case ExactObjectInspectionOutcome.PositivelyAbsent:
                first = CanonicalProviderObservationTime(DateTimeOffset.UtcNow);
                await objectAbsenceQuiescence(ct).ConfigureAwait(false);
                var repeated = await reconciler.InspectExactAsync(locator, ct).ConfigureAwait(false);
                if (repeated.Outcome != ExactObjectInspectionOutcome.PositivelyAbsent)
                    return (RetainedContinuationStep.None, false);
                second = CanonicalProviderObservationTime(DateTimeOffset.UtcNow);
                if (second <= first) second = first.AddTicks(10);
                resolution = "PositiveAbsence";
                break;
            default:
                return (RetainedContinuationStep.None, false);
        }

        var evidence = ObjectReconciliationEvidence(expected, resolution, first, second, length, digest);
        try
        {
            var acknowledged = await ExecuteR2ObjectResolutionAsync(reconcilerScope, before, expected,
                resolution, length, digest, first, second, evidence, ct).ConfigureAwait(false);
            return (RetainedContinuationStep.ReconcileIncompleteObject, acknowledged);
        }
        catch (PostgresException exception)
            when (exception.SqlState == "P0001"
                && exception.MessageText == "RAW_EXPORT_PROVISIONAL_OBJECT_ATTEMPT_NOT_ACTIVE")
        {
            return (RetainedContinuationStep.None, false);
        }
    }

    private static async Task<(long Length, byte[] Digest)> HashExactCiphertextAsync(
        ExactObjectRead exact, CancellationToken ct)
    {
        if (exact.CiphertextLength is < 1 or > ProvisionalObjectCustodyOptions.FixedMaximumSinglePartCiphertextBytes)
            throw Invalid();
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[64 * 1024];
        long total = 0;
        while (true)
        {
            var read = await exact.Ciphertext.ReadAsync(buffer, ct).ConfigureAwait(false);
            if (read == 0) break;
            total = checked(total + read);
            if (total > exact.CiphertextLength) throw Invalid();
            hash.AppendData(buffer, 0, read);
        }
        if (total != exact.CiphertextLength) throw Invalid();
        return (total, hash.GetHashAndReset());
    }

    private static byte[] ObjectReconciliationEvidence(
        RawExportR2ObjectContext value,
        string resolution,
        DateTimeOffset first,
        DateTimeOffset? second,
        long? length,
        byte[]? digest) =>
        C1HashCanonical.Compute("tip-88c1-object-reconcile-observation-v1",
            new C1HashCanonical.Scalar(Convert.ToHexString(value.ObjectBindingDigest).ToLowerInvariant()),
            new C1HashCanonical.Scalar(value.PutOperationId!.Value.ToString("N")),
            new C1HashCanonical.Scalar(resolution),
            new C1HashCanonical.Scalar(first.UtcTicks.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new C1HashCanonical.Scalar(second?.UtcTicks.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "none"),
            new C1HashCanonical.Scalar(length?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "none"),
            new C1HashCanonical.Scalar(digest is null ? "none" : Convert.ToHexString(digest).ToLowerInvariant()));

    private static bool Fixed(byte[]? left, byte[] right) =>
        left is { Length: 32 } && right.Length == 32
        && CryptographicOperations.FixedTimeEquals(left, right);

    private static async Task<RawExportR2ObjectContext> ReadR2ObjectAsync(
        CaptureRuntimeCustodyProviderScopes.RoleScope scope,
        RawSourceRetentionContinuation before,
        CancellationToken ct)
    {
        var db = scope.Services.GetRequiredService<TagEkycDbContext>();
        await using var tx = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        try
        {
            var repository = new RawExportR2Repository(db);
            await repository.SetActorAsync(before.CustodyPrincipalId, ct).ConfigureAwait(false);
            var current = await repository.ReadObjectContextAsync(before.ObjectCustodyId!.Value, ct).ConfigureAwait(false);
            if (current is null || !SameR2Object(current, before)) throw Invalid();
            await tx.CommitAsync(ct).ConfigureAwait(false);
            return current;
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    private static async Task<bool> ExecuteR2ObjectResolutionAsync(
        CaptureRuntimeCustodyProviderScopes.RoleScope scope,
        RawSourceRetentionContinuation before,
        RawExportR2ObjectContext expected,
        string resolution,
        long? length,
        byte[]? digest,
        DateTimeOffset first,
        DateTimeOffset? second,
        byte[] evidence,
        CancellationToken ct)
    {
        var db = scope.Services.GetRequiredService<TagEkycDbContext>();
        await using var tx = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        try
        {
            var repository = new RawExportR2Repository(db);
            await repository.SetActorAsync(before.CustodyPrincipalId, ct).ConfigureAwait(false);
            var current = await repository.ReadObjectContextAsync(expected.ObjectCustodyId, ct).ConfigureAwait(false);
            if (current is null || !SameR2Object(current, expected))
            {
                await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                return false;
            }
            var result = await repository.ResolvePutOutcomeAsync(expected.ObjectCustodyId,
                expected.StateRevision, resolution, length, digest, first, second, evidence, ct)
                .ConfigureAwait(false);
            var target = resolution switch
            {
                "RecoveredPresent" => "ObjectPresentPendingVerification",
                "PositiveAbsence" => "NoObjectEstablished",
                "RecoveredMismatch" => "ObjectConflict",
                _ => throw Invalid(),
            };
            if (result.OutcomeCode is not ("RecoveredPresent" or "PositiveAbsence" or "ConditionalConflict" or "ExistingMatch")
                || result.ObjectCustodyId != expected.ObjectCustodyId || result.ObjectState != target
                || result.StateRevision != expected.StateRevision + 1)
            {
                await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                return false;
            }
            await tx.CommitAsync(ct).ConfigureAwait(false);
            return true;
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    private static bool SameR2Object(RawExportR2ObjectContext value,
        RawSourceRetentionContinuation before) =>
        value.ObjectCustodyId == before.ObjectCustodyId
        && value.AttemptId == before.AttemptId
        && value.AttemptKeyReservationId == before.AttemptKeyReservationId
        && value.SourceArtifactId == before.SourceArtifactId
        && value.State == before.ObjectState
        && value.StateRevision == before.ObjectStateRevision
        && value.ObjectBindingDigest is { Length: 32 };

    private static bool SameR2Object(RawExportR2ObjectContext left,
        RawExportR2ObjectContext right) =>
        left.ObjectCustodyId == right.ObjectCustodyId
        && left.AttemptId == right.AttemptId
        && left.AttemptKeyReservationId == right.AttemptKeyReservationId
        && left.SourceArtifactId == right.SourceArtifactId
        && left.ProvisionalObjectIdentity == right.ProvisionalObjectIdentity
        && left.EncryptionAttemptRevision == right.EncryptionAttemptRevision
        && left.AttemptFence == right.AttemptFence
        && left.EncryptionAttemptFingerprint.AsSpan().SequenceEqual(right.EncryptionAttemptFingerprint)
        && left.ObjectKey == right.ObjectKey
        && left.ObjectBindingDigest.AsSpan().SequenceEqual(right.ObjectBindingDigest)
        && left.State == right.State
        && left.StateRevision == right.StateRevision
        && left.PutOperationId == right.PutOperationId
        && left.CiphertextLength == right.CiphertextLength
        && NullableBytesEqual(left.CiphertextDigest, right.CiphertextDigest)
        && NullableBytesEqual(left.ProviderReceiptDigest, right.ProviderReceiptDigest)
        && NullableBytesEqual(left.VerificationEvidenceDigest, right.VerificationEvidenceDigest);

    private async Task<(RetainedContinuationStep Step, bool Acknowledged)> AdvanceTerminalObjectAsync(
        CaptureRuntimeCustodyProviderScopes.RoleScope reconcilerScope,
        RawSourceRetentionContinuation before, CancellationToken ct)
    {
        if (before.ObjectCustodyId is null || before.ObjectStateRevision is null) throw Invalid();
        if (before.ObjectState == "ObjectPresentPendingVerification")
        {
            var acknowledged = await ExecuteReconcilerObjectMutationAsync(reconcilerScope, before,
                async (repository, current, token) =>
                {
                    await repository.MarkObjectCleanupRequiredAsync(current, "VerificationFailed", token)
                        .ConfigureAwait(false);
                    return true;
                }, ct).ConfigureAwait(false);
            return (RetainedContinuationStep.MarkTerminalObjectCleanup, acknowledged);
        }
        if (before.ObjectState != "CleanupPending") return (RetainedContinuationStep.None, false);

        var expected = await ReadReconcileObjectAsync(reconcilerScope, before, ct).ConfigureAwait(false);
        var reconciler = reconcilerScope.Services.GetRequiredService<IProvisionalObjectReconciler>();
        var locator = new ExactObjectLocator(expected.ProvisionalObjectIdentity,
            expected.ObjectKey, expected.ObjectBindingDigest);
        var firstInspection = await reconciler.InspectExactAsync(locator, ct).ConfigureAwait(false);
        if (firstInspection.Outcome == ExactObjectInspectionOutcome.PositivelyAbsent)
        {
            var first = CanonicalProviderObservationTime(DateTimeOffset.UtcNow);
            await objectAbsenceQuiescence(ct).ConfigureAwait(false);
            var secondInspection = await reconciler.InspectExactAsync(locator, ct).ConfigureAwait(false);
            if (secondInspection.Outcome != ExactObjectInspectionOutcome.PositivelyAbsent)
                return (RetainedContinuationStep.None, false);
            var second = CanonicalProviderObservationTime(DateTimeOffset.UtcNow);
            if (second <= first) second = first.AddTicks(10);
            var evidence = C1HashCanonical.Compute("tip-88c1-object-delete-absence-evidence-v1",
                new C1HashCanonical.Scalar(Convert.ToHexString(expected.ObjectBindingDigest).ToLowerInvariant()),
                new C1HashCanonical.Scalar(expected.StateRevision.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new C1HashCanonical.Scalar(first.UtcTicks.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new C1HashCanonical.Scalar(second.UtcTicks.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new C1HashCanonical.Scalar("PositiveAbsenceConfirmed"));
            var acknowledged = await ExecuteReconcilerObjectMutationAsync(reconcilerScope, before,
                async (repository, current, token) =>
                {
                    await repository.RecordObjectAbsenceConfirmedAsync(current, first, second, evidence, token)
                        .ConfigureAwait(false);
                    return true;
                }, ct).ConfigureAwait(false);
            return (RetainedContinuationStep.ConfirmTerminalObjectAbsence, acknowledged);
        }
        if (firstInspection.Outcome != ExactObjectInspectionOutcome.Present)
            return (RetainedContinuationStep.None, false);

        await using var lifecycleScope = await scopes.OpenLifecycleAsync(ct).ConfigureAwait(false);
        var lifecycleContext = await ReadLifecycleObjectAsync(lifecycleScope, before, ct).ConfigureAwait(false);
        var lifecycle = lifecycleScope.Services.GetRequiredService<IProvisionalObjectLifecycle>();
        var deletion = await lifecycle.DeleteExactAsync(new(lifecycleContext.ProvisionalObjectIdentity,
            lifecycleContext.ObjectKey, lifecycleContext.ObjectBindingDigest), ct).ConfigureAwait(false);
        if (deletion.Outcome != ExactDeleteOutcome.DeletedAcknowledged || deletion.StatusCode != 204)
            return (RetainedContinuationStep.None, false);
        var deleted = await ExecuteLifecycleObjectMutationAsync(lifecycleScope, before, lifecycleContext,
            async (repository, current, token) =>
            {
                await repository.RecordObjectDeleteAcknowledgedAsync(current, token).ConfigureAwait(false);
                return true;
            }, ct).ConfigureAwait(false);
        return (RetainedContinuationStep.CleanupTerminalObject, deleted);
    }

    private static DateTimeOffset CanonicalProviderObservationTime(DateTimeOffset value) =>
        new(value.UtcTicks - value.UtcTicks % 10, TimeSpan.Zero);

    private static async Task<SourceObjectReconcileContext> ReadReconcileObjectAsync(
        CaptureRuntimeCustodyProviderScopes.RoleScope scope,
        RawSourceRetentionContinuation before, CancellationToken ct)
    {
        var db = scope.Services.GetRequiredService<TagEkycDbContext>();
        await using var tx = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        try
        {
            var repository = new RawExportSourceFinalizationRepository(db);
            await repository.SetActorLocalAsync(before.CustodyPrincipalId, ct).ConfigureAwait(false);
            var current = await repository.ReadObjectReconcileContextAsync(
                before.ObjectCustodyId!.Value, ct).ConfigureAwait(false);
            if (current is null || !SameReconcileObject(current, before)) throw Invalid();
            await tx.CommitAsync(ct).ConfigureAwait(false);
            return current;
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    private static async Task<SourceObjectLifecycleContext> ReadLifecycleObjectAsync(
        CaptureRuntimeCustodyProviderScopes.RoleScope scope,
        RawSourceRetentionContinuation before, CancellationToken ct)
    {
        var db = scope.Services.GetRequiredService<TagEkycDbContext>();
        await using var tx = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        try
        {
            var repository = new RawExportSourceFinalizationRepository(db);
            await repository.SetActorLocalAsync(before.CustodyPrincipalId, ct).ConfigureAwait(false);
            var current = await repository.ReadObjectLifecycleContextAsync(
                before.ObjectCustodyId!.Value, ct).ConfigureAwait(false);
            if (current is null || !SameLifecycleObject(current, before)) throw Invalid();
            await tx.CommitAsync(ct).ConfigureAwait(false);
            return current;
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    private static async Task<bool> ExecuteReconcilerObjectMutationAsync(
        CaptureRuntimeCustodyProviderScopes.RoleScope scope, RawSourceRetentionContinuation before,
        Func<RawExportSourceFinalizationRepository, SourceObjectReconcileContext, CancellationToken, Task<bool>> operation,
        CancellationToken ct)
    {
        var db = scope.Services.GetRequiredService<TagEkycDbContext>();
        await using var tx = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        try
        {
            var repository = new RawExportSourceFinalizationRepository(db);
            await repository.SetActorLocalAsync(before.CustodyPrincipalId, ct).ConfigureAwait(false);
            var current = await repository.ReadObjectReconcileContextAsync(
                before.ObjectCustodyId!.Value, ct).ConfigureAwait(false);
            if (current is null || !SameReconcileObject(current, before)
                || !await operation(repository, current, ct).ConfigureAwait(false))
            {
                await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                return false;
            }
            await tx.CommitAsync(ct).ConfigureAwait(false);
            return true;
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    private static async Task<bool> ExecuteLifecycleObjectMutationAsync(
        CaptureRuntimeCustodyProviderScopes.RoleScope scope, RawSourceRetentionContinuation before,
        SourceObjectLifecycleContext expected,
        Func<RawExportSourceFinalizationRepository, SourceObjectLifecycleContext, CancellationToken, Task<bool>> operation,
        CancellationToken ct)
    {
        var db = scope.Services.GetRequiredService<TagEkycDbContext>();
        await using var tx = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        try
        {
            var repository = new RawExportSourceFinalizationRepository(db);
            await repository.SetActorLocalAsync(before.CustodyPrincipalId, ct).ConfigureAwait(false);
            var current = await repository.ReadObjectLifecycleContextAsync(
                before.ObjectCustodyId!.Value, ct).ConfigureAwait(false);
            if (current is null || !SameLifecycleObject(current, expected)
                || !await operation(repository, current, ct).ConfigureAwait(false))
            {
                await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                return false;
            }
            await tx.CommitAsync(ct).ConfigureAwait(false);
            return true;
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    private static bool SameReconcileObject(SourceObjectReconcileContext value,
        RawSourceRetentionContinuation before) =>
        value.ObjectCustodyId == before.ObjectCustodyId
        && value.AttemptId == before.AttemptId
        && value.AttemptKeyReservationId == before.AttemptKeyReservationId
        && value.SourceArtifactId == before.SourceArtifactId
        && value.State == before.ObjectState
        && value.StateRevision == before.ObjectStateRevision
        && value.ObjectBindingDigest is { Length: 32 };

    private static bool SameLifecycleObject(SourceObjectLifecycleContext value,
        RawSourceRetentionContinuation before) =>
        value.ObjectCustodyId == before.ObjectCustodyId
        && value.AttemptId == before.AttemptId
        && value.State == before.ObjectState
        && value.StateRevision == before.ObjectStateRevision
        && value.ObjectBindingDigest is { Length: 32 };

    private static bool SameLifecycleObject(SourceObjectLifecycleContext left,
        SourceObjectLifecycleContext right) =>
        left.ObjectCustodyId == right.ObjectCustodyId
        && left.AttemptId == right.AttemptId
        && left.ProvisionalObjectIdentity == right.ProvisionalObjectIdentity
        && left.ObjectKey == right.ObjectKey
        && left.ObjectBindingDigest.AsSpan().SequenceEqual(right.ObjectBindingDigest)
        && left.State == right.State
        && left.StateRevision == right.StateRevision
        && left.PutOperationId == right.PutOperationId
        && left.CleanupReasonCode == right.CleanupReasonCode
        && NullableBytesEqual(left.CleanupEvidenceDigest, right.CleanupEvidenceDigest)
        && left.CleanupRequestedAtUtc == right.CleanupRequestedAtUtc
        && NullableBytesEqual(left.ProviderReceiptDigest, right.ProviderReceiptDigest);

    private static bool NullableBytesEqual(byte[]? left, byte[]? right) =>
        left is null ? right is null : right is not null && left.AsSpan().SequenceEqual(right);

    private static async Task<SourceKeyRecoveryContext?> ReadKeyAsync(CaptureRuntimeCustodyProviderScopes.RoleScope scope,
        RawSourceRetentionContinuation before, CancellationToken ct)
    {
        var db = scope.Services.GetRequiredService<TagEkycDbContext>();
        await db.Database.OpenConnectionAsync(ct).ConfigureAwait(false);
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
            var repository = new RawExportSourceFinalizationRepository(db);
            await repository.SetActorLocalAsync(before.CustodyPrincipalId, ct).ConfigureAwait(false);
            var key = await repository.ReadKeyRecoveryContextAsync(before.AttemptKeyReservationId, ct).ConfigureAwait(false);
            await tx.CommitAsync(ct).ConfigureAwait(false);
            if (key is null) return null;
            if (key.AttemptId != before.AttemptId || key.AttemptKeyContextFingerprint is not { Length: 32 }) throw Invalid();
            return key;
        }
        finally
        {
            await db.Database.CloseConnectionAsync().ConfigureAwait(false);
        }
    }

    private async Task<bool> ResolveTerminalKeyProviderAsync(CaptureRuntimeCustodyProviderScopes.RoleScope scope,
        RawSourceRetentionContinuation before, SourceKeyRecoveryContext key,
        IKekProvisioningRecoveryOperation recovery, CancellationToken ct)
    {
        if (key.ProviderOperationToken is null || key.PreparationId is null || key.ProviderOperationId is null)
            throw Invalid();
        var resolution = await recovery.ResolveProvisioningOperationAsync(key.ProviderOperationToken.Value,
            key.AttemptKeyContextFingerprint, ct).ConfigureAwait(false);
        return resolution switch
        {
            KekProvisioningResolution.NoProviderResult absence => await ExecuteReconcilerKeyMutationAsync(scope,
                before, key, (repository, token) => repository.ResolveKeyProviderOutcomeAsync(
                    before.AttemptKeyReservationId, key, "NoProviderResult", null, null,
                    absence.AbsenceProofReceipt, token), new[] { "ResolvedNoResult" }, ct),
            KekProvisioningResolution.WrappedResultRecovered recovered => await ExecuteReconcilerKeyMutationAsync(scope,
                before, key, (repository, token) => repository.MarkKeyCleanupRequiredAsync(
                    before.AttemptKeyReservationId, key, recovered.Material.ProviderResourceReference, token),
                new[] { "CleanupRequired", "ExistingMatch" }, ct),
            KekProvisioningResolution.ProviderResourceCleanupRequired cleanup => await ExecuteReconcilerKeyMutationAsync(scope,
                before, key, (repository, token) => repository.MarkKeyCleanupRequiredAsync(
                    before.AttemptKeyReservationId, key, cleanup.CleanupReference, token),
                new[] { "CleanupRequired", "ExistingMatch" }, ct),
            _ => false,
        };
    }

    private async Task<bool> CleanupTerminalKeyProviderAsync(CaptureRuntimeCustodyProviderScopes.RoleScope scope,
        RawSourceRetentionContinuation before, SourceKeyRecoveryContext key,
        IKekProvisioningRecoveryOperation recovery, CancellationToken ct)
    {
        if (key.ProviderCleanupReference is not { Length: > 0 } cleanupReference
            || key.ProviderOperationToken is null || key.PreparationId is null || key.ProviderOperationId is null)
            throw Invalid();
        var result = await recovery.CleanupProvisioningOperationAsync(cleanupReference,
            key.AttemptKeyContextFingerprint, ct).ConfigureAwait(false);
        return result switch
        {
            KekProvisioningCleanupResult.Cleaned cleaned => await ExecuteReconcilerKeyMutationAsync(scope,
                before, key, (repository, token) => repository.AcknowledgeKeyCleanupAsync(
                    before.AttemptKeyReservationId, key, "Cleaned", cleanupReference, cleaned.Receipt, token),
                new[] { "Acknowledged" }, ct),
            KekProvisioningCleanupResult.AlreadyAbsent absent => await ExecuteReconcilerKeyMutationAsync(scope,
                before, key, (repository, token) => repository.AcknowledgeKeyCleanupAsync(
                    before.AttemptKeyReservationId, key, "AlreadyAbsent", cleanupReference, absent.Receipt, token),
                new[] { "Acknowledged" }, ct),
            KekProvisioningCleanupResult.CleanupUnavailable => await ObserveTerminalKeyCleanupAsync(scope,
                before, key, "CleanupUnavailable", cleanupReference, ct),
            KekProvisioningCleanupResult.CleanupOutcomeUnknown => await ObserveTerminalKeyCleanupAsync(scope,
                before, key, "CleanupOutcomeUnknown", cleanupReference, ct),
            KekProvisioningCleanupResult.CleanupFailed => await ObserveTerminalKeyCleanupAsync(scope,
                before, key, "CleanupFailed", cleanupReference, ct),
            _ => false,
        };
    }

    private Task<bool> ObserveTerminalKeyCleanupAsync(CaptureRuntimeCustodyProviderScopes.RoleScope scope,
        RawSourceRetentionContinuation before, SourceKeyRecoveryContext key, string kind,
        string cleanupReference, CancellationToken ct) =>
        ExecuteReconcilerKeyMutationAsync(scope, before, key,
            (repository, token) => repository.ObserveKeyCleanupAsync(before.AttemptKeyReservationId,
                key, kind, cleanupReference, null, token),
            new[] { "CleanupObserved", "DeadlineIntervention" }, ct);

    private static async Task<bool> ExecuteReconcilerKeyMutationAsync(
        CaptureRuntimeCustodyProviderScopes.RoleScope scope, RawSourceRetentionContinuation before,
        SourceKeyRecoveryContext expected, Func<RawExportSourceFinalizationRepository, CancellationToken, Task<string>> operation,
        IReadOnlyCollection<string> successfulResults, CancellationToken ct)
    {
        var db = scope.Services.GetRequiredService<TagEkycDbContext>();
        await using var tx = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        try
        {
            var repository = new RawExportSourceFinalizationRepository(db);
            await repository.SetActorLocalAsync(before.CustodyPrincipalId, ct).ConfigureAwait(false);
            var current = await repository.ReadKeyRecoveryContextAsync(before.AttemptKeyReservationId, ct).ConfigureAwait(false);
            if (current is null || !SameKeyContext(current, expected))
            {
                await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                return false;
            }
            var result = await operation(repository, ct).ConfigureAwait(false);
            if (!successfulResults.Contains(result, StringComparer.Ordinal))
            {
                await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                return false;
            }
            await tx.CommitAsync(ct).ConfigureAwait(false);
            return true;
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    private static bool SameKeyContext(SourceKeyRecoveryContext left, SourceKeyRecoveryContext right) =>
        left.PreparationDisposition == right.PreparationDisposition
        && left.AttemptId == right.AttemptId
        && left.AttemptKeyContextFingerprint.AsSpan().SequenceEqual(right.AttemptKeyContextFingerprint)
        && left.PreparationId == right.PreparationId
        && left.PreparationFence == right.PreparationFence
        && left.ProviderOperationToken?.Value == right.ProviderOperationToken?.Value
        && left.ProviderOperationId == right.ProviderOperationId
        && left.ProviderOperationState == right.ProviderOperationState
        && left.ProviderCleanupReference == right.ProviderCleanupReference;

    private Task<bool> RequestTerminalKeyAbandonAsync(RawSourceRetentionContinuation before, CancellationToken ct) =>
        ExecuteLifecycleKeyMutationAsync(before, "raw_export_request_abandon_attempt_key_reservation",
            before.R2TerminalIntentCode, new[] { "AbandonRequested", "AlreadyRequested" }, ct);

    private Task<bool> FinalizeTerminalKeyAbandonAsync(RawSourceRetentionContinuation before, CancellationToken ct) =>
        ExecuteLifecycleKeyMutationAsync(before, "raw_export_finalize_abandon_attempt_key_reservation",
            null, new[] { "ReservationAbandoned", "AlreadyAbandoned" }, ct);

    private async Task<bool> ExecuteLifecycleKeyMutationAsync(RawSourceRetentionContinuation before,
        string function, string? reason, IReadOnlyCollection<string> successfulResults, CancellationToken ct)
    {
        await using var scope = await scopes.OpenLifecycleAsync(ct).ConfigureAwait(false);
        var db = scope.Services.GetRequiredService<TagEkycDbContext>();
        await db.Database.OpenConnectionAsync(ct).ConfigureAwait(false);
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
            try
            {
                var repository = new RawExportSourceFinalizationRepository(db);
                await repository.SetActorLocalAsync(before.CustodyPrincipalId, ct).ConfigureAwait(false);
                var connection = db.Database.GetDbConnection() as NpgsqlConnection
                    ?? throw new InvalidOperationException("A3_CONTINUATION_POSTGRES_REQUIRED");
                await using var command = new NpgsqlCommand(reason is null
                    ? $"SELECT tagekyc.{function}(@id)"
                    : $"SELECT tagekyc.{function}(@id,@reason)",
                    connection, tx.GetDbTransaction() as NpgsqlTransaction);
                command.Parameters.AddWithValue("id", NpgsqlDbType.Uuid, before.AttemptKeyReservationId);
                if (reason is not null) command.Parameters.AddWithValue("reason", NpgsqlDbType.Text, reason);
                var result = await command.ExecuteScalarAsync(ct).ConfigureAwait(false) as string
                    ?? throw new InvalidOperationException("A3_CONTINUATION_EMPTY_KEY_RESULT");
                if (!successfulResults.Contains(result, StringComparer.Ordinal))
                {
                    await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                    return false;
                }
                await tx.CommitAsync(ct).ConfigureAwait(false);
                return true;
            }
            catch
            {
                await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                throw;
            }
        }
        finally
        {
            await db.Database.CloseConnectionAsync().ConfigureAwait(false);
        }
    }

    private async Task<bool> RevokeTerminalKeyAsync(RawSourceRetentionContinuation before, CancellationToken ct)
    {
        await using var scope = await scopes.OpenLifecycleAsync(ct).ConfigureAwait(false);
        var db = scope.Services.GetRequiredService<TagEkycDbContext>();
        await db.Database.OpenConnectionAsync(ct).ConfigureAwait(false);
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
            try
            {
                var repository = new RawExportSourceFinalizationRepository(db);
                await repository.SetActorLocalAsync(before.CustodyPrincipalId, ct).ConfigureAwait(false);
                // This is settlement of the current terminal attempt, not R6
                // supersession. Bind the already-durable terminal cause into
                // the immutable key-revocation event and evidence digest.
                var connection = db.Database.GetDbConnection() as NpgsqlConnection
                    ?? throw new InvalidOperationException("A3_CONTINUATION_POSTGRES_REQUIRED");
                await using var command = new NpgsqlCommand(
                    "SELECT tagekyc.raw_export_revoke_attempt_key_reservation(@id,@reason)",
                    connection, tx.GetDbTransaction() as NpgsqlTransaction);
                command.Parameters.AddWithValue("id", NpgsqlDbType.Uuid, before.AttemptKeyReservationId);
                command.Parameters.AddWithValue("reason", NpgsqlDbType.Text,
                    before.R2TerminalIntentCode ?? throw Invalid());
                var result = await command.ExecuteScalarAsync(ct).ConfigureAwait(false) as string
                    ?? throw new InvalidOperationException("A3_CONTINUATION_EMPTY_KEY_RESULT");
                if (result is not ("Revoked" or "AlreadyRevoked"))
                {
                    await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                    return false;
                }
                await tx.CommitAsync(ct).ConfigureAwait(false);
                return true;
            }
            catch
            {
                await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                throw;
            }
        }
        finally
        {
            await db.Database.CloseConnectionAsync().ConfigureAwait(false);
        }
    }

    internal async Task<IReadOnlyList<Guid>> ScanAsync(Guid? cursor, CancellationToken ct)
    {
        await using var scope = await scopes.OpenReconcilerAsync(ct);
        return await Reader(scope).ScanAsync(cursor, 100, ct);
    }

    // One selected existing stage and one post-stage readback, never revision
    // arithmetic or an internal retry. A later poll selects from fresh CP08.
    internal async Task<RetainedPublicationProgress> AdvancePublicationAsync(Guid source, CancellationToken ct)
    {
        if (source == Guid.Empty) throw Invalid();
        await using var scope = await scopes.OpenReconcilerAsync(ct);
        var reader = Reader(scope);
        var before = await reader.ReadAsync(source, ct);
        if (before is null) return new(RetainedPublicationStep.None, null);
        return await AdvancePublicationAsync(source, scope, reader, before, ct);
    }

    private static async Task<RetainedPublicationProgress> AdvancePublicationAsync(Guid source,
        CaptureRuntimeCustodyProviderScopes.RoleScope scope, RawSourceRetentionContinuationRepository reader,
        RawSourceRetentionContinuation before, CancellationToken ct)
    {
        var step = SelectPublicationStep(before);
        if (step == RetainedPublicationStep.None) return new(step, before);
        var db = scope.Services.GetRequiredService<TagEkycDbContext>();
        var denied = false;
        var successful = false;
        Guid? publication = null;
        long? revision = null;
        long? fence = null;
        switch (step)
        {
            case RetainedPublicationStep.Stage:
                var staged = await new RawExportR3StagingService(db).StageAsync(new(
                    before.CustodyPrincipalId, before.AttemptId, before.ObjectCustodyId!.Value,
                    before.ReservationRevision, before.EncryptionAttemptRevision, before.Fence,
                    before.ObjectStateRevision!.Value), ct);
                switch (staged.Disposition)
                {
                    case RawExportR3StageDisposition.Staged:
                    case RawExportR3StageDisposition.ExistingMatch:
                        if (staged.SourceArtifactId != source || staged.AttemptId != before.AttemptId
                            || staged.ObjectCustodyId != before.ObjectCustodyId
                            || staged.StagedCiphertextFingerprintSchemaVersion != 2
                            || staged.StagedCiphertextFingerprint is not { Length: 32 }
                            || staged.StagedAtUtc is null) throw Invalid();
                        successful = true; revision = staged.ReservationRevision; fence = staged.Fence;
                        break;
                    case RawExportR3StageDisposition.SourceRetentionNotAuthorized: denied = true; break;
                    case RawExportR3StageDisposition.StateConflict: break;
                    case RawExportR3StageDisposition.NotFound: throw Invalid();
                    default: throw Invalid();
                }
                break;
            case RetainedPublicationStep.Commit:
                var committed = await new RawExportSourceFinalizationService(db).CommitAsync(new(
                    before.CustodyPrincipalId, before.AttemptId, before.ReservationRevision,
                    before.EncryptionAttemptRevision, before.Fence, before.ObjectStateRevision!.Value), ct);
                if (committed.Disposition is not (SourceCommitDisposition.Committed or SourceCommitDisposition.ExistingMatch)
                    && (committed.SourcePublicationId is not null || committed.SourceArtifactId is not null
                        || committed.AttemptId is not null || committed.ObjectCustodyId is not null
                        || committed.CommitEvidenceDigest is not null || committed.CommittedAtUtc is not null
                        || committed.ReservationRevision is not null || committed.Fence is not null)) throw Invalid();
                switch (committed.Disposition)
                {
                    case SourceCommitDisposition.Committed:
                    case SourceCommitDisposition.ExistingMatch:
                        if (committed.SourceArtifactId != source || committed.AttemptId != before.AttemptId
                            || committed.ObjectCustodyId != before.ObjectCustodyId
                            || committed.SourcePublicationId is null || committed.SourcePublicationId == Guid.Empty)
                            throw Invalid();
                        if (committed.CommitEvidenceDigest is not { Length: 32 } || committed.CommittedAtUtc is null) throw Invalid();
                        successful = true; publication = committed.SourcePublicationId;
                        revision = committed.ReservationRevision; fence = committed.Fence;
                        break;
                    case SourceCommitDisposition.SourceRetentionNotAuthorized: denied = true; break;
                    case SourceCommitDisposition.StateConflict: break;
                    case SourceCommitDisposition.NotFound: throw Invalid();
                    default: throw Invalid();
                }
                break;
            case RetainedPublicationStep.Publish:
                var published = await new RawExportSourceFinalizationService(db).PublishAsync(new(
                    before.CustodyPrincipalId, before.SourcePublicationId!.Value,
                    before.ReservationRevision, before.Fence), ct);
                if (published.Disposition is not (SourcePublishDisposition.Available or SourcePublishDisposition.ExistingMatch)
                    && (published.SourcePublicationId is not null || published.SourceArtifactId is not null
                        || published.OpaqueCommittedLocatorId is not null || published.AvailableEvidenceDigest is not null
                        || published.ReservationRevision is not null || published.Fence is not null
                        || published.AvailableAtUtc is not null || published.CleanupDisposition is not null)) throw Invalid();
                switch (published.Disposition)
                {
                    case SourcePublishDisposition.Available:
                    case SourcePublishDisposition.ExistingMatch:
                        if (published.SourceArtifactId != source || published.SourcePublicationId != before.SourcePublicationId
                            || published.OpaqueCommittedLocatorId is null || published.OpaqueCommittedLocatorId == Guid.Empty
                            || published.AvailableEvidenceDigest is not { Length: 32 } || published.AvailableAtUtc is null
                            || published.CleanupDisposition is not ("Pending" or "NoObsoleteResidue" or "Completed")) throw Invalid();
                        successful = true; publication = published.SourcePublicationId;
                        revision = published.ReservationRevision; fence = published.Fence;
                        break;
                    case SourcePublishDisposition.SourceRetentionNotAuthorized: denied = true; break;
                    case SourcePublishDisposition.StateConflict: break;
                    case SourcePublishDisposition.NotFound: throw Invalid();
                    default: throw Invalid();
                }
                break;
            default: throw Invalid();
        }
        var after = await reader.ReadAsync(source, ct);
        if (after is null || !SameLineage(before, after)) throw Invalid();
        if (successful && (revision is null or < 1 || fence is null or < 1
            || after.ReservationRevision != revision || after.Fence != fence
            || after.AttemptId != before.AttemptId || after.ObjectCustodyId != before.ObjectCustodyId
            || (publication is not null && after.SourcePublicationId != publication)
            || (step == RetainedPublicationStep.Stage && after.CustodyState != "Staged")
            || (step == RetainedPublicationStep.Commit && after.PublicationState != "Committed")
            || (step == RetainedPublicationStep.Publish && (after.CustodyState != "Available" || after.PublicationState != "Available"))))
            throw Invalid();
        return new(step, after, denied);
    }

    internal static RetainedPublicationStep SelectPublicationStep(RawSourceRetentionContinuation row)
    {
        // Never let a publication branch overtake a durable terminal intent or
        // resurrect an operationally terminated attempt. SQL checks again.
        if (row.R2TerminalIntentCode is not null || row.R2TerminationDisposition is not null
            || row.R2TerminalOutcomeCode is not null) return RetainedPublicationStep.None;
        if (row.CustodyState == "Reserved" && row.ObjectState == "VerifiedCompleted"
            && row.SourcePublicationId is null) return RetainedPublicationStep.Stage;
        if (row.CustodyState == "Staged" && row.ObjectState == "VerifiedCompleted")
        {
            if (row.SourcePublicationId is null) return RetainedPublicationStep.Commit;
            if (row.PublicationState == "Committed") return RetainedPublicationStep.Publish;
        }
        return RetainedPublicationStep.None;
    }

    private static bool SameLineage(RawSourceRetentionContinuation a, RawSourceRetentionContinuation b) =>
        a.SourceArtifactId == b.SourceArtifactId && a.CustodyPrincipalId == b.CustodyPrincipalId
        && a.ClientApplicationId == b.ClientApplicationId && a.VerificationSessionId == b.VerificationSessionId
        && a.RuntimeBindingId == b.RuntimeBindingId && a.RetentionAuthorityId == b.RetentionAuthorityId
        && a.RetentionAuthorityRevision == b.RetentionAuthorityRevision;
    private static RawSourceRetentionContinuationRepository Reader(CaptureRuntimeCustodyProviderScopes.RoleScope scope) =>
        new(scope.Services.GetRequiredService<NpgsqlDataSource>());
    private static InvalidOperationException Invalid() => new("RAW_INGRESS_CONTINUATION_NOT_READY");
}

// Public A1 port adapter over the existing internal R2-R6 primitives. Keeping
// it beside the continuation pipeline prevents a second custody pipeline or a
// public exposure of provider-specific contracts.
internal sealed class CaptureRuntimeRawIngressBodyPipeline(
    CaptureRuntimeCustodyProviderScopes scopes,
    IKekOperationProvider keyProvider,
    IKekProvisioningRecoveryOperation keyRecovery,
    IContentCommitmentService contentCommitments,
    DurableKeyCustodyOptions keyOptions,
    RawIngressBrokerOptions brokerOptions,
    CancellationToken hostStopping) : ICaptureRuntimeRawIngressBodyPipeline
{
    public async Task<CaptureRuntimeRawIngressAdmissionResult> ProcessAsync(
        CaptureRuntimeRawIngressAdmissionContext context,
        RawIngressBrokerHandoff handoff,
        Stream body,
        CancellationToken cancellationToken)
    {
        if (!Valid(context, handoff, body)) throw Invalid();

        RawExportR2WriterResult written;
        await using (var writer = await scopes.OpenWriterAsync(cancellationToken).ConfigureAwait(false))
        {
            var db = writer.Services.GetRequiredService<TagEkycDbContext>();
            var source = writer.Services.GetRequiredService<NpgsqlDataSource>();
            var recorder = new RawExportR2TerminalIntentRecorder(source, handoff.CustodyActorPrincipalId,
                brokerOptions.RequestTimeoutMilliseconds, hostStopping);
            var orchestrator = new RawExportR2EncryptionOrchestrator(
                new RawExportR2Repository(db),
                new PostgresAttemptKeyReservationProvider(db, new PostgresKeyProviderOperationMap(db), keyProvider),
                new AttemptAeadEncryptionOperationService(db, keyProvider, keyOptions),
                contentCommitments,
                writer.Services.GetRequiredService<IProvisionalObjectWriter>(),
                recorder,
                context.ClaimedPlaintextLength);
            written = await orchestrator.ExecuteAsync(new(
                handoff.CustodyActorPrincipalId,
                handoff.AttemptKeyReservationId,
                handoff.AttemptId,
                handoff.SourceArtifactId,
                handoff.ExpectedEncryptionAttemptRevision,
                handoff.ExpectedFence,
                body), cancellationToken).ConfigureAwait(false);
        }

        var observed = ProjectObservation(written.InputObservation);
        if (observed is not null) return observed;
        if (written.Disposition is RawExportR2WriterDisposition.PreCustodyRejected)
            throw Invalid();
        if (written.Disposition is RawExportR2WriterDisposition.PreCustodyRetryable
            or RawExportR2WriterDisposition.PreCustodyOperatorRequired
            or RawExportR2WriterDisposition.NotArmed)
            return Outcome(CaptureRuntimeRawIngressOutcome.TemporarilyUnavailable);

        var continuation = new CaptureRuntimeSourcePipeline(scopes, keyRecovery, null,
            keyProvider, contentCommitments, keyOptions);
        RawSourceRetentionContinuation? snapshot = null;
        for (var step = 0; step < 12; step++)
        {
            var progress = await continuation.AdvanceAsync(
                handoff.SourceArtifactId, brokerOptions.RequestTimeoutMilliseconds, cancellationToken)
                .ConfigureAwait(false);
            snapshot = progress.Snapshot;
            var projected = ProjectSnapshot(snapshot);
            if (projected is not null) return projected;
            if (progress.Step == RetainedContinuationStep.None) break;
        }

        return snapshot is null
            ? throw Invalid()
            : Outcome(snapshot.R2TerminalIntentCode is null
                ? CaptureRuntimeRawIngressOutcome.ResumePending
                : CaptureRuntimeRawIngressOutcome.TemporarilyUnavailable);
    }

    private static CaptureRuntimeRawIngressAdmissionResult? ProjectObservation(RawExportR2InputObservation observation)
    {
        if (observation.Kind is RawExportR2InputCompletionKind.ActualLimitExceeded
            or RawExportR2InputCompletionKind.CleanShortEof
            or RawExportR2InputCompletionKind.ContentCommitmentMismatch)
        {
            if (!observation.TerminalIntentPersisted) throw Invalid();
            return null;
        }
        return observation.Kind switch
        {
            RawExportR2InputCompletionKind.TransportInterrupted
                or RawExportR2InputCompletionKind.CommitmentProviderUnavailable
                or RawExportR2InputCompletionKind.EncryptionProviderFailure =>
                Outcome(CaptureRuntimeRawIngressOutcome.TemporarilyUnavailable),
            RawExportR2InputCompletionKind.NotCompleted or RawExportR2InputCompletionKind.CompleteMatch => null,
            _ => throw Invalid()
        };
    }

    private static CaptureRuntimeRawIngressAdmissionResult? ProjectSnapshot(RawSourceRetentionContinuation? snapshot)
    {
        if (snapshot is null) return null;
        if (snapshot.R2TerminalOutcomeCode is { } terminal)
            return terminal switch
            {
                RawExportSourceIngressCodes.ArtifactSizeLimitExceeded =>
                    Outcome(CaptureRuntimeRawIngressOutcome.ArtifactSizeLimitExceeded),
                RawExportSourceIngressCodes.ContentCommitmentMismatch =>
                    Outcome(CaptureRuntimeRawIngressOutcome.ContentCommitmentMismatch),
                RawExportSourceIngressCodes.RecaptureRequired =>
                    Outcome(CaptureRuntimeRawIngressOutcome.RecaptureRequired),
                _ => throw Invalid()
            };
        if (snapshot.CustodyState == "Available" && snapshot.PublicationState == "Available"
            && snapshot.SourceArtifactId != Guid.Empty)
            return new(CaptureRuntimeRawIngressOutcome.Available, snapshot.SourceArtifactId,
                "Available", "Available", null);
        return null;
    }

    private static bool Valid(CaptureRuntimeRawIngressAdmissionContext context,
        RawIngressBrokerHandoff handoff, Stream body) =>
        body.CanRead && context.CaptureAgentId != Guid.Empty && context.ClaimedPlaintextLength > 0 &&
        handoff.SourceArtifactId != Guid.Empty && handoff.AttemptKeyReservationId != Guid.Empty &&
        handoff.AttemptId != Guid.Empty && handoff.ExpectedEncryptionAttemptRevision > 0 &&
        handoff.ExpectedFence > 0 && handoff.CustodyActorPrincipalId != Guid.Empty &&
        handoff.ExecutionExpiresAtUtc.Offset == TimeSpan.Zero;

    private static CaptureRuntimeRawIngressAdmissionResult Outcome(CaptureRuntimeRawIngressOutcome outcome) =>
        new(outcome, null, null, null, null);

    private static InvalidOperationException Invalid() => new("RAW_INGRESS_BODY_PIPELINE_NOT_READY");
}
