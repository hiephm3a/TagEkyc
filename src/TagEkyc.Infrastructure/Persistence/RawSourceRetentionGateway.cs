using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using TagEkyc.Application.Ports;
using TagEkyc.Infrastructure.CaptureRuntime;

namespace TagEkyc.Infrastructure.Persistence;

public sealed class RawSourceRetentionGateway(ICaptureRuntimeDbContextFactory contexts) : IRawSourceRetentionGateway
{
    public async Task<RawSourceRecordResult> RecordAsync(RawSourceRecordCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await using var db = await contexts.CreateAsync(cancellationToken);
            await db.Database.OpenConnectionAsync(cancellationToken);
            var connection = (NpgsqlConnection)db.Database.GetDbConnection();
            await using var tx = await connection.BeginTransactionAsync(cancellationToken);
            await SetActor(connection, tx, request.PrincipalId, cancellationToken);
            await using var command = new NpgsqlCommand(
                "SELECT * FROM tagekyc.raw_source_record_consent_reference(@principal,@client,@session,@reference,@source,@revision,@textversion,@texthash,@from,@until,@key,@fingerprint)", connection, tx);
            Add(command, "principal", NpgsqlDbType.Uuid, request.PrincipalId);
            Add(command, "client", NpgsqlDbType.Uuid, request.ClientApplicationId);
            Add(command, "session", NpgsqlDbType.Uuid, request.VerificationSessionId);
            Add(command, "reference", NpgsqlDbType.Text, request.Request.ExternalConsentArtifactRef);
            Add(command, "source", NpgsqlDbType.Text, request.Request.SourceVersion);
            Add(command, "revision", NpgsqlDbType.Bigint, request.Request.ExpectedReferenceRevision);
            Add(command, "textversion", NpgsqlDbType.Text, request.Request.ConsentTextVersion);
            Add(command, "texthash", NpgsqlDbType.Text, request.Request.ConsentTextContentHash);
            Add(command, "from", NpgsqlDbType.TimestampTz, request.Request.ValidFromUtc);
            Add(command, "until", NpgsqlDbType.TimestampTz, request.Request.ValidUntilUtc);
            Add(command, "key", NpgsqlDbType.Uuid, request.IdempotencyKey);
            Add(command, "fingerprint", NpgsqlDbType.Bytea, request.RequestFingerprint);
            RawSourceRecordResult result;
            await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                if (reader.FieldCount != 4 || !await reader.ReadAsync(cancellationToken)) return RecordUnavailable();
                result = new(reader.GetString(0), Id(reader, 1), Revision(reader, 2), Id(reader, 3));
                if (!RecordShapeValid(result) || await reader.ReadAsync(cancellationToken)) return RecordUnavailable();
            }
            await tx.CommitAsync(cancellationToken);
            return result;
        }
        catch (NpgsqlException) { return RecordUnavailable(); }
    }

    public async Task<RawSourceWithdrawResult> WithdrawAsync(RawSourceWithdrawCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await using var db = await contexts.CreateAsync(cancellationToken);
            await db.Database.OpenConnectionAsync(cancellationToken);
            var connection = (NpgsqlConnection)db.Database.GetDbConnection();
            await using var tx = await connection.BeginTransactionAsync(cancellationToken);
            await SetActor(connection, tx, request.PrincipalId, cancellationToken);
            await using var command = new NpgsqlCommand(
                "SELECT * FROM tagekyc.raw_source_withdraw_consent_reference(@principal,@client,@reference,@revision,@source,@decision,@key,@fingerprint)", connection, tx);
            Add(command, "principal", NpgsqlDbType.Uuid, request.PrincipalId);
            Add(command, "client", NpgsqlDbType.Uuid, request.ClientApplicationId);
            Add(command, "reference", NpgsqlDbType.Uuid, request.ConsentReferenceId);
            Add(command, "revision", NpgsqlDbType.Bigint, request.Request.ExpectedReferenceRevision);
            Add(command, "source", NpgsqlDbType.Text, request.Request.SourceVersion);
            Add(command, "decision", NpgsqlDbType.Text, request.Request.DecisionRef);
            Add(command, "key", NpgsqlDbType.Uuid, request.IdempotencyKey);
            Add(command, "fingerprint", NpgsqlDbType.Bytea, request.RequestFingerprint);
            RawSourceWithdrawResult result;
            await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                if (reader.FieldCount != 3 || !await reader.ReadAsync(cancellationToken)) return WithdrawUnavailable();
                result = new(reader.GetString(0), Id(reader, 1), Revision(reader, 2));
                if (!WithdrawShapeValid(result) || await reader.ReadAsync(cancellationToken)) return WithdrawUnavailable();
            }
            await tx.CommitAsync(cancellationToken);
            return result;
        }
        catch (NpgsqlException) { return WithdrawUnavailable(); }
    }

    private static async Task SetActor(NpgsqlConnection connection, NpgsqlTransaction tx, Guid principal, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("SELECT pg_catalog.set_config('tagekyc.actor_principal_id',@actor,true)", connection, tx);
        Add(command, "actor", NpgsqlDbType.Text, principal.ToString("D"));
        await command.ExecuteNonQueryAsync(ct);
    }
    private static void Add(NpgsqlCommand command, string name, NpgsqlDbType type, object value) =>
        command.Parameters.AddWithValue(name, type, value);
    private static Guid? Id(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetGuid(ordinal);
    private static long? Revision(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);
    private static bool RecordShapeValid(RawSourceRecordResult value) => value.ResultCode switch
    {
        "Bound" or "Replay" => value.ConsentReferenceId is { } id && id != Guid.Empty &&
            value.ConsentReferenceRevision > 0 && value.ConsentBindingId is { } binding && binding != Guid.Empty,
        "Conflict" or "Denied" => value.ConsentReferenceId is null && value.ConsentReferenceRevision is null && value.ConsentBindingId is null,
        _ => false
    };
    private static bool WithdrawShapeValid(RawSourceWithdrawResult value) => value.ResultCode switch
    {
        "Withdrawn" or "Replay" => value.ConsentReferenceId is { } id && id != Guid.Empty && value.ConsentReferenceRevision > 0,
        "Conflict" or "Denied" => value.ConsentReferenceId is null && value.ConsentReferenceRevision is null,
        _ => false
    };
    private static RawSourceRecordResult RecordUnavailable() => new("NOT_READY", null, null, null);
    private static RawSourceWithdrawResult WithdrawUnavailable() => new("NOT_READY", null, null);
}
