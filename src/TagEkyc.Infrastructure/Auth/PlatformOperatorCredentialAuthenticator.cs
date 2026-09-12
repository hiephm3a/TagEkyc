using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using TagEkyc.Application.CaptureRuntime;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.CaptureRuntime;
using TagEkyc.Infrastructure.CaptureRuntime;

namespace TagEkyc.Infrastructure.Auth;

public sealed class PlatformOperatorCredentialAuthenticator(
    ICaptureRuntimeDbContextFactory contexts,
    ICaptureRuntimeVerifierPepperSource peppers) : IPlatformOperatorCredentialAuthenticator
{
    private const string Prefix = "teo_";

    public async Task<SessionOperationResult<AuthenticatedPlatformOperatorContext>> AuthenticateAsync(
        string? presentedKey,
        CancellationToken cancellationToken = default)
    {
        if (presentedKey is null || !presentedKey.StartsWith(Prefix, StringComparison.Ordinal) ||
            presentedKey.Length != Prefix.Length + CaptureRuntimeVerifierCryptography.CanonicalTextLength)
        {
            return Denied();
        }

        var presentedSecret = new byte[CaptureRuntimeVerifierCryptography.SecretByteLength];
        if (!CaptureRuntimeVerifierCryptography.TryDecodeCanonicalSecret(
                presentedKey[Prefix.Length..], presentedSecret))
        {
            return Denied();
        }

        try
        {
            var lookupPrefix = presentedKey.Substring(Prefix.Length, 12);
            await using var db = await contexts.CreateAsync(cancellationToken).ConfigureAwait(false);
            await db.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var command = (NpgsqlCommand)db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT * FROM tagekyc.platform_operator_authenticate(@prefix,@now)";
            command.Parameters.Add(new NpgsqlParameter("prefix", NpgsqlDbType.Text) { Value = lookupPrefix });
            var now = DateTimeOffset.UtcNow;
            command.Parameters.Add(new NpgsqlParameter("now", NpgsqlDbType.TimestampTz) { Value = now });

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                return Denied();
            }

            var credentialId = reader.GetGuid(0);
            var principalId = reader.GetGuid(1);
            var expectedDigest = reader.GetFieldValue<byte[]>(2);
            var pepperVersion = reader.GetInt32(3);
            var scopes = reader.GetFieldValue<string[]>(4);
            var revision = reader.GetInt64(5);

            using var pepperLease = await ResolvePepperAsync(pepperVersion, cancellationToken)
                .ConfigureAwait(false);
            if (pepperLease is null)
            {
                return NotReady();
            }

            var actualDigest = CaptureRuntimeVerifierCryptography.ComputeDigest(
                pepperLease.Key.Span,
                CaptureRuntimeVerifierPepperDomain.PlatformCredential,
                presentedSecret);
            try
            {
                if (!CaptureRuntimeVerifierCryptography.FixedTimeEquals(actualDigest, expectedDigest))
                {
                    return Denied();
                }
            }
            finally
            {
                CaptureRuntimeVerifierCryptography.Zero(actualDigest);
            }

            return SessionOperationResult<AuthenticatedPlatformOperatorContext>.Success(
                new AuthenticatedPlatformOperatorContext(
                    credentialId,
                    principalId,
                    "OperatorAdmin",
                    scopes.ToHashSet(StringComparer.Ordinal),
                    lookupPrefix,
                    revision,
                    now));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (NpgsqlException)
        {
            return NotReady();
        }
        finally
        {
            CaptureRuntimeVerifierCryptography.Zero(presentedSecret);
        }
    }

    private async ValueTask<ICaptureRuntimeVerifierPepperLease?> ResolvePepperAsync(
        int version,
        CancellationToken cancellationToken) =>
        await peppers.TryResolveAsync(
            version,
            CaptureRuntimeVerifierPepperDomain.PlatformCredential,
            cancellationToken).ConfigureAwait(false);

    private static SessionOperationResult<AuthenticatedPlatformOperatorContext> Denied() =>
        SessionOperationResult<AuthenticatedPlatformOperatorContext>.Failure(
            CaptureRuntimeErrorCodes.AccessDenied,
            "Access denied.",
            403);

    private static SessionOperationResult<AuthenticatedPlatformOperatorContext> NotReady() =>
        SessionOperationResult<AuthenticatedPlatformOperatorContext>.Failure(
            CaptureRuntimeErrorCodes.NotReady,
            "Capture Runtime authentication is not ready.",
            503);
}
