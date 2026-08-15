using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed class RawExportSourceFinalizationRepository(TagEkycDbContext db)
{
    internal async Task SetActorLocalAsync(Guid actorPrincipalId, CancellationToken ct)
    {
        await using var sql = CreateCommand("SELECT pg_catalog.set_config('tagekyc.actor_principal_id',@actor,true)");
        sql.Parameters.AddWithValue("actor", NpgsqlDbType.Text, actorPrincipalId.ToString("D"));
        await sql.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    internal async Task<SourceCommitResult> CommitAsync(CommitStagedSourceCommand value, CancellationToken ct)
    {
        await using var sql = CreateCommand("SELECT * FROM tagekyc.raw_export_commit_staged_source(@attempt,@reservation_revision,@attempt_revision,@fence,@object_revision)");
        Add(sql, "attempt", NpgsqlDbType.Uuid, value.AttemptId);
        Add(sql, "reservation_revision", NpgsqlDbType.Bigint, value.ExpectedReservationRevision);
        Add(sql, "attempt_revision", NpgsqlDbType.Bigint, value.ExpectedAttemptRevision);
        Add(sql, "fence", NpgsqlDbType.Bigint, value.ExpectedFence);
        Add(sql, "object_revision", NpgsqlDbType.Bigint, value.ExpectedObjectStateRevision);
        await using var reader = await ReadOneAsync(sql, ct);
        return new(Parse<SourceCommitDisposition>(reader, "Outcome"), G(reader,"SourcePublicationId"), G(reader,"SourceArtifactId"),
            G(reader,"AttemptId"), G(reader,"ObjectCustodyId"), B(reader,"CommitEvidenceDigest"), T(reader,"CommittedAtUtc"),
            L(reader,"ReservationRevision"), L(reader,"Fence"));
    }

    internal async Task<SourcePublishResult> PublishAsync(PublishAvailableSourceCommand value, CancellationToken ct)
    {
        await using var sql = CreateCommand("SELECT * FROM tagekyc.raw_export_publish_available_source(@publication,@reservation_revision,@fence)");
        Add(sql,"publication",NpgsqlDbType.Uuid,value.SourcePublicationId);
        Add(sql,"reservation_revision",NpgsqlDbType.Bigint,value.ExpectedReservationRevision);
        Add(sql,"fence",NpgsqlDbType.Bigint,value.ExpectedFence);
        await using var reader = await ReadOneAsync(sql,ct);
        return new(Parse<SourcePublishDisposition>(reader,"Outcome"),G(reader,"SourcePublicationId"),G(reader,"SourceArtifactId"),
            G(reader,"OpaqueCommittedLocatorId"),B(reader,"AvailableEvidenceDigest"),L(reader,"ReservationRevision"),
            L(reader,"Fence"),T(reader,"AvailableAtUtc"),S(reader,"CleanupDisposition"));
    }

    internal async Task<SourceCleanupReadResult> ReadNextAsync(ReadNextSourceCleanupItemCommand value, CancellationToken ct)
    {
        await using var sql=CreateCommand("SELECT * FROM tagekyc.raw_export_read_next_source_cleanup_item(@publication,@revision)");
        Add(sql,"publication",NpgsqlDbType.Uuid,value.SourcePublicationId); Add(sql,"revision",NpgsqlDbType.Bigint,value.ExpectedPublicationRevision);
        await using var reader=await ReadOneAsync(sql,ct);
        return new(Parse<SourceCleanupReadDisposition>(reader,"Outcome"),G(reader,"SourcePublicationId"),L(reader,"PublicationRevision"),
            G(reader,"CleanupItemId"),S(reader,"ResourceKind"),G(reader,"ResourceId"),G(reader,"ResourceAttemptId"),
            L(reader,"CleanupItemRevision"),L(reader,"PlannedResourceRevision"));
    }

    internal async Task<SourceCleanupCompleteResult> CompleteAsync(CompleteSourceCleanupItemCommand value, CancellationToken ct)
    {
        await using var sql=CreateCommand("SELECT * FROM tagekyc.raw_export_complete_source_cleanup_item(@item,@revision)");
        Add(sql,"item",NpgsqlDbType.Uuid,value.CleanupItemId); Add(sql,"revision",NpgsqlDbType.Bigint,value.ExpectedCleanupItemRevision);
        await using var reader=await ReadOneAsync(sql,ct);
        return new(Parse<SourceCleanupCompleteDisposition>(reader,"Outcome"),G(reader,"SourcePublicationId"),L(reader,"PublicationRevision"),
            G(reader,"CleanupItemId"),S(reader,"ResourceKind"),G(reader,"ResourceId"),S(reader,"CompletionDisposition"),
            B(reader,"CleanupEvidenceDigest"),T(reader,"CompletedAtUtc"),L(reader,"CleanupItemRevision"));
    }

    internal async Task<SourceCleanupFinalizeResult> FinalizeAsync(FinalizeSourceCleanupCommand value, CancellationToken ct)
    {
        await using var sql=CreateCommand("SELECT * FROM tagekyc.raw_export_finalize_source_cleanup(@publication,@revision)");
        Add(sql,"publication",NpgsqlDbType.Uuid,value.SourcePublicationId); Add(sql,"revision",NpgsqlDbType.Bigint,value.ExpectedPublicationRevision);
        await using var reader=await ReadOneAsync(sql,ct);
        return new(Parse<SourceCleanupFinalizeDisposition>(reader,"Outcome"),G(reader,"SourcePublicationId"),L(reader,"PublicationRevision"),
            S(reader,"CleanupDisposition"),B(reader,"CleanupEvidenceDigest"),T(reader,"FinalizedAtUtc"));
    }

    internal async Task<SourceObjectLifecycleContext?> ReadObjectLifecycleContextAsync(Guid id,CancellationToken ct)
    {
        await using var sql=CreateCommand("SELECT * FROM tagekyc.raw_export_read_provisional_object_lifecycle_context(@id)"); Add(sql,"id",NpgsqlDbType.Uuid,id);
        await using var reader=await sql.ExecuteReaderAsync(CommandBehavior.SingleRow,ct).ConfigureAwait(false); if(!await reader.ReadAsync(ct).ConfigureAwait(false)) return null;
        return new(reader.GetGuid(reader.GetOrdinal("ObjectCustodyId")),reader.GetGuid(reader.GetOrdinal("AttemptId")),reader.GetGuid(reader.GetOrdinal("ProvisionalObjectIdentity")),
            reader.GetString(reader.GetOrdinal("ObjectKey")),(byte[])reader[reader.GetOrdinal("ObjectBindingDigest")],reader.GetString(reader.GetOrdinal("State")),reader.GetInt64(reader.GetOrdinal("StateRevision")),
            G(reader,"PutOperationId"),S(reader,"CleanupReasonCode"),B(reader,"CleanupEvidenceDigest"),T(reader,"CleanupRequestedAtUtc"),B(reader,"ProviderReceiptDigest"));
    }

    internal async Task<SourceObjectReconcileContext?> ReadObjectReconcileContextAsync(Guid id,CancellationToken ct)
    {
        await using var sql=CreateCommand("SELECT * FROM tagekyc.raw_export_read_provisional_object_reconcile_context(@id)");
        Add(sql,"id",NpgsqlDbType.Uuid,id);
        await using var reader=await sql.ExecuteReaderAsync(CommandBehavior.SingleRow,ct).ConfigureAwait(false);
        if(!await reader.ReadAsync(ct).ConfigureAwait(false)) return null;
        return new(reader.GetGuid(reader.GetOrdinal("ObjectCustodyId")),reader.GetGuid(reader.GetOrdinal("AttemptId")),
            reader.GetGuid(reader.GetOrdinal("AttemptKeyReservationId")),reader.GetGuid(reader.GetOrdinal("SourceArtifactId")),
            reader.GetGuid(reader.GetOrdinal("ProvisionalObjectIdentity")),reader.GetString(reader.GetOrdinal("ObjectKey")),
            (byte[])reader[reader.GetOrdinal("ObjectBindingDigest")],reader.GetString(reader.GetOrdinal("State")),
            reader.GetInt64(reader.GetOrdinal("StateRevision")));
    }

    internal Task RecordObjectAbsenceConfirmedAsync(SourceObjectReconcileContext value,DateTimeOffset first,DateTimeOffset second,byte[] evidence,CancellationToken ct) =>
        ObjectMutationAsync("raw_export_record_provisional_object_absence_confirmed",ct,
            ("id",value.ObjectCustodyId),("revision",value.StateRevision),("first",first),("second",second),("evidence",evidence));

    internal async Task<SourceKeyRecoveryContext?> ReadKeyRecoveryContextAsync(Guid id,CancellationToken ct)
    {
        await using var sql=CreateCommand("SELECT * FROM tagekyc.raw_export_read_current_attempt_key_recovery_context(@id)");
        Add(sql,"id",NpgsqlDbType.Uuid,id);
        await using var reader=await sql.ExecuteReaderAsync(CommandBehavior.SingleRow,ct).ConfigureAwait(false);
        if(!await reader.ReadAsync(ct).ConfigureAwait(false)) return null;
        var token=S(reader,"current_provider_operation_token");
        return new(reader.GetString(reader.GetOrdinal("preparation_disposition")),reader.GetGuid(reader.GetOrdinal("attempt_id")),
            (byte[])reader[reader.GetOrdinal("attempt_key_context_fingerprint")],G(reader,"current_preparation_id"),
            reader.GetInt64(reader.GetOrdinal("current_preparation_fence")),token is null?null:new ProviderOperationToken(token),
            G(reader,"provider_operation_id"),S(reader,"provider_operation_state"),S(reader,"provider_cleanup_reference"));
    }

    internal Task<string> ResolveKeyProviderOutcomeAsync(Guid reservationId,SourceKeyRecoveryContext context,string resolution,string? cleanupReference,string? operationReceipt,string? absenceReceipt,CancellationToken ct) =>
        KeyScalarAsync("raw_export_resolve_attempt_key_provider_outcome",ct,
            ("reservation",reservationId),("preparation",context.PreparationId!.Value),("fence",context.PreparationFence),
            ("resolution",resolution),("cleanup",(object?)cleanupReference??DBNull.Value),("receipt",(object?)operationReceipt??DBNull.Value),("absence",(object?)absenceReceipt??DBNull.Value));

    internal Task<string> MarkKeyCleanupRequiredAsync(Guid reservationId,SourceKeyRecoveryContext context,string cleanupReference,CancellationToken ct) =>
        KeyScalarAsync("raw_export_mark_key_provider_cleanup_required",ct,
            ("operation",context.ProviderOperationId!.Value),("reservation",reservationId),("preparation",context.PreparationId!.Value),
            ("fence",context.PreparationFence),("token",context.ProviderOperationToken!.Value.Value),("cleanup",cleanupReference));

    internal Task<string> ObserveKeyCleanupAsync(Guid reservationId,SourceKeyRecoveryContext context,string kind,string cleanupReference,string? receipt,CancellationToken ct) =>
        KeyScalarAsync("raw_export_record_key_provider_cleanup_observation",ct,
            ("operation",context.ProviderOperationId!.Value),("reservation",reservationId),("preparation",context.PreparationId!.Value),
            ("fence",context.PreparationFence),("token",context.ProviderOperationToken!.Value.Value),("kind",kind),("cleanup",cleanupReference),("receipt",(object?)receipt??DBNull.Value));

    internal Task<string> AcknowledgeKeyCleanupAsync(Guid reservationId,SourceKeyRecoveryContext context,string kind,string cleanupReference,string receipt,CancellationToken ct) =>
        KeyScalarAsync("raw_export_acknowledge_key_provider_cleanup",ct,
            ("operation",context.ProviderOperationId!.Value),("reservation",reservationId),("preparation",context.PreparationId!.Value),
            ("fence",context.PreparationFence),("token",context.ProviderOperationToken!.Value.Value),("kind",kind),("cleanup",cleanupReference),("receipt",receipt));

    internal Task MarkObjectCleanupRequiredAsync(SourceObjectLifecycleContext value,CancellationToken ct) => ObjectMutationAsync(
        "raw_export_mark_provisional_object_cleanup_required",ct,("id",value.ObjectCustodyId),("revision",value.StateRevision),("reason","SourceConsumed"),
        ("evidence",C1HashCanonical.Compute("tip-88c1-object-cleanup-evidence-v1",new C1HashCanonical.Scalar(Convert.ToHexString(value.ObjectBindingDigest).ToLowerInvariant()),new C1HashCanonical.Scalar(value.State),new C1HashCanonical.Scalar(value.StateRevision.ToString(System.Globalization.CultureInfo.InvariantCulture)),new C1HashCanonical.Scalar("SourceConsumed"))));

    internal Task RecordObjectDeleteAcknowledgedAsync(SourceObjectLifecycleContext value,CancellationToken ct) => ObjectMutationAsync(
        "raw_export_record_provisional_object_delete_acknowledged",ct,("id",value.ObjectCustodyId),("revision",value.StateRevision),("status",204),
        ("evidence",C1HashCanonical.Compute("tip-88c1-object-delete-ack-evidence-v1",new C1HashCanonical.Scalar(Convert.ToHexString(value.ObjectBindingDigest).ToLowerInvariant()),new C1HashCanonical.Scalar(value.StateRevision.ToString(System.Globalization.CultureInfo.InvariantCulture)),new C1HashCanonical.Scalar("DeleteAcknowledged"),new C1HashCanonical.Scalar("204"))));

    internal Task<string> RevokeKeyAsync(Guid attemptKeyReservationId,CancellationToken ct) =>
        KeyMutationAsync("raw_export_revoke_attempt_key_reservation",attemptKeyReservationId,"SourceFinalizationSupersededAttempt",ct);

    internal Task<string> RequestKeyAbandonAsync(Guid attemptKeyReservationId,CancellationToken ct) =>
        KeyMutationAsync("raw_export_request_abandon_attempt_key_reservation",attemptKeyReservationId,"SourceFinalizationSupersededAttempt",ct);

    internal Task<string> FinalizeKeyAbandonAsync(Guid attemptKeyReservationId,CancellationToken ct) =>
        KeyMutationAsync("raw_export_finalize_abandon_attempt_key_reservation",attemptKeyReservationId,null,ct);

    private async Task<string> KeyMutationAsync(string function,Guid id,string? reason,CancellationToken ct)
    {
        await using var sql=CreateCommand(reason is null
            ? $"SELECT tagekyc.{function}(@id)"
            : $"SELECT tagekyc.{function}(@id,@reason)");
        Add(sql,"id",NpgsqlDbType.Uuid,id);
        if(reason is not null) Add(sql,"reason",NpgsqlDbType.Text,reason);
        var result=await sql.ExecuteScalarAsync(ct).ConfigureAwait(false);
        return result as string ?? throw new InvalidOperationException("RAW_EXPORT_FINALIZATION_EMPTY_KEY_RESULT");
    }

    private async Task<string> KeyScalarAsync(string function,CancellationToken ct,params (string Name,object Value)[] values)
    {
        await using var sql=CreateCommand(string.Empty);var args=new List<string>();
        foreach(var (name,value) in values){args.Add("@"+name);sql.Parameters.AddWithValue(name,value);}
        sql.CommandText=$"SELECT tagekyc.{function}({string.Join(',',args)})";
        return await sql.ExecuteScalarAsync(ct).ConfigureAwait(false) as string
            ?? throw new InvalidOperationException("RAW_EXPORT_FINALIZATION_EMPTY_KEY_RESULT");
    }

    private async Task ObjectMutationAsync(string function,CancellationToken ct,params (string Name,object Value)[] values)
    {
        await using var sql=CreateCommand(string.Empty); var args=new List<string>(); foreach(var (name,value) in values){args.Add("@"+name);sql.Parameters.AddWithValue(name,value);} sql.CommandText=$"SELECT * FROM tagekyc.{function}({string.Join(',',args)})";
        await using var reader=await sql.ExecuteReaderAsync(CommandBehavior.SingleRow,ct).ConfigureAwait(false); if(!await reader.ReadAsync(ct).ConfigureAwait(false)) throw new InvalidOperationException("RAW_EXPORT_FINALIZATION_EMPTY_OBJECT_RESULT");
        var outcome=reader.GetString(reader.GetOrdinal("OutcomeCode")); if(outcome is not ("CleanupRequired" or "ExistingMatch" or "Deleted" or "Quarantined")) throw new InvalidOperationException("RAW_EXPORT_FINALIZATION_OBJECT_STATE_CONFLICT");
    }

    private NpgsqlCommand CreateCommand(string text)
    {
        var connection=db.Database.GetDbConnection() as NpgsqlConnection ?? throw new InvalidOperationException("RAW_EXPORT_FINALIZATION_POSTGRES_REQUIRED");
        if(connection.State!=ConnectionState.Open) throw new InvalidOperationException("RAW_EXPORT_FINALIZATION_CONNECTION_NOT_OPEN");
        var command=connection.CreateCommand(); command.CommandText=text;
        command.Transaction=db.Database.CurrentTransaction?.GetDbTransaction() as NpgsqlTransaction;
        return command;
    }
    private static async Task<NpgsqlDataReader> ReadOneAsync(NpgsqlCommand sql,CancellationToken ct)
    {
        var reader=await sql.ExecuteReaderAsync(CommandBehavior.SingleRow,ct).ConfigureAwait(false);
        if(!await reader.ReadAsync(ct).ConfigureAwait(false)){await reader.DisposeAsync(); throw new InvalidOperationException("RAW_EXPORT_FINALIZATION_EMPTY_RESULT");}
        return reader;
    }
    private static void Add(NpgsqlCommand c,string n,NpgsqlDbType t,object v)=>c.Parameters.AddWithValue(n,t,v);
    private static TEnum Parse<TEnum>(NpgsqlDataReader r,string n) where TEnum:struct,Enum =>
        Enum.TryParse<TEnum>(r.GetString(r.GetOrdinal(n)),out var value)?value:throw new InvalidOperationException("RAW_EXPORT_FINALIZATION_UNKNOWN_OUTCOME");
    private static Guid? G(NpgsqlDataReader r,string n){var i=r.GetOrdinal(n);return r.IsDBNull(i)?null:r.GetGuid(i);}
    private static long? L(NpgsqlDataReader r,string n){var i=r.GetOrdinal(n);return r.IsDBNull(i)?null:r.GetInt64(i);}
    private static string? S(NpgsqlDataReader r,string n){var i=r.GetOrdinal(n);return r.IsDBNull(i)?null:r.GetString(i);}
    private static byte[]? B(NpgsqlDataReader r,string n){var i=r.GetOrdinal(n);return r.IsDBNull(i)?null:(byte[])r[i];}
    private static DateTimeOffset? T(NpgsqlDataReader r,string n){var i=r.GetOrdinal(n);return r.IsDBNull(i)?null:r.GetFieldValue<DateTimeOffset>(i);}
}
