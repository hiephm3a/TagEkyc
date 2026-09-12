using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.CaptureRuntime;

namespace TagEkyc.Infrastructure.CaptureRuntime;

// Each public method selects one literal, ratified callable; there is no operation dispatcher.
public sealed class CaptureRuntimeExecutionPersistenceBoundary(ICaptureRuntimeDbContextFactory contexts)
    : ICaptureRuntimeExecutionGateway
{
    public async Task<CaptureCapabilityPersistenceResult> IssueOrReplaceCapabilityAsync(
        CaptureCapabilityPersistenceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await using var db = await contexts.CreateAsync(cancellationToken);
            await db.Database.OpenConnectionAsync(cancellationToken);
            var connection = (NpgsqlConnection)db.Database.GetDbConnection();
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(
                "SELECT * FROM tagekyc.capture_runtime_issue_or_replace_capability(@client,@session,@action,@current,@revision,@key,@new,@prefix,@digest,@version,@fingerprint,@now)", connection, transaction);
            Add(command, "client", NpgsqlDbType.Uuid, request.ClientApplicationId);
            Add(command, "session", NpgsqlDbType.Uuid, request.VerificationSessionId);
            Add(command, "action", NpgsqlDbType.Text, request.Request.Action);
            Add(command, "current", NpgsqlDbType.Uuid, request.Request.CurrentCapabilityId);
            Add(command, "revision", NpgsqlDbType.Bigint, request.Request.ExpectedRevision);
            Add(command, "key", NpgsqlDbType.Uuid, request.IdempotencyKey);
            Add(command, "new", NpgsqlDbType.Uuid, request.NewCapabilityId);
            Add(command, "prefix", NpgsqlDbType.Text, request.LookupPrefix);
            Add(command, "digest", NpgsqlDbType.Bytea, request.Digest);
            Add(command, "version", NpgsqlDbType.Integer, request.PepperVersion);
            Add(command, "fingerprint", NpgsqlDbType.Bytea, request.RequestFingerprint);
            Add(command, "now", NpgsqlDbType.TimestampTz, request.Now);
            CaptureCapabilityPersistenceResult result;
            await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                if (!await reader.ReadAsync(cancellationToken)) return UnavailableCapability();
                result = new(reader.GetString(0), NullableGuid(reader, 1), reader.GetBoolean(2),
                    NullableTime(reader, 3), reader.IsDBNull(4) ? null : reader.GetString(4),
                    reader.IsDBNull(5) ? null : reader.GetInt64(5));
                if (await reader.ReadAsync(cancellationToken)) return UnavailableCapability();
            }
            // Expiry-on-denied is an authoritative transition, not a reason to roll B back.
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (NpgsqlException) { return UnavailableCapability(); }
    }

    public async Task<SessionOperationResult<CaptureCapabilityVerifier>> ResolveCapabilityVerifierAsync(
        Guid capabilityId, CancellationToken cancellationToken)
    {
        try
        {
        await using var db = await contexts.CreateAsync(cancellationToken);
        await db.Database.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT * FROM tagekyc.capture_runtime_resolve_capability_verifier(@capability)",
            (NpgsqlConnection)db.Database.GetDbConnection());
        Add(command, "capability", NpgsqlDbType.Uuid, capabilityId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return Failure<CaptureCapabilityVerifier>("ACCESS_DENIED");
        var value = new CaptureCapabilityVerifier(reader.GetFieldValue<byte[]>(0), reader.GetInt32(1));
        if (value.Digest.Length != 32 || value.PepperVersion <= 0 || await reader.ReadAsync(cancellationToken))
            return Failure<CaptureCapabilityVerifier>("NOT_READY");
        return SessionOperationResult<CaptureCapabilityVerifier>.Success(value);
        }
        catch (NpgsqlException) { return Failure<CaptureCapabilityVerifier>("NOT_READY"); }
    }

    public async Task<SessionOperationResult<CaptureRuntimeBindingResponse>> BindCapabilityAsync(
        CaptureRuntimeBindPersistenceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await using var db = await contexts.CreateAsync(cancellationToken);
            await db.Database.OpenConnectionAsync(cancellationToken);
            var connection = (NpgsqlConnection)db.Database.GetDbConnection();
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(
                "SELECT * FROM tagekyc.capture_runtime_bind_capability(@agent,@installation,@credential,@generation,@capability,@verified,@operation,@fingerprint,@now)",
                connection, transaction);
            Actor(command, request.Actor);
            Add(command, "capability", NpgsqlDbType.Uuid, request.CapabilityId);
            Add(command, "verified", NpgsqlDbType.Boolean, request.SecretVerified);
            Add(command, "operation", NpgsqlDbType.Uuid, request.BindOperationId);
            Add(command, "fingerprint", NpgsqlDbType.Bytea, request.RequestFingerprint);
            Add(command, "now", NpgsqlDbType.TimestampTz, request.Now);
            SessionOperationResult<CaptureRuntimeBindingResponse> result;
            await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                result = await ReadBinding(reader, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (NpgsqlException) { return Failure<CaptureRuntimeBindingResponse>("NOT_READY"); }
    }

    public async Task<SessionOperationResult<CaptureRuntimeBindingResponse>> ReconcileBindingAsync(
        AuthenticatedCaptureRuntimeContext actor, CaptureRuntimeReconcileRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var db = await contexts.CreateAsync(cancellationToken);
            await db.Database.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(
                "SELECT * FROM tagekyc.capture_runtime_reconcile_binding(@agent,@installation,@credential,@generation,@capability,@operation,@now)",
                (NpgsqlConnection)db.Database.GetDbConnection());
            Actor(command, actor);
            Add(command, "capability", NpgsqlDbType.Uuid, request.CaptureCapabilityId);
            Add(command, "operation", NpgsqlDbType.Uuid, request.BindOperationId);
            Add(command, "now", NpgsqlDbType.TimestampTz, DateTimeOffset.UtcNow);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await ReadBinding(reader, cancellationToken);
        }
        catch (NpgsqlException) { return Failure<CaptureRuntimeBindingResponse>("NOT_READY"); }
    }

    public async Task<SessionOperationResult<CaptureRuntimeConfigurationResponse>> ResolveConfigurationAsync(
        AuthenticatedCaptureRuntimeContext actor, CancellationToken cancellationToken)
    {
        try
        {
            await using var db = await contexts.CreateAsync(cancellationToken);
            await db.Database.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(
                "SELECT * FROM tagekyc.capture_runtime_resolve_configuration(@agent,@installation,@credential,@generation,@now)",
                (NpgsqlConnection)db.Database.GetDbConnection());
            Actor(command, actor);
            Add(command, "now", NpgsqlDbType.TimestampTz, DateTimeOffset.UtcNow);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return Failure<CaptureRuntimeConfigurationResponse>("ACCESS_DENIED");
            var code = reader.GetString(0);
            if (code != "AVAILABLE") return Failure<CaptureRuntimeConfigurationResponse>(code);
            for (var column = 1; column < 16; column++)
                if (reader.IsDBNull(column)) return Failure<CaptureRuntimeConfigurationResponse>("NOT_READY");
            var value = new CaptureRuntimeConfigurationResponse(
                reader.GetGuid(1), reader.GetGuid(2), reader.GetInt64(3),
                reader.GetFieldValue<DateTimeOffset>(4), reader.GetFieldValue<DateTimeOffset>(5),
                reader.GetBoolean(6), reader.GetInt32(7), reader.GetInt32(8), reader.GetInt32(9),
                reader.GetInt32(10), reader.GetInt32(11), reader.GetInt64(12),
                reader.GetInt32(13), reader.GetInt64(14), reader.GetInt32(15));
            if (await reader.ReadAsync(cancellationToken)) return Failure<CaptureRuntimeConfigurationResponse>("NOT_READY");
            return SessionOperationResult<CaptureRuntimeConfigurationResponse>.Success(value);
        }
        catch (NpgsqlException) { return Failure<CaptureRuntimeConfigurationResponse>("NOT_READY"); }
    }

    private static async Task<SessionOperationResult<CaptureRuntimeBindingResponse>> ReadBinding(
        NpgsqlDataReader reader, CancellationToken cancellationToken)
    {
        if (!await reader.ReadAsync(cancellationToken)) return Failure<CaptureRuntimeBindingResponse>("NOT_READY");
        var code = reader.GetString(0);
        if (code is not ("CREATED" or "AVAILABLE")) return Failure<CaptureRuntimeBindingResponse>(code);
        for (var i = 1; i < 7; i++) if (reader.IsDBNull(i)) return Failure<CaptureRuntimeBindingResponse>("NOT_READY");
        var value = new CaptureRuntimeBindingResponse(reader.GetGuid(1), reader.GetFieldValue<DateTimeOffset>(2),
            reader.GetInt64(3), reader.GetInt64(4), reader.GetInt64(5), reader.GetInt64(6));
        if (value.BindingId == Guid.Empty || value.RuntimeRevision <= 0 || value.InstallationRevision <= 0 ||
            value.CredentialRevision <= 0 || value.CapabilityRevision <= 0 ||
            await reader.ReadAsync(cancellationToken)) return Failure<CaptureRuntimeBindingResponse>("NOT_READY");
        return SessionOperationResult<CaptureRuntimeBindingResponse>.Success(value, isReplay: code == "AVAILABLE");
    }

    private static void Actor(NpgsqlCommand command, AuthenticatedCaptureRuntimeContext actor)
    {
        Add(command, "agent", NpgsqlDbType.Uuid, actor.CaptureAgentId);
        Add(command, "installation", NpgsqlDbType.Uuid, actor.DeviceInstallationId);
        Add(command, "credential", NpgsqlDbType.Uuid, actor.CredentialId);
        Add(command, "generation", NpgsqlDbType.Bigint, actor.CredentialGeneration);
    }

    private static void Add(NpgsqlCommand command, string name, NpgsqlDbType type, object? value) =>
        command.Parameters.Add(new NpgsqlParameter(name, type) { Value = value ?? DBNull.Value });
    private static Guid? NullableGuid(NpgsqlDataReader reader, int index) => reader.IsDBNull(index) ? null : reader.GetGuid(index);
    private static DateTimeOffset? NullableTime(NpgsqlDataReader reader, int index) => reader.IsDBNull(index) ? null : reader.GetFieldValue<DateTimeOffset>(index);
    private static CaptureCapabilityPersistenceResult UnavailableCapability() => new("NOT_READY", null, false, null, null, null);
    private static SessionOperationResult<T> Failure<T>(string code) => code switch
    {
        "INVALID_INPUT" => SessionOperationResult<T>.Failure("REQUEST_INVALID", "Request is invalid.", 400),
        "ACCESS_DENIED" => SessionOperationResult<T>.Failure("ACCESS_DENIED", "Access denied.", 403),
        "RESOURCE_NOT_AVAILABLE" => SessionOperationResult<T>.Failure("RESOURCE_NOT_AVAILABLE", "Resource not available.", 404),
        "CONFLICT" or "TERMINALIZED_EXPIRED_AND_DENIED" => SessionOperationResult<T>.Failure("CONFLICT", "Conflict.", 409),
        _ => SessionOperationResult<T>.Failure("NOT_READY", "Capture Runtime is not ready.", 503)
    };
}
