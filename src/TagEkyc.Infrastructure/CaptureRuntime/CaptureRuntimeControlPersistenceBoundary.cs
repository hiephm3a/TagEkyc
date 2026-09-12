using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.CaptureRuntime;

namespace TagEkyc.Infrastructure.CaptureRuntime;

public sealed class CaptureRuntimeControlPersistenceBoundary(ICaptureRuntimeOperatorDbContextFactory contexts)
    : ICaptureRuntimeControlGateway
{
    public async Task<SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>> PublishTrustProfileAsync(
        AuthenticatedPlatformOperatorContext actor, CaptureRuntimeTrustProfilePublicationRequest request,
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
            command.CommandText = "SELECT * FROM tagekyc.capture_runtime_publish_trust_profile(@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9,@p10,@p11)";
            command.Parameters.Add(new NpgsqlParameter("p0", NpgsqlDbType.Uuid) { Value = actor.CredentialId });
            command.Parameters.Add(new NpgsqlParameter("p1", NpgsqlDbType.Uuid) { Value = idempotencyKey });
            command.Parameters.Add(new NpgsqlParameter("p2", NpgsqlDbType.Uuid) { Value = request.CatalogId });
            command.Parameters.Add(new NpgsqlParameter("p3", NpgsqlDbType.Bigint) { Value = request.ExpectedHeadRevision });
            command.Parameters.Add(new NpgsqlParameter("p4", NpgsqlDbType.TimestampTz) { Value = request.EffectiveAtUtc });
            command.Parameters.Add(new NpgsqlParameter("p5", NpgsqlDbType.TimestampTz) { Value = request.ExpiresAtUtc });
            command.Parameters.Add(new NpgsqlParameter("p6", NpgsqlDbType.Text) { Value = request.RuntimeType });
            command.Parameters.Add(new NpgsqlParameter("p7", NpgsqlDbType.Boolean) { Value = request.RetainedRawEnabled });
            command.Parameters.Add(new NpgsqlParameter("p8", NpgsqlDbType.Boolean) { Value = request.AllowTrustedEvidence });
            command.Parameters.Add(new NpgsqlParameter("p9", NpgsqlDbType.Boolean) { Value = request.RequireHandoffAttestation });
            command.Parameters.Add(new NpgsqlParameter("p10", NpgsqlDbType.Bytea) { Value = requestFingerprint });
            command.Parameters.Add(new NpgsqlParameter("p11", NpgsqlDbType.TimestampTz) { Value = now });
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                return SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
            var code = reader.IsDBNull(0) ? null : reader.GetString(0);
            if (!(code is "Applied" or "Replay"))
            {
                var error = MapFailure(code);
                await reader.DisposeAsync();
                await transaction.CommitAsync(cancellationToken);
                return SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>.Failure(error.Code, error.Message, error.StatusCode);
            }
            CaptureRuntimeCatalogPublicationResponse value = new(reader.GetGuid(1), reader.GetInt64(2), reader.GetInt64(3));
            if (await reader.ReadAsync(cancellationToken))
                return SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
            await reader.DisposeAsync();
            await transaction.CommitAsync(cancellationToken);
            return SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>.Success(value, isReplay: code == "Replay");
        }
        catch (PostgresException error)
        {
            var failure = MapException(error);
            return SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>.Failure(failure.Code, failure.Message, failure.StatusCode);
        }
        catch (Exception error) when (error is NpgsqlException or InvalidOperationException or InvalidCastException or ArgumentException)
        {
            return SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
        }
    }

    public async Task<SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>> PublishRolePolicyAsync(
        AuthenticatedPlatformOperatorContext actor, CaptureRuntimeRolePolicyPublicationRequest request,
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
            command.CommandText = "SELECT * FROM tagekyc.capture_runtime_publish_role_policy(@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7)";
            command.Parameters.Add(new NpgsqlParameter("p0", NpgsqlDbType.Uuid) { Value = actor.CredentialId });
            command.Parameters.Add(new NpgsqlParameter("p1", NpgsqlDbType.Uuid) { Value = idempotencyKey });
            command.Parameters.Add(new NpgsqlParameter("p2", NpgsqlDbType.Uuid) { Value = request.CatalogId });
            command.Parameters.Add(new NpgsqlParameter("p3", NpgsqlDbType.Bigint) { Value = request.ExpectedHeadRevision });
            command.Parameters.Add(new NpgsqlParameter("p4", NpgsqlDbType.TimestampTz) { Value = request.EffectiveAtUtc });
            command.Parameters.Add(new NpgsqlParameter("p5", NpgsqlDbType.Array | NpgsqlDbType.Text) { Value = request.Roles.ToArray() });
            command.Parameters.Add(new NpgsqlParameter("p6", NpgsqlDbType.Bytea) { Value = requestFingerprint });
            command.Parameters.Add(new NpgsqlParameter("p7", NpgsqlDbType.TimestampTz) { Value = now });
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                return SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
            var code = reader.IsDBNull(0) ? null : reader.GetString(0);
            if (!(code is "Applied" or "Replay"))
            {
                var error = MapFailure(code);
                await reader.DisposeAsync();
                await transaction.CommitAsync(cancellationToken);
                return SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>.Failure(error.Code, error.Message, error.StatusCode);
            }
            CaptureRuntimeCatalogPublicationResponse value = new(reader.GetGuid(1), reader.GetInt64(2), reader.GetInt64(3));
            if (await reader.ReadAsync(cancellationToken))
                return SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
            await reader.DisposeAsync();
            await transaction.CommitAsync(cancellationToken);
            return SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>.Success(value, isReplay: code == "Replay");
        }
        catch (PostgresException error)
        {
            var failure = MapException(error);
            return SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>.Failure(failure.Code, failure.Message, failure.StatusCode);
        }
        catch (Exception error) when (error is NpgsqlException or InvalidOperationException or InvalidCastException or ArgumentException)
        {
            return SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
        }
    }

    public async Task<SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>> PublishConfigurationAsync(
        AuthenticatedPlatformOperatorContext actor, CaptureRuntimeConfigurationPublicationRequest request,
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
            command.CommandText = "SELECT * FROM tagekyc.capture_runtime_publish_configuration(@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9,@p10,@p11,@p12,@p13,@p14,@p15,@p16,@p17)";
            command.Parameters.Add(new NpgsqlParameter("p0", NpgsqlDbType.Uuid) { Value = actor.CredentialId });
            command.Parameters.Add(new NpgsqlParameter("p1", NpgsqlDbType.Uuid) { Value = idempotencyKey });
            command.Parameters.Add(new NpgsqlParameter("p2", NpgsqlDbType.Uuid) { Value = request.CatalogId });
            command.Parameters.Add(new NpgsqlParameter("p3", NpgsqlDbType.Bigint) { Value = request.ExpectedHeadRevision });
            command.Parameters.Add(new NpgsqlParameter("p4", NpgsqlDbType.TimestampTz) { Value = request.EffectiveAtUtc });
            command.Parameters.Add(new NpgsqlParameter("p5", NpgsqlDbType.TimestampTz) { Value = request.ExpiresAtUtc });
            command.Parameters.Add(new NpgsqlParameter("p6", NpgsqlDbType.Boolean) { Value = request.RawExportEnabled });
            command.Parameters.Add(new NpgsqlParameter("p7", NpgsqlDbType.Integer) { Value = request.PlaintextBudgetSeconds });
            command.Parameters.Add(new NpgsqlParameter("p8", NpgsqlDbType.Integer) { Value = request.RawExportSourceClaimSafetyMarginMilliseconds });
            command.Parameters.Add(new NpgsqlParameter("p9", NpgsqlDbType.Integer) { Value = request.CaptureAgentConfigurationPollingIntervalSeconds });
            command.Parameters.Add(new NpgsqlParameter("p10", NpgsqlDbType.Integer) { Value = request.RawExportSourceMaximumChipDg2PortraitBytes });
            command.Parameters.Add(new NpgsqlParameter("p11", NpgsqlDbType.Integer) { Value = request.RawExportSourceMaximumLiveSelfieImageBytes });
            command.Parameters.Add(new NpgsqlParameter("p12", NpgsqlDbType.Bigint) { Value = request.RawExportCaptureMaximumAggregatePlaintextBytesPerHost });
            command.Parameters.Add(new NpgsqlParameter("p13", NpgsqlDbType.Integer) { Value = request.RawExportCustodyMaximumPlaintextWindowBytesPerStream });
            command.Parameters.Add(new NpgsqlParameter("p14", NpgsqlDbType.Bigint) { Value = request.RawExportCustodyMaximumAggregatePlaintextWindowBytesPerDeployment });
            command.Parameters.Add(new NpgsqlParameter("p15", NpgsqlDbType.Integer) { Value = request.RawExportIngressMaximumPreAdmissionBufferedBytes });
            command.Parameters.Add(new NpgsqlParameter("p16", NpgsqlDbType.Bytea) { Value = requestFingerprint });
            command.Parameters.Add(new NpgsqlParameter("p17", NpgsqlDbType.TimestampTz) { Value = now });
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                return SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
            var code = reader.IsDBNull(0) ? null : reader.GetString(0);
            if (!(code is "Applied" or "Replay"))
            {
                var error = MapFailure(code);
                await reader.DisposeAsync();
                await transaction.CommitAsync(cancellationToken);
                return SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>.Failure(error.Code, error.Message, error.StatusCode);
            }
            CaptureRuntimeCatalogPublicationResponse value = new(reader.GetGuid(1), reader.GetInt64(2), reader.GetInt64(3));
            if (await reader.ReadAsync(cancellationToken))
                return SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
            await reader.DisposeAsync();
            await transaction.CommitAsync(cancellationToken);
            return SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>.Success(value, isReplay: code == "Replay");
        }
        catch (PostgresException error)
        {
            var failure = MapException(error);
            return SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>.Failure(failure.Code, failure.Message, failure.StatusCode);
        }
        catch (Exception error) when (error is NpgsqlException or InvalidOperationException or InvalidCastException or ArgumentException)
        {
            return SessionOperationResult<CaptureRuntimeCatalogPublicationResponse>.Failure(CaptureRuntimeErrorCodes.NotReady, "Not ready.", 503);
        }
    }

    private static SessionOperationError MapFailure(string? code) => code switch
    {
        "InvalidInput" => new(CaptureRuntimeErrorCodes.RequestInvalid, "Request is invalid.", 400),
        "Denied" or "TerminalizedExpiredAndDenied" => new(CaptureRuntimeErrorCodes.AccessDenied, "Access denied.", 403),
        "ResourceNotAvailable" => new(CaptureRuntimeErrorCodes.ResourceNotAvailable, "Resource not available.", 404),
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
