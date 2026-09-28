using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using TagEkyc.Api;
using TagEkyc.Application;
using TagEkyc.Application.CaptureRuntime;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.CaptureRuntime;

namespace TagEkyc.IntegrationTests;

public sealed class SiteQualificationMeasurementPlaneTests
{
    private static readonly Guid Credential = Guid.Parse("60000000-0000-4000-8000-000000000001");
    private static readonly Guid Idempotency = Guid.Parse("70000000-0000-4000-8000-000000000001");
    private static readonly Guid Run = Guid.Parse("80000000-0000-4000-8000-000000000001");

    [Fact]
    public async Task Exact_active_single_use_run_crosses_site_gate_and_reaches_admission()
    {
        var store = new RunStore { Observed = new(Run, SiteRawIngressQualificationRunMode.FullBodyHeldCommit, true), Consume = true };
        var admission = new Admission();
        await using var app = await StartAsync(store, admission);

        var result = await SendAsync(app, Credential, 1, Idempotency);

        Assert.Equal(403, result.Response.StatusCode);
        Assert.Equal(1, store.ObserveCalls);
        Assert.Equal(1, store.ConsumeCalls);
        Assert.Equal(1, admission.Calls);
        Assert.Equal(0, result.Body.ReadCalls);
    }

    [Fact]
    public async Task Ordinary_request_remains_site_blocked_before_admission_and_body()
    {
        var store = new RunStore();
        var admission = new Admission();
        await using var app = await StartAsync(store, admission);

        var result = await SendAsync(app, Credential, 1, Idempotency);

        await AssertSiteBlockedAsync(result, admission);
        Assert.Equal(1, store.ObserveCalls);
        Assert.Equal(0, store.ConsumeCalls);
    }

    [Theory]
    [InlineData("credential")]
    [InlineData("generation")]
    [InlineData("idempotency")]
    public async Task Wrong_binding_arm_remains_site_blocked(string arm)
    {
        var headers = new Dictionary<string, string>(Headers(Credential, 1, Idempotency),
            StringComparer.Ordinal);
        var store = new RunStore
        {
            RequiredBinding = BindingFromHeaders(headers, Credential, 1, Idempotency),
            Observed = new(Run, SiteRawIngressQualificationRunMode.FullBodyHeldCommit, true),
            Consume = true
        };
        var admission = new Admission();
        await using var app = await StartAsync(store, admission);
        var credential = arm == "credential" ? Guid.Parse("61000000-0000-4000-8000-000000000001") : Credential;
        var generation = arm == "generation" ? 2 : 1;
        var idempotency = arm == "idempotency" ? Guid.Parse("71000000-0000-4000-8000-000000000001") : Idempotency;
        headers[CaptureRuntimeCrt1RequestParser.CredentialIdHeader] = credential.ToString("N");
        headers[CaptureRuntimeCrt1RequestParser.CredentialGenerationHeader] =
            generation.ToString(CultureInfo.InvariantCulture);
        headers["Idempotency-Key"] = idempotency.ToString("N");

        var result = await SendHeadersAsync(app, headers);

        await AssertSiteBlockedAsync(result, admission);
        Assert.Equal(0, store.ConsumeCalls);
    }

    [Fact]
    public async Task Newly_signed_request_with_different_exact_metadata_cannot_use_registered_run()
    {
        using var signingKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var original = SignedHeaders(signingKey, Guid.Parse("90000000-0000-4000-8000-000000000001"));
        var changed = SignedHeaders(signingKey, Guid.Parse("90000000-0000-4000-8000-000000000002"));
        Assert.True(signingKey.VerifyData(changed.Preimage, changed.Signature, HashAlgorithmName.SHA256,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation));
        Assert.NotEqual(original.Binding.IngressMetadataSha256, changed.Binding.IngressMetadataSha256);

        var store = new RunStore
        {
            RequiredBinding = original.Binding,
            Observed = new(Run, SiteRawIngressQualificationRunMode.FullBodyHeldCommit, true),
            Consume = true
        };
        var admission = new Admission();
        var authenticator = new SignatureAuthenticator(signingKey);
        await using var app = await StartAsync(store, admission, authenticator: authenticator);

        var accepted = await SendHeadersAsync(app, original.Headers);
        Assert.Equal(403, accepted.Response.StatusCode);
        Assert.Equal(1, admission.Calls);
        Assert.Equal(1, authenticator.VerifiedCalls);

        var rejected = await SendHeadersAsync(app, changed.Headers);
        await AssertSiteBlockedAsync(rejected, admission, expectedAdmissionCalls: 1);
        Assert.Equal(1, authenticator.VerifiedCalls);
        Assert.Equal(2, store.ObserveCalls);
        Assert.Equal(1, store.ConsumeCalls);
    }

    [Fact]
    public async Task Expired_run_remains_site_blocked()
    {
        var store = new RunStore { Observed = new(Run, SiteRawIngressQualificationRunMode.FullBodyHeldCommit, false) };
        var admission = new Admission();
        await using var app = await StartAsync(store, admission);

        await AssertSiteBlockedAsync(await SendAsync(app, Credential, 1, Idempotency), admission);
        Assert.Equal(0, store.ConsumeCalls);
    }

    [Fact]
    public async Task Already_consumed_run_cannot_be_reused()
    {
        var store = new RunStore { Observed = new(Run, SiteRawIngressQualificationRunMode.FullBodyHeldCommit, true), Consume = false };
        var admission = new Admission();
        await using var app = await StartAsync(store, admission);

        await AssertSiteBlockedAsync(await SendAsync(app, Credential, 1, Idempotency), admission);
        Assert.Equal(1, store.ConsumeCalls);
    }

    [Fact]
    public async Task Api_key_header_on_raw_request_cannot_select_the_bypass()
    {
        var store = new RunStore { Observed = new(Run, SiteRawIngressQualificationRunMode.FullBodyHeldCommit, true), Consume = true };
        var admission = new Admission();
        await using var app = await StartAsync(store, admission);

        var result = await SendAsync(app, Credential, 1, Idempotency,
            context => context.Request.Headers["X-TagEkyc-Api-Key"] = "qualification-key");

        await AssertSiteBlockedAsync(result, admission);
        Assert.Equal(0, store.ObserveCalls);
    }

    [Fact]
    public async Task Hostile_application_read_before_broker_commit_sets_server_measurement_bad()
    {
        var store = new RunStore
        {
            Observed = new(Run, SiteRawIngressQualificationRunMode.FullBodyHeldCommit, true),
            Consume = true
        };
        var admission = new ReadingAdmission();
        await using var app = await StartAsync(store, admission);

        var result = await SendAsync(app, Credential, 1, Idempotency);

        Assert.Equal(403, result.Response.StatusCode);
        Assert.Equal(1, admission.Calls);
        Assert.True(result.Body.ReadCalls > 0);
        Assert.True(store.RecordedBodyReads > 0);
    }

    [Fact]
    public async Task Lost_final_mode_aborts_after_one_admission_without_reposting()
    {
        var store = new RunStore
        {
            Observed = new(Run, SiteRawIngressQualificationRunMode.LostFinalNoRetry, true),
            Consume = true
        };
        var admission = new Admission();
        await using var app = await StartAsync(store, admission);

        SendResult? result = null;
        try { result = await SendAsync(app, Credential, 1, Idempotency); }
        catch (Exception error) when (error is IOException or OperationCanceledException) { }

        Assert.Equal(1, store.ObserveCalls);
        Assert.Equal(1, store.ConsumeCalls);
        Assert.Equal(1, admission.Calls);
        Assert.True(result is null || result.Response.HttpContext.RequestAborted.IsCancellationRequested);
    }

    [Fact]
    public async Task Renewal_run_is_measured_while_current_site_pass_remains_active()
    {
        var store = new RunStore
        {
            Observed = new(Run, SiteRawIngressQualificationRunMode.FullBodyHeldCommit, true),
            Consume = true
        };
        var admission = new Admission();
        await using var app = await StartAsync(store, admission, new QualifiedSiteGate());

        var result = await SendAsync(app, Credential, 1, Idempotency);

        Assert.Equal(403, result.Response.StatusCode);
        Assert.Equal(1, store.ObserveCalls);
        Assert.Equal(1, store.ConsumeCalls);
        Assert.Equal(1, admission.Calls);
    }

    [Fact]
    public async Task Qualified_normal_submission_is_unaffected_when_measurement_lookup_fails()
    {
        var store = new RunStore { ThrowOnObserve = true };
        var admission = new Admission();
        await using var app = await StartAsync(store, admission, new QualifiedSiteGate());

        var result = await SendAsync(app, Credential, 1, Idempotency);

        Assert.Equal(403, result.Response.StatusCode);
        Assert.Equal(1, admission.Calls);
        Assert.Equal(0, store.ConsumeCalls);
    }

    private static async Task AssertSiteBlockedAsync(SendResult result, Admission admission,
        int expectedAdmissionCalls = 0)
    {
        Assert.Equal(503, result.Response.StatusCode);
        var payload = await ReadAsync(result.Response);
        Assert.Contains(CaptureRuntimeSiteTransportQualificationPolicy.InvalidCode, payload, StringComparison.Ordinal);
        Assert.DoesNotContain("CAPTURE_RUNTIME_ACTIVATION_EVIDENCE_INCOMPLETE", payload, StringComparison.Ordinal);
        Assert.DoesNotContain("CAPTURE_RUNTIME_ACTIVATION_EVIDENCE_INVALID", payload, StringComparison.Ordinal);
        Assert.DoesNotContain("CAPTURE_RUNTIME_STARTUP_NOT_READY", payload, StringComparison.Ordinal);
        Assert.Equal(expectedAdmissionCalls, admission.Calls);
        Assert.Equal(0, result.Body.ReadCalls);
    }

    private static async Task<WebApplication> StartAsync(RunStore store,
        ICaptureRuntimeRawIngressAdmission admission,
        ISiteRawIngressTransportQualificationRuntimeGate? gate = null,
        ICaptureRuntimeRequestAuthenticator? authenticator = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<ISiteRawIngressTransportQualificationRuntimeGate>(
            gate ?? new ClosedSiteGate());
        builder.Services.AddSingleton<ICaptureRuntimeSiteTransportQualificationSettingsProvider, Settings>();
        builder.Services.AddSingleton<ISiteRawIngressQualificationRunStore>(store);
        builder.Services.AddSingleton<ICaptureRuntimeRequestAuthenticator>(
            authenticator ?? new AcceptedAuthenticator());
        builder.Services.AddSingleton<ICaptureRuntimeRawIngressAdmission>(admission);
        builder.Services.AddScoped<ISiteRawIngressQualificationRequestMeasurement, RequestMeasurement>();
        var app = builder.Build();
        app.MapCaptureRuntimeRawIngressEndpoints();
        await app.StartAsync();
        return app;
    }

    private static async Task<SendResult> SendAsync(WebApplication app, Guid credential, long generation,
        Guid idempotency, Action<HttpContext>? configure = null)
        => await SendHeadersAsync(app, Headers(credential, generation, idempotency), configure);

    private static async Task<SendResult> SendHeadersAsync(WebApplication app,
        IReadOnlyDictionary<string, string> headers, Action<HttpContext>? configure = null)
    {
        var body = new Body();
        var result = await app.GetTestServer().SendAsync(context =>
        {
            context.Request.Method = "POST";
            context.Request.Path = "/api/ekyc/raw-export/source-ingress";
            context.Request.ContentType = "image/jpeg";
            context.Request.ContentLength = 17;
            context.Request.Body = body;
            foreach (var (name, value) in headers)
                context.Request.Headers[name] = value;
            configure?.Invoke(context);
        });
        return new(result.Response, body);
    }

    private static SignedHeaderSet SignedHeaders(ECDsa key, Guid sessionId)
    {
        var headers = new Dictionary<string, string>(Headers(Credential, 1, Idempotency),
            StringComparer.Ordinal);
        headers["X-TagEkyc-Verification-Session-Id"] = sessionId.ToString("N");
        var binding = BindingFromHeaders(headers, Credential, 1, Idempotency);
        var nonceText = headers[CaptureRuntimeCrt1RequestParser.NonceHeader];
        var preimage = Encoding.UTF8.GetBytes(string.Join('\n',
            "TAG-EKYC-CRT1", "POST", "/api/ekyc/raw-export/source-ingress",
            Credential.ToString("N"), "1", headers[CaptureRuntimeCrt1RequestParser.TimestampHeader],
            nonceText, "image/jpeg", "17", new string('a', 64),
            "ingress=IngressMetadataSha256=" + binding.IngressMetadataSha256) + "\n");
        var signature = key.SignData(preimage, HashAlgorithmName.SHA256,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        headers[CaptureRuntimeCrt1RequestParser.SignatureHeader] = Base64Url(signature);
        return new(headers, binding, preimage, signature);
    }

    private static IReadOnlyDictionary<string, string> Headers(Guid credential, long generation, Guid idempotency)
    {
        var now = DateTimeOffset.Parse("2026-09-27T00:00:00.0000000+00:00", CultureInfo.InvariantCulture);
        var headers = new Dictionary<string, string>
        {
            ["X-TagEkyc-Agent-Configuration-Revision"] = "7",
            ["X-TagEkyc-Verification-Session-Id"] = Guid.NewGuid().ToString("N"),
            ["X-TagEkyc-Capture-Artifact-Id"] = Guid.NewGuid().ToString("N"),
            ["X-TagEkyc-Capture-Revision"] = "3",
            ["X-TagEkyc-Raw-Class"] = "ChipDg2Portrait",
            ["Idempotency-Key"] = idempotency.ToString("N"),
            ["X-TagEkyc-Captured-At-Utc"] = now.ToString("O", CultureInfo.InvariantCulture),
            ["X-TagEkyc-Retention-Started-At-Utc"] = now.ToString("O", CultureInfo.InvariantCulture),
            ["X-TagEkyc-Retention-Expires-At-Utc"] = now.AddSeconds(60).ToString("O", CultureInfo.InvariantCulture),
            ["X-TagEkyc-Retention-Budget-Seconds"] = "60"
        };
        var metadata = Encoding.UTF8.GetBytes(string.Concat(headers.Select(item => $"{item.Key}={item.Value}\n")));
        var binding = "ingress=IngressMetadataSha256=" +
                      Convert.ToHexString(SHA256.HashData(metadata)).ToLowerInvariant();
        var nonce = RandomNumberGenerator.GetBytes(32);
        headers[CaptureRuntimeCrt1RequestParser.CredentialIdHeader] = credential.ToString("N");
        headers[CaptureRuntimeCrt1RequestParser.CredentialGenerationHeader] = generation.ToString(CultureInfo.InvariantCulture);
        headers[CaptureRuntimeCrt1RequestParser.TimestampHeader] = now.UtcDateTime.ToString(
            "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture);
        headers[CaptureRuntimeCrt1RequestParser.NonceHeader] = Base64Url(nonce);
        headers[CaptureRuntimeCrt1RequestParser.SignatureHeader] = Base64Url(new byte[64]);
        headers["X-TagEkyc-Plaintext-Sha256"] = new string('a', 64);
        _ = binding;
        return headers;
    }

    private static SiteRawIngressQualificationRunBinding ExpectedBinding(
        Guid credential, long generation, Guid idempotency)
    {
        var headers = Headers(credential, generation, idempotency);
        return BindingFromHeaders(headers, credential, generation, idempotency);
    }

    private static SiteRawIngressQualificationRunBinding BindingFromHeaders(
        IReadOnlyDictionary<string, string> headers, Guid credential, long generation,
        Guid idempotency)
    {
        var names = new[] { "X-TagEkyc-Agent-Configuration-Revision", "X-TagEkyc-Verification-Session-Id",
            "X-TagEkyc-Capture-Artifact-Id", "X-TagEkyc-Capture-Revision", "X-TagEkyc-Raw-Class",
            "Idempotency-Key", "X-TagEkyc-Captured-At-Utc", "X-TagEkyc-Retention-Started-At-Utc",
            "X-TagEkyc-Retention-Expires-At-Utc", "X-TagEkyc-Retention-Budget-Seconds" };
        var bytes = Encoding.UTF8.GetBytes(string.Concat(names.Select(name => $"{name}={headers[name]}\n")));
        return new(credential, generation, idempotency,
            Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(),
            "image/jpeg", 17, new string('a', 64));
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static async Task<string> ReadAsync(HttpResponse response)
    {
        return await new StreamReader(response.Body).ReadToEndAsync();
    }

    private sealed class ClosedSiteGate : ISiteRawIngressTransportQualificationRuntimeGate
    {
        public CaptureRuntimeSiteTransportQualificationEvaluation Evaluate(DateTimeOffset now) =>
            new(CaptureRuntimeSiteTransportQualificationState.Missing,
                CaptureRuntimeSiteTransportQualificationPolicy.InvalidCode);
    }

    private sealed class QualifiedSiteGate : ISiteRawIngressTransportQualificationRuntimeGate
    {
        public CaptureRuntimeSiteTransportQualificationEvaluation Evaluate(DateTimeOffset now) =>
            new(CaptureRuntimeSiteTransportQualificationState.Qualified, "SITE_QUALIFIED");
    }

    private sealed class Settings : ICaptureRuntimeSiteTransportQualificationSettingsProvider
    {
        public CaptureRuntimeSiteTransportQualificationSettings Current { get; } = new(
            "synthetic-site", "https://127.0.0.1:8443", "synthetic-revision", TimeSpan.FromMinutes(5));
    }

    private sealed class AcceptedAuthenticator : ICaptureRuntimeRequestAuthenticator
    {
        public Task<SessionOperationResult<AuthenticatedCaptureRuntimeContext>> AuthenticateAsync(
            CaptureRuntimeSignedRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(SessionOperationResult<AuthenticatedCaptureRuntimeContext>.Success(new(
                Guid.Parse("40000000-0000-4000-8000-000000000001"),
                Guid.Parse("50000000-0000-4000-8000-000000000001"), request.CredentialId,
                request.CredentialGeneration, new byte[32],
                Guid.Parse("10000000-0000-4000-8000-000000000001"), 1, 1, 1, 1,
                request.SignedAtUtc, request.Nonce, SHA256.HashData(request.ExactSignedPreimage.Span))));
    }

    private sealed class SignatureAuthenticator(ECDsa key) : ICaptureRuntimeRequestAuthenticator
    {
        internal int VerifiedCalls;
        public Task<SessionOperationResult<AuthenticatedCaptureRuntimeContext>> AuthenticateAsync(
            CaptureRuntimeSignedRequest request, CancellationToken cancellationToken = default)
        {
            var verified = key.VerifyData(request.ExactSignedPreimage.Span, request.Signature,
                HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
            if (!verified)
                return Task.FromResult(SessionOperationResult<AuthenticatedCaptureRuntimeContext>.Failure(
                    CaptureRuntimeErrorCodes.AccessDenied, "Signature invalid.", 403));
            VerifiedCalls++;
            return Task.FromResult(SessionOperationResult<AuthenticatedCaptureRuntimeContext>.Success(new(
                Guid.Parse("40000000-0000-4000-8000-000000000001"),
                Guid.Parse("50000000-0000-4000-8000-000000000001"), request.CredentialId,
                request.CredentialGeneration, new byte[32],
                Guid.Parse("10000000-0000-4000-8000-000000000001"), 1, 1, 1, 1,
                request.SignedAtUtc, request.Nonce, SHA256.HashData(request.ExactSignedPreimage.Span))));
        }
    }

    private sealed class Admission : ICaptureRuntimeRawIngressAdmission
    {
        internal int Calls;
        public ValueTask<CaptureRuntimeRawIngressAdmissionResult> AdmitAsync(
            CaptureRuntimeRawIngressAdmissionContext context, Stream body, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref Calls);
            return ValueTask.FromResult(new CaptureRuntimeRawIngressAdmissionResult(
                CaptureRuntimeRawIngressOutcome.BindingInvalid, null, null, null, null));
        }
    }

    private sealed class ReadingAdmission : ICaptureRuntimeRawIngressAdmission
    {
        internal int Calls;
        public async ValueTask<CaptureRuntimeRawIngressAdmissionResult> AdmitAsync(
            CaptureRuntimeRawIngressAdmissionContext context, Stream body,
            CancellationToken cancellationToken)
        {
            Calls++;
            var buffer = new byte[1];
            _ = await body.ReadAsync(buffer, cancellationToken);
            return new(CaptureRuntimeRawIngressOutcome.BindingInvalid, null, null, null, null);
        }
    }

    private sealed class RequestMeasurement : ISiteRawIngressQualificationRequestMeasurement
    {
        public Guid? QualificationRunId { get; private set; }
        public bool BrokerCommitted { get; private set; }
        public int BodyReadsWhileBrokerHeld { get; private set; }
        public void Begin(Guid qualificationRunId) => QualificationRunId = qualificationRunId;
        public void MarkBrokerCommitted() => BrokerCommitted = true;
        public void ObserveBodyRead() { if (!BrokerCommitted) BodyReadsWhileBrokerHeld++; }
    }

    private sealed class RunStore : ISiteRawIngressQualificationRunStore
    {
        internal SiteRawIngressQualificationRunBinding? RequiredBinding;
        internal SiteRawIngressQualificationRawPost? Observed;
        internal bool Consume;
        internal int ObserveCalls;
        internal int ConsumeCalls;
        internal int RecordedBodyReads;
        internal bool ThrowOnObserve;
        public Task<bool> EnrollSyntheticCredentialAsync(
            SiteRawIngressQualificationSyntheticCredentialEnrollment enrollment,
            DateTimeOffset now, CancellationToken cancellationToken) => Task.FromResult(true);

        public Task<SiteRawIngressQualificationRunHandle?> RegisterAsync(
            SiteRawIngressQualificationRunRegistration registration, DateTimeOffset now,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<SiteRawIngressQualificationRawPost?> ObserveRawPostAsync(
            CaptureRuntimeSiteTransportQualificationSettings settings,
            SiteRawIngressQualificationRunBinding binding, DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            ObserveCalls++;
            if (ThrowOnObserve) throw new InvalidOperationException("measurement unavailable");
            var matches = RequiredBinding is null || RequiredBinding == binding;
            return Task.FromResult(matches ? Observed : null);
        }

        public Task<bool> ConsumeAuthenticatedAsync(Guid qualificationRunId,
            SiteRawIngressQualificationRunBinding binding, DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            ConsumeCalls++;
            return Task.FromResult(Consume);
        }

        public Task<bool> ReleaseBrokerAsync(Guid qualificationRunId, DateTimeOffset now,
            SiteRawIngressQualificationRunAccess access,
            CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> AcknowledgeBrokerCommitAsync(Guid qualificationRunId, DateTimeOffset now,
            SiteRawIngressQualificationRunAccess access,
            CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> RecordAgentObservationAsync(Guid qualificationRunId,
            SiteRawIngressQualificationAgentObservation observation, DateTimeOffset now,
            SiteRawIngressQualificationRunAccess access,
            CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> RecordServerBodyReadsAsync(Guid qualificationRunId, int readsWhileBrokerHeld,
            DateTimeOffset now, CancellationToken cancellationToken)
        {
            RecordedBodyReads += readsWhileBrokerHeld;
            return Task.FromResult(true);
        }
        public Task<SiteRawIngressQualificationRunReport?> ReadAsync(Guid qualificationRunId,
            DateTimeOffset now, SiteRawIngressQualificationRunAccess access,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class Body : MemoryStream
    {
        internal int ReadCalls;
        internal Body() : base(new byte[17], false) { }
        public override int Read(byte[] buffer, int offset, int count)
        { ReadCalls++; return base.Read(buffer, offset, count); }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        { ReadCalls++; return base.ReadAsync(buffer, cancellationToken); }
    }

    private sealed record SendResult(HttpResponse Response, Body Body);
    private sealed record SignedHeaderSet(IReadOnlyDictionary<string, string> Headers,
        SiteRawIngressQualificationRunBinding Binding, byte[] Preimage, byte[] Signature);
}
