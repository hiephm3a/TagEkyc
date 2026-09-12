using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TagEkyc.Api;
using TagEkyc.Application.Ports;
using TagEkyc.Infrastructure.Auth;
using TagEkyc.Infrastructure.CaptureRuntime;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88C1C6BA1RawIngressBoundaryTests(PostgresPersistenceFixture postgres)
{
    private const string Path = "/api/ekyc/raw-export/source-ingress";

    [Fact]
    public async Task RawIngressAuthSuccess_HandsUnreadBodyToA3ExactlyOnce()
    {
        using var signer = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var identity = await SeedAsync(signer);
        var body = new CountingBody();
        var admission = new CountingAdmission(postgres.ConnectionString, body);
        await using var app = await StartAsync(admission);
        var request = SignedRequest(identity.Credential, signer);
        var result = await SendAsync(app, request, body);

        Assert.Equal(200, result.Response.StatusCode);
        Assert.Equal(1, admission.Calls);
        Assert.Equal(0, body.ReadCalls);
        Assert.Equal(0, body.BytesRead);
        Assert.Equal(1, admission.CommittedNonceCountAtEntry);
        Assert.Equal(identity.Agent, admission.Context!.CaptureAgentId);
        Assert.Equal(identity.Installation, admission.Context.DeviceInstallationId);
        Assert.Equal(identity.Credential, admission.Context.CredentialId);
        Assert.Equal(identity.RolePolicy, admission.Context.RolePolicyId);
        Assert.Equal(1, admission.Context.CredentialGeneration);
        Assert.Equal(1, admission.Context.RolePolicyRevision);
        Assert.Equal(request.Nonce, admission.Context.Nonce);
        Assert.Equal(request.Fingerprint, admission.Context.SignedEnvelopeFingerprint);
        var handoff = admission.Context;
        Assert.Equal(long.Parse(request.Headers["X-TagEkyc-Agent-Configuration-Revision"]), handoff.AgentConfigurationRevision);
        Assert.Equal(Guid.Parse(request.Headers["X-TagEkyc-Verification-Session-Id"]), handoff.VerificationSessionId);
        Assert.Equal(Guid.Parse(request.Headers["X-TagEkyc-Capture-Artifact-Id"]), handoff.CaptureArtifactId);
        Assert.Equal(int.Parse(request.Headers["X-TagEkyc-Capture-Revision"]), handoff.CaptureRevision);
        Assert.Equal(request.Headers["X-TagEkyc-Raw-Class"], handoff.RawClass);
        Assert.Equal(Guid.Parse(request.Headers["Idempotency-Key"]), handoff.IngressIdempotencyKey);
        Assert.Equal(DateTimeOffset.Parse(request.Headers["X-TagEkyc-Captured-At-Utc"]), handoff.CapturedAtUtc);
        Assert.Equal(DateTimeOffset.Parse(request.Headers["X-TagEkyc-Retention-Started-At-Utc"]), handoff.PlaintextRetentionStartedAtUtc);
        Assert.Equal(DateTimeOffset.Parse(request.Headers["X-TagEkyc-Retention-Expires-At-Utc"]), handoff.PlaintextRetentionExpiresAtUtc);
        Assert.Equal(long.Parse(request.Headers["X-TagEkyc-Retention-Budget-Seconds"]), handoff.PlaintextRetentionBudgetSeconds);
        Assert.Equal("image/jpeg", handoff.MediaType);
        Assert.Equal(17, handoff.ClaimedPlaintextLength);
        Assert.Equal(request.Headers["X-TagEkyc-Plaintext-Sha256"], handoff.ClaimedPlaintextDigest);

        // The same real signed HTTP request cannot hand the stream over twice.
        var replayBody = new CountingBody();
        var replay = await SendAsync(app, request, replayBody);
        Assert.Equal(403, replay.Response.StatusCode);
        Assert.Equal(1, admission.Calls);
        Assert.Equal(0, replayBody.ReadCalls);
        Assert.Equal(1, await NonceCountAsync(identity.Credential));
    }

    [Theory]
    [InlineData("malformed-metadata", 400, true)]
    [InlineData("invalid-signature", 403, true)]
    [InlineData("metadata-tampered", 403, true)]
    [InlineData("wrong-generation", 403, true)]
    [InlineData("missing-role", 403, true)]
    [InlineData("revoked-credential", 403, true)]
    [InlineData("missing-a3", 503, false)]
    [InlineData("missing-authenticator", 503, true)]
    [InlineData("factory-throws", 503, true)]
    [InlineData("mixed-client-key", 403, true)]
    [InlineData("mixed-platform-key", 403, true)]
    public async Task RawIngressAuthDenial_DoesNotReadBodyOrInvokeA3(string scenario, int status, bool registerA3)
    {
        using var signer = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var identity = await SeedAsync(signer, scenario == "missing-role", scenario == "revoked-credential");
        var body = new CountingBody();
        var admission = new CountingAdmission(postgres.ConnectionString, body);
        await using var app = await StartAsync(registerA3 ? admission : null, scenario);
        var request = SignedRequest(identity.Credential, signer, scenario == "wrong-generation" ? 2 : 1);
        if (scenario == "malformed-metadata") request.Headers["X-TagEkyc-Capture-Revision"] = "03";
        if (scenario == "metadata-tampered") request.Headers["X-TagEkyc-Capture-Revision"] = "4";
        if (scenario == "mixed-client-key") request.Headers["X-TagEkyc-Api-Key"] = "must-not-fallback";
        if (scenario == "mixed-platform-key") request.Headers["X-TagEkyc-Platform-Operator-Key"] = "must-not-fallback";
        if (scenario == "invalid-signature")
        {
            var signature = Convert.FromBase64String(request.Headers[CaptureRuntimeCrt1RequestParser.SignatureHeader]
                .Replace('-', '+').Replace('_', '/') + "==");
            signature[0] ^= 1;
            request.Headers[CaptureRuntimeCrt1RequestParser.SignatureHeader] = Base64Url(signature);
        }
        var result = await SendAsync(app, request, body);
        Assert.Equal(status, result.Response.StatusCode);
        Assert.Equal(0, body.ReadCalls);
        Assert.Equal(0, body.BytesRead);
        Assert.Equal(0, admission.Calls);
        // Missing A3 is checked before admitting nonce N; denial must not manufacture N.
        Assert.Equal(0, await NonceCountAsync(identity.Credential));
        using var json = await JsonDocument.ParseAsync(result.Response.Body);
        Assert.Equal(new[] { "code", "correlationId" },
            json.RootElement.EnumerateObject().Select(property => property.Name).Order().ToArray());
        Assert.Equal(status switch { 400 => "REQUEST_INVALID", 403 => "ACCESS_DENIED", _ => "NOT_READY" },
            json.RootElement.GetProperty("code").GetString());
    }

    [Theory]
    [InlineData(CaptureRuntimeRawIngressOutcome.Available, 200)]
    [InlineData(CaptureRuntimeRawIngressOutcome.AlreadyAvailable, 200)]
    [InlineData(CaptureRuntimeRawIngressOutcome.EvaluationInProgress, 409)]
    [InlineData(CaptureRuntimeRawIngressOutcome.BindingInvalid, 403)]
    [InlineData(CaptureRuntimeRawIngressOutcome.CapacityUnavailable, 503)]
    [InlineData(CaptureRuntimeRawIngressOutcome.TransportProtocolInvalid, 400)]
    public async Task RawIngressA3Outcome_MapsWithoutReadingBody(CaptureRuntimeRawIngressOutcome outcome, int status)
    {
        using var signer = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var identity = await SeedAsync(signer);
        var body = new CountingBody();
        var admission = new CountingAdmission(postgres.ConnectionString, body) { Outcome = outcome };
        await using var app = await StartAsync(admission);
        var result = await SendAsync(app, SignedRequest(identity.Credential, signer), body);
        Assert.Equal(status, result.Response.StatusCode);
        Assert.Equal(1, admission.Calls);
        Assert.Equal(1, admission.CommittedNonceCountAtEntry);
        Assert.Equal(0, body.ReadCalls);
        using var json = await JsonDocument.ParseAsync(result.Response.Body);
        Assert.Equal(outcome switch
        {
            CaptureRuntimeRawIngressOutcome.Available => "RAW_EXPORT_SOURCE_AVAILABLE",
            CaptureRuntimeRawIngressOutcome.AlreadyAvailable => "RAW_EXPORT_SOURCE_ALREADY_AVAILABLE",
            CaptureRuntimeRawIngressOutcome.EvaluationInProgress => "RAW_EXPORT_SOURCE_CLAIM_EVALUATION_IN_PROGRESS",
            CaptureRuntimeRawIngressOutcome.BindingInvalid => "RAW_EXPORT_SOURCE_BINDING_INVALID",
            CaptureRuntimeRawIngressOutcome.CapacityUnavailable => "RAW_EXPORT_SOURCE_CAPACITY_UNAVAILABLE",
            _ => "RAW_EXPORT_SOURCE_TRANSPORT_PROTOCOL_INVALID"
        }, json.RootElement.GetProperty("outcomeCode").GetString());
    }

    [Fact]
    public async Task RawIngressEvaluation_RetryHorizonIsAllowedWithoutOtherFields()
    {
        using var signer = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var identity = await SeedAsync(signer);
        var body = new CountingBody();
        var retry = DateTimeOffset.UtcNow.AddMinutes(1);
        var admission = new CountingAdmission(postgres.ConnectionString, body)
        { Outcome = CaptureRuntimeRawIngressOutcome.EvaluationInProgress, RetryNotBeforeUtc = retry };
        await using var app = await StartAsync(admission);
        var result = await SendAsync(app, SignedRequest(identity.Credential, signer), body);
        Assert.Equal(409, result.Response.StatusCode);
        Assert.Equal(1, admission.Calls);
        Assert.Equal(1, admission.CommittedNonceCountAtEntry);
        Assert.Equal(0, body.ReadCalls);
        using var json = await JsonDocument.ParseAsync(result.Response.Body);
        Assert.Equal("RAW_EXPORT_SOURCE_CLAIM_EVALUATION_IN_PROGRESS", json.RootElement.GetProperty("outcomeCode").GetString());
        Assert.Equal(retry, json.RootElement.GetProperty("retryNotBeforeUtc").GetDateTimeOffset());
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("sourceArtifactId").ValueKind);
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("currentSourceState").ValueKind);
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("currentDisposition").ValueKind);
    }

    private async Task<WebApplication> StartAsync(CountingAdmission? admission, string? scenario = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var connection = new NpgsqlConnectionStringBuilder(postgres.ConnectionString)
        {
            Options = "-c role=tagekyc_capture_runtime_authenticator", Pooling = false
        }.ConnectionString;
        builder.Services.AddSingleton<ICaptureRuntimeDbContextFactory>(new CaptureRuntimeDbContextFactory(
            new CaptureRuntimeResolvedDatabaseOptions(connection, connection)));
        if (scenario != "missing-authenticator")
        {
            if (scenario == "factory-throws")
                builder.Services.AddSingleton<ICaptureRuntimeRequestAuthenticator>(_ => throw new InvalidOperationException("Synthetic dependency construction failure"));
            else builder.Services.AddSingleton<ICaptureRuntimeRequestAuthenticator, CaptureRuntimeRequestAuthenticator>();
        }
        if (admission is not null) builder.Services.AddSingleton<ICaptureRuntimeRawIngressAdmission>(admission);
        var app = builder.Build();
        app.MapCaptureRuntimeRawIngressEndpoints();
        await app.StartAsync();
        return app;
    }

    [Theory]
    [InlineData("undefined-enum")]
    [InlineData("available-missing-id")]
    [InlineData("already-available-missing-id")]
    [InlineData("available-forbidden-state")]
    [InlineData("already-available-forbidden-disposition")]
    [InlineData("available-forbidden-retry")]
    [InlineData("binding-forbidden-field")]
    [InlineData("capacity-forbidden-field")]
    [InlineData("transport-forbidden-field")]
    [InlineData("evaluation-forbidden-field")]
    [InlineData("evaluation-forbidden-id")]
    [InlineData("evaluation-forbidden-disposition")]
    [InlineData("port-throws")]
    public async Task RawIngressInvalidA3Result_FailsClosedAfterCommittedNonce(string scenario)
    {
        using var signer = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var identity = await SeedAsync(signer);
        var body = new CountingBody();
        var admission = new CountingAdmission(postgres.ConnectionString, body) { InvalidScenario = scenario };
        await using var app = await StartAsync(admission);
        var result = await SendAsync(app, SignedRequest(identity.Credential, signer), body);
        Assert.Equal(503, result.Response.StatusCode);
        Assert.Equal(1, admission.Calls);
        Assert.Equal(1, admission.CommittedNonceCountAtEntry);
        Assert.Equal(1, await NonceCountAsync(identity.Credential));
        Assert.Equal(0, body.ReadCalls);
        using var json = await JsonDocument.ParseAsync(result.Response.Body);
        Assert.Equal(new[] { "code", "correlationId" }, json.RootElement.EnumerateObject().Select(p => p.Name).Order().ToArray());
        Assert.Equal("NOT_READY", json.RootElement.GetProperty("code").GetString());
    }

    private static Task<HttpContext> SendAsync(WebApplication app, SignedEnvelope envelope, CountingBody body) =>
        app.GetTestServer().SendAsync(context =>
        {
            context.Request.Method = "POST";
            context.Request.Path = Path;
            context.Request.ContentType = "image/jpeg";
            context.Request.ContentLength = 17;
            context.Request.Body = body;
            foreach (var header in envelope.Headers) context.Request.Headers[header.Key] = header.Value;
        });

    private static SignedEnvelope SignedRequest(Guid credential, ECDsa signer, long generation = 1)
    {
        var now = DateTimeOffset.UtcNow;
        var headers = new Dictionary<string, string>
        {
            ["X-TagEkyc-Agent-Configuration-Revision"] = "7",
            ["X-TagEkyc-Verification-Session-Id"] = Guid.NewGuid().ToString("N"),
            ["X-TagEkyc-Capture-Artifact-Id"] = Guid.NewGuid().ToString("N"),
            ["X-TagEkyc-Capture-Revision"] = "3",
            ["X-TagEkyc-Raw-Class"] = "ChipDg2Portrait",
            ["Idempotency-Key"] = Guid.NewGuid().ToString("N"),
            ["X-TagEkyc-Captured-At-Utc"] = now.ToString("O", CultureInfo.InvariantCulture),
            ["X-TagEkyc-Retention-Started-At-Utc"] = now.ToString("O", CultureInfo.InvariantCulture),
            ["X-TagEkyc-Retention-Expires-At-Utc"] = now.AddSeconds(60).ToString("O", CultureInfo.InvariantCulture),
            ["X-TagEkyc-Retention-Budget-Seconds"] = "60"
        };
        var metadata = Encoding.UTF8.GetBytes(string.Concat(headers.Select(header => $"{header.Key}={header.Value}\n")));
        var binding = "ingress=IngressMetadataSha256=" + Convert.ToHexString(SHA256.HashData(metadata)).ToLowerInvariant();
        var nonce = RandomNumberGenerator.GetBytes(32);
        var timestamp = now.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture);
        var bodyDigest = new string('a', 64);
        var preimage = Encoding.UTF8.GetBytes(string.Join('\n', "TAG-EKYC-CRT1", "POST", Path,
            credential.ToString("N"), generation.ToString(CultureInfo.InvariantCulture), timestamp,
            Base64Url(nonce), "image/jpeg", "17", bodyDigest, binding) + "\n");
        headers[CaptureRuntimeCrt1RequestParser.CredentialIdHeader] = credential.ToString("N");
        headers[CaptureRuntimeCrt1RequestParser.CredentialGenerationHeader] = generation.ToString(CultureInfo.InvariantCulture);
        headers[CaptureRuntimeCrt1RequestParser.TimestampHeader] = timestamp;
        headers[CaptureRuntimeCrt1RequestParser.NonceHeader] = Base64Url(nonce);
        headers[CaptureRuntimeCrt1RequestParser.SignatureHeader] = Base64Url(signer.SignData(preimage,
            HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation));
        headers["X-TagEkyc-Plaintext-Sha256"] = bodyDigest;
        return new SignedEnvelope(headers, nonce, SHA256.HashData(preimage));
    }

    private async Task<Identity> SeedAsync(ECDsa signer, bool missingRole = false, bool revoked = false)
    {
        var identity = new Identity(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        // Synthetic setup follows the already-bound runtime proof's durable graph; production
        // authentication below uses only the real resolver and nonce transition functions.
        command.CommandText = """
            INSERT INTO tagekyc.platform_operator_credentials VALUES
              (@operator,@prefix,decode(repeat('11',32),'hex'),1,@principal,
               ARRAY['operator.capture-runtime.manage'],'Active',1,now()-interval '1 hour',now()+interval '1 day',NULL,NULL);
            INSERT INTO tagekyc.capture_runtime_role_policy_revisions VALUES
              (@role,1,@roles,now()-interval '1 hour',@operator,now());
            INSERT INTO tagekyc.capture_runtime_role_policy_heads VALUES (@role,1,1,now());
            INSERT INTO tagekyc.capture_runtime_trust_profile_revisions VALUES
              (@trust,1,'Managed',true,true,false,now()-interval '1 hour',now()+interval '1 day',@operator,now());
            INSERT INTO tagekyc.capture_runtime_trust_profile_heads VALUES (@trust,1,1,now());
            INSERT INTO tagekyc.capture_runtime_configuration_revisions VALUES
              (@config,1,now()-interval '1 hour',now()+interval '1 day',true,60,100,60,1000,1000,10000,10000,10000,1024,@operator,now());
            INSERT INTO tagekyc.capture_runtime_configuration_heads VALUES (@config,1,1,now());
            INSERT INTO tagekyc.capture_runtime_registrations VALUES
              (@agent,'Managed',@trust,1,@config,1,NULL,NULL,NULL,'Active',1,now(),NULL,NULL,NULL,NULL);
            INSERT INTO tagekyc.capture_runtime_installations VALUES
              (@installation,@agent,NULL,NULL,'Pending',1,now(),NULL,NULL,NULL,NULL);
            INSERT INTO tagekyc.capture_runtime_credential_generations VALUES
              (@credential,1,@installation,@candidate,@spki,'TAG-EKYC-CRT1-ECDSA-P256-SHA256',@thumbprint,
               @role,1,now()-interval '1 hour',now()+interval '1 day','Pending',1,NULL,NULL,NULL,NULL);
            UPDATE tagekyc.capture_runtime_installations SET "CurrentCredentialId"=@credential,
              "CurrentCredentialGeneration"=1,"LifecycleState"='Active',"Revision"=2 WHERE "DeviceInstallationId"=@installation;
            UPDATE tagekyc.capture_runtime_credential_generations SET "State"='Active',"Revision"=2 WHERE "CredentialId"=@credential;
            """;
        foreach (var (name, value) in new (string, object)[]
        {
            ("operator", Guid.NewGuid()), ("prefix", Guid.NewGuid().ToString("N")[..12]),
            ("principal", Guid.NewGuid()), ("role", identity.RolePolicy),
            ("roles", new[] { missingRole ? "Configuration" : "RawIngress" }),
            ("trust", Guid.NewGuid()), ("config", Guid.NewGuid()), ("agent", identity.Agent),
            ("installation", identity.Installation), ("credential", identity.Credential),
            ("candidate", Guid.NewGuid()), ("spki", signer.ExportSubjectPublicKeyInfo()),
            ("thumbprint", SHA256.HashData(signer.ExportSubjectPublicKeyInfo()))
        }) command.Parameters.AddWithValue(name, value);
        await command.ExecuteNonQueryAsync();
        if (revoked)
        {
            command.CommandText = """
                UPDATE tagekyc.capture_runtime_credential_generations SET "State"='Revoked',
                  "RevokedAtUtc"=now(),"TerminalReason"='SyntheticProof',"Revision"=3 WHERE "CredentialId"=@credential;
                UPDATE tagekyc.capture_runtime_installations SET "LifecycleState"='Revoked',
                  "RevokedAtUtc"=now(),"LifecycleReason"='SyntheticProof',"Revision"=3 WHERE "DeviceInstallationId"=@installation;
                """;
            await command.ExecuteNonQueryAsync();
        }
        await transaction.CommitAsync();
        return identity;
    }

    private Task<long> NonceCountAsync(Guid credential) => CountNonceAsync(postgres.ConnectionString, credential);

    private static async Task<long> CountNonceAsync(string connectionString, Guid credential, byte[]? nonce = null)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT count(*) FROM tagekyc.capture_runtime_request_nonces WHERE "CredentialId"=@credential
            """ + (nonce is null ? "" : " AND \"Nonce\"=@nonce");
        command.Parameters.AddWithValue("credential", credential);
        if (nonce is not null) command.Parameters.AddWithValue("nonce", nonce);
        return (long)(await command.ExecuteScalarAsync())!;
    }

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private sealed record Identity(Guid Agent, Guid Installation, Guid Credential, Guid RolePolicy);
    private sealed record SignedEnvelope(Dictionary<string, string> Headers, byte[] Nonce, byte[] Fingerprint);

    private sealed class CountingAdmission(string connectionString, CountingBody expectedBody) : ICaptureRuntimeRawIngressAdmission
    {
        public int Calls { get; private set; }
        public long CommittedNonceCountAtEntry { get; private set; }
        public CaptureRuntimeRawIngressAdmissionContext? Context { get; private set; }
        public CaptureRuntimeRawIngressOutcome Outcome { get; init; } = CaptureRuntimeRawIngressOutcome.Available;
        public string? InvalidScenario { get; init; }
        public DateTimeOffset? RetryNotBeforeUtc { get; init; }
        public async ValueTask<CaptureRuntimeRawIngressAdmissionResult> AdmitAsync(
            CaptureRuntimeRawIngressAdmissionContext context, Stream body, CancellationToken cancellationToken)
        {
            Calls++;
            Assert.Same(expectedBody, body);
            Assert.Equal(0, expectedBody.ReadCalls);
            Context = context;
            // A separate connection cannot observe uncommitted N: this is persisted evidence,
            // not a captured input assertion or an authenticator mock.
            CommittedNonceCountAtEntry = await CountNonceAsync(connectionString, context.CredentialId, context.Nonce);
            Assert.Equal(1, CommittedNonceCountAtEntry);
            if (InvalidScenario == "port-throws") throw new InvalidOperationException("Synthetic A3 exception must not leak");
            if (InvalidScenario is not null)
            {
                if (InvalidScenario == "available-forbidden-state")
                    return new(CaptureRuntimeRawIngressOutcome.Available, Guid.NewGuid(), "must-not-leak", null, null);
                if (InvalidScenario == "already-available-forbidden-disposition")
                    return new(CaptureRuntimeRawIngressOutcome.AlreadyAvailable, Guid.NewGuid(), null, "must-not-leak", null);
                if (InvalidScenario == "available-forbidden-retry")
                    return new(CaptureRuntimeRawIngressOutcome.Available, Guid.NewGuid(), null, null, DateTimeOffset.UtcNow);
                if (InvalidScenario.StartsWith("evaluation-forbidden", StringComparison.Ordinal))
                    return new(CaptureRuntimeRawIngressOutcome.EvaluationInProgress,
                        InvalidScenario == "evaluation-forbidden-id" ? Guid.NewGuid() : null,
                        InvalidScenario == "evaluation-forbidden-field" ? "must-not-leak" : null,
                        InvalidScenario == "evaluation-forbidden-disposition" ? "must-not-leak" : null,
                        null);
                var invalidOutcome = InvalidScenario switch
                {
                    "undefined-enum" => (CaptureRuntimeRawIngressOutcome)999,
                    "available-missing-id" => CaptureRuntimeRawIngressOutcome.Available,
                    "already-available-missing-id" => CaptureRuntimeRawIngressOutcome.AlreadyAvailable,
                    "binding-forbidden-field" => CaptureRuntimeRawIngressOutcome.BindingInvalid,
                    "capacity-forbidden-field" => CaptureRuntimeRawIngressOutcome.CapacityUnavailable,
                    _ => CaptureRuntimeRawIngressOutcome.TransportProtocolInvalid
                };
                return new(invalidOutcome, null, InvalidScenario.EndsWith("forbidden-field", StringComparison.Ordinal) ? "must-not-leak" : null, null, null);
            }
            return new(Outcome, Outcome is CaptureRuntimeRawIngressOutcome.Available or CaptureRuntimeRawIngressOutcome.AlreadyAvailable
                ? Guid.NewGuid() : null, null, null, RetryNotBeforeUtc);
        }
    }

    private sealed class CountingBody : MemoryStream
    {
        public int ReadCalls { get; private set; }
        public long BytesRead { get; private set; }
        public CountingBody() : base(new byte[17], false) { }
        public override int Read(byte[] buffer, int offset, int count) { ReadCalls++; var n = base.Read(buffer, offset, count); BytesRead += n; return n; }
        public override int Read(Span<byte> buffer) { ReadCalls++; var n = base.Read(buffer); BytesRead += n; return n; }
        public override int ReadByte() { ReadCalls++; var n = base.ReadByte(); if (n >= 0) BytesRead++; return n; }
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token)
        { ReadCalls++; var n = base.Read(buffer, offset, count); BytesRead += n; return Task.FromResult(n); }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token = default)
        { ReadCalls++; var n = base.Read(buffer.Span); BytesRead += n; return ValueTask.FromResult(n); }
        public override void CopyTo(Stream destination, int bufferSize)
        { ReadCalls++; var remaining = Length - Position; base.CopyTo(destination, bufferSize); BytesRead += remaining; }
        public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken token)
        { ReadCalls++; var remaining = Length - Position; await base.CopyToAsync(destination, bufferSize, token); BytesRead += remaining; }
    }
}
