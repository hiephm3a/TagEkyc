using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TagEkyc.Application.CaptureRuntime;
using TagEkyc.Application.Ports;
using TagEkyc.Contracts.CaptureRuntime;
using TagEkyc.Infrastructure.Auth;
using TagEkyc.Infrastructure.CaptureRuntime;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88C1C6BA1PlatformAuthenticationTests(PostgresPersistenceFixture postgres)
{
    [Fact]
    public async Task PlatformAuthentication_RealPersistedVersionAndDigest_ContextIsolationAndClosedDenials()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a1_platform_auth");
        await using var db = isolated.CreateDbContext();
        var credential = Guid.NewGuid();
        var principal = Guid.NewGuid();
        const string masterText = "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8";
        var secret = RandomNumberGenerator.GetBytes(32);
        var text = CaptureRuntimeVerifierCryptography.EncodeCanonicalSecret(secret);
        var presented = "teo_" + text;
        var master = Convert.FromBase64String(masterText + "=");
        var domain = CaptureRuntimeVerifierCryptography.DeriveDomainKey(master, CaptureRuntimeVerifierPepperDomain.PlatformCredential);
        var digest = CaptureRuntimeVerifierCryptography.ComputeDigest(domain, CaptureRuntimeVerifierPepperDomain.PlatformCredential, secret);
        var file = Path.Combine(Path.GetTempPath(), "tagekyc-platform-pepper-" + Guid.NewGuid().ToString("N"));
        await File.WriteAllBytesAsync(file, Encoding.ASCII.GetBytes(masterText));
        try
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO tagekyc.platform_operator_credentials
                ("CredentialId","KeyLookupPrefix","SecretDigest","VerifierPepperVersion","PrincipalId","Scopes",
                 "State","Revision","IssuedAtUtc","ExpiresAtUtc")
                VALUES ({credential},{text[..12]},{digest},7,{principal},ARRAY['operator.capture-runtime.manage'],
                        'Active',1,now()-interval '2 days',now()+interval '1 day')
                """);
            var connection = db.Database.GetConnectionString()!;
            var factory = new CaptureRuntimeDbContextFactory(new(
                new NpgsqlConnectionStringBuilder(connection) { Options = "-c role=tagekyc_capture_runtime_authenticator", Pooling = false }.ConnectionString,
                connection));
            // CurrentVersion deliberately has no resolvable material. Success must use
            // the durable version 7, not current-version inference or fallback.
            var provider = new CaptureRuntimeVerifierPepperProvider(new()
            {
                CurrentVersion = 9,
                Versions = [new() { Version = 7, SecretRef = "file:" + file },
                    new() { Version = 9, SecretRef = "file:" + file + ".absent" }]
            });
            var authenticator = new PlatformOperatorCredentialAuthenticator(factory, provider);
            var before = await db.Database.SqlQueryRaw<string>(
                "SELECT row_to_json(c)::text AS \"Value\" FROM tagekyc.platform_operator_credentials c WHERE \"CredentialId\"='" + credential + "'").SingleAsync();
            var accepted = await authenticator.AuthenticateAsync(presented);
            Assert.True(accepted.IsSuccess, accepted.Error?.Code);
            var actor = Assert.IsType<AuthenticatedPlatformOperatorContext>(accepted.Value);
            Assert.Equal(credential, actor.CredentialId);
            Assert.Equal(principal, actor.PrincipalId);
            Assert.Equal("OperatorAdmin", actor.CallerCategory);
            Assert.Equal(new[] { "operator.capture-runtime.manage" }, actor.Scopes.Order());
            Assert.Equal(text[..12], actor.KeyLookupPrefix);
            Assert.Equal(1, actor.CredentialRevision);
            Assert.DoesNotContain(typeof(AuthenticatedPlatformOperatorContext).GetProperties(),
                p => p.Name is "ClientApplicationId" or "ApiKeyId");

            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
            var tail = alphabet.IndexOf(text[^1]);
            Assert.Equal(0, tail % 4);
            var invalid = new List<string?> { null, "", text, "teo_" + text[..^1], presented + "\n", "TEO_" + text };
            var wrongSecret = secret.ToArray();
            wrongSecret[^1] ^= 1;
            var wrongText = CaptureRuntimeVerifierCryptography.EncodeCanonicalSecret(wrongSecret);
            Assert.Equal(text[..12], wrongText[..12]);
            invalid.Add("teo_" + wrongText); // Reaches the SAME persisted verifier; digest must reject.
            CryptographicOperations.ZeroMemory(wrongSecret);
            for (var delta = 1; delta <= 3; delta++)
            {
                var alias = text[..^1] + alphabet[tail + delta];
                Assert.Equal(secret, Convert.FromBase64String(alias.Replace('-', '+').Replace('_', '/') + "="));
                invalid.Add("teo_" + alias);
            }
            foreach (var bad in invalid)
            {
                var denied = await authenticator.AuthenticateAsync(bad);
                Assert.False(denied.IsSuccess);
                Assert.Null(denied.Value);
                Assert.Equal(CaptureRuntimeErrorCodes.AccessDenied, denied.Error?.Code);
                Assert.DoesNotContain(text, denied.Error?.Message ?? "");
            }
            Assert.Equal(before, await db.Database.SqlQueryRaw<string>(
                "SELECT row_to_json(c)::text AS \"Value\" FROM tagekyc.platform_operator_credentials c WHERE \"CredentialId\"='" + credential + "'").SingleAsync());
            // Invalid probes cannot poison the canonical positive control.
            Assert.True((await authenticator.AuthenticateAsync(presented)).IsSuccess);

            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE tagekyc.platform_operator_credentials SET \"ExpiresAtUtc\"=now()-interval '1 day' WHERE \"CredentialId\"={credential}");
            Assert.Equal(CaptureRuntimeErrorCodes.AccessDenied, (await authenticator.AuthenticateAsync(presented)).Error?.Code);
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE tagekyc.platform_operator_credentials SET \"ExpiresAtUtc\"=now()+interval '1 day',\"State\"='Revoked',\"Revision\"=2,\"RevokedAtUtc\"=now(),\"RevocationReason\"='DeploymentRevocation' WHERE \"CredentialId\"={credential}");
            Assert.Equal(CaptureRuntimeErrorCodes.AccessDenied, (await authenticator.AuthenticateAsync(presented)).Error?.Code);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(secret);
            CryptographicOperations.ZeroMemory(master);
            CryptographicOperations.ZeroMemory(domain);
            CryptographicOperations.ZeroMemory(digest);
            File.Delete(file);
        }
    }
}
