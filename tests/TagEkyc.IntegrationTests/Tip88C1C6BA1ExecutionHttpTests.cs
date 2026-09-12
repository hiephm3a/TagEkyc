using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TagEkyc.Api;
using TagEkyc.Application;
using TagEkyc.Application.CaptureRuntime;
using TagEkyc.Application.LocalDev;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Infrastructure.Auth;
using TagEkyc.Infrastructure.CaptureRuntime;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88C1C6BA1ExecutionHttpTests(PostgresPersistenceFixture postgres)
{
    private static readonly Guid Session = Guid.NewGuid();
    private static readonly Guid Client = Guid.NewGuid();
    private static readonly Guid Credential = Guid.Parse("60000000-0000-4000-8000-000000000001");

    [Fact]
    public async Task R20ToR23_RealHttpCryptoNonceAndBusinessPersistence_ReplayAndConditionalRefresh()
    {
        await postgres.ResetDatabaseAsync();
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a1_execution_http");
        await using var db = isolated.CreateDbContext();
        using var signer = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        await Seed(db, signer);
        var path = Path.Combine(Path.GetTempPath(), "tagekyc-execution-pepper-" + Guid.NewGuid().ToString("N"));
        // Use the canonical shared synthetic master, without whitespace normalization.
        await File.WriteAllBytesAsync(path, Encoding.ASCII.GetBytes("AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8"));
        try
        {
            await using var app = await Start(db.Database.GetConnectionString()!, path);
            using var http = app.GetTestClient();
            var issuePath = $"/api/ekyc/verification-sessions/{Session:N}/capture-capabilities";
            var issueKey = Guid.NewGuid();
            using var issue = Request(HttpMethod.Post, issuePath, "{\"Action\":\"Issue\"}", issueKey);
            using var issued = await http.SendAsync(issue);
            Assert.Equal(HttpStatusCode.Created, issued.StatusCode);
            using var issuedJson = JsonDocument.Parse(await issued.Content.ReadAsStringAsync());
            var capability = Guid.Parse(issuedJson.RootElement.GetProperty("CaptureCapabilityId").GetString()!);
            var secret = issuedJson.RootElement.GetProperty("Secret").GetString()!;
            Assert.Equal(43, secret.Length);
            async Task<string> DurableIssue() => await db.Database.SqlQueryRaw<string>($"""
                SELECT jsonb_build_object('capability',to_jsonb(c),'operation',to_jsonb(o))::text AS "Value"
                FROM tagekyc.capture_capabilities c
                JOIN tagekyc.capture_capability_operations o ON o."ResultCapabilityId"=c."CaptureCapabilityId"
                WHERE c."CaptureCapabilityId"='{capability}' AND o."OperationKind"='Issue'
                """).SingleAsync();
            var durableBeforeReplay = await DurableIssue();
            using var replayIssue = Request(HttpMethod.Post, issuePath, "{\"Action\":\"Issue\"}", issueKey);
            using var issueReplay = await http.SendAsync(replayIssue);
            Assert.Equal(HttpStatusCode.Conflict, issueReplay.StatusCode);
            var replayText = await issueReplay.Content.ReadAsStringAsync();
            using var replayJson = JsonDocument.Parse(replayText);
            Assert.Equal(new[] { "code", "correlationId" }, replayJson.RootElement.EnumerateObject()
                .Select(property => property.Name).Order(StringComparer.Ordinal).ToArray());
            Assert.Equal("EXISTING_MATCH_SECRET_UNAVAILABLE", replayJson.RootElement.GetProperty("code").GetString());
            Assert.False(string.IsNullOrWhiteSpace(replayJson.RootElement.GetProperty("correlationId").GetString()));
            Assert.DoesNotContain(secret, replayText);
            Assert.DoesNotContain(capability.ToString("N"), replayText);
            Assert.DoesNotContain(capability.ToString("D"), replayText);
            Assert.Equal(durableBeforeReplay, await DurableIssue());
            Assert.Equal(1, await Count(db, "capture_capability_operations", "\"OperationKind\"='Issue'"));
            Assert.Equal(1, await Count(db, "capture_capability_events", "TRUE"));

            var bindKey = Guid.NewGuid();
            var bindBody = JsonSerializer.Serialize(new { CaptureCapabilityId = capability.ToString("N"), CaptureCapabilitySecret = secret, BindOperationId = bindKey.ToString("N") });
            var secretBytes = Convert.FromBase64String(secret.Replace('-', '+').Replace('_', '/') + "=");
            var bindingText = $"CaptureCapabilityId={capability:N};BindOperationId={bindKey:N};SecretSha256={Convert.ToHexString(SHA256.HashData(secretBytes)).ToLowerInvariant()}";
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
            var canonicalTail = alphabet.IndexOf(secret[^1]);
            Assert.Equal(0, canonicalTail % 4);
            for (var delta = 1; delta <= 3; delta++)
            {
                var alias = secret[..^1] + alphabet[canonicalTail + delta];
                // These are distinct text encodings of exactly the SAME secret.
                // Digest mismatch cannot be the reason the negative control fails.
                var aliasBytes = Convert.FromBase64String(alias.Replace('-', '+').Replace('_', '/') + "=");
                Assert.Equal(secretBytes, aliasBytes);
                CryptographicOperations.ZeroMemory(aliasBytes);
                var aliasBody = JsonSerializer.Serialize(new { CaptureCapabilityId = capability.ToString("N"), CaptureCapabilitySecret = alias, BindOperationId = bindKey.ToString("N") });
                using var aliasRequest = Signed(signer, "/api/ekyc/capture-runtime/executions/bind", aliasBody, bindKey, bindingText);
                using var aliasResponse = await http.SendAsync(aliasRequest);
                Assert.Equal(HttpStatusCode.BadRequest, aliasResponse.StatusCode);
                var error = await aliasResponse.Content.ReadAsStringAsync();
                Assert.DoesNotContain(alias, error);
                Assert.DoesNotContain(secret, error);
                Assert.Equal(0, await Count(db, "capture_execution_bindings", "TRUE"));
                Assert.Equal(0, await Count(db, "capture_capability_operations", "\"OperationKind\"='Bind'"));
                Assert.Equal(1, await Count(db, "capture_capability_events", "TRUE"));
                Assert.Equal(0, await Nonces(db));
            }
            CryptographicOperations.ZeroMemory(secretBytes);
            using var bind = Signed(signer, "/api/ekyc/capture-runtime/executions/bind", bindBody, bindKey, bindingText);
            using var bound = await http.SendAsync(bind);
            Assert.Equal(HttpStatusCode.Created, bound.StatusCode);
            var boundText = await bound.Content.ReadAsStringAsync();
            using var bindAgain = Signed(signer, "/api/ekyc/capture-runtime/executions/bind", bindBody, bindKey, bindingText);
            using var boundAgain = await http.SendAsync(bindAgain);
            Assert.Equal(HttpStatusCode.OK, boundAgain.StatusCode);
            Assert.Equal(boundText, await boundAgain.Content.ReadAsStringAsync());
            Assert.Equal(1, await Count(db, "capture_capability_operations", "\"OperationKind\"='Bind'"));
            Assert.Equal(1, await Count(db, "capture_execution_bindings", "TRUE"));
            Assert.Equal(2, await Nonces(db));

            var reconcileBody = JsonSerializer.Serialize(new { CaptureCapabilityId = capability.ToString("N"), BindOperationId = bindKey.ToString("N") });
            using var reconcile = Signed(signer, "/api/ekyc/capture-runtime/executions/reconcile", reconcileBody, null,
                $"CaptureCapabilityId={capability:N};BindOperationId={bindKey:N}");
            using var reconciled = await http.SendAsync(reconcile);
            Assert.Equal(HttpStatusCode.OK, reconciled.StatusCode);
            Assert.Equal(boundText, await reconciled.Content.ReadAsStringAsync());
            Assert.Equal(3, await Nonces(db));
            Assert.Equal(2, await Count(db, "capture_capability_operations", "TRUE"));
            Assert.Equal(2, await Count(db, "capture_capability_events", "TRUE"));

            using var config = Signed(signer, "/api/ekyc/capture-runtime/self/configuration", null, null, "");
            using var configured = await http.SendAsync(config);
            Assert.Equal(HttpStatusCode.OK, configured.StatusCode);
            var configBytes = await configured.Content.ReadAsByteArrayAsync();
            var etag = configured.Headers.ETag!.ToString();
            Assert.Equal("\"" + Url(SHA256.HashData(configBytes)) + "\"", etag);
            using var conditional = Signed(signer, "/api/ekyc/capture-runtime/self/configuration", null, null, "");
            conditional.Headers.TryAddWithoutValidation("If-None-Match", etag);
            using var notModified = await http.SendAsync(conditional);
            Assert.Equal(HttpStatusCode.NotModified, notModified.StatusCode);
            Assert.Empty(await notModified.Content.ReadAsByteArrayAsync());
            Assert.Equal(etag, notModified.Headers.ETag!.ToString());
            Assert.Equal(5, await Nonces(db));
            // Seed a genuine expired immutable revision, then move the mutable runtime
            // reference to it. The prior ETag must not bypass current resolution.
            await db.Database.ExecuteSqlRawAsync("""
                INSERT INTO tagekyc.capture_runtime_configuration_revisions VALUES
                ('30000000-0000-4000-8000-000000000001',2,now()-interval '2 days',now()-interval '1 day',true,60,100,60,1000,1000,10000,10000,10000,1024,'00000000-0000-4000-8000-000000000001',now());
                UPDATE tagekyc.capture_runtime_registrations SET "ConfigurationRevision"=2,"Revision"="Revision"+1
                WHERE "CaptureAgentId"='40000000-0000-4000-8000-000000000001';
                """);
            using var stale = Signed(signer, "/api/ekyc/capture-runtime/self/configuration", null, null, "");
            stale.Headers.TryAddWithoutValidation("If-None-Match", etag);
            using var denied = await http.SendAsync(stale);
            Assert.Equal(HttpStatusCode.NotFound, denied.StatusCode);
            Assert.DoesNotContain(Encoding.UTF8.GetString(configBytes), await denied.Content.ReadAsStringAsync());
            Assert.Equal(6, await Nonces(db));
        }
        finally { File.Delete(path); }
    }

    private static async Task Seed(TagEkycDbContext db, ECDsa signer)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "TagEkyc.sln"))) root = root.Parent;
        var source = File.ReadAllText(Path.Combine(root!.FullName, "docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c6b_a1_postgresql16_runtime_proof_companion.md"));
        var start = source.IndexOf("INSERT INTO tagekyc.platform_operator_credentials", StringComparison.Ordinal);
        var end = source.IndexOf("-- Publish newer heads", start, StringComparison.Ordinal);
        var seed = source[start..end].Replace("ARRAY['Configuration']", "ARRAY['Bind','Configuration']", StringComparison.Ordinal)
            .Replace("decode(repeat('04',91),'hex')", $"decode('{Convert.ToHexString(signer.ExportSubjectPublicKeyInfo())}','hex')", StringComparison.Ordinal)
            .Replace("decode(repeat('22',32),'hex')", $"decode('{Convert.ToHexString(SHA256.HashData(signer.ExportSubjectPublicKeyInfo()))}','hex')", StringComparison.Ordinal);
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(seed);
        db.Sessions.Add(new() { Id = Session, ClientApplicationId = Client, SubjectRef = "synthetic", Purpose = "SyntheticProof",
            Profile = "StandardEkycProfile", State = "Created", Result = "NotAvailable", AssuranceLevel = "None", RequiredChecksJson = "[\"CaptureQuality\"]",
            PolicySnapshotId = "synthetic", RetentionClass = "LocalDevEphemeral", DeletionEligibility = "NotEvaluated", LegalHoldStatus = "None", PurgeBlockReason = "None",
            RequestId = "r", CorrelationId = "c", BindingNonceHash = "synthetic-challenge", CreatedAt = DateTimeOffset.UtcNow, ExpiresAt = DateTimeOffset.UtcNow.AddHours(1) });
        await db.SaveChangesAsync(); await tx.CommitAsync(); db.ChangeTracker.Clear();
    }

    private static Task<int> Count(TagEkycDbContext db, string table, string predicate) => db.Database.SqlQueryRaw<int>($"SELECT count(*)::integer AS \"Value\" FROM tagekyc.{table} WHERE \"VerificationSessionId\"='{Session}' AND {predicate}").SingleAsync();
    private static Task<int> Nonces(TagEkycDbContext db) => db.Database.SqlQueryRaw<int>($"SELECT count(*)::integer AS \"Value\" FROM tagekyc.capture_runtime_request_nonces WHERE \"CredentialId\"='{Credential}'").SingleAsync();
    private static HttpRequestMessage Request(HttpMethod method, string path, string? body, Guid? key)
    {
        var request = new HttpRequestMessage(method, path);
        if (body is not null) { request.Content = new ByteArrayContent(Encoding.UTF8.GetBytes(body)); request.Content.Headers.ContentType = new("application/json"); }
        if (key is { } id) request.Headers.Add("Idempotency-Key", id.ToString("N"));
        return request;
    }
    private static HttpRequestMessage Signed(ECDsa signer, string path, string? body, Guid? key, string binding)
    {
        var request = Request(body is null ? HttpMethod.Get : HttpMethod.Post, path, body, key);
        var bytes = Encoding.UTF8.GetBytes(body ?? ""); var nonce = Url(RandomNumberGenerator.GetBytes(32));
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture);
        var preimage = Encoding.UTF8.GetBytes(string.Join('\n', "TAG-EKYC-CRT1", request.Method.Method, path, Credential.ToString("N"), "1", timestamp,
            nonce, body is null ? "" : "application/json", bytes.Length.ToString(CultureInfo.InvariantCulture), Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(), binding) + "\n");
        request.Headers.Add(CaptureRuntimeCrt1RequestParser.CredentialIdHeader, Credential.ToString("N"));
        request.Headers.Add(CaptureRuntimeCrt1RequestParser.CredentialGenerationHeader, "1");
        request.Headers.Add(CaptureRuntimeCrt1RequestParser.TimestampHeader, timestamp);
        request.Headers.Add(CaptureRuntimeCrt1RequestParser.NonceHeader, nonce);
        request.Headers.Add(CaptureRuntimeCrt1RequestParser.SignatureHeader, Url(signer.SignData(preimage, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation)));
        return request;
    }
    private static string Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static async Task<WebApplication> Start(string connection, string pepperPath)
    {
        var builder = WebApplication.CreateBuilder(); builder.WebHost.UseTestServer();
        CaptureRuntimeDbContextFactory Factory(string role) => new(new(new NpgsqlConnectionStringBuilder(connection) { Options = "-c role=" + role, Pooling = false }.ConnectionString, connection));
        builder.Services.AddSingleton<ICaptureRuntimeRequestAuthenticator>(new CaptureRuntimeRequestAuthenticator(Factory("tagekyc_capture_runtime_authenticator")));
        builder.Services.AddSingleton<ICaptureRuntimeExecutionGateway>(new CaptureRuntimeExecutionPersistenceBoundary(Factory("tagekyc_capture_runtime_application")));
        builder.Services.AddSingleton<ICaptureRuntimeVerifierPepperSource>(new CaptureRuntimeVerifierPepperProvider(new() { CurrentVersion = 1,
            Versions = [new() { Version = 1, SecretRef = "file:" + pepperPath }] }));
        builder.Services.AddTagEkycPostgresPersistence(connection);
        builder.Services.AddSingleton<ILocalDevClientPolicyProvider, LocalDevRuntimePolicySource>();
        builder.Services.AddScoped<VerificationEvidenceApplicationService>();
        builder.Services.AddScoped<IAuthorityNeutralVerificationEvidencePlanner>(sp => sp.GetRequiredService<VerificationEvidenceApplicationService>());
        builder.Services.AddScoped<IAuthorityNeutralVerificationEvidenceWriter>(sp => sp.GetRequiredService<VerificationEvidenceApplicationService>());
        builder.Services.AddScoped<IAppendBusinessTransaction, EfAppendBusinessTransaction>();
        builder.Services.AddScoped<ICaptureRuntimeAppendAuthority, CaptureRuntimeAppendAuthority>();
        builder.Services.AddScoped<ICaptureRuntimeAppendGateway, CaptureRuntimeAppendApplicationService>();
        builder.Services.AddScoped<ICaptureRuntimeExecutionService, CaptureRuntimeExecutionApplicationService>();
        builder.Services.AddSingleton<IApiKeyAuthenticator, ClientAuth>();
        var app = builder.Build(); app.MapCaptureRuntimeExecutionEndpoints(); await app.StartAsync(); return app;
    }
    private sealed class ClientAuth : IApiKeyAuthenticator
    {
        public Task<SessionOperationResult<AuthenticatedClientContext>> AuthenticateAsync(HttpContext context, string? requiredScope = null, CancellationToken cancellationToken = default)
            => Task.FromResult(SessionOperationResult<AuthenticatedClientContext>.Success(new(Guid.NewGuid(), Client, "synthetic-client", AuthenticatedCallerCategory.BusinessConsumer, new HashSet<string> { "business.session.read" })));
    }
}
