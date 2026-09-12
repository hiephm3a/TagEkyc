using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.CaptureRuntime;

namespace TagEkyc.Infrastructure.CaptureRuntime;

public sealed class CaptureRuntimeManagementPersistenceBoundary(ICaptureRuntimeOperatorDbContextFactory contexts)
    : ICaptureRuntimeManagementGateway
{
    public async Task<SessionOperationResult<CaptureRuntimeBootstrapPersistenceResult>> IssueBootstrapAsync(
        AuthenticatedPlatformOperatorContext actor, CaptureRuntimeBootstrapIssueRequest request,
        Guid idempotencyKey, byte[] requestFingerprint,
        string keyLookupPrefix, byte[] secretDigest, int verifierPepperVersion,
        DateTimeOffset now, CancellationToken cancellationToken)
    {
        try
        {
            await using var db = await contexts.CreateAsync(cancellationToken);
            await db.Database.OpenConnectionAsync(cancellationToken);
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            await using var command = (NpgsqlCommand)db.Database.GetDbConnection().CreateCommand();
            command.Transaction = (NpgsqlTransaction)transaction.GetDbTransaction();
            command.CommandText = "SELECT * FROM tagekyc.capture_runtime_issue_bootstrap(@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9,@p10,@p11,@p12,@p13,@p14,@p15)";
            command.Parameters.Add(new NpgsqlParameter("p0", NpgsqlDbType.Uuid) { Value = actor.CredentialId });
            command.Parameters.Add(new NpgsqlParameter("p1", NpgsqlDbType.Uuid) { Value = idempotencyKey });
            command.Parameters.Add(new NpgsqlParameter("p2", NpgsqlDbType.Text) { Value = request.RuntimeType });
            command.Parameters.Add(new NpgsqlParameter("p3", NpgsqlDbType.Uuid) { Value = request.TrustProfileId });
            command.Parameters.Add(new NpgsqlParameter("p4", NpgsqlDbType.Bigint) { Value = request.TrustProfileRevision });
            command.Parameters.Add(new NpgsqlParameter("p5", NpgsqlDbType.Uuid) { Value = request.RolePolicyId });
            command.Parameters.Add(new NpgsqlParameter("p6", NpgsqlDbType.Bigint) { Value = request.RolePolicyRevision });
            command.Parameters.Add(new NpgsqlParameter("p7", NpgsqlDbType.Uuid) { Value = request.ConfigurationId });
            command.Parameters.Add(new NpgsqlParameter("p8", NpgsqlDbType.Bigint) { Value = request.ConfigurationRevision });
            command.Parameters.Add(new NpgsqlParameter("p9", NpgsqlDbType.TimestampTz) { Value = request.ExpiresAtUtc });
            command.Parameters.Add(new NpgsqlParameter("p10", NpgsqlDbType.Bytea) { Value = Convert.FromHexString(request.HandoffAttestationDigest) });
            command.Parameters.Add(new NpgsqlParameter("p11", NpgsqlDbType.Text) { Value = keyLookupPrefix });
            command.Parameters.Add(new NpgsqlParameter("p12", NpgsqlDbType.Bytea) { Value = secretDigest });
            command.Parameters.Add(new NpgsqlParameter("p13", NpgsqlDbType.Integer) { Value = verifierPepperVersion });
            command.Parameters.Add(new NpgsqlParameter("p14", NpgsqlDbType.Bytea) { Value = requestFingerprint });
            command.Parameters.Add(new NpgsqlParameter("p15", NpgsqlDbType.TimestampTz) { Value = now });
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                return SessionOperationResult<CaptureRuntimeBootstrapPersistenceResult>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
            var code = reader.IsDBNull(0) ? null : reader.GetString(0);
            if (!(code == "Created" && reader.GetBoolean(2)))
            {
                var error = MapFailure(code);
                await reader.DisposeAsync();
                await transaction.CommitAsync(cancellationToken);
                return SessionOperationResult<CaptureRuntimeBootstrapPersistenceResult>.Failure(error.Code, error.Message, error.StatusCode);
            }
            CaptureRuntimeBootstrapPersistenceResult value = new(reader.GetGuid(1), reader.GetBoolean(2), reader.GetFieldValue<DateTimeOffset>(3), reader.GetInt64(4));
            if (await reader.ReadAsync(cancellationToken))
                return SessionOperationResult<CaptureRuntimeBootstrapPersistenceResult>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
            await reader.DisposeAsync();
            await transaction.CommitAsync(cancellationToken);
            return SessionOperationResult<CaptureRuntimeBootstrapPersistenceResult>.Success(value, isReplay: code == "Replay");
        }
        catch (PostgresException error)
        {
            var failure = MapException(error);
            return SessionOperationResult<CaptureRuntimeBootstrapPersistenceResult>.Failure(failure.Code, failure.Message, failure.StatusCode);
        }
        catch (Exception error) when (error is NpgsqlException or InvalidOperationException or InvalidCastException or ArgumentException)
        {
            return SessionOperationResult<CaptureRuntimeBootstrapPersistenceResult>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
        }
    }

    public async Task<SessionOperationResult<CaptureRuntimeBootstrapLifecycleResponse>> RevokeBootstrapAsync(
        AuthenticatedPlatformOperatorContext actor, CaptureRuntimeBootstrapRevokeRequest request,
        Guid idempotencyKey, byte[] requestFingerprint,
        DateTimeOffset now, CancellationToken cancellationToken)
    {
        try
        {
            await using var db = await contexts.CreateAsync(cancellationToken);
            await db.Database.OpenConnectionAsync(cancellationToken);
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            await using var command = (NpgsqlCommand)db.Database.GetDbConnection().CreateCommand();
            command.Transaction = (NpgsqlTransaction)transaction.GetDbTransaction();
            command.CommandText = "SELECT * FROM tagekyc.capture_runtime_revoke_bootstrap(@p0,@p1,@p2,@p3,@p4,@p5,@p6)";
            command.Parameters.Add(new NpgsqlParameter("p0", NpgsqlDbType.Uuid) { Value = actor.CredentialId });
            command.Parameters.Add(new NpgsqlParameter("p1", NpgsqlDbType.Uuid) { Value = idempotencyKey });
            command.Parameters.Add(new NpgsqlParameter("p2", NpgsqlDbType.Uuid) { Value = request.BootstrapIssuanceId });
            command.Parameters.Add(new NpgsqlParameter("p3", NpgsqlDbType.Bigint) { Value = request.ExpectedRevision });
            command.Parameters.Add(new NpgsqlParameter("p4", NpgsqlDbType.Text) { Value = request.Reason });
            command.Parameters.Add(new NpgsqlParameter("p5", NpgsqlDbType.Bytea) { Value = requestFingerprint });
            command.Parameters.Add(new NpgsqlParameter("p6", NpgsqlDbType.TimestampTz) { Value = now });
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                return SessionOperationResult<CaptureRuntimeBootstrapLifecycleResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
            var code = reader.IsDBNull(0) ? null : reader.GetString(0);
            if (!(code == "Revoked"))
            {
                var error = MapFailure(code);
                await reader.DisposeAsync();
                await transaction.CommitAsync(cancellationToken);
                return SessionOperationResult<CaptureRuntimeBootstrapLifecycleResponse>.Failure(error.Code, error.Message, error.StatusCode);
            }
            CaptureRuntimeBootstrapLifecycleResponse value = new(reader.GetGuid(1), reader.GetString(2), reader.GetInt64(3), reader.IsDBNull(4) ? null : reader.GetFieldValue<DateTimeOffset>(4));
            if (await reader.ReadAsync(cancellationToken))
                return SessionOperationResult<CaptureRuntimeBootstrapLifecycleResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
            await reader.DisposeAsync();
            await transaction.CommitAsync(cancellationToken);
            return SessionOperationResult<CaptureRuntimeBootstrapLifecycleResponse>.Success(value, isReplay: code == "Replay");
        }
        catch (PostgresException error)
        {
            var failure = MapException(error);
            return SessionOperationResult<CaptureRuntimeBootstrapLifecycleResponse>.Failure(failure.Code, failure.Message, failure.StatusCode);
        }
        catch (Exception error) when (error is NpgsqlException or InvalidOperationException or InvalidCastException or ArgumentException)
        {
            return SessionOperationResult<CaptureRuntimeBootstrapLifecycleResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
        }
    }

    public async Task<SessionOperationResult<CaptureRuntimeLifecycleResponse>> SuspendRuntimeAsync(
        AuthenticatedPlatformOperatorContext actor, RuntimeLifecycleRequest request,
        Guid idempotencyKey, byte[] requestFingerprint,
        DateTimeOffset now, CancellationToken cancellationToken)
    {
        try
        {
            await using var db = await contexts.CreateAsync(cancellationToken);
            await db.Database.OpenConnectionAsync(cancellationToken);
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            await using var command = (NpgsqlCommand)db.Database.GetDbConnection().CreateCommand();
            command.Transaction = (NpgsqlTransaction)transaction.GetDbTransaction();
            command.CommandText = "SELECT * FROM tagekyc.capture_runtime_suspend(@p0,@p1,@p2,@p3,@p4,@p5,@p6)";
            command.Parameters.Add(new NpgsqlParameter("p0", NpgsqlDbType.Uuid) { Value = actor.CredentialId });
            command.Parameters.Add(new NpgsqlParameter("p1", NpgsqlDbType.Uuid) { Value = idempotencyKey });
            command.Parameters.Add(new NpgsqlParameter("p2", NpgsqlDbType.Uuid) { Value = request.CaptureAgentId });
            command.Parameters.Add(new NpgsqlParameter("p3", NpgsqlDbType.Bigint) { Value = request.ExpectedRevision });
            command.Parameters.Add(new NpgsqlParameter("p4", NpgsqlDbType.Text) { Value = request.Reason });
            command.Parameters.Add(new NpgsqlParameter("p5", NpgsqlDbType.Bytea) { Value = requestFingerprint });
            command.Parameters.Add(new NpgsqlParameter("p6", NpgsqlDbType.TimestampTz) { Value = now });
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                return SessionOperationResult<CaptureRuntimeLifecycleResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
            var code = reader.IsDBNull(0) ? null : reader.GetString(0);
            if (!(code == "Applied"))
            {
                var error = MapFailure(code);
                await reader.DisposeAsync();
                await transaction.CommitAsync(cancellationToken);
                return SessionOperationResult<CaptureRuntimeLifecycleResponse>.Failure(error.Code, error.Message, error.StatusCode);
            }
            CaptureRuntimeLifecycleResponse value = new(reader.GetGuid(1), reader.GetString(2), reader.GetInt64(3), reader.GetFieldValue<DateTimeOffset>(4));
            if (await reader.ReadAsync(cancellationToken))
                return SessionOperationResult<CaptureRuntimeLifecycleResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
            await reader.DisposeAsync();
            await transaction.CommitAsync(cancellationToken);
            return SessionOperationResult<CaptureRuntimeLifecycleResponse>.Success(value, isReplay: code == "Replay");
        }
        catch (PostgresException error)
        {
            var failure = MapException(error);
            return SessionOperationResult<CaptureRuntimeLifecycleResponse>.Failure(failure.Code, failure.Message, failure.StatusCode);
        }
        catch (Exception error) when (error is NpgsqlException or InvalidOperationException or InvalidCastException or ArgumentException)
        {
            return SessionOperationResult<CaptureRuntimeLifecycleResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
        }
    }

    public async Task<SessionOperationResult<CaptureRuntimeLifecycleResponse>> ReactivateRuntimeAsync(
        AuthenticatedPlatformOperatorContext actor, RuntimeLifecycleRequest request,
        Guid idempotencyKey, byte[] requestFingerprint,
        DateTimeOffset now, CancellationToken cancellationToken)
    {
        try
        {
            await using var db = await contexts.CreateAsync(cancellationToken);
            await db.Database.OpenConnectionAsync(cancellationToken);
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            await using var command = (NpgsqlCommand)db.Database.GetDbConnection().CreateCommand();
            command.Transaction = (NpgsqlTransaction)transaction.GetDbTransaction();
            command.CommandText = "SELECT * FROM tagekyc.capture_runtime_reactivate(@p0,@p1,@p2,@p3,@p4,@p5,@p6)";
            command.Parameters.Add(new NpgsqlParameter("p0", NpgsqlDbType.Uuid) { Value = actor.CredentialId });
            command.Parameters.Add(new NpgsqlParameter("p1", NpgsqlDbType.Uuid) { Value = idempotencyKey });
            command.Parameters.Add(new NpgsqlParameter("p2", NpgsqlDbType.Uuid) { Value = request.CaptureAgentId });
            command.Parameters.Add(new NpgsqlParameter("p3", NpgsqlDbType.Bigint) { Value = request.ExpectedRevision });
            command.Parameters.Add(new NpgsqlParameter("p4", NpgsqlDbType.Text) { Value = request.Reason });
            command.Parameters.Add(new NpgsqlParameter("p5", NpgsqlDbType.Bytea) { Value = requestFingerprint });
            command.Parameters.Add(new NpgsqlParameter("p6", NpgsqlDbType.TimestampTz) { Value = now });
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                return SessionOperationResult<CaptureRuntimeLifecycleResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
            var code = reader.IsDBNull(0) ? null : reader.GetString(0);
            if (!(code == "Applied"))
            {
                var error = MapFailure(code);
                await reader.DisposeAsync();
                await transaction.CommitAsync(cancellationToken);
                return SessionOperationResult<CaptureRuntimeLifecycleResponse>.Failure(error.Code, error.Message, error.StatusCode);
            }
            CaptureRuntimeLifecycleResponse value = new(reader.GetGuid(1), reader.GetString(2), reader.GetInt64(3), reader.GetFieldValue<DateTimeOffset>(4));
            if (await reader.ReadAsync(cancellationToken))
                return SessionOperationResult<CaptureRuntimeLifecycleResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
            await reader.DisposeAsync();
            await transaction.CommitAsync(cancellationToken);
            return SessionOperationResult<CaptureRuntimeLifecycleResponse>.Success(value, isReplay: code == "Replay");
        }
        catch (PostgresException error)
        {
            var failure = MapException(error);
            return SessionOperationResult<CaptureRuntimeLifecycleResponse>.Failure(failure.Code, failure.Message, failure.StatusCode);
        }
        catch (Exception error) when (error is NpgsqlException or InvalidOperationException or InvalidCastException or ArgumentException)
        {
            return SessionOperationResult<CaptureRuntimeLifecycleResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
        }
    }

    public async Task<SessionOperationResult<CaptureRuntimeLifecycleResponse>> RevokeRuntimeAsync(
        AuthenticatedPlatformOperatorContext actor, RuntimeLifecycleRequest request,
        Guid idempotencyKey, byte[] requestFingerprint,
        DateTimeOffset now, CancellationToken cancellationToken)
    {
        try
        {
            await using var db = await contexts.CreateAsync(cancellationToken);
            await db.Database.OpenConnectionAsync(cancellationToken);
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            await using var command = (NpgsqlCommand)db.Database.GetDbConnection().CreateCommand();
            command.Transaction = (NpgsqlTransaction)transaction.GetDbTransaction();
            command.CommandText = "SELECT * FROM tagekyc.capture_runtime_revoke(@p0,@p1,@p2,@p3,@p4,@p5,@p6)";
            command.Parameters.Add(new NpgsqlParameter("p0", NpgsqlDbType.Uuid) { Value = actor.CredentialId });
            command.Parameters.Add(new NpgsqlParameter("p1", NpgsqlDbType.Uuid) { Value = idempotencyKey });
            command.Parameters.Add(new NpgsqlParameter("p2", NpgsqlDbType.Uuid) { Value = request.CaptureAgentId });
            command.Parameters.Add(new NpgsqlParameter("p3", NpgsqlDbType.Bigint) { Value = request.ExpectedRevision });
            command.Parameters.Add(new NpgsqlParameter("p4", NpgsqlDbType.Text) { Value = request.Reason });
            command.Parameters.Add(new NpgsqlParameter("p5", NpgsqlDbType.Bytea) { Value = requestFingerprint });
            command.Parameters.Add(new NpgsqlParameter("p6", NpgsqlDbType.TimestampTz) { Value = now });
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                return SessionOperationResult<CaptureRuntimeLifecycleResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
            var code = reader.IsDBNull(0) ? null : reader.GetString(0);
            if (!(code == "Applied"))
            {
                var error = MapFailure(code);
                await reader.DisposeAsync();
                await transaction.CommitAsync(cancellationToken);
                return SessionOperationResult<CaptureRuntimeLifecycleResponse>.Failure(error.Code, error.Message, error.StatusCode);
            }
            CaptureRuntimeLifecycleResponse value = new(reader.GetGuid(1), reader.GetString(2), reader.GetInt64(3), reader.GetFieldValue<DateTimeOffset>(4));
            if (await reader.ReadAsync(cancellationToken))
                return SessionOperationResult<CaptureRuntimeLifecycleResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
            await reader.DisposeAsync();
            await transaction.CommitAsync(cancellationToken);
            return SessionOperationResult<CaptureRuntimeLifecycleResponse>.Success(value, isReplay: code == "Replay");
        }
        catch (PostgresException error)
        {
            var failure = MapException(error);
            return SessionOperationResult<CaptureRuntimeLifecycleResponse>.Failure(failure.Code, failure.Message, failure.StatusCode);
        }
        catch (Exception error) when (error is NpgsqlException or InvalidOperationException or InvalidCastException or ArgumentException)
        {
            return SessionOperationResult<CaptureRuntimeLifecycleResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
        }
    }

    public async Task<SessionOperationResult<CaptureRuntimeLifecycleResponse>> RetireRuntimeAsync(
        AuthenticatedPlatformOperatorContext actor, RuntimeLifecycleRequest request,
        Guid idempotencyKey, byte[] requestFingerprint,
        DateTimeOffset now, CancellationToken cancellationToken)
    {
        try
        {
            await using var db = await contexts.CreateAsync(cancellationToken);
            await db.Database.OpenConnectionAsync(cancellationToken);
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            await using var command = (NpgsqlCommand)db.Database.GetDbConnection().CreateCommand();
            command.Transaction = (NpgsqlTransaction)transaction.GetDbTransaction();
            command.CommandText = "SELECT * FROM tagekyc.capture_runtime_retire(@p0,@p1,@p2,@p3,@p4,@p5,@p6)";
            command.Parameters.Add(new NpgsqlParameter("p0", NpgsqlDbType.Uuid) { Value = actor.CredentialId });
            command.Parameters.Add(new NpgsqlParameter("p1", NpgsqlDbType.Uuid) { Value = idempotencyKey });
            command.Parameters.Add(new NpgsqlParameter("p2", NpgsqlDbType.Uuid) { Value = request.CaptureAgentId });
            command.Parameters.Add(new NpgsqlParameter("p3", NpgsqlDbType.Bigint) { Value = request.ExpectedRevision });
            command.Parameters.Add(new NpgsqlParameter("p4", NpgsqlDbType.Text) { Value = request.Reason });
            command.Parameters.Add(new NpgsqlParameter("p5", NpgsqlDbType.Bytea) { Value = requestFingerprint });
            command.Parameters.Add(new NpgsqlParameter("p6", NpgsqlDbType.TimestampTz) { Value = now });
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                return SessionOperationResult<CaptureRuntimeLifecycleResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
            var code = reader.IsDBNull(0) ? null : reader.GetString(0);
            if (!(code == "Applied"))
            {
                var error = MapFailure(code);
                await reader.DisposeAsync();
                await transaction.CommitAsync(cancellationToken);
                return SessionOperationResult<CaptureRuntimeLifecycleResponse>.Failure(error.Code, error.Message, error.StatusCode);
            }
            CaptureRuntimeLifecycleResponse value = new(reader.GetGuid(1), reader.GetString(2), reader.GetInt64(3), reader.GetFieldValue<DateTimeOffset>(4));
            if (await reader.ReadAsync(cancellationToken))
                return SessionOperationResult<CaptureRuntimeLifecycleResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
            await reader.DisposeAsync();
            await transaction.CommitAsync(cancellationToken);
            return SessionOperationResult<CaptureRuntimeLifecycleResponse>.Success(value, isReplay: code == "Replay");
        }
        catch (PostgresException error)
        {
            var failure = MapException(error);
            return SessionOperationResult<CaptureRuntimeLifecycleResponse>.Failure(failure.Code, failure.Message, failure.StatusCode);
        }
        catch (Exception error) when (error is NpgsqlException or InvalidOperationException or InvalidCastException or ArgumentException)
        {
            return SessionOperationResult<CaptureRuntimeLifecycleResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
        }
    }

    public async Task<SessionOperationResult<CaptureRuntimeCredentialResponse>> RevokeCredentialAsync(
        AuthenticatedPlatformOperatorContext actor, CaptureRuntimeCredentialRevokeRequest request,
        Guid idempotencyKey, byte[] requestFingerprint,
        DateTimeOffset now, CancellationToken cancellationToken)
    {
        try
        {
            await using var db = await contexts.CreateAsync(cancellationToken);
            await db.Database.OpenConnectionAsync(cancellationToken);
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            await using var command = (NpgsqlCommand)db.Database.GetDbConnection().CreateCommand();
            command.Transaction = (NpgsqlTransaction)transaction.GetDbTransaction();
            command.CommandText = "SELECT * FROM tagekyc.capture_runtime_revoke_credential(@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9)";
            command.Parameters.Add(new NpgsqlParameter("p0", NpgsqlDbType.Uuid) { Value = actor.CredentialId });
            command.Parameters.Add(new NpgsqlParameter("p1", NpgsqlDbType.Uuid) { Value = idempotencyKey });
            command.Parameters.Add(new NpgsqlParameter("p2", NpgsqlDbType.Uuid) { Value = request.CaptureAgentId });
            command.Parameters.Add(new NpgsqlParameter("p3", NpgsqlDbType.Uuid) { Value = request.InstallationId });
            command.Parameters.Add(new NpgsqlParameter("p4", NpgsqlDbType.Uuid) { Value = request.CredentialId });
            command.Parameters.Add(new NpgsqlParameter("p5", NpgsqlDbType.Bigint) { Value = request.Generation });
            command.Parameters.Add(new NpgsqlParameter("p6", NpgsqlDbType.Bigint) { Value = request.ExpectedCredentialRevision });
            command.Parameters.Add(new NpgsqlParameter("p7", NpgsqlDbType.Text) { Value = request.Reason });
            command.Parameters.Add(new NpgsqlParameter("p8", NpgsqlDbType.Bytea) { Value = requestFingerprint });
            command.Parameters.Add(new NpgsqlParameter("p9", NpgsqlDbType.TimestampTz) { Value = now });
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                return SessionOperationResult<CaptureRuntimeCredentialResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
            var code = reader.IsDBNull(0) ? null : reader.GetString(0);
            if (!(code == "Revoked"))
            {
                var error = MapFailure(code);
                await reader.DisposeAsync();
                await transaction.CommitAsync(cancellationToken);
                return SessionOperationResult<CaptureRuntimeCredentialResponse>.Failure(error.Code, error.Message, error.StatusCode);
            }
            CaptureRuntimeCredentialResponse value = new(reader.GetGuid(1), reader.GetInt64(2), reader.GetString(3), reader.GetInt64(4), reader.IsDBNull(5) ? null : reader.GetFieldValue<DateTimeOffset>(5));
            if (await reader.ReadAsync(cancellationToken))
                return SessionOperationResult<CaptureRuntimeCredentialResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
            await reader.DisposeAsync();
            await transaction.CommitAsync(cancellationToken);
            return SessionOperationResult<CaptureRuntimeCredentialResponse>.Success(value, isReplay: code == "Replay");
        }
        catch (PostgresException error)
        {
            var failure = MapException(error);
            return SessionOperationResult<CaptureRuntimeCredentialResponse>.Failure(failure.Code, failure.Message, failure.StatusCode);
        }
        catch (Exception error) when (error is NpgsqlException or InvalidOperationException or InvalidCastException or ArgumentException)
        {
            return SessionOperationResult<CaptureRuntimeCredentialResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
        }
    }

    public async Task<SessionOperationResult<CaptureRuntimeRotationResponse>> AuthorizeRotationAsync(
        AuthenticatedPlatformOperatorContext actor, CaptureRuntimeRotationAuthorizeRequest request,
        Guid idempotencyKey, byte[] requestFingerprint,
        DateTimeOffset now, CancellationToken cancellationToken)
    {
        try
        {
            await using var db = await contexts.CreateAsync(cancellationToken);
            await db.Database.OpenConnectionAsync(cancellationToken);
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            await using var command = (NpgsqlCommand)db.Database.GetDbConnection().CreateCommand();
            command.Transaction = (NpgsqlTransaction)transaction.GetDbTransaction();
            command.CommandText = "SELECT * FROM tagekyc.capture_runtime_authorize_rotation(@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9)";
            command.Parameters.Add(new NpgsqlParameter("p0", NpgsqlDbType.Uuid) { Value = actor.CredentialId });
            command.Parameters.Add(new NpgsqlParameter("p1", NpgsqlDbType.Uuid) { Value = idempotencyKey });
            command.Parameters.Add(new NpgsqlParameter("p2", NpgsqlDbType.Uuid) { Value = request.CaptureAgentId });
            command.Parameters.Add(new NpgsqlParameter("p3", NpgsqlDbType.Uuid) { Value = request.InstallationId });
            command.Parameters.Add(new NpgsqlParameter("p4", NpgsqlDbType.Uuid) { Value = request.CredentialId });
            command.Parameters.Add(new NpgsqlParameter("p5", NpgsqlDbType.Bigint) { Value = request.CurrentGeneration });
            command.Parameters.Add(new NpgsqlParameter("p6", NpgsqlDbType.Bigint) { Value = request.ExpectedCredentialRevision });
            command.Parameters.Add(new NpgsqlParameter("p7", NpgsqlDbType.TimestampTz) { Value = request.ExpiresAtUtc });
            command.Parameters.Add(new NpgsqlParameter("p8", NpgsqlDbType.Bytea) { Value = requestFingerprint });
            command.Parameters.Add(new NpgsqlParameter("p9", NpgsqlDbType.TimestampTz) { Value = now });
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                return SessionOperationResult<CaptureRuntimeRotationResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
            var code = reader.IsDBNull(0) ? null : reader.GetString(0);
            if (!(code is "Applied" or "Replay"))
            {
                var error = MapFailure(code);
                await reader.DisposeAsync();
                await transaction.CommitAsync(cancellationToken);
                return SessionOperationResult<CaptureRuntimeRotationResponse>.Failure(error.Code, error.Message, error.StatusCode);
            }
            CaptureRuntimeRotationResponse value = new(reader.GetGuid(1), reader.GetGuid(2), reader.GetGuid(3), reader.GetGuid(4), reader.GetInt64(5), reader.GetString(6), reader.GetInt64(7), reader.GetFieldValue<DateTimeOffset>(8));
            if (await reader.ReadAsync(cancellationToken))
                return SessionOperationResult<CaptureRuntimeRotationResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
            await reader.DisposeAsync();
            await transaction.CommitAsync(cancellationToken);
            return SessionOperationResult<CaptureRuntimeRotationResponse>.Success(value, isReplay: code == "Replay");
        }
        catch (PostgresException error)
        {
            var failure = MapException(error);
            return SessionOperationResult<CaptureRuntimeRotationResponse>.Failure(failure.Code, failure.Message, failure.StatusCode);
        }
        catch (Exception error) when (error is NpgsqlException or InvalidOperationException or InvalidCastException or ArgumentException)
        {
            return SessionOperationResult<CaptureRuntimeRotationResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
        }
    }

    public async Task<SessionOperationResult<CaptureRuntimeRotationLifecycleResponse>> RevokeRotationAsync(
        AuthenticatedPlatformOperatorContext actor, CaptureRuntimeRotationRevokeRequest request,
        Guid idempotencyKey, byte[] requestFingerprint,
        DateTimeOffset now, CancellationToken cancellationToken)
    {
        try
        {
            await using var db = await contexts.CreateAsync(cancellationToken);
            await db.Database.OpenConnectionAsync(cancellationToken);
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            await using var command = (NpgsqlCommand)db.Database.GetDbConnection().CreateCommand();
            command.Transaction = (NpgsqlTransaction)transaction.GetDbTransaction();
            command.CommandText = "SELECT * FROM tagekyc.capture_runtime_revoke_rotation(@p0,@p1,@p2,@p3,@p4,@p5,@p6)";
            command.Parameters.Add(new NpgsqlParameter("p0", NpgsqlDbType.Uuid) { Value = actor.CredentialId });
            command.Parameters.Add(new NpgsqlParameter("p1", NpgsqlDbType.Uuid) { Value = idempotencyKey });
            command.Parameters.Add(new NpgsqlParameter("p2", NpgsqlDbType.Uuid) { Value = request.RotationId });
            command.Parameters.Add(new NpgsqlParameter("p3", NpgsqlDbType.Bigint) { Value = request.ExpectedRevision });
            command.Parameters.Add(new NpgsqlParameter("p4", NpgsqlDbType.Text) { Value = request.Reason });
            command.Parameters.Add(new NpgsqlParameter("p5", NpgsqlDbType.Bytea) { Value = requestFingerprint });
            command.Parameters.Add(new NpgsqlParameter("p6", NpgsqlDbType.TimestampTz) { Value = now });
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                return SessionOperationResult<CaptureRuntimeRotationLifecycleResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
            var code = reader.IsDBNull(0) ? null : reader.GetString(0);
            if (!(code is "Applied" or "Replay"))
            {
                var error = MapFailure(code);
                await reader.DisposeAsync();
                await transaction.CommitAsync(cancellationToken);
                return SessionOperationResult<CaptureRuntimeRotationLifecycleResponse>.Failure(error.Code, error.Message, error.StatusCode);
            }
            CaptureRuntimeRotationLifecycleResponse value = new(reader.GetGuid(1), reader.GetString(2), reader.GetInt64(3), reader.IsDBNull(4) ? null : reader.GetFieldValue<DateTimeOffset>(4));
            if (await reader.ReadAsync(cancellationToken))
                return SessionOperationResult<CaptureRuntimeRotationLifecycleResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
            await reader.DisposeAsync();
            await transaction.CommitAsync(cancellationToken);
            return SessionOperationResult<CaptureRuntimeRotationLifecycleResponse>.Success(value, isReplay: code == "Replay");
        }
        catch (PostgresException error)
        {
            var failure = MapException(error);
            return SessionOperationResult<CaptureRuntimeRotationLifecycleResponse>.Failure(failure.Code, failure.Message, failure.StatusCode);
        }
        catch (Exception error) when (error is NpgsqlException or InvalidOperationException or InvalidCastException or ArgumentException)
        {
            return SessionOperationResult<CaptureRuntimeRotationLifecycleResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
        }
    }

    public async Task<SessionOperationResult<CaptureRuntimeRolePolicyAssignmentResponse>> AssignRolePolicyAsync(
        AuthenticatedPlatformOperatorContext actor, CaptureRuntimeRolePolicyAssignmentRequest request,
        Guid idempotencyKey, byte[] requestFingerprint,
        DateTimeOffset now, CancellationToken cancellationToken)
    {
        try
        {
            await using var db = await contexts.CreateAsync(cancellationToken);
            await db.Database.OpenConnectionAsync(cancellationToken);
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            await using var command = (NpgsqlCommand)db.Database.GetDbConnection().CreateCommand();
            command.Transaction = (NpgsqlTransaction)transaction.GetDbTransaction();
            command.CommandText = "SELECT * FROM tagekyc.capture_runtime_assign_role_policy(@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7)";
            command.Parameters.Add(new NpgsqlParameter("p0", NpgsqlDbType.Uuid) { Value = actor.CredentialId });
            command.Parameters.Add(new NpgsqlParameter("p1", NpgsqlDbType.Uuid) { Value = idempotencyKey });
            command.Parameters.Add(new NpgsqlParameter("p2", NpgsqlDbType.Uuid) { Value = request.AgentId });
            command.Parameters.Add(new NpgsqlParameter("p3", NpgsqlDbType.Uuid) { Value = request.RolePolicyId });
            command.Parameters.Add(new NpgsqlParameter("p4", NpgsqlDbType.Bigint) { Value = request.RolePolicyRevision });
            command.Parameters.Add(new NpgsqlParameter("p5", NpgsqlDbType.Bigint) { Value = request.ExpectedRuntimeRevision });
            command.Parameters.Add(new NpgsqlParameter("p6", NpgsqlDbType.Bytea) { Value = requestFingerprint });
            command.Parameters.Add(new NpgsqlParameter("p7", NpgsqlDbType.TimestampTz) { Value = now });
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                return SessionOperationResult<CaptureRuntimeRolePolicyAssignmentResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
            var code = reader.IsDBNull(0) ? null : reader.GetString(0);
            if (!(code is "Applied" or "Replay"))
            {
                var error = MapFailure(code);
                await reader.DisposeAsync();
                await transaction.CommitAsync(cancellationToken);
                return SessionOperationResult<CaptureRuntimeRolePolicyAssignmentResponse>.Failure(error.Code, error.Message, error.StatusCode);
            }
            CaptureRuntimeRolePolicyAssignmentResponse value = new(reader.GetGuid(1), reader.GetGuid(2), reader.GetInt64(3), reader.GetInt64(4));
            if (await reader.ReadAsync(cancellationToken))
                return SessionOperationResult<CaptureRuntimeRolePolicyAssignmentResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
            await reader.DisposeAsync();
            await transaction.CommitAsync(cancellationToken);
            return SessionOperationResult<CaptureRuntimeRolePolicyAssignmentResponse>.Success(value, isReplay: code == "Replay");
        }
        catch (PostgresException error)
        {
            var failure = MapException(error);
            return SessionOperationResult<CaptureRuntimeRolePolicyAssignmentResponse>.Failure(failure.Code, failure.Message, failure.StatusCode);
        }
        catch (Exception error) when (error is NpgsqlException or InvalidOperationException or InvalidCastException or ArgumentException)
        {
            return SessionOperationResult<CaptureRuntimeRolePolicyAssignmentResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
        }
    }

    public async Task<SessionOperationResult<CaptureRuntimeConfigurationAssignmentResponse>> AssignConfigurationAsync(
        AuthenticatedPlatformOperatorContext actor, CaptureRuntimeConfigurationAssignmentRequest request,
        Guid idempotencyKey, byte[] requestFingerprint,
        DateTimeOffset now, CancellationToken cancellationToken)
    {
        try
        {
            await using var db = await contexts.CreateAsync(cancellationToken);
            await db.Database.OpenConnectionAsync(cancellationToken);
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            await using var command = (NpgsqlCommand)db.Database.GetDbConnection().CreateCommand();
            command.Transaction = (NpgsqlTransaction)transaction.GetDbTransaction();
            command.CommandText = "SELECT * FROM tagekyc.capture_runtime_assign_configuration(@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8)";
            command.Parameters.Add(new NpgsqlParameter("p0", NpgsqlDbType.Uuid) { Value = actor.CredentialId });
            command.Parameters.Add(new NpgsqlParameter("p1", NpgsqlDbType.Uuid) { Value = idempotencyKey });
            command.Parameters.Add(new NpgsqlParameter("p2", NpgsqlDbType.Uuid) { Value = request.AgentId });
            command.Parameters.Add(new NpgsqlParameter("p3", NpgsqlDbType.Uuid) { Value = request.ConfigurationId });
            command.Parameters.Add(new NpgsqlParameter("p4", NpgsqlDbType.Bigint) { Value = request.ConfigurationRevision });
            command.Parameters.Add(new NpgsqlParameter("p5", NpgsqlDbType.Bigint) { Value = request.ExpectedRuntimeRevision });
            command.Parameters.Add(new NpgsqlParameter("p6", NpgsqlDbType.Uuid) { Value = request.OverrideId.HasValue ? request.OverrideId.Value : DBNull.Value });
            command.Parameters.Add(new NpgsqlParameter("p7", NpgsqlDbType.Bytea) { Value = requestFingerprint });
            command.Parameters.Add(new NpgsqlParameter("p8", NpgsqlDbType.TimestampTz) { Value = now });
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                return SessionOperationResult<CaptureRuntimeConfigurationAssignmentResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
            var code = reader.IsDBNull(0) ? null : reader.GetString(0);
            if (!(code is "Applied" or "Replay"))
            {
                var error = MapFailure(code);
                await reader.DisposeAsync();
                await transaction.CommitAsync(cancellationToken);
                return SessionOperationResult<CaptureRuntimeConfigurationAssignmentResponse>.Failure(error.Code, error.Message, error.StatusCode);
            }
            CaptureRuntimeConfigurationAssignmentResponse value = new(reader.GetGuid(1), reader.GetGuid(2), reader.GetInt64(3), reader.IsDBNull(4) ? null : reader.GetGuid(4), reader.GetInt64(5));
            if (await reader.ReadAsync(cancellationToken))
                return SessionOperationResult<CaptureRuntimeConfigurationAssignmentResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
            await reader.DisposeAsync();
            await transaction.CommitAsync(cancellationToken);
            return SessionOperationResult<CaptureRuntimeConfigurationAssignmentResponse>.Success(value, isReplay: code == "Replay");
        }
        catch (PostgresException error)
        {
            var failure = MapException(error);
            return SessionOperationResult<CaptureRuntimeConfigurationAssignmentResponse>.Failure(failure.Code, failure.Message, failure.StatusCode);
        }
        catch (Exception error) when (error is NpgsqlException or InvalidOperationException or InvalidCastException or ArgumentException)
        {
            return SessionOperationResult<CaptureRuntimeConfigurationAssignmentResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
        }
    }

    public async Task<SessionOperationResult<CaptureRuntimeReadinessResponse>> ReadReadinessAsync(
        AuthenticatedPlatformOperatorContext actor, Guid captureAgentId, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var db = await contexts.CreateAsync(cancellationToken);
            await db.Database.OpenConnectionAsync(cancellationToken);
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            await using var command = (NpgsqlCommand)db.Database.GetDbConnection().CreateCommand();
            command.Transaction = (NpgsqlTransaction)transaction.GetDbTransaction();
            command.CommandText = "SELECT * FROM tagekyc.capture_runtime_read_readiness(@id,@now)";
            command.Parameters.AddWithValue("id", NpgsqlDbType.Uuid, captureAgentId);
            command.Parameters.AddWithValue("now", NpgsqlDbType.TimestampTz, now);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                return SessionOperationResult<CaptureRuntimeReadinessResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
            var resultCode = reader.GetString(0);
            if (resultCode != "AVAILABLE")
            {
                var error = MapFailure(resultCode);
                return SessionOperationResult<CaptureRuntimeReadinessResponse>.Failure(error.Code, error.Message, error.StatusCode);
            }
            var value = new CaptureRuntimeReadinessResponse(reader.GetGuid(1), reader.GetString(2),
                reader.GetInt64(3), reader.GetString(4), reader.GetInt64(5), reader.GetString(6),
                reader.GetInt64(7), reader.GetInt64(8), reader.GetInt64(9), reader.GetInt64(10),
                reader.GetInt64(11), reader.GetFieldValue<int[]>(12), reader.GetBoolean(13),
                reader.GetBoolean(14), reader.GetBoolean(15), false, false);
            if (await reader.ReadAsync(cancellationToken))
                return SessionOperationResult<CaptureRuntimeReadinessResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
            await reader.DisposeAsync();
            await transaction.CommitAsync(cancellationToken);
            return SessionOperationResult<CaptureRuntimeReadinessResponse>.Success(value);
        }
        catch (PostgresException error)
        {
            var failure = MapException(error);
            return SessionOperationResult<CaptureRuntimeReadinessResponse>.Failure(failure.Code, failure.Message, failure.StatusCode);
        }
        catch (Exception error) when (error is NpgsqlException or InvalidOperationException or InvalidCastException or ArgumentException)
        {
            return SessionOperationResult<CaptureRuntimeReadinessResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
        }
    }

    private static SessionOperationError MapFailure(string? code) => code switch
    {
        "InvalidInput" => new(CaptureRuntimeErrorCodes.RequestInvalid, "Request is invalid.", 400),
        "Denied" or "TerminalizedExpiredAndDenied" => new(CaptureRuntimeErrorCodes.AccessDenied, "Access denied.", 403),
        "ResourceNotAvailable" or "RESOURCE_NOT_AVAILABLE" => new(CaptureRuntimeErrorCodes.ResourceNotAvailable, "Resource not available.", 404),
        "Conflict" => new(CaptureRuntimeErrorCodes.Conflict, "Conflict.", 409),
        "ExistingMatchSecretUnavailable" => new(CaptureRuntimeErrorCodes.ExistingMatchSecretUnavailable, "Secret unavailable.", 409),
        _ => new(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503)
    };

    private static SessionOperationError MapException(PostgresException error) => error.MessageText switch
    {
        "TIP88C1C6BA_OPERATOR_DENIED" => new(CaptureRuntimeErrorCodes.AccessDenied, "Access denied.", 403),
        "TIP88C1C6BA_ROTATION_INPUT_INVALID" or "TIP88C1C6BA_OVERRIDE_INVALID" => new(CaptureRuntimeErrorCodes.RequestInvalid, "Request is invalid.", 400),
        "TIP88C1C6BA_ROLE_NOT_FOUND" or "TIP88C1C6BA_CONFIGURATION_NOT_FOUND" => new(CaptureRuntimeErrorCodes.ResourceNotAvailable, "Resource not available.", 404),
        "TIP88C1C6BA_ROTATION_CONFLICT" or "TIP88C1C6BA_RUNTIME_CONFLICT"
            or "TIP88C1C6BA_TRUST_HEAD_CONFLICT" or "TIP88C1C6BA_ROLE_HEAD_CONFLICT"
            or "TIP88C1C6BA_CONFIGURATION_HEAD_CONFLICT" => new(CaptureRuntimeErrorCodes.Conflict, "Conflict.", 409),
        _ => new(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503)
    };
}
