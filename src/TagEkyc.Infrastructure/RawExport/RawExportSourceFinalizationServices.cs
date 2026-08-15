using Microsoft.EntityFrameworkCore;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed class RawExportSourceFinalizationService(TagEkycDbContext db)
{
    internal Task<SourceCommitResult> CommitAsync(CommitStagedSourceCommand command,CancellationToken ct=default) =>
        ExecuteAsync(command.ActorPrincipalId,command,static (r,c,v,t)=>r.CommitAsync(v,t),ct);
    internal Task<SourcePublishResult> PublishAsync(PublishAvailableSourceCommand command,CancellationToken ct=default) =>
        ExecuteAsync(command.ActorPrincipalId,command,static (r,c,v,t)=>r.PublishAsync(v,t),ct);

    private async Task<TResult> ExecuteAsync<TCommand,TResult>(Guid actor,TCommand value,
        Func<RawExportSourceFinalizationRepository,TagEkycDbContext,TCommand,CancellationToken,Task<TResult>> operation,CancellationToken ct)
    {
        Validate(actor,value);
        await db.Database.OpenConnectionAsync(ct).ConfigureAwait(false);
        try
        {
            await using var tx=await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
            try
            {
                var repository=new RawExportSourceFinalizationRepository(db);
                await repository.SetActorLocalAsync(actor,ct).ConfigureAwait(false);
                var result=await operation(repository,db,value,ct).ConfigureAwait(false);
                await tx.CommitAsync(ct).ConfigureAwait(false); return result;
            }
            catch { await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false); throw; }
        }
        finally { await db.Database.CloseConnectionAsync().ConfigureAwait(false); }
    }
    private static void Validate<T>(Guid actor,T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if(actor==Guid.Empty) throw new ArgumentException("RAW_EXPORT_FINALIZATION_ARGUMENT_INVALID");
        if(value is CommitStagedSourceCommand c && (c.AttemptId==Guid.Empty||c.ExpectedReservationRevision<1||c.ExpectedAttemptRevision<1||c.ExpectedFence<1||c.ExpectedObjectStateRevision<1)) throw new ArgumentException("RAW_EXPORT_FINALIZATION_ARGUMENT_INVALID");
        if(value is PublishAvailableSourceCommand p && (p.SourcePublicationId==Guid.Empty||p.ExpectedReservationRevision<1||p.ExpectedFence<1)) throw new ArgumentException("RAW_EXPORT_FINALIZATION_ARGUMENT_INVALID");
    }
}

internal sealed class RawExportSourceCleanupReconciliationService(TagEkycDbContext db)
{
    internal async Task<SourceCleanupReadResult> ReadNextAsync(ReadNextSourceCleanupItemCommand command,CancellationToken ct=default)
    {
        if(command.ActorPrincipalId==Guid.Empty||command.SourcePublicationId==Guid.Empty||command.ExpectedPublicationRevision<1) throw new ArgumentException("RAW_EXPORT_FINALIZATION_ARGUMENT_INVALID");
        await db.Database.OpenConnectionAsync(ct).ConfigureAwait(false);
        try
        {
            await using var tx=await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
            var repo=new RawExportSourceFinalizationRepository(db); await repo.SetActorLocalAsync(command.ActorPrincipalId,ct).ConfigureAwait(false);
            var result=await repo.ReadNextAsync(command,ct).ConfigureAwait(false); await tx.CommitAsync(ct).ConfigureAwait(false); return result;
        }
        finally { await db.Database.CloseConnectionAsync().ConfigureAwait(false); }
    }

    internal async Task<SourceCleanupCompleteResult> ReconcileObjectAsync(
        ReadNextSourceCleanupItemCommand command,IProvisionalObjectReconciler reconciler,
        CancellationToken ct=default)
    {
        ArgumentNullException.ThrowIfNull(reconciler);
        var item=await ReadNextAsync(command,ct).ConfigureAwait(false);
        if(item.Disposition!=SourceCleanupReadDisposition.ItemAvailable || item.ResourceKind!="ProvisionalObject"
           || item.ResourceId is null || item.CleanupItemId is null || item.CleanupItemRevision is null)
            return Pending();
        var context=await ExecuteAsync(command.ActorPrincipalId,item.ResourceId.Value,
            static(r,v,t)=>r.ReadObjectReconcileContextAsync(v,t),ct).ConfigureAwait(false);
        if(context is null || context.State!="CleanupPending") return Pending();
        var locator=new ExactObjectLocator(context.ProvisionalObjectIdentity,context.ObjectKey,context.ObjectBindingDigest);
        var first=await reconciler.InspectExactAsync(locator,ct).ConfigureAwait(false);
        if(first.Outcome==ExactObjectInspectionOutcome.PositivelyAbsent)
        {
            var firstAt=PostgresTimestamp(DateTimeOffset.UtcNow); await Task.Delay(10,ct).ConfigureAwait(false);
            var second=await reconciler.InspectExactAsync(locator,ct).ConfigureAwait(false);
            if(second.Outcome!=ExactObjectInspectionOutcome.PositivelyAbsent) return Pending();
            var secondAt=PostgresTimestamp(DateTimeOffset.UtcNow);
            var evidence=C1HashCanonical.Compute("tip-88c1-object-delete-absence-evidence-v1",
                new C1HashCanonical.Scalar(Convert.ToHexString(context.ObjectBindingDigest).ToLowerInvariant()),
                new C1HashCanonical.Scalar(context.StateRevision.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new C1HashCanonical.Scalar(firstAt.UtcDateTime.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new C1HashCanonical.Scalar(secondAt.UtcDateTime.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new C1HashCanonical.Scalar("PositiveAbsenceConfirmed"));
            await ExecuteAsync(command.ActorPrincipalId,(context,firstAt,secondAt,evidence),
                static async(r,v,t)=>{await r.RecordObjectAbsenceConfirmedAsync(v.context,v.firstAt,v.secondAt,v.evidence,t).ConfigureAwait(false);return true;},ct).ConfigureAwait(false);
        }
        else return Pending();
        return Pending();
    }

    internal async Task<SourceCleanupCompleteResult> ReconcileKeyAsync(
        ReadNextSourceCleanupItemCommand command,IKekProvisioningRecoveryOperation recovery,CancellationToken ct=default)
    {
        ArgumentNullException.ThrowIfNull(recovery);
        var item=await ReadNextAsync(command,ct).ConfigureAwait(false);
        if(item.Disposition!=SourceCleanupReadDisposition.ItemAvailable || item.ResourceKind!="AttemptKeyReservation"
           || item.ResourceId is null || item.CleanupItemId is null || item.CleanupItemRevision is null) return Pending();
        var context=await ExecuteAsync(command.ActorPrincipalId,item.ResourceId.Value,
            static(r,v,t)=>r.ReadKeyRecoveryContextAsync(v,t),ct).ConfigureAwait(false);
        if(context is null) return Pending();
        if(context.PreparationDisposition is "Revoked" or "ReservationAbandoned")
            return Pending();
        if(context.ProviderOperationState is "CleanedUp" or "AbsenceProven")
            return Pending();
        if(context.PreparationDisposition!="AbandonRequested" || context.PreparationId is null
           || context.ProviderOperationToken is null || context.ProviderOperationId is null) return Pending();

        if(context.ProviderCleanupReference is { Length:>0 } cleanupReference)
        {
            var cleanup=await recovery.CleanupProvisioningOperationAsync(cleanupReference,context.AttemptKeyContextFingerprint,ct).ConfigureAwait(false);
            if(cleanup is KekProvisioningCleanupResult.Cleaned cleaned)
                await AcknowledgeAsync(command.ActorPrincipalId,item.ResourceId.Value,context,"Cleaned",cleanupReference,cleaned.Receipt,ct).ConfigureAwait(false);
            else if(cleanup is KekProvisioningCleanupResult.AlreadyAbsent absent)
                await AcknowledgeAsync(command.ActorPrincipalId,item.ResourceId.Value,context,"AlreadyAbsent",cleanupReference,absent.Receipt,ct).ConfigureAwait(false);
            else
            {
                var kind=cleanup switch { KekProvisioningCleanupResult.CleanupUnavailable=>"CleanupUnavailable",KekProvisioningCleanupResult.CleanupOutcomeUnknown=>"CleanupOutcomeUnknown",_=>"CleanupFailed" };
                await ExecuteAsync(command.ActorPrincipalId,(item.ResourceId.Value,context,kind,cleanupReference),
                    static(r,v,t)=>r.ObserveKeyCleanupAsync(v.Value,v.context,v.kind,v.cleanupReference,null,t),ct).ConfigureAwait(false);
                return Pending();
            }
            return Pending();
        }

        var resolution=await recovery.ResolveProvisioningOperationAsync(context.ProviderOperationToken.Value,context.AttemptKeyContextFingerprint,ct).ConfigureAwait(false);
        if(resolution is KekProvisioningResolution.NoProviderResult noResult)
        {
            await ExecuteAsync(command.ActorPrincipalId,(item.ResourceId.Value,context,noResult.AbsenceProofReceipt),
                static(r,v,t)=>r.ResolveKeyProviderOutcomeAsync(v.Value,v.context,"NoProviderResult",null,null,v.AbsenceProofReceipt,t),ct).ConfigureAwait(false);
            return Pending();
        }
        var required=resolution switch
        {
            KekProvisioningResolution.ProviderResourceCleanupRequired x=>x.CleanupReference,
            KekProvisioningResolution.WrappedResultRecovered x=>x.Material.ProviderResourceReference,
            _=>null
        };
        if(required is not null)
            await ExecuteAsync(command.ActorPrincipalId,(item.ResourceId.Value,context,required),
                static(r,v,t)=>r.MarkKeyCleanupRequiredAsync(v.Value,v.context,v.required,t),ct).ConfigureAwait(false);
        return Pending();
    }

    private Task<string> AcknowledgeAsync(Guid actor,Guid id,SourceKeyRecoveryContext context,string kind,string reference,string receipt,CancellationToken ct) =>
        ExecuteAsync(actor,(id,context,kind,reference,receipt),static(r,v,t)=>r.AcknowledgeKeyCleanupAsync(v.id,v.context,v.kind,v.reference,v.receipt,t),ct);

    private async Task<TResult> ExecuteAsync<TValue,TResult>(Guid actor,TValue value,Func<RawExportSourceFinalizationRepository,TValue,CancellationToken,Task<TResult>> operation,CancellationToken ct)
    {
        if(actor==Guid.Empty) throw new ArgumentException("RAW_EXPORT_FINALIZATION_ARGUMENT_INVALID");
        await db.Database.OpenConnectionAsync(ct).ConfigureAwait(false);
        try { await using var tx=await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false); var repository=new RawExportSourceFinalizationRepository(db);
            if(actor!=Guid.Empty) await repository.SetActorLocalAsync(actor,ct).ConfigureAwait(false);
            var result=await operation(repository,value,ct).ConfigureAwait(false);await tx.CommitAsync(ct).ConfigureAwait(false);return result; }
        finally { await db.Database.CloseConnectionAsync().ConfigureAwait(false); }
    }

    private static SourceCleanupCompleteResult Pending()=>new(SourceCleanupCompleteDisposition.ResourceNotTerminal,null,null,null,null,null,null,null,null,null);
    private static DateTimeOffset PostgresTimestamp(DateTimeOffset value) =>
        new(value.UtcTicks-(value.UtcTicks%10),TimeSpan.Zero);
}

internal sealed class RawExportSourceCleanupService(TagEkycDbContext db)
{
    internal Task<SourceCleanupCompleteResult> CompleteAsync(CompleteSourceCleanupItemCommand command,CancellationToken ct=default) => ExecuteAsync(command.ActorPrincipalId,command,static(r,v,t)=>r.CompleteAsync(v,t),ct);
    internal Task<SourceCleanupFinalizeResult> FinalizeAsync(FinalizeSourceCleanupCommand command,CancellationToken ct=default) => ExecuteAsync(command.ActorPrincipalId,command,static(r,v,t)=>r.FinalizeAsync(v,t),ct);
    internal async Task<SourceCleanupCompleteResult> SettleNextAsync(
        ReadNextSourceCleanupItemCommand command,IProvisionalObjectLifecycle lifecycle,CancellationToken ct=default)
    {
        ArgumentNullException.ThrowIfNull(lifecycle);
        var item=await new RawExportSourceCleanupReconciliationService(db).ReadNextAsync(command,ct).ConfigureAwait(false);
        if(item.Disposition!=SourceCleanupReadDisposition.ItemAvailable || item.CleanupItemId is null
           || item.ResourceId is null || item.CleanupItemRevision is null)
            return new(SourceCleanupCompleteDisposition.StateConflict,null,null,null,null,null,null,null,null,null);
        var complete=new CompleteSourceCleanupItemCommand(command.ActorPrincipalId,item.CleanupItemId.Value,item.CleanupItemRevision.Value);
        return item.ResourceKind switch
        {
            "ProvisionalObject" => await DeleteObsoleteObjectAndCompleteAsync(complete,item.ResourceId.Value,lifecycle,ct).ConfigureAwait(false),
            "AttemptKeyReservation" => await SettleObsoleteKeyAndCompleteAsync(complete,item.ResourceId.Value,ct).ConfigureAwait(false),
            _ => new(SourceCleanupCompleteDisposition.StateConflict,null,null,null,null,null,null,null,null,null)
        };
    }

    private async Task<SourceCleanupCompleteResult> DeleteObsoleteObjectAndCompleteAsync(
        CompleteSourceCleanupItemCommand command,Guid objectCustodyId,IProvisionalObjectLifecycle lifecycle,CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(lifecycle);
        var context=await ExecuteAsync(command.ActorPrincipalId,objectCustodyId,static(r,v,t)=>r.ReadObjectLifecycleContextAsync(v,t),ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("RAW_EXPORT_FINALIZATION_OBJECT_NOT_FOUND");
        if(context.State=="VerifiedCompleted")
        {
            await ExecuteAsync(command.ActorPrincipalId,context,static async(r,v,t)=>{await r.MarkObjectCleanupRequiredAsync(v,t).ConfigureAwait(false);return true;},ct).ConfigureAwait(false);
            context=await ExecuteAsync(command.ActorPrincipalId,objectCustodyId,static(r,v,t)=>r.ReadObjectLifecycleContextAsync(v,t),ct).ConfigureAwait(false)
                ?? throw new InvalidOperationException("RAW_EXPORT_FINALIZATION_OBJECT_NOT_FOUND");
        }
        if(context.State=="CleanupPending")
        {
            var locator=new ExactObjectLocator(context.ProvisionalObjectIdentity,context.ObjectKey,context.ObjectBindingDigest);
            var deleted=await lifecycle.DeleteExactAsync(locator,ct).ConfigureAwait(false);
            if(deleted.Outcome!=ExactDeleteOutcome.DeletedAcknowledged || deleted.StatusCode!=204)
                return new(SourceCleanupCompleteDisposition.ResourceNotTerminal,null,null,null,null,null,null,null,null,null);
            await ExecuteAsync(command.ActorPrincipalId,context,static async(r,v,t)=>{await r.RecordObjectDeleteAcknowledgedAsync(v,t).ConfigureAwait(false);return true;},ct).ConfigureAwait(false);
        }
        return await CompleteAsync(command,ct).ConfigureAwait(false);
    }

    private async Task<SourceCleanupCompleteResult> SettleObsoleteKeyAndCompleteAsync(
        CompleteSourceCleanupItemCommand command,Guid attemptKeyReservationId,CancellationToken ct)
    {
        if(attemptKeyReservationId==Guid.Empty) throw new ArgumentException("RAW_EXPORT_FINALIZATION_ARGUMENT_INVALID");
        var revoke=await ExecuteAsync(command.ActorPrincipalId,attemptKeyReservationId,
            static(r,v,t)=>r.RevokeKeyAsync(v,t),ct).ConfigureAwait(false);
        if(revoke is "Revoked" or "AlreadyRevoked")
            return await CompleteAsync(command,ct).ConfigureAwait(false);

        var request=await ExecuteAsync(command.ActorPrincipalId,attemptKeyReservationId,
            static(r,v,t)=>r.RequestKeyAbandonAsync(v,t),ct).ConfigureAwait(false);
        if(request is not ("AbandonRequested" or "AlreadyRequested" or "StateConflict"))
            return new(SourceCleanupCompleteDisposition.ResourceNotTerminal,null,null,null,null,null,null,null,null,null);

        var finalized=await ExecuteAsync(command.ActorPrincipalId,attemptKeyReservationId,
            static(r,v,t)=>r.FinalizeKeyAbandonAsync(v,t),ct).ConfigureAwait(false);
        if(finalized is not ("ReservationAbandoned" or "AlreadyAbandoned"))
            return new(SourceCleanupCompleteDisposition.ResourceNotTerminal,null,null,null,null,null,null,null,null,null);
        return await CompleteAsync(command,ct).ConfigureAwait(false);
    }
    private async Task<TResult> ExecuteAsync<TCommand,TResult>(Guid actor,TCommand value,Func<RawExportSourceFinalizationRepository,TCommand,CancellationToken,Task<TResult>> op,CancellationToken ct)
    {
        if(actor==Guid.Empty) throw new ArgumentException("RAW_EXPORT_FINALIZATION_ARGUMENT_INVALID"); ArgumentNullException.ThrowIfNull(value);
        if(value is CompleteSourceCleanupItemCommand c && (c.CleanupItemId==Guid.Empty||c.ExpectedCleanupItemRevision<1)) throw new ArgumentException("RAW_EXPORT_FINALIZATION_ARGUMENT_INVALID");
        if(value is FinalizeSourceCleanupCommand f && (f.SourcePublicationId==Guid.Empty||f.ExpectedPublicationRevision<1)) throw new ArgumentException("RAW_EXPORT_FINALIZATION_ARGUMENT_INVALID");
        await db.Database.OpenConnectionAsync(ct).ConfigureAwait(false);
        try
        {
            await using var tx=await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
            try { var repo=new RawExportSourceFinalizationRepository(db); await repo.SetActorLocalAsync(actor,ct).ConfigureAwait(false); var result=await op(repo,value,ct).ConfigureAwait(false); await tx.CommitAsync(ct).ConfigureAwait(false); return result; }
            catch { await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false); throw; }
        }
        finally { await db.Database.CloseConnectionAsync().ConfigureAwait(false); }
    }
}
