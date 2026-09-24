using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
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
using TagEkyc.CaptureAgent.Client;
using TagEkyc.CaptureAgent.Core;
using TagEkyc.Infrastructure.Auth;
using TagEkyc.Infrastructure.CaptureRuntime;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Contracts.CaptureAgent;
using TagEkyc.Contracts.CaptureRuntime;
using TagEkyc.Contracts.Common;
using TagEkyc.Contracts.TrustedAdapter;
using TagEkyc.Domain;

namespace TagEkyc.IntegrationTests;

public sealed class Tip88C1C6BA3AgentRawCrt1ParserTests
{
    [Fact]
    public void A3_AgentRawCrt1MatchesServerParser()
    {
        using var signer = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var credential = Guid.NewGuid();
        var nonce = RandomNumberGenerator.GetBytes(32);
        var now = DateTimeOffset.Parse("2026-09-15T01:02:03.4567890Z");
        var metadata = new RawExportSourceIngressMetadata(7, Guid.NewGuid(), Guid.NewGuid(), 3,
            TagEkyc.CaptureAgent.Core.RawExportRawClass.ChipDg2Portrait, Guid.NewGuid(), "image/jpeg", 17, new string('a', 64),
            now, now, now.AddSeconds(60), 60);
        var preimage = CaptureRuntimeWireCodec.Crt1RawIngress(credential, 1, now, nonce, metadata);
        var signature = signer.SignData(preimage, HashAlgorithmName.SHA256,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/ekyc/raw-export/source-ingress";
        context.Request.ContentType = "image/jpeg";
        context.Request.ContentLength = 17;
        var headers = new Dictionary<string, string>
        {
            ["X-TagEkyc-Agent-Configuration-Revision"] = "7",
            ["X-TagEkyc-Verification-Session-Id"] = metadata.VerificationSessionId.ToString("N"),
            ["X-TagEkyc-Capture-Artifact-Id"] = metadata.CaptureArtifactId.ToString("N"),
            ["X-TagEkyc-Capture-Revision"] = "3",
            ["X-TagEkyc-Raw-Class"] = "ChipDg2Portrait",
            ["Idempotency-Key"] = metadata.IngressIdempotencyKey.ToString("N"),
            ["X-TagEkyc-Captured-At-Utc"] = now.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffffzzz", System.Globalization.CultureInfo.InvariantCulture),
            ["X-TagEkyc-Retention-Started-At-Utc"] = now.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffffzzz", System.Globalization.CultureInfo.InvariantCulture),
            ["X-TagEkyc-Retention-Expires-At-Utc"] = now.AddSeconds(60).ToString("yyyy-MM-dd'T'HH:mm:ss.fffffffzzz", System.Globalization.CultureInfo.InvariantCulture),
            ["X-TagEkyc-Retention-Budget-Seconds"] = "60",
            ["X-TagEkyc-Plaintext-Sha256"] = new string('a', 64),
            [CaptureRuntimeCrt1RequestParser.CredentialIdHeader] = credential.ToString("N"),
            [CaptureRuntimeCrt1RequestParser.CredentialGenerationHeader] = "1",
            [CaptureRuntimeCrt1RequestParser.TimestampHeader] = CaptureRuntimeWireCodec.Timestamp(now),
            [CaptureRuntimeCrt1RequestParser.NonceHeader] = CaptureRuntimeWireCodec.Base64Url(nonce),
            [CaptureRuntimeCrt1RequestParser.SignatureHeader] = CaptureRuntimeWireCodec.Base64Url(signature),
        };
        foreach (var header in headers) context.Request.Headers[header.Key] = header.Value;

        Assert.True(CaptureRuntimeCrt1RequestParser.TryComputeIngressMetadataDigest(context.Request, out var digest));
        Assert.True(CaptureRuntimeCrt1RequestParser.TryCreate(context.Request, "RawIngress", "image/jpeg", 17,
            new string('a', 64), "ingress=IngressMetadataSha256=" + digest, out var parsed));
        Assert.Equal(preimage, parsed!.ExactSignedPreimage.ToArray());
        Assert.True(signer.VerifyData(parsed.ExactSignedPreimage.Span, parsed.Signature,
            HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation));
    }
}

// Deliberately NOT PostgresPersistenceCollection: never starts/stops shared Compose.
[SupportedOSPlatform("windows")]
public sealed class Tip88C1C6BA2ClientServerAcceptanceTests : IAsyncLifetime
{
    private PostgresPersistenceFixture postgres = null!;
    public async Task InitializeAsync()
    {
        if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64)
            throw new InvalidOperationException("A2 acceptance requires Windows x64; unsupported execution is not a pass.");
        var run = Environment.GetEnvironmentVariable("TAGEKYC_A2_TEST_RUN_ID");
        var connection = Environment.GetEnvironmentVariable("TAGEKYC_A2_TEST_CONNECTION_STRING");
        if (!Guid.TryParseExact(run, "N", out var id) || id.ToString("N") != run || string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("A2 acceptance requires its isolated runner.");
        var options = new NpgsqlConnectionStringBuilder(connection);
        if (options.Host != "127.0.0.1" || options.Database != "tagekyc_a2_" + run || options.Port is < 1024 or > 65535)
            throw new InvalidOperationException("A2 acceptance database identity is invalid.");
        var inspect = new ProcessStartInfo("docker") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        inspect.ArgumentList.Add("inspect"); inspect.ArgumentList.Add("tagekyc-a2-" + run);
        using var process = Process.Start(inspect)!;
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0) throw new InvalidOperationException("A2 acceptance container is unavailable.");
        using (var document = JsonDocument.Parse(output))
        {
            var container = document.RootElement[0];
            var binding = container.GetProperty("NetworkSettings").GetProperty("Ports").GetProperty("5432/tcp")[0];
            if (container.GetProperty("Name").GetString() != "/tagekyc-a2-" + run ||
                container.GetProperty("Config").GetProperty("Labels").GetProperty("tagekyc.a2.run").GetString() != run ||
                binding.GetProperty("HostIp").GetString() != "127.0.0.1" || binding.GetProperty("HostPort").GetString() != options.Port.ToString())
                throw new InvalidOperationException("A2 acceptance container binding mismatch.");
        }
        postgres = new PostgresPersistenceFixture(connection);
        await postgres.ResetDatabaseAsync();
    }
    public Task DisposeAsync() => Task.CompletedTask; // unique container lifecycle belongs exclusively to runner

    [Fact]
    public async Task Enroll_LostResponse_ReplaysExactBodyAndKeepsCandidate()
    {
        await using var clone = await postgres.CreateDisposableCurrentDatabaseAsync("a2_command_enroll"); await using var db = clone.CreateDbContext();
        using var secret = new SensitiveByteBuffer(RandomNumberGenerator.GetBytes(32)); var issuance = await SeedBootstrap(db, secret.DebugUnsafeBackingArray);
        var app = await Start(db.Database.GetConnectionString()!);
        var transport = new LostResponse(app.GetTestServer().CreateHandler());
        await using var runtime = new RuntimeFixture(app, transport);
        var commands = Commands(runtime); var diagnostic = commands.Diagnose(); var operation = Guid.NewGuid();
        using var envelope = new SensitiveByteBuffer(JsonSerializer.SerializeToUtf8Bytes(new { SchemaVersion = 1, BootstrapIssuanceId = issuance.ToString("N"),
            BootstrapSecret = CaptureRuntimeWireCodec.Base64Url(secret.DebugUnsafeBackingArray), ExpiresAtUtc = CaptureRuntimeWireCodec.Timestamp(DateTimeOffset.UtcNow.AddMinutes(5)),
            HandoffAttestationDigest = CaptureRuntimeWireCodec.Digest(CaptureRuntimeAgentCommands.DiagnosticBytes(diagnostic)), RedeemOperationId = operation.ToString("N"), Diagnostic = diagnostic }));
        using var first = new MemoryStream();
        Assert.Equal(3, await commands.RunAsync(["enroll", "--stdin"], new MemoryStream(envelope.DebugUnsafeBackingArray), first));
        var before = runtime.Owner.ReadRequired(); Assert.Equal("EnrollmentPending", before.State);
        using var ack = new DurableAck(runtime.Owner);
        Assert.Equal(0, await commands.RunAsync(["enroll", "--stdin"], new MemoryStream(envelope.DebugUnsafeBackingArray), ack));
        Assert.Equal(before.CandidateKeyId, runtime.Identity.CandidateKeyId); Assert.Equal(2, transport.Bodies.Count);
        Assert.Equal(transport.Bodies[0], transport.Bodies[1]); Assert.Equal(transport.Operations[0], transport.Operations[1]);
        Assert.Equal(operation.ToString("N"), transport.Operations[0]);
        Assert.DoesNotContain(CaptureRuntimeWireCodec.Base64Url(secret.DebugUnsafeBackingArray), Encoding.UTF8.GetString(ack.ToArray()));
        await using var observer = clone.CreateDbContext();
        Assert.Equal(1, await observer.Database.SqlQueryRaw<int>("SELECT count(*)::integer AS \"Value\" FROM tagekyc.capture_runtime_bootstrap_redemption_operations").SingleAsync());
        Assert.Equal(1, await observer.Database.SqlQueryRaw<int>("SELECT count(*)::integer AS \"Value\" FROM tagekyc.capture_runtime_bootstrap_redemption_events").SingleAsync());
    }

    [Fact]
    public async Task Rotate_SuccessorReplay_RetainsExactBody()
    {
        await using var clone = await postgres.CreateDisposableCurrentDatabaseAsync("a2_command_rotate"); await using var db = clone.CreateDbContext();
        await using var runtime = await RuntimeFixture.Create(db); var original = runtime.Identity;
        var rotation = Guid.NewGuid();
        await using (var connection = new NpgsqlConnection(db.Database.GetConnectionString()))
        {
            await connection.OpenAsync(); await using var command = new NpgsqlCommand("""
                UPDATE tagekyc.capture_runtime_registrations r
                SET "NextRolePolicyId"=g."RolePolicyId","NextRolePolicyRevision"=g."RolePolicyRevision"
                FROM tagekyc.capture_runtime_credential_generations g
                WHERE r."CaptureAgentId"=@agent AND g."CredentialId"=@credential AND g."Generation"=1;
                INSERT INTO tagekyc.capture_runtime_rotation_authorizations
                ("RotationAuthorizationId","CaptureAgentId","DeviceInstallationId","CredentialId","CurrentGeneration",
                 "AuthorizeOperationId","RequestFingerprint","AuthorizedByCredentialId","AuthorizedAtUtc","ExpiresAtUtc","State","Revision")
                SELECT @rotation,@agent,@installation,@credential,1,@op,decode(repeat('44',32),'hex'),
                    "CredentialId",now()-interval '1 minute',now()+interval '5 minutes','Active',1
                FROM tagekyc.platform_operator_credentials LIMIT 1
                """, connection);
            command.Parameters.AddWithValue("rotation", rotation); command.Parameters.AddWithValue("agent", original.CaptureAgentId!.Value);
            command.Parameters.AddWithValue("installation", original.DeviceInstallationId!.Value); command.Parameters.AddWithValue("credential", original.CredentialId!.Value);
            command.Parameters.AddWithValue("op", Guid.NewGuid()); Assert.Equal(2, await command.ExecuteNonQueryAsync());
        }
        using var transport = new LostResponse(runtime.App.GetTestServer().CreateHandler());
        using var client = new CaptureRuntimeHttpClient(new Uri(runtime.Journal.ServerOrigin), runtime.Journal, runtime.Keys, transport: transport);
        var commands = Commands(runtime, client);
        var envelope = JsonSerializer.SerializeToUtf8Bytes(new { SchemaVersion = 1, RotationId = rotation.ToString("N"),
            CaptureAgentId = original.CaptureAgentId!.Value.ToString("N"), DeviceInstallationId = original.DeviceInstallationId!.Value.ToString("N"),
            CredentialId = original.CredentialId!.Value.ToString("N"), CurrentGeneration = 1, ExpiresAtUtc = CaptureRuntimeWireCodec.Timestamp(DateTimeOffset.UtcNow.AddMinutes(5)) });
        using var first = new MemoryStream();
        Assert.Equal(3, await commands.RunAsync(["rotate", "--stdin"], new MemoryStream(envelope), first));
        Assert.Equal(new[] { HttpStatusCode.OK }, transport.Statuses);
        var pending = runtime.Identity; Assert.Equal("RotationPending", pending.State);
        Assert.True(CngKey.Exists(WindowsCaptureRuntimeKeyStore.GetKeyName(original.CandidateKeyId)));
        Assert.True(CngKey.Exists(WindowsCaptureRuntimeKeyStore.GetKeyName(pending.PendingRotation!.SuccessorCandidateKeyId)));
        using var ack = new DurableAck(runtime.Owner);
        Assert.Equal(0, await commands.RunAsync(["rotate", "--resume"], Stream.Null, ack));
        Assert.Equal(original.CredentialId, runtime.Identity.CredentialId); Assert.Equal(2, runtime.Identity.Generation);
        Assert.False(CngKey.Exists(WindowsCaptureRuntimeKeyStore.GetKeyName(original.CandidateKeyId)));
        Assert.Equal(new[] { "1", "2" }, transport.Generations); Assert.Equal(transport.Bodies[0], transport.Bodies[1]);
        Assert.Equal(transport.Operations[0], transport.Operations[1]); Assert.NotEqual(transport.Nonces[0], transport.Nonces[1]);
        await using var observer = clone.CreateDbContext();
        Assert.Equal(1, await observer.Database.SqlQueryRaw<int>("SELECT count(*)::integer AS \"Value\" FROM tagekyc.capture_runtime_rotation_completion_operations").SingleAsync());
        Assert.Equal(2, await observer.Database.SqlQueryRaw<int>("SELECT count(*)::integer AS \"Value\" FROM tagekyc.capture_runtime_request_nonces").SingleAsync());
    }

    private static CaptureRuntimeAgentCommands Commands(RuntimeFixture runtime, CaptureRuntimeHttpClient? client = null)
    {
        var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0); listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
        return new(runtime.Journal, runtime.Keys, client ?? runtime.Client, port, typeof(Tip88C1C6BA2ClientServerAcceptanceTests).Assembly.Location);
    }
    private sealed class DurableAck(CaptureRuntimeAgentCoordinator owner) : MemoryStream
    {
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default)
        { Assert.Equal("Active", owner.ReadRequired().State); return base.WriteAsync(buffer, ct); }
    }
    private sealed class LostResponse(HttpMessageHandler inner) : DelegatingHandler(inner)
    {
        public List<byte[]> Bodies { get; } = []; public List<string> Operations { get; } = [];
        public List<HttpStatusCode> Statuses { get; } = [];
        public List<string> Generations { get; } = []; public List<string> Nonces { get; } = [];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Bodies.Add(await request.Content!.ReadAsByteArrayAsync(ct)); Operations.Add(request.Headers.GetValues("Idempotency-Key").Single());
            if (request.Headers.TryGetValues("X-TagEkyc-Capture-Runtime-Credential-Generation", out var generations)) Generations.Add(generations.Single());
            if (request.Headers.TryGetValues("X-TagEkyc-Capture-Runtime-Nonce", out var nonces)) Nonces.Add(nonces.Single());
            var response = await base.SendAsync(request, ct);
            Statuses.Add(response.StatusCode);
            Assert.True(response.IsSuccessStatusCode, "Expected committed server response before transport loss: " + response.StatusCode);
            if (Bodies.Count == 1) { response.Dispose(); throw new HttpRequestException("synthetic response lost after server commit"); }
            return response;
        }
        protected override void Dispose(bool disposing) { foreach (var body in Bodies) CryptographicOperations.ZeroMemory(body); base.Dispose(disposing); }
    }

    [Fact]
    public async Task Crt1_ExactBytes_ServerVerifierAccepts()
    {
        await using var clone = await postgres.CreateDisposableCurrentDatabaseAsync("a2_crt1_http");
        await using var db = clone.CreateDbContext(); await using var runtime = await RuntimeFixture.Create(db);
        var baseline = await runtime.Client.FetchRawExportConfigurationAsync(null);
        Assert.Equal(runtime.Identity.CaptureAgentId, baseline.Document!.CaptureAgentId);
        // Same real CNG key and otherwise valid request: mutate only what is signed.
        // A verifier that always rejects fails the positive controls before and after.
        for (var atom = 0; atom < 14; atom++)
        {
            using var altered = new CaptureRuntimeHttpClient(new Uri(runtime.Journal.ServerOrigin), runtime.Journal,
                new ChangedSignedAtom(runtime.Keys, atom), transport: runtime.App.GetTestServer().CreateHandler());
            var denial = await Assert.ThrowsAsync<CaptureRuntimeHttpException>(() => altered.FetchRawExportConfigurationAsync(null));
            Assert.Equal(403, denial.StatusCode);
        }
        foreach (var mutation in new[] { "alias-nonce", "foreign-api-key" })
        {
            using var changed = new CaptureRuntimeHttpClient(new Uri(runtime.Journal.ServerOrigin), runtime.Journal,
                mutation == "alias-nonce" ? new ChangedSignedAtom(runtime.Keys, 14) : runtime.Keys,
                transport: new InvalidHeader(runtime.App.GetTestServer().CreateHandler(), mutation));
            var denied = await Assert.ThrowsAsync<CaptureRuntimeHttpException>(() => changed.FetchRawExportConfigurationAsync(null));
            Assert.Equal(mutation == "alias-nonce" ? 400 : 403, denied.StatusCode); // parser vs foreign-auth precedence; neither claims nonce
        }
        Assert.Equal(baseline.Document, (await runtime.Client.FetchRawExportConfigurationAsync(null)).Document);
        await using var observer = clone.CreateDbContext();
        Assert.Equal(2, await observer.Database.SqlQueryRaw<int>("SELECT count(*)::integer AS \"Value\" FROM tagekyc.capture_runtime_request_nonces").SingleAsync());
    }

    private sealed class ChangedSignedAtom(ICaptureRuntimeKeyStore inner, int atom) : ICaptureRuntimeKeyStore
    {
        public CaptureRuntimePublicKey Open(Guid id, ReadOnlySpan<byte> thumb) => inner.Open(id, thumb);
        public byte[] Sign(Guid id, ReadOnlySpan<byte> thumb, ReadOnlySpan<byte> bytes)
        {
            var original = bytes.ToArray(); var text = Encoding.UTF8.GetString(original); var lines = text.Split('\n');
            Assert.Equal(12, lines.Length); Assert.Empty(lines[^1]);
            byte[] changed;
            if (atom == 14) { lines[6] = AliasNonce(lines[6]); changed = Encoding.UTF8.GetBytes(string.Join('\n', lines)); }
            else if (atom < 11) { lines[atom] += "x"; changed = Encoding.UTF8.GetBytes(string.Join('\n', lines)); }
            else changed = atom switch { 11 => original[..^1], 12 => original.Concat(new byte[] { 10 }).ToArray(),
                _ => Encoding.UTF8.GetBytes(text.Replace("\n", "\r\n", StringComparison.Ordinal)) };
            Assert.NotEqual(original, changed); return inner.Sign(id, thumb, changed);
        }
        public CaptureRuntimePublicKey CreateReserved(Guid id) => throw new NotSupportedException();
        public void DeleteConfirmedAbandoned(Guid id, ReadOnlySpan<byte> thumb) => throw new NotSupportedException();
        public void DeleteQueuedAbandoned(Guid id) => throw new NotSupportedException();
    }

    private sealed class InvalidHeader(HttpMessageHandler inner, string mutation) : DelegatingHandler(inner)
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (mutation == "foreign-api-key") request.Headers.Add("X-TagEkyc-Api-Key", "synthetic-forbidden-key");
            else
            {
                const string name = "X-TagEkyc-Capture-Runtime-Nonce";
                var original = request.Headers.GetValues(name).Single();
                var alias = AliasNonce(original);
                Assert.NotEqual(original, alias);
                static byte[] Decode(string value) => Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/') + "=");
                Assert.Equal(Decode(original), Decode(alias)); // same nonce bytes, forbidden noncanonical spelling
                request.Headers.Remove(name); request.Headers.Add(name, alias);
            }
            return base.SendAsync(request, ct);
        }
    }

    private static string AliasNonce(string original)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
        return original[..^1] + alphabet[alphabet.IndexOf(original[^1]) | 1];
    }

    [Fact]
    public async Task Bind_RestartReconcile_UsesNonsecretJournal()
    {
        await using var clone = await postgres.CreateDisposableCurrentDatabaseAsync("a2_bind_http");
        await using var db = clone.CreateDbContext(); await using var runtime = await RuntimeFixture.Create(db);
        var seeded = await SeedSessionCapability(db);
        using var secret = new SensitiveByteBuffer(seeded.Secret);
        var pending = runtime.Owner.BeginHandoff("synthetic-signflow", seeded.Session, seeded.Capability, DateTimeOffset.UtcNow.AddMinutes(5));
        Assert.Equal(404, (await Assert.ThrowsAsync<CaptureRuntimeHttpException>(() => runtime.Client.ReconcileAsync(seeded.Capability, pending.BindOperationId))).StatusCode);
        var bound = await runtime.Client.BindAsync(seeded.Capability, secret, pending.BindOperationId);
        // Lose the response before journal confirmation; recover through a NEW owner/client.
        using var restarted = new CaptureRuntimeHttpClient(new Uri(runtime.Journal.ServerOrigin), runtime.Journal, runtime.Keys,
            transport: runtime.App.GetTestServer().CreateHandler());
        var recovered = await restarted.ReconcileAsync(seeded.Capability, pending.BindOperationId);
        Assert.Equal(bound, recovered);
        var owner = new CaptureRuntimeAgentCoordinator(runtime.Journal, runtime.Keys);
        owner.ConfirmBinding(seeded.Capability, pending.BindOperationId, recovered);
        Assert.False(owner.ReadRequired().PendingBind!.HandoffAccepted);
        Assert.Throws<CaptureRuntimeLocalException>(() => owner.ClaimDelegation(bound.BindingId));
        owner.AcceptHandoff(seeded.Capability, pending.BindOperationId, secret);
        Assert.All(seeded.Secret, x => Assert.Equal(0, x));
        Assert.True(owner.ClaimDelegation(bound.BindingId).DelegationAttempted);
        Assert.Throws<CaptureRuntimeLocalException>(() => runtime.Owner.ClaimDelegation(bound.BindingId));
        await using var observer = clone.CreateDbContext();
        Assert.Equal(1, await observer.Database.SqlQueryRaw<int>("SELECT count(*)::integer AS \"Value\" FROM tagekyc.capture_execution_bindings").SingleAsync());
        Assert.Equal(1, await observer.Database.SqlQueryRaw<int>("SELECT count(*)::integer AS \"Value\" FROM tagekyc.capture_capability_operations WHERE \"OperationKind\"='Bind'").SingleAsync());
        Assert.DoesNotContain(CaptureRuntimeWireCodec.Base64Url(Enumerable.Repeat((byte)111, 32).ToArray()), File.ReadAllText(Path.Combine(runtime.Journal.DirectoryPath, "state.json")));
    }

    [Fact]
    public async Task RuntimeAppend_UsesBindingAndNoApiKey()
    {
        await using var clone = await postgres.CreateDisposableCurrentDatabaseAsync("a2_append_http");
        await using var db = clone.CreateDbContext(); await using var runtime = await RuntimeFixture.Create(db);
        var seeded = await SeedSessionCapability(db);
        using var secret = new SensitiveByteBuffer(seeded.Secret);
        var pending = runtime.Owner.BeginHandoff("synthetic", seeded.Session, seeded.Capability, DateTimeOffset.UtcNow.AddMinutes(5));
        var binding = await runtime.Client.BindAsync(seeded.Capability, secret, pending.BindOperationId);
        runtime.Owner.ConfirmBinding(seeded.Capability, pending.BindOperationId, binding);
        runtime.Owner.AcceptHandoff(seeded.Capability, pending.BindOperationId, secret); runtime.Owner.ClaimDelegation(binding.BindingId);
        var context = new Tip71SessionContext(seeded.Session.ToString("N"), "synthetic-challenge", "MUST-NOT-BE-AUTHORITY", "MUST-NOT-BE-AUTHORITY", "synthetic-request", "synthetic-correlation");
        var hash = "sha256:" + new string('a', 64);
        var captureRequest = Tip71EvidenceRequestFactory.NfcArtifact(context, hash);
        var capture = await runtime.Client.AppendCaptureArtifactAsync(context.SessionId, captureRequest, "old-run-id|artifact");
        Assert.True(capture.Accepted);
        var replay = await runtime.Client.AppendCaptureArtifactAsync(context.SessionId, captureRequest, "ignored-legacy-key");
        Assert.True(replay.Deduplicated); Assert.Equal(capture.CaptureArtifactId, replay.CaptureArtifactId);
        var nfc = Tip71EvidenceRequestFactory.NfcPassedEvidence(context, capture.CaptureArtifactId, hash, DateTimeOffset.UtcNow, Tip74ChipAuthFlag.ResponseValid);
        var evidence = await runtime.Client.AppendEvidenceResultAsync(context.SessionId, nfc, "old-run-id|evidence");
        Assert.True(evidence.Accepted);
        var selfieHash = "sha256:" + new string('b',64); var mediaHash = "sha256:" + new string('c',64);
        var selfie = await runtime.Client.AppendCaptureArtifactAsync(context.SessionId,Tip71EvidenceRequestFactory.LiveSelfieArtifact(context,selfieHash));
        var media = await runtime.Client.AppendCaptureArtifactAsync(context.SessionId,Tip73EvidenceRequestFactory.LiveMediaArtifact(context,mediaHash));
        var face = Tip71EvidenceRequestFactory.FaceMatchPassedEvidence(context,
            new(capture.CaptureArtifactId,hash,evidence.EvidenceResultId,string.Empty),new(selfie.CaptureArtifactId,selfieHash,DateTimeOffset.UtcNow),
            new(.92m,"synthetic-face-engine","v1"));
        var live = Tip73EvidenceRequestFactory.LivenessPassedEvidence(context,new(media.CaptureArtifactId,mediaHash,DateTimeOffset.UtcNow),
            new(.91m,"fixture-liveness","v1"));
        live = live with { LivenessEvidenceDecisionBasis=live.LivenessEvidenceDecisionBasis! with { Method="fixture-liveness" } };
        using var dropping = new DropDecisionBasis(runtime.App.GetTestServer().CreateHandler(),runtime.Keys,runtime.Identity,binding.BindingId);
        using var checkedClient = new CaptureRuntimeHttpClient(new Uri(runtime.Journal.ServerOrigin),runtime.Journal,runtime.Keys,transport:dropping);
        foreach(var item in new[] { (Request:face,Field:"FaceMatchEvidenceDecisionBasis",Error:"FACE_MATCH_DECISION_BASIS_REQUIRED"),
            (Request:live,Field:"LivenessEvidenceDecisionBasis",Error:"LIVENESS_DECISION_BASIS_REQUIRED") })
        {
            dropping.Field=item.Field;
            Assert.Equal(item.Error,(await Assert.ThrowsAsync<CaptureAgentFlowException>(() => checkedClient.AppendEvidenceResultAsync(context.SessionId,item.Request))).ReasonCode);
            Assert.True((await checkedClient.AppendEvidenceResultAsync(context.SessionId,item.Request)).Accepted);
        }
        Assert.Equal(2,dropping.Dropped);
        await using var observer = clone.CreateDbContext();
        var rows = await observer.CaptureArtifacts.Where(x => x.VerificationSessionId == seeded.Session).ToArrayAsync();
        Assert.Equal(3,rows.Length);
        Assert.All(rows,row => { Assert.Equal(runtime.Identity.CaptureAgentId!.Value.ToString("N"),row.CaptureAgentId);
            Assert.Equal(runtime.Identity.DeviceInstallationId!.Value.ToString("N"),row.DeviceId); });
        Assert.Equal(3, await observer.EvidenceResults.CountAsync(x => x.VerificationSessionId == seeded.Session));
        Assert.Equal(6, await observer.AppendIdempotencyRecords.CountAsync(x => x.VerificationSessionId == seeded.Session));
        Assert.Equal(6, runtime.Owner.ReadRequired().SubmissionOperations.Count(x => x.ResultAcknowledged));
        Assert.Equal(10, await observer.Database.SqlQueryRaw<int>("SELECT count(*)::integer AS \"Value\" FROM tagekyc.capture_runtime_request_nonces").SingleAsync());
    }

    private sealed class DropDecisionBasis(HttpMessageHandler inner,ICaptureRuntimeKeyStore keys,CaptureRuntimeJournalState identity,Guid binding) : DelegatingHandler(inner)
    {
        public string? Field; public int Dropped;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {
            if (Field is not { } field) return await base.SendAsync(request,ct);
            Field=null; Dropped++;
            var original=await request.Content!.ReadAsByteArrayAsync(ct);
            byte[]? changed=null;
            try
            {
                var root=JsonNode.Parse(original)!.AsObject();
                Assert.True(root["Payload"]!.AsObject().Remove(field));
                changed=JsonSerializer.SerializeToUtf8Bytes(root);
                request.Content.Dispose(); request.Content=new ByteArrayContent(changed);
                request.Content.Headers.ContentType=new("application/json");
                var nonce=request.Headers.GetValues("X-TagEkyc-Capture-Runtime-Nonce").Single();
                var nonceBytes=Convert.FromBase64String(nonce.Replace('-','+').Replace('_','/')+"=");
                var stamp=DateTimeOffset.Parse(request.Headers.GetValues("X-TagEkyc-Capture-Runtime-Timestamp").Single());
                var op=Guid.ParseExact(request.Headers.GetValues("Idempotency-Key").Single(),"N");
                var signed=CaptureRuntimeWireCodec.Crt1("POST",request.RequestUri!.AbsolutePath,identity.CredentialId!.Value,identity.Generation!.Value,
                    stamp,nonceBytes,"application/json",changed,CaptureRuntimeWireCodec.AppendLine(binding,op));
                request.Headers.Remove("X-TagEkyc-Capture-Runtime-Signature");
                request.Headers.Add("X-TagEkyc-Capture-Runtime-Signature",CaptureRuntimeWireCodec.Base64Url(keys.Sign(identity.CandidateKeyId,
                    Convert.FromHexString(identity.PublicKeyThumbprint!),signed)));
                return await base.SendAsync(request,ct); // real valid signature; only the domain basis is missing
            }
            finally { CryptographicOperations.ZeroMemory(original); if(changed is not null) CryptographicOperations.ZeroMemory(changed); }
        }
    }

    private static async Task<(Guid Session, Guid Capability, byte[] Secret)> SeedSessionCapability(TagEkycDbContext db)
    {
        var session = Guid.NewGuid(); var capability = Guid.NewGuid(); var secret = Enumerable.Repeat((byte)111, 32).ToArray();
        db.Sessions.Add(new() { Id = session, ClientApplicationId = LocalDevRuntimePolicySource.BusinessClientId,
            SubjectRef = "synthetic-nonpatient", Purpose = "PATIENT_REGISTRATION", Profile = nameof(VerificationProfile.StandardEkycProfile),
            State = nameof(VerificationSessionState.Created), Result = nameof(VerificationResult.NotAvailable), AssuranceLevel = nameof(AssuranceLevel.None),
            RequiredChecksJson = "[\"DocumentNfc\",\"FaceMatch\",\"Liveness\"]", PolicySnapshotId = PolicySnapshotId.LocalDevS1.Value,
            RetentionClass = nameof(RetentionClass.LocalDevEphemeral), DeletionEligibility = nameof(DeletionEligibility.NotEvaluated),
            LegalHoldStatus = nameof(LegalHoldStatus.None), PurgeBlockReason = nameof(PurgeBlockReason.None), RequestId = "synthetic-request",
            CorrelationId = "synthetic-correlation", BindingNonceHash = "synthetic-challenge", CreatedAt = DateTimeOffset.UtcNow, ExpiresAt = DateTimeOffset.UtcNow.AddHours(1) });
        await db.SaveChangesAsync();
        await using var connection = new NpgsqlConnection(db.Database.GetConnectionString()); await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        var principal = Guid.Parse("00000000-0000-4000-8000-000000000001");
        await using (var actor = new NpgsqlCommand(
            "SELECT pg_catalog.set_config('tagekyc.actor_principal_id',@principal,true)", connection, transaction))
        {
            actor.Parameters.AddWithValue("principal", principal.ToString("D"));
            await actor.ExecuteNonQueryAsync();
        }
        await using var command = new NpgsqlCommand("""
            SELECT result_code FROM tagekyc.capture_runtime_issue_or_replace_capability(
            @client,@session,'Issue',NULL,NULL,@op,@cap,@prefix,@digest,7,@fingerprint,now(),
            @principal,NULL::uuid,NULL::jsonb)
            """, connection, transaction);
        command.Parameters.AddWithValue("client", LocalDevRuntimePolicySource.BusinessClientId); command.Parameters.AddWithValue("session", session);
        command.Parameters.AddWithValue("op", Guid.NewGuid()); command.Parameters.AddWithValue("cap", capability); command.Parameters.AddWithValue("prefix", capability.ToString("N")[..12]);
        command.Parameters.AddWithValue("digest", CaptureRuntimeVerifierCryptography.ComputeDigest(Enumerable.Repeat((byte)9, 32).ToArray(), CaptureRuntimeVerifierPepperDomain.CapabilityDigest, secret));
        command.Parameters.AddWithValue("fingerprint", SHA256.HashData("synthetic-issue"u8));
        command.Parameters.AddWithValue("principal", principal);
        Assert.Equal("CREATED", await command.ExecuteScalarAsync());
        await transaction.CommitAsync();
        return (session, capability, secret);
    }

    private sealed class RuntimeFixture : IAsyncDisposable
    {
        private readonly string root = Path.Combine(Path.GetTempPath(), "tagekyc-a2-" + Guid.NewGuid().ToString("N"));
        public WindowsCaptureRuntimeJournal Journal { get; }
        public WindowsCaptureRuntimeKeyStore Keys { get; } = new();
        public CaptureRuntimeAgentCoordinator Owner { get; }
        public CaptureRuntimeJournalState Identity => Owner.ReadRequired();
        public WebApplication App { get; }
        public CaptureRuntimeHttpClient Client { get; }
        public RuntimeFixture(WebApplication app, HttpMessageHandler? transport = null)
        {
            App = app; Journal = new(new Uri("https://synthetic.invalid"), root); Owner = new(Journal, Keys);
            Client = new(new Uri(Journal.ServerOrigin), Journal, Keys, transport: transport ?? app.GetTestServer().CreateHandler());
        }
        public static async Task<RuntimeFixture> Create(TagEkycDbContext db)
        {
            using var secret = new SensitiveByteBuffer(RandomNumberGenerator.GetBytes(32)); var issuance = await SeedBootstrap(db, secret.DebugUnsafeBackingArray);
            var result = new RuntimeFixture(await Start(db.Database.GetConnectionString()!));
            try
            {
                var candidate = result.Owner.ReserveCandidate(result.Journal.ServerOrigin, result.Journal.AccountSid);
                var key = result.Keys.Open(candidate.CandidateKeyId, Convert.FromHexString(candidate.PublicKeyThumbprint!));
                var nonce = RandomNumberGenerator.GetBytes(32); var now = DateTimeOffset.UtcNow; var op = Guid.NewGuid();
                var proof = result.Keys.Sign(key.CandidateKeyId, key.Thumbprint, CaptureRuntimeWireCodec.Enroll1(issuance, key.CandidateKeyId, key.Spki, key.Thumbprint, now, nonce, op, secret.Span));
                var pending = new CaptureRuntimeEnrollmentPending(issuance, op, now.AddMinutes(5), now, CaptureRuntimeWireCodec.Base64Url(nonce), CaptureRuntimeWireCodec.Base64Url(proof), new string('a', 64));
                result.Owner.PrepareEnrollment(pending);
                result.Owner.ConfirmEnrollment(pending, await result.Client.RedeemAsync(result.Owner.ReadRequired(), secret)); return result;
            }
            catch { await result.DisposeAsync(); throw; }
        }
        public async ValueTask DisposeAsync()
        {
            Client.Dispose(); await App.DisposeAsync();
            if (Directory.Exists(Journal.DirectoryPath))
            {
                CaptureRuntimeJournalState? state; using (var tx = Journal.Acquire()) state = tx.Read();
                if (state is not null)
                {
                    var locators = new HashSet<Guid>(state.CleanupCandidateKeyIds) { state.CandidateKeyId };
                    if (state.PendingRotation is { } pending) locators.Add(pending.SuccessorCandidateKeyId);
                    foreach (var locator in locators) Keys.DeleteQueuedAbandoned(locator);
                    foreach (var locator in locators) Assert.False(CngKey.Exists(WindowsCaptureRuntimeKeyStore.GetKeyName(locator)));
                }
            }
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task Enroll_TransportExactReplayAndCrt1Configuration_ServerBoundary()
    {
        await using var clone = await postgres.CreateDisposableCurrentDatabaseAsync("a2_enroll_http");
        await using var db = clone.CreateDbContext();
        var secretBytes = Enumerable.Range(0, 32).Select(x => (byte)x).ToArray();
        var issuance = await SeedBootstrap(db, secretBytes);
        await using var app = await Start(db.Database.GetConnectionString()!);
        var root = Path.Combine(Path.GetTempPath(), "tagekyc-a2-" + Guid.NewGuid().ToString("N"));
        var journal = new WindowsCaptureRuntimeJournal(new Uri("https://synthetic.invalid"), root);
        var keys = new WindowsCaptureRuntimeKeyStore();
        var coordinator = new CaptureRuntimeAgentCoordinator(journal, keys);
        var candidate = coordinator.ReserveCandidate(journal.ServerOrigin, journal.AccountSid);
        var key = keys.Open(candidate.CandidateKeyId, Convert.FromHexString(candidate.PublicKeyThumbprint!));
        CaptureRuntimePublicKey? successorKey = null;
        try
        {
            using var secret = new SensitiveByteBuffer(secretBytes);
            var nonce = RandomNumberGenerator.GetBytes(32); var now = DateTimeOffset.UtcNow; var operation = Guid.NewGuid();
            var proof = keys.Sign(key.CandidateKeyId, key.Thumbprint,
                CaptureRuntimeWireCodec.Enroll1(issuance, key.CandidateKeyId, key.Spki, key.Thumbprint, now, nonce, operation, secret.Span));
            var pending = candidate with { Revision = candidate.Revision + 1, State = "EnrollmentPending",
                PendingEnrollment = new(issuance, operation, now.AddMinutes(5), now, CaptureRuntimeWireCodec.Base64Url(nonce),
                    CaptureRuntimeWireCodec.Base64Url(proof), new string('a', 64)) };
            using (var tx = journal.Acquire()) tx.Write(pending, candidate.Revision);
            using var client = new CaptureRuntimeHttpClient(new Uri(journal.ServerOrigin), journal, keys, transport: app.GetTestServer().CreateHandler());
            var first = await client.RedeemAsync(pending, secret);
            // Simulated lost response: journal still contains exact pre-call template.
            var recovered = await client.RedeemAsync(coordinator.ReadRequired(), secret);
            Assert.Equal(first, recovered);
            await using var observer = new NpgsqlConnection(db.Database.GetConnectionString()); await observer.OpenAsync();
            async Task<long> Count(string table)
            {
                await using var command = new NpgsqlCommand($"SELECT count(*) FROM tagekyc.{table} WHERE \"BootstrapIssuanceId\"=@id", observer);
                command.Parameters.AddWithValue("id", issuance); return (long)(await command.ExecuteScalarAsync())!;
            }
            Assert.Equal(1, await Count("capture_runtime_bootstrap_redemption_operations"));
            Assert.Equal(1, await Count("capture_runtime_bootstrap_redemption_events"));
            using var exactBody = CaptureRuntimeWireCodec.EnrollmentBody(issuance, secret.Span, key.CandidateKeyId,
                key.Spki, key.Thumbprint, now, nonce, proof);
            using var mutated = new HttpRequestMessage(HttpMethod.Post, "/api/ekyc/capture-runtime/enrollments/redeem");
            mutated.Headers.Add("Idempotency-Key", operation.ToString("N"));
            mutated.Content = new ByteArrayContent(exactBody.Span.ToArray().Concat(new byte[] { 32 }).ToArray());
            mutated.Content.Headers.ContentType = new("application/json");
            using var rawClient = app.GetTestClient();
            using var conflict = await rawClient.SendAsync(mutated);
            Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
            using (var tx = journal.Acquire()) tx.Write(pending with { Revision = pending.Revision + 1, State = "Active",
                CaptureAgentId = first.CaptureAgentId, DeviceInstallationId = first.DeviceInstallationId,
                CredentialId = first.CredentialId, Generation = first.Generation, PendingEnrollment = null }, pending.Revision);
            var config = await client.FetchRawExportConfigurationAsync(null);
            Assert.Equal(first.CaptureAgentId, config.Document!.CaptureAgentId);
            var cache = new RawExportAgentConfigurationCache(first.CaptureAgentId);
            var limits = new RawExportAgentConfigurationLimits(600);
            cache.Apply(config, DateTimeOffset.UtcNow, limits);
            var unchanged = await client.FetchRawExportConfigurationAsync(config.ETag);
            Assert.True(unchanged.NotModified); cache.Apply(unchanged, DateTimeOffset.UtcNow, limits);
            Assert.Equal(config.Document, cache.GetCurrent(DateTimeOffset.UtcNow, limits));
            await using var nonceCount = new NpgsqlCommand("SELECT count(*) FROM tagekyc.capture_runtime_request_nonces WHERE \"CredentialId\"=@id", observer);
            nonceCount.Parameters.AddWithValue("id", first.CredentialId);
            Assert.Equal(2L, await nonceCount.ExecuteScalarAsync());

            // Real R13 transport, including successor-key lost-response replay.
            var rotation = Guid.NewGuid(); var rotationOperation = Guid.NewGuid();
            await using (var authorize = new NpgsqlCommand("""
                INSERT INTO tagekyc.capture_runtime_role_policy_revisions
                SELECT @nextrole,1,ARRAY['Configuration'],now()-interval '1 minute',"IssuedByCredentialId",now()
                FROM tagekyc.capture_runtime_bootstrap_issuances WHERE "BootstrapIssuanceId"=@issuance;
                INSERT INTO tagekyc.capture_runtime_role_policy_heads VALUES (@nextrole,1,1,now());
                UPDATE tagekyc.capture_runtime_registrations SET "NextRolePolicyId"=@nextrole,"NextRolePolicyRevision"=1 WHERE "CaptureAgentId"=@agent;
                INSERT INTO tagekyc.capture_runtime_rotation_authorizations
                 ("RotationAuthorizationId","CaptureAgentId","DeviceInstallationId","CredentialId","CurrentGeneration",
                  "AuthorizeOperationId","RequestFingerprint","AuthorizedByCredentialId","AuthorizedAtUtc","ExpiresAtUtc","State","Revision")
                SELECT @rotation,@agent,@installation,@credential,1,@operation,decode(repeat('44',32),'hex'),
                  "IssuedByCredentialId",now()-interval '1 minute',now()+interval '5 minutes','Active',1
                FROM tagekyc.capture_runtime_bootstrap_issuances WHERE "BootstrapIssuanceId"=@issuance;
                """, observer))
            {
                authorize.Parameters.AddWithValue("nextrole", Guid.NewGuid()); authorize.Parameters.AddWithValue("issuance", issuance);
                authorize.Parameters.AddWithValue("agent", first.CaptureAgentId); authorize.Parameters.AddWithValue("installation", first.DeviceInstallationId);
                authorize.Parameters.AddWithValue("credential", first.CredentialId); authorize.Parameters.AddWithValue("rotation", rotation);
                authorize.Parameters.AddWithValue("operation", Guid.NewGuid()); await authorize.ExecuteNonQueryAsync();
            }
            var successorJournal = new WindowsCaptureRuntimeJournal(new Uri(journal.ServerOrigin), Path.Combine(root, "synthetic-successor"));
            var successorState = new CaptureRuntimeAgentCoordinator(successorJournal, keys).ReserveCandidate(successorJournal.ServerOrigin, successorJournal.AccountSid);
            successorKey = keys.Open(successorState.CandidateKeyId, Convert.FromHexString(successorState.PublicKeyThumbprint!));
            var rotationProof = keys.Sign(successorKey.CandidateKeyId, successorKey.Thumbprint,
                CaptureRuntimeWireCodec.Rotate1(rotation, first.CaptureAgentId, first.DeviceInstallationId, first.CredentialId, 1,
                    successorKey.CandidateKeyId, successorKey.Spki, successorKey.Thumbprint, rotationOperation));
            var active = coordinator.ReadRequired();
            var rotationPending = new CaptureRuntimeRotationPending(rotation, rotationOperation, 1, successorKey.CandidateKeyId,
                CaptureRuntimeWireCodec.Base64Url(successorKey.Spki), CaptureRuntimeWireCodec.Digest(successorKey.Spki),
                CaptureRuntimeWireCodec.Base64Url(rotationProof), DateTimeOffset.UtcNow.AddMinutes(5), "Predecessor");
            using (var tx = journal.Acquire()) tx.Write(active with { Revision = active.Revision + 1,
                State = "RotationPending", PendingRotation = rotationPending }, active.Revision);
            var completed = await client.CompleteRotationAsync(false);
            Assert.Equal(first.CredentialId, completed.CredentialId); Assert.Equal(2, completed.Generation);
            var uncertain = coordinator.ReadRequired();
            using (var tx = journal.Acquire()) tx.Write(uncertain with { Revision = uncertain.Revision + 1,
                PendingRotation = rotationPending with { AttemptedBranch = "Successor" } }, uncertain.Revision);
            Assert.Equal(completed, await client.CompleteRotationAsync(true));
            await using var completionCount = new NpgsqlCommand("SELECT count(*) FROM tagekyc.capture_runtime_rotation_completion_operations WHERE \"RotationAuthorizationId\"=@rotation", observer);
            completionCount.Parameters.AddWithValue("rotation", rotation);
            Assert.Equal(1L, await completionCount.ExecuteScalarAsync());
            Assert.Equal(4L, await nonceCount.ExecuteScalarAsync());
        }
        finally
        {
            CryptographicOperations.ZeroMemory(secretBytes);
            keys.DeleteConfirmedAbandoned(key.CandidateKeyId, key.Thumbprint);
            if (successorKey is not null) keys.DeleteConfirmedAbandoned(successorKey.CandidateKeyId, successorKey.Thumbprint);
            Directory.Delete(root, true);
        }
    }

    private static async Task<Guid> SeedBootstrap(TagEkycDbContext db, byte[] secret)
    {
        var issuance = Guid.NewGuid();
        await using var connection = new NpgsqlConnection(db.Database.GetConnectionString()); await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO tagekyc.platform_operator_credentials VALUES
              (@operator,@prefix,decode(repeat('11',32),'hex'),1,@principal,
               ARRAY['operator.capture-runtime.manage'],'Active',1,now()-interval '1 hour',now()+interval '1 day',NULL,NULL);
            INSERT INTO tagekyc.capture_runtime_role_policy_revisions VALUES
              (@role,1,ARRAY['Bind','CaptureObservation','Configuration','CredentialRotation','TrustedEvidence'],now()-interval '1 hour',@operator,now());
            INSERT INTO tagekyc.capture_runtime_role_policy_heads VALUES (@role,1,1,now());
            INSERT INTO tagekyc.capture_runtime_trust_profile_revisions VALUES
              (@trust,1,'Managed',true,true,false,now()-interval '1 hour',now()+interval '1 day',@operator,now());
            INSERT INTO tagekyc.capture_runtime_trust_profile_heads VALUES (@trust,1,1,now());
            INSERT INTO tagekyc.capture_runtime_configuration_revisions VALUES
              (@config,1,now()-interval '1 hour',now()+interval '1 day',true,60,100,60,1000,1000,10000,10000,10000,1024,@operator,now());
            INSERT INTO tagekyc.capture_runtime_configuration_heads VALUES (@config,1,1,now());
            INSERT INTO tagekyc.capture_runtime_bootstrap_issuances
             ("BootstrapIssuanceId","KeyLookupPrefix","SecretDigest","VerifierPepperVersion",
              "RuntimeType","TrustProfileId","TrustProfileRevision","RolePolicyId","RolePolicyRevision",
              "ConfigurationId","ConfigurationRevision","AttestationRequirementDigest","RequestFingerprint",
              "IssueOperationId","IssuedByCredentialId","IssuedAtUtc","ExpiresAtUtc","State","Revision")
            VALUES(@issuance,@bootstrap_prefix,@digest,7,'Managed',@trust,1,@role,1,@config,1,
              decode(repeat('22',32),'hex'),decode(repeat('33',32),'hex'),@issue_operation,@operator,
              now()-interval '1 hour',now()+interval '1 hour','Active',1);
            """;
        foreach (var (name, value) in new (string, object)[]
        {
            ("operator",Guid.NewGuid()),("prefix",Guid.NewGuid().ToString("N")[..12]),("principal",Guid.NewGuid()),
            ("role",Guid.NewGuid()),("trust",Guid.NewGuid()),("config",Guid.NewGuid()),("issuance",issuance),
            ("bootstrap_prefix",Guid.NewGuid().ToString("N")[..12]),("issue_operation",Guid.NewGuid()),
            ("digest",CaptureRuntimeVerifierCryptography.ComputeDigest(Enumerable.Repeat((byte)9,32).ToArray(), CaptureRuntimeVerifierPepperDomain.BootstrapDigest,secret))
        }) command.Parameters.AddWithValue(name, value);
        await command.ExecuteNonQueryAsync(); return issuance;
    }

    private static async Task<WebApplication> Start(string connection)
    {
        var builder = WebApplication.CreateBuilder(); builder.WebHost.UseTestServer();
        CaptureRuntimeDbContextFactory Factory(string role) => new(new(new NpgsqlConnectionStringBuilder(connection) { Options = "-c role=" + role, Pooling = false }.ConnectionString, connection));
        builder.Services.AddSingleton<ICaptureRuntimeRequestAuthenticator>(new CaptureRuntimeRequestAuthenticator(Factory("tagekyc_capture_runtime_authenticator")));
        builder.Services.AddSingleton<ICaptureRuntimeRotationCompletionAuthenticator>(new CaptureRuntimeRequestAuthenticator(Factory("tagekyc_capture_runtime_authenticator")));
        builder.Services.AddSingleton<ICaptureRuntimeRotationGateway>(new CaptureRuntimeRotationPersistenceBoundary(Factory("tagekyc_capture_runtime_application")));
        builder.Services.AddScoped<ICaptureRuntimeRotationService, CaptureRuntimeRotationApplicationService>();
        builder.Services.AddSingleton<ICaptureRuntimeEnrollmentGateway>(new CaptureRuntimeEnrollmentPersistenceBoundary(Factory("tagekyc_capture_runtime_application")));
        builder.Services.AddSingleton<ICaptureRuntimeExecutionGateway>(new CaptureRuntimeExecutionPersistenceBoundary(Factory("tagekyc_capture_runtime_application")));
        builder.Services.AddSingleton<ICaptureRuntimeVerifierPepperSource, SyntheticPeppers>();
        builder.Services.AddScoped<ICaptureRuntimeEnrollmentService, CaptureRuntimeEnrollmentApplicationService>();
        builder.Services.AddTagEkycPostgresPersistence(connection);
        builder.Services.AddSingleton<ILocalDevClientPolicyProvider, LocalDevRuntimePolicySource>();
        builder.Services.AddScoped<VerificationEvidenceApplicationService>();
        builder.Services.AddScoped<IAuthorityNeutralVerificationEvidencePlanner>(sp => sp.GetRequiredService<VerificationEvidenceApplicationService>());
        builder.Services.AddScoped<IAuthorityNeutralVerificationEvidenceWriter>(sp => sp.GetRequiredService<VerificationEvidenceApplicationService>());
        builder.Services.AddScoped<IAppendBusinessTransaction, EfAppendBusinessTransaction>();
        builder.Services.AddScoped<ICaptureRuntimeAppendAuthority, CaptureRuntimeAppendAuthority>();
        builder.Services.AddScoped<ICaptureRuntimeAppendGateway, CaptureRuntimeAppendApplicationService>();
        builder.Services.AddScoped<ICaptureRuntimeExecutionService, CaptureRuntimeExecutionApplicationService>();
        var app = builder.Build(); app.MapCaptureRuntimeManagementEndpoints(); app.MapCaptureRuntimeExecutionEndpoints(); app.MapCaptureRuntimeRotationEndpoints();
        await app.StartAsync(); return app;
    }
    private sealed class SyntheticPeppers : ICaptureRuntimeVerifierPepperSource
    {
        public int CurrentVersion => 7;
        public ValueTask<ICaptureRuntimeVerifierPepperLease?> TryResolveAsync(int version, CaptureRuntimeVerifierPepperDomain domain, CancellationToken ct) =>
            ValueTask.FromResult<ICaptureRuntimeVerifierPepperLease?>(version == 7 ? new Lease(domain) : null);
    }
    private sealed class Lease(CaptureRuntimeVerifierPepperDomain domain) : ICaptureRuntimeVerifierPepperLease
    {
        private readonly byte[] key = Enumerable.Repeat((byte)9, 32).ToArray();
        public int Version => 7;
        public CaptureRuntimeVerifierPepperDomain Domain => domain;
        public ReadOnlyMemory<byte> Key => key;
        public void Dispose() => CryptographicOperations.ZeroMemory(key);
    }
}
