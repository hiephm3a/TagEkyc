using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.CaptureRuntime;
using TagEkyc.Infrastructure.CaptureRuntime;

namespace TagEkyc.Infrastructure.Auth;

public sealed class CaptureRuntimeRequestAuthenticator(
    ICaptureRuntimeDbContextFactory contexts) : ICaptureRuntimeRequestAuthenticator, ICaptureRuntimeRotationCompletionAuthenticator
{
    private static readonly TimeSpan MaximumAge = TimeSpan.FromSeconds(120);
    private static readonly TimeSpan MaximumFutureSkew = TimeSpan.FromSeconds(30);

    public async Task<SessionOperationResult<AuthenticatedCaptureRuntimeContext>> AuthenticateAsync(
        CaptureRuntimeSignedRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var now = DateTimeOffset.UtcNow;
        if (request.CredentialId == Guid.Empty || request.CredentialGeneration <= 0 ||
            request.Nonce is not { Length: 32 } || request.Signature is not { Length: 64 } ||
            string.IsNullOrWhiteSpace(request.RequiredRole) || request.ExactSignedPreimage.IsEmpty ||
            request.SignedAtUtc < now - MaximumAge || request.SignedAtUtc > now + MaximumFutureSkew)
        {
            return Denied();
        }

        try
        {
            await using var db = await contexts.CreateAsync(cancellationToken).ConfigureAwait(false);
            await db.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            var connection = (NpgsqlConnection)db.Database.GetDbConnection();

            var verifier = await ResolveVerifierAsync(
                connection,
                request.CredentialId,
                request.CredentialGeneration,
                now,
                cancellationToken).ConfigureAwait(false);
            if (verifier is null || !Verify(verifier.PublicVerifierSpki, verifier.PublicKeyThumbprint,
                    request.ExactSignedPreimage.Span, request.Signature))
            {
                return Denied();
            }

            var claim = await ClaimNonceAsync(connection, verifier, request, now, cancellationToken)
                .ConfigureAwait(false);
            if (claim is null || !string.Equals(claim.ResultCode, "ADMITTED", StringComparison.Ordinal))
            {
                return Denied();
            }

            return SessionOperationResult<AuthenticatedCaptureRuntimeContext>.Success(
                new AuthenticatedCaptureRuntimeContext(
                    verifier.CaptureAgentId,
                    verifier.DeviceInstallationId,
                    request.CredentialId,
                    request.CredentialGeneration,
                    verifier.PublicKeyThumbprint,
                    claim.RolePolicyId,
                    claim.RolePolicyRevision,
                    claim.RuntimeRevision,
                    claim.InstallationRevision,
                    claim.CredentialRevision,
                    request.SignedAtUtc,
                    request.Nonce.ToArray(),
                    SHA256.HashData(request.ExactSignedPreimage.Span)));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (NpgsqlException)
        {
            return NotReady();
        }
        catch (CryptographicException)
        {
            return Denied();
        }
    }


    public async Task<SessionOperationResult<CaptureRuntimeRotationAuthentication>> AuthenticateRotationCompletionAsync(
        CaptureRuntimeSignedRequest request, Guid rotationId, CaptureRuntimeRotationCompleteRequest body,
        CaptureRuntimeRotationFingerprintCandidates candidates, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(candidates);
        var now = DateTimeOffset.UtcNow;
        if (request.CredentialId == Guid.Empty || request.CredentialGeneration <= 0 ||
            rotationId == Guid.Empty || body.CandidateKeyId == Guid.Empty ||
            request.Nonce is not { Length: 32 } || request.Signature is not { Length: 64 } ||
            request.ExactSignedPreimage.IsEmpty ||
            request.SignedAtUtc < now - MaximumAge || request.SignedAtUtc > now + MaximumFutureSkew ||
            candidates.AsPredecessor is not null and not { Length: 32 } ||
            candidates.AsSuccessor is not null and not { Length: 32 } ||
            (candidates.AsPredecessor is null && candidates.AsSuccessor is null) ||
            body.SuccessorPublicKeyThumbprint is not { Length: 64 } ||
            body.SuccessorPublicKeyThumbprint.Any(c => c is not (>= '0' and <= '9') and not (>= 'a' and <= 'f')))
            return RotationFailure(CaptureRuntimeErrorCodes.AccessDenied, 403);
        try
        {
            await using var db = await contexts.CreateAsync(cancellationToken).ConfigureAwait(false);
            await db.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            var connection = (NpgsqlConnection)db.Database.GetDbConnection();

            // No rotation/completion lookup precedes this one presented-verifier read.
            var verifier = await ResolveVerifierAsync(connection, request.CredentialId,
                request.CredentialGeneration, now, cancellationToken).ConfigureAwait(false);
            if (verifier is null || !Verify(verifier.PublicVerifierSpki, verifier.PublicKeyThumbprint,
                    request.ExactSignedPreimage.Span, request.Signature))
                return RotationFailure(CaptureRuntimeErrorCodes.AccessDenied, 403);

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                SELECT * FROM tagekyc.capture_runtime_claim_rotation_completion_nonce(
                  @credential,@generation,@rotation,@candidate,@thumbprint,@predecessor,@successor,@nonce,@signed,@now)
                """;
            command.Parameters.AddWithValue("credential", NpgsqlDbType.Uuid, request.CredentialId);
            command.Parameters.AddWithValue("generation", NpgsqlDbType.Bigint, request.CredentialGeneration);
            command.Parameters.AddWithValue("rotation", NpgsqlDbType.Uuid, rotationId);
            command.Parameters.AddWithValue("candidate", NpgsqlDbType.Uuid, body.CandidateKeyId);
            command.Parameters.AddWithValue("thumbprint", NpgsqlDbType.Bytea, Convert.FromHexString(body.SuccessorPublicKeyThumbprint));
            command.Parameters.AddWithValue("predecessor", NpgsqlDbType.Bytea, (object?)candidates.AsPredecessor ?? DBNull.Value);
            command.Parameters.AddWithValue("successor", NpgsqlDbType.Bytea, (object?)candidates.AsSuccessor ?? DBNull.Value);
            command.Parameters.AddWithValue("nonce", NpgsqlDbType.Bytea, request.Nonce);
            command.Parameters.AddWithValue("signed", NpgsqlDbType.TimestampTz, request.SignedAtUtc);
            command.Parameters.AddWithValue("now", NpgsqlDbType.TimestampTz, now);
            CaptureRuntimeRotationAuthentication result;
            await using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
            {
                if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    return RotationFailure(CaptureRuntimeErrorCodes.AccessDenied, 403);
                for (var column = 0; column < 7; column++)
                    if (reader.IsDBNull(column)) return RotationFailure(CaptureRuntimeErrorCodes.NotReady, 503);
                var branch = reader.GetString(0) switch
                {
                    "Predecessor" => CaptureRuntimeRotationBranch.Predecessor,
                    "Successor" => CaptureRuntimeRotationBranch.Successor,
                    _ => (CaptureRuntimeRotationBranch)(-1)
                };
                var selected = reader.GetFieldValue<byte[]>(1);
                var expected = branch switch
                {
                    CaptureRuntimeRotationBranch.Predecessor => candidates.AsPredecessor,
                    CaptureRuntimeRotationBranch.Successor => candidates.AsSuccessor,
                    _ => null
                };
                if (expected is null || selected.Length != 32 ||
                    !CryptographicOperations.FixedTimeEquals(expected, selected) ||
                    reader.GetInt64(2) <= 0 || reader.GetInt64(3) <= 0 || reader.GetInt64(4) <= 0 ||
                    reader.GetGuid(5) == Guid.Empty || reader.GetInt64(6) <= 0)
                    return RotationFailure(CaptureRuntimeErrorCodes.NotReady, 503);
                result = new CaptureRuntimeRotationAuthentication(new AuthenticatedCaptureRuntimeContext(
                    verifier.CaptureAgentId, verifier.DeviceInstallationId, request.CredentialId,
                    request.CredentialGeneration, verifier.PublicKeyThumbprint,
                    reader.GetGuid(5), reader.GetInt64(6), reader.GetInt64(2), reader.GetInt64(3), reader.GetInt64(4),
                    request.SignedAtUtc, request.Nonce.ToArray(), SHA256.HashData(request.ExactSignedPreimage.Span)),
                    branch, selected);
                if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    return RotationFailure(CaptureRuntimeErrorCodes.NotReady, 503);
            }
            // Separate durable N. Later ROTATE1 or B failure must not erase this claim.
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return SessionOperationResult<CaptureRuntimeRotationAuthentication>.Success(result);
        }
        catch (OperationCanceledException) { throw; }
        catch (NpgsqlException) { return RotationFailure(CaptureRuntimeErrorCodes.NotReady, 503); }
        catch (CryptographicException) { return RotationFailure(CaptureRuntimeErrorCodes.AccessDenied, 403); }
    }

    private static SessionOperationResult<CaptureRuntimeRotationAuthentication> RotationFailure(string code, int status) =>
        SessionOperationResult<CaptureRuntimeRotationAuthentication>.Failure(code,
            status == 503 ? "Capture Runtime authentication is not ready." : "Access denied.", status);


    private static async Task<VerifierRow?> ResolveVerifierAsync(
        NpgsqlConnection connection,
        Guid credentialId,
        long generation,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM tagekyc.capture_runtime_resolve_verifier(@credential,@generation,@now)";
        command.Parameters.Add(new NpgsqlParameter("credential", NpgsqlDbType.Uuid) { Value = credentialId });
        command.Parameters.Add(new NpgsqlParameter("generation", NpgsqlDbType.Bigint) { Value = generation });
        command.Parameters.Add(new NpgsqlParameter("now", NpgsqlDbType.TimestampTz) { Value = now });
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return new VerifierRow(
            reader.GetGuid(0), reader.GetGuid(1), reader.GetFieldValue<byte[]>(4),
            reader.GetFieldValue<byte[]>(5));
    }

    private static async Task<NonceClaimRow?> ClaimNonceAsync(
        NpgsqlConnection connection,
        VerifierRow verifier,
        CaptureRuntimeSignedRequest request,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM tagekyc.capture_runtime_claim_nonce(@agent,@installation,@credential,@generation,@role,@nonce,@signed,@admitted)";
        command.Parameters.Add(new NpgsqlParameter("agent", NpgsqlDbType.Uuid) { Value = verifier.CaptureAgentId });
        command.Parameters.Add(new NpgsqlParameter("installation", NpgsqlDbType.Uuid) { Value = verifier.DeviceInstallationId });
        command.Parameters.Add(new NpgsqlParameter("credential", NpgsqlDbType.Uuid) { Value = request.CredentialId });
        command.Parameters.Add(new NpgsqlParameter("generation", NpgsqlDbType.Bigint) { Value = request.CredentialGeneration });
        command.Parameters.Add(new NpgsqlParameter("role", NpgsqlDbType.Text) { Value = request.RequiredRole });
        command.Parameters.Add(new NpgsqlParameter("nonce", NpgsqlDbType.Bytea) { Value = request.Nonce });
        command.Parameters.Add(new NpgsqlParameter("signed", NpgsqlDbType.TimestampTz) { Value = request.SignedAtUtc });
        command.Parameters.Add(new NpgsqlParameter("admitted", NpgsqlDbType.TimestampTz) { Value = now });
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        var result = reader.GetString(0);
        if (!string.Equals(result, "ADMITTED", StringComparison.Ordinal))
        {
            return new NonceClaimRow(result, 0, 0, 0, Guid.Empty, 0);
        }

        return new NonceClaimRow(
            result, reader.GetInt64(1), reader.GetInt64(2), reader.GetInt64(3),
            reader.GetGuid(4), reader.GetInt64(5));
    }

    private static bool Verify(
        byte[] publicVerifierSpki,
        byte[] expectedThumbprint,
        ReadOnlySpan<byte> exactPreimage,
        byte[] signature)
    {
        if (publicVerifierSpki.Length != 91 || expectedThumbprint.Length != 32 ||
            !CryptographicOperations.FixedTimeEquals(SHA256.HashData(publicVerifierSpki), expectedThumbprint))
        {
            return false;
        }

        using var verifier = ECDsa.Create();
        verifier.ImportSubjectPublicKeyInfo(publicVerifierSpki, out var consumed);
        return consumed == publicVerifierSpki.Length && verifier.KeySize == 256 &&
               verifier.VerifyData(
                   exactPreimage,
                   signature,
                   HashAlgorithmName.SHA256,
                   DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
    }

    private static SessionOperationResult<AuthenticatedCaptureRuntimeContext> Denied() =>
        SessionOperationResult<AuthenticatedCaptureRuntimeContext>.Failure(
            CaptureRuntimeErrorCodes.AccessDenied, "Access denied.", 403);

    private static SessionOperationResult<AuthenticatedCaptureRuntimeContext> NotReady() =>
        SessionOperationResult<AuthenticatedCaptureRuntimeContext>.Failure(
            CaptureRuntimeErrorCodes.NotReady, "Capture Runtime authentication is not ready.", 503);

    private sealed record VerifierRow(
        Guid CaptureAgentId,
        Guid DeviceInstallationId,
        byte[] PublicVerifierSpki,
        byte[] PublicKeyThumbprint);

    private sealed record NonceClaimRow(
        string ResultCode,
        long RuntimeRevision,
        long InstallationRevision,
        long CredentialRevision,
        Guid RolePolicyId,
        long RolePolicyRevision);
}
