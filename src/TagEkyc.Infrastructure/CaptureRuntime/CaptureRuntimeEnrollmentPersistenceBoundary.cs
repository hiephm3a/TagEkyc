using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.CaptureRuntime;

namespace TagEkyc.Infrastructure.CaptureRuntime;

/// <summary>R05 only. Cryptographic material preparation belongs to Application.</summary>
public sealed class CaptureRuntimeEnrollmentPersistenceBoundary(ICaptureRuntimeDbContextFactory contexts)
    : ICaptureRuntimeEnrollmentGateway
{
    public async Task<SessionOperationResult<int?>> ResolveBootstrapVerifierVersionAsync(
        Guid bootstrapIssuanceId, CancellationToken cancellationToken)
    {
        try
        {
            await using var db = await contexts.CreateAsync(cancellationToken).ConfigureAwait(false);
            await db.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT verifier_pepper_version FROM tagekyc.capture_runtime_resolve_bootstrap_verifier(@issuance)";
            command.Parameters.Add(new NpgsqlParameter("issuance", NpgsqlDbType.Uuid) { Value = bootstrapIssuanceId });
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                return SessionOperationResult<int?>.Success(null);
            if (reader.IsDBNull(0) || reader.GetInt32(0) <= 0)
                return Failure<int?>(CaptureRuntimeErrorCodes.NotReady, 503);
            var version = reader.GetInt32(0);
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                return Failure<int?>(CaptureRuntimeErrorCodes.NotReady, 503);
            return SessionOperationResult<int?>.Success(version);
        }
        catch (OperationCanceledException) { throw; }
        catch (NpgsqlException) { return Failure<int?>(CaptureRuntimeErrorCodes.NotReady, 503); }
    }

    public async Task<SessionOperationResult<CaptureRuntimeEnrollmentResponse>> RedeemBootstrapAsync(
        CaptureRuntimeEnrollmentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await using var db = await contexts.CreateAsync(cancellationToken).ConfigureAwait(false);
            await db.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            var connection = (NpgsqlConnection)db.Database.GetDbConnection();
            await using var command = connection.CreateCommand();
            command.Transaction = (NpgsqlTransaction)transaction.GetDbTransaction();
            command.CommandText = "SELECT * FROM tagekyc.capture_runtime_redeem_bootstrap(@issuance,@operation,@fingerprint,@candidate,@spki,@thumbprint,@signed,@nonce,@proof,@digest,@now)";
            command.Parameters.AddWithValue("issuance", NpgsqlDbType.Uuid, request.BootstrapIssuanceId);
            command.Parameters.AddWithValue("operation", NpgsqlDbType.Uuid, request.RedeemOperationId);
            command.Parameters.AddWithValue("fingerprint", NpgsqlDbType.Bytea, request.RequestFingerprint);
            command.Parameters.AddWithValue("candidate", NpgsqlDbType.Uuid, request.CandidateKeyId);
            command.Parameters.AddWithValue("spki", NpgsqlDbType.Bytea, request.PublicVerifierSpki);
            command.Parameters.AddWithValue("thumbprint", NpgsqlDbType.Bytea, request.PublicKeyThumbprint);
            command.Parameters.AddWithValue("signed", NpgsqlDbType.TimestampTz, request.SignedAtUtc);
            command.Parameters.AddWithValue("nonce", NpgsqlDbType.Bytea, request.Nonce);
            command.Parameters.AddWithValue("proof", NpgsqlDbType.Bytea, request.CandidateProof);
            command.Parameters.AddWithValue("digest", NpgsqlDbType.Bytea, request.BootstrapDigest);
            command.Parameters.AddWithValue("now", NpgsqlDbType.TimestampTz, request.NowUtc);
            SessionOperationResult<CaptureRuntimeEnrollmentResponse> result;
            await using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
            {
                if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    return Failure<CaptureRuntimeEnrollmentResponse>(CaptureRuntimeErrorCodes.NotReady, 503);
                result = Map(reader);
                if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    return Failure<CaptureRuntimeEnrollmentResponse>(CaptureRuntimeErrorCodes.NotReady, 503);
            }
            // Expiry-on-denied is a durable mutation: commit before mapping its 403.
            // Unknown/impossible result shapes roll back instead of authorizing success.
            if (result.Error?.StatusCode == 503) return result;
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }
        catch (OperationCanceledException) { throw; }
        catch (NpgsqlException) { return Failure<CaptureRuntimeEnrollmentResponse>(CaptureRuntimeErrorCodes.NotReady, 503); }
    }

    private static SessionOperationResult<CaptureRuntimeEnrollmentResponse> Map(NpgsqlDataReader reader)
    {
        if (reader.IsDBNull(0)) return Failure<CaptureRuntimeEnrollmentResponse>(CaptureRuntimeErrorCodes.NotReady, 503);
        var code = reader.GetString(0);
        if (code is "Created" or "Replay")
        {
            for (var column = 1; column <= 7; column++)
                if (reader.IsDBNull(column)) return Failure<CaptureRuntimeEnrollmentResponse>(CaptureRuntimeErrorCodes.NotReady, 503);
            var response = new CaptureRuntimeEnrollmentResponse(reader.GetGuid(1), reader.GetGuid(2), reader.GetGuid(3),
                reader.GetInt64(4), reader.GetInt64(5), reader.GetInt64(6), reader.GetInt64(7));
            if (response.CaptureAgentId == Guid.Empty || response.DeviceInstallationId == Guid.Empty ||
                response.CredentialId == Guid.Empty || response.Generation <= 0 || response.RuntimeRevision <= 0 ||
                response.InstallationRevision <= 0 || response.CredentialRevision <= 0)
                return Failure<CaptureRuntimeEnrollmentResponse>(CaptureRuntimeErrorCodes.NotReady, 503);
            return SessionOperationResult<CaptureRuntimeEnrollmentResponse>.Success(response, isReplay: code == "Replay");
        }
        for (var column = 1; column <= 7; column++)
            if (!reader.IsDBNull(column)) return Failure<CaptureRuntimeEnrollmentResponse>(CaptureRuntimeErrorCodes.NotReady, 503);
        return code switch
        {
            "InvalidInput" => Failure<CaptureRuntimeEnrollmentResponse>(CaptureRuntimeErrorCodes.RequestInvalid, 400),
            "Denied" or "TerminalizedExpiredAndDenied" => Failure<CaptureRuntimeEnrollmentResponse>(CaptureRuntimeErrorCodes.AccessDenied, 403),
            "Conflict" => Failure<CaptureRuntimeEnrollmentResponse>(CaptureRuntimeErrorCodes.Conflict, 409),
            _ => Failure<CaptureRuntimeEnrollmentResponse>(CaptureRuntimeErrorCodes.NotReady, 503)
        };
    }

    private static SessionOperationResult<T> Failure<T>(string code, int status) =>
        SessionOperationResult<T>.Failure(code, status == 503 ? "Capture Runtime is not ready." : "Enrollment request was denied.", status);
}
