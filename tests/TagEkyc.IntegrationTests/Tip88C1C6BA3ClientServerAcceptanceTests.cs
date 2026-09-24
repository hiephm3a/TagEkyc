using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using TagEkyc.Api;
using TagEkyc.Application;
using TagEkyc.Application.Ports;
using TagEkyc.Application.RawExport;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.CaptureAgent.Client;
using TagEkyc.CaptureAgent.Core;
using TagEkyc.Contracts.CaptureRuntime;
using TagEkyc.Contracts.CaptureAgent;
using TagEkyc.Contracts.Common;
using TagEkyc.Contracts.TrustedAdapter;
using TagEkyc.Infrastructure.RawExport;
using RawIngressBrokerResult = TagEkyc.Contracts.RawExport.RawIngressBrokerResult;
using RawIngressBrokerHandoff = TagEkyc.Contracts.RawExport.RawIngressBrokerHandoff;
using CaptureAgentFinalResult = TagEkyc.Contracts.RawExport.CaptureAgentFinalResult;

namespace TagEkyc.IntegrationTests;

// Explicit opt-in, cross-repository acceptance.  Agent emits the wire request;
// the actual Server endpoint/parser authenticates it before the unread body is
// handed off exactly once.
public sealed class Tip88C1C6BA3ClientServerAcceptanceTests
{
    public static TheoryData<CaptureRuntimeRawIngressOutcome, int, string> BusinessResults => new()
    {
        { CaptureRuntimeRawIngressOutcome.BindingInvalid, 403, "RAW_EXPORT_SOURCE_BINDING_INVALID" },
        { CaptureRuntimeRawIngressOutcome.NotFoundOrNotAllowed, 403, "NOT_FOUND_OR_NOT_ALLOWED" },
        { CaptureRuntimeRawIngressOutcome.TransportProtocolInvalid, 400, "RAW_EXPORT_SOURCE_TRANSPORT_PROTOCOL_INVALID" },
        { CaptureRuntimeRawIngressOutcome.CapabilityUnavailable, 503, "RAW_EXPORT_SOURCE_CAPABILITY_UNAVAILABLE" },
        { CaptureRuntimeRawIngressOutcome.ArtifactSizeLimitExceeded, 413, "RAW_EXPORT_SOURCE_ARTIFACT_SIZE_LIMIT_EXCEEDED" },
        { CaptureRuntimeRawIngressOutcome.PlaintextRetentionInvalid, 422, "RAW_EXPORT_SOURCE_PLAINTEXT_RETENTION_INVALID" },
        { CaptureRuntimeRawIngressOutcome.CapacityUnavailable, 503, "RAW_EXPORT_SOURCE_CAPACITY_UNAVAILABLE" },
        { CaptureRuntimeRawIngressOutcome.IdempotencyBusy, 409, "RAW_EXPORT_SOURCE_IDEMPOTENCY_BUSY" },
        { CaptureRuntimeRawIngressOutcome.EvaluationInProgress, 409, "RAW_EXPORT_SOURCE_CLAIM_EVALUATION_IN_PROGRESS" },
        { CaptureRuntimeRawIngressOutcome.ClaimTokenInvalid, 403, "RAW_EXPORT_SOURCE_CLAIM_TOKEN_INVALID" },
        { CaptureRuntimeRawIngressOutcome.ClaimRestartRequired, 409, "RAW_EXPORT_SOURCE_CLAIM_RESTART_REQUIRED" },
        { CaptureRuntimeRawIngressOutcome.SourceRetentionNotAuthorized, 403, "SOURCE_RETENTION_NOT_AUTHORIZED" },
        { CaptureRuntimeRawIngressOutcome.HistoricCommitmentKeyUnavailable, 503, "RAW_EXPORT_SOURCE_HISTORIC_COMMITMENT_KEY_UNAVAILABLE" },
        { CaptureRuntimeRawIngressOutcome.FingerprintConflict, 409, "RAW_EXPORT_SOURCE_FINGERPRINT_CONFLICT" },
        { CaptureRuntimeRawIngressOutcome.ReservationBusy, 409, "RAW_EXPORT_SOURCE_RESERVATION_BUSY" },
        { CaptureRuntimeRawIngressOutcome.AlreadyAvailable, 200, "RAW_EXPORT_SOURCE_ALREADY_AVAILABLE" },
        { CaptureRuntimeRawIngressOutcome.TemporarilyUnavailable, 503, "RAW_EXPORT_SOURCE_TEMPORARILY_UNAVAILABLE" },
        { CaptureRuntimeRawIngressOutcome.ContentCommitmentMismatch, 422, "CONTENT_COMMITMENT_MISMATCH" },
        { CaptureRuntimeRawIngressOutcome.RecaptureRequired, 409, "RECAPTURE_REQUIRED" },
        { CaptureRuntimeRawIngressOutcome.ResumePending, 202, "RAW_EXPORT_SOURCE_RESUME_PENDING" },
        { CaptureRuntimeRawIngressOutcome.Available, 200, "RAW_EXPORT_SOURCE_AVAILABLE" },
    };

    public static TheoryData<string, string> ExcludedResults
    {
        get
        {
            var root = new DirectoryInfo(AppContext.BaseDirectory);
            while (root is not null && !File.Exists(Path.Combine(root.FullName, "TagEkyc.sln")))
                root = root.Parent;
            Assert.NotNull(root);
            var parent = File.ReadAllText(Path.Combine(root.FullName, "docs", "tips",
                "tip_88c1_secure_raw_source_sealed_assembly",
                "tip_88c1_c6b_a3_broker_composition_dispatch_v0_6.md"));
            var ledger = parent.Split("### 6.2 Complete 36-row ownership/status/shape ledger", StringSplitOptions.None);
            Assert.Equal(2, ledger.Length);
            var section = ledger[1].Split("### 6.3 Coordinated implementation obligations", StringSplitOptions.None);
            Assert.Equal(2, section.Length);
            var rows = section[0].Split('\n')
                .Where(line => line.StartsWith("| O", StringComparison.Ordinal))
                .Select(line => line.Split('|').Select(cell => cell.Trim()).ToArray())
                .Where(row => row[4] == "NO EGRESS")
                .ToArray();
            Assert.Equal(Enumerable.Range(23, 14).Select(index => $"O{index:00}"), rows.Select(row => row[1]));
            var results = new TheoryData<string, string>();
            foreach (var row in rows) results.Add(row[1], row[2]);
            return results;
        }
    }

    [Fact]
    public void All21ServerHttpCatalogueExactlyCoversOutcomeEnum() =>
        Assert.Equal(Enum.GetValues<CaptureRuntimeRawIngressOutcome>().Order(),
            BusinessResults.ToArray().Select(row => (CaptureRuntimeRawIngressOutcome)row[0]).Order());

    [Theory]
    [MemberData(nameof(BusinessResults))]
    public async Task All21ServerHttpResultsTraverseProductionAgentClient(
        CaptureRuntimeRawIngressOutcome outcome, int expectedStatus, string expectedCode)
    {
        using var fixture = new AgentFixture(RawExportRawClass.ChipDg2Portrait);
        var authentication = new ExactAuthenticator(fixture.Keys);
        var sourceId = Guid.Parse("44444444-4444-4444-8444-444444444444");
        var retryAt = fixture.Clock.UtcNow.AddSeconds(5).ToUniversalTime();
        var admission = new OutcomeAdmission(outcome, sourceId, retryAt);
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddCurrentSiteQualificationForRawIngressTests();
        builder.Services.AddSingleton<ICaptureRuntimeRequestAuthenticator>(authentication);
        builder.Services.AddSingleton<ICaptureRuntimeRawIngressAdmission>(admission);
        await using var app = builder.Build();
        app.MapCaptureRuntimeRawIngressEndpoints();
        await app.StartAsync();
        using var rawTransport = new CapturingHandler(app.GetTestServer().CreateHandler());
        using var control = new RejectingHandler();
        using var client = new CaptureRuntimeHttpClient(new("https://localhost"), fixture.Journal,
            fixture.Keys, fixture.Clock, control, rawTransport: rawTransport);

        var result = await client.SubmitRawExportSourceAsync(fixture.Metadata, fixture.Lease);

        Assert.Equal(1, authentication.Calls);
        Assert.Equal(1, admission.Calls);
        Assert.Equal(expectedCode, result.OutcomeCode);
        var successful = outcome is CaptureRuntimeRawIngressOutcome.Available or
            CaptureRuntimeRawIngressOutcome.AlreadyAvailable;
        Assert.Equal(successful ? sourceId.ToString("D") : null, result.SourceArtifactId);
        Assert.Equal(successful ? "Available" : null, result.CurrentSourceState);
        Assert.Equal(successful ? "Available" : null, result.CurrentDisposition);
        Assert.Equal(outcome == CaptureRuntimeRawIngressOutcome.EvaluationInProgress ? retryAt : null,
            result.RetryNotBeforeUtc);
        var captured = Assert.IsType<string>(rawTransport.RawResponse);
        var separator = captured.IndexOf(':');
        Assert.Equal(expectedStatus, int.Parse(captured[..separator]));
        using var response = JsonDocument.Parse(captured[(separator + 1)..]);
        var expectedFields = successful
            ? new[] { "outcomeCode", "sourceArtifactId", "currentSourceState", "currentDisposition" }
            : outcome == CaptureRuntimeRawIngressOutcome.EvaluationInProgress
                ? new[] { "outcomeCode", "retryNotBeforeUtc" }
                : new[] { "outcomeCode" };
        Assert.Equal(expectedFields.Order(StringComparer.Ordinal),
            response.RootElement.EnumerateObject().Select(x => x.Name).Order(StringComparer.Ordinal));
        Assert.Equal(expectedCode, response.RootElement.GetProperty("outcomeCode").GetString());
    }

    [Theory]
    [MemberData(nameof(BusinessResults))]
    public async Task All21ServerHttpWrongStatusAndExtraFieldFailClosedAtAgent(
        CaptureRuntimeRawIngressOutcome outcome, int expectedStatus, string expectedCode)
    {
        foreach (var extraField in new[] { false, true })
        {
            using var fixture = new AgentFixture(RawExportRawClass.ChipDg2Portrait);
            var authentication = new ExactAuthenticator(fixture.Keys);
            var sourceId = Guid.Parse("44444444-4444-4444-8444-444444444444");
            var admission = new OutcomeAdmission(outcome, sourceId, fixture.Clock.UtcNow.AddSeconds(5).ToUniversalTime());
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Services.AddCurrentSiteQualificationForRawIngressTests();
            builder.Services.AddSingleton<ICaptureRuntimeRequestAuthenticator>(authentication);
            builder.Services.AddSingleton<ICaptureRuntimeRawIngressAdmission>(admission);
            await using var app = builder.Build();
            app.MapCaptureRuntimeRawIngressEndpoints();
            await app.StartAsync();
            using var rawTransport = new ResponseMutationHandler(
                app.GetTestServer().CreateHandler(), expectedStatus, extraField);
            using var control = new RejectingHandler();
            using var client = new CaptureRuntimeHttpClient(new("https://localhost"), fixture.Journal,
                fixture.Keys, fixture.Clock, control, rawTransport: rawTransport);

            var error = await Assert.ThrowsAsync<CaptureAgentFlowException>(() =>
                client.SubmitRawExportSourceAsync(fixture.Metadata, fixture.Lease));

            Assert.Equal("NOT_READY", error.ReasonCode);
            Assert.Equal(1, authentication.Calls);
            Assert.Equal(1, admission.Calls);
            Assert.Equal(expectedCode, rawTransport.OriginalOutcomeCode);
        }
    }

    [Theory]
    [MemberData(nameof(ExcludedResults))]
    public async Task A3_S02_All14ExcludedCodesCannotEgress(string rowId, string internalCode)
    {
        using var fixture = new AgentFixture(RawExportRawClass.ChipDg2Portrait);
        var authentication = new ExactAuthenticator(fixture.Keys);
        var broker = new InjectedInternalCodeBroker(internalCode);
        var bodyPipeline = new RejectBodyPipeline();
        var admission = new CaptureRuntimeRawIngressAdmissionService(
            new RawExportIngressCapacity(1, 1, 1024, 1024), broker, bodyPipeline, 1024);
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddCurrentSiteQualificationForRawIngressTests();
        builder.Services.AddSingleton<ICaptureRuntimeRequestAuthenticator>(authentication);
        builder.Services.AddSingleton<ICaptureRuntimeRawIngressAdmission>(admission);
        await using var app = builder.Build();
        app.MapCaptureRuntimeRawIngressEndpoints();
        await app.StartAsync();
        using var rawTransport = new CapturingHandler(app.GetTestServer().CreateHandler());
        using var control = new RejectingHandler();
        using var client = new CaptureRuntimeHttpClient(new("https://localhost"), fixture.Journal,
            fixture.Keys, fixture.Clock, control, rawTransport: rawTransport);

        var error = await Assert.ThrowsAsync<CaptureAgentFlowException>(() =>
            client.SubmitRawExportSourceAsync(fixture.Metadata, fixture.Lease));

        Assert.Equal("NOT_READY", error.ReasonCode);
        Assert.Equal(1, authentication.Calls);
        Assert.Equal(1, broker.Calls);
        Assert.Equal(0, bodyPipeline.Calls);
        var captured = Assert.IsType<string>(rawTransport.RawResponse);
        Assert.StartsWith("503:", captured, StringComparison.Ordinal);
        Assert.DoesNotContain(internalCode, captured, StringComparison.Ordinal);
        using var response = JsonDocument.Parse(captured[4..]);
        Assert.Equal(new[] { "code", "correlationId" },
            response.RootElement.EnumerateObject().Select(field => field.Name).Order(StringComparer.Ordinal));
        Assert.Equal("NOT_READY", response.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task CanonicalR25ResponseTraversesRealAgentOrchestratorAndRawIngress()
    {
        using var fixture = new AgentFixture(RawExportRawClass.ChipDg2Portrait);
        var authentication = new ExactAuthenticator(fixture.Keys, "CaptureObservation", "TrustedEvidence", "RawIngress");
        var execution = new IntegratedExecution(fixture.Journal.SessionId);
        var admission = new IntegratedAdmission();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddCurrentSiteQualificationForRawIngressTests();
        builder.Services.AddSingleton<ICaptureRuntimeRequestAuthenticator>(authentication);
        builder.Services.AddSingleton<ICaptureRuntimeExecutionService>(execution);
        builder.Services.AddSingleton<ICaptureRuntimeRawIngressAdmission>(admission);
        await using var app = builder.Build();
        app.MapCaptureRuntimeExecutionEndpoints();
        app.MapCaptureRuntimeRawIngressEndpoints();
        await app.StartAsync();
        using var control = new CapturingHandler(app.GetTestServer().CreateHandler());
        using var rawTransport = new CapturingHandler(app.GetTestServer().CreateHandler());
        using var client = new CaptureRuntimeHttpClient(new("https://localhost"), fixture.Journal,
            fixture.Keys, fixture.Clock, control,
            rawTransport: rawTransport);
        var agent = fixture.Journal.State.CaptureAgentId!.Value;
        var cache = new RawExportAgentConfigurationCache(agent);
        var limits = new RawExportAgentConfigurationLimits(600);
        cache.Apply(new(false, new(agent, Guid.NewGuid(), 7, fixture.Clock.UtcNow.AddMinutes(-1),
            fixture.Clock.UtcNow.AddMinutes(20), true, 300, 100, 60, 1024, 1024, 4096, 1024, 4096, 1024),
            "\"retained\""), fixture.Clock.UtcNow, limits);
        using var owner = new RawExportRetainedSubmissionOwner(client, cache, limits,
            new(new(1024, 1024, 2, 2048)), fixture.Clock);
        var capture = new SyntheticCapture(fixture.Clock.UtcNow);
        var orchestrator = new CaptureAgentOrchestrator(capture, capture, new FixtureFaceMatchScoreSource(.92m),
            new FixtureLivenessScoreSource(.91m), client, capture, retainedSubmissionOwner: owner);
        var configuration = new CaptureAgentSubmitOnlyConfiguration(new("subject", "purpose", agent.ToString("N"),
            Guid.NewGuid().ToString("N"), "request", "correlation", fixture.Clock.UtcNow.AddMinutes(10), false,
            new(fixture.Journal.SessionId.ToString("N"), "challenge", agent.ToString("N"), "device", "request", "correlation"),
            BiometricRetentionMode: CaptureAgentBiometricRetentionMode.RawVault),
            "run", CaptureAgentRuntimeMode.Integration, CaptureAgentEvidenceMode.Fixture, true, 1);

        var result = await orchestrator.RunSubmitOnlyAsync(configuration);

        Assert.True(result.Succeeded, $"{result.SanitizedStatusCode}; execution={execution.Calls}; auth={authentication.Calls}; raw={admission.Calls}; rawResponse={rawTransport.RawResponse}");
        Assert.Contains(control.EvidenceResponses, value => value.Contains("\"rawClass\":\"ChipDg2Portrait\"", StringComparison.Ordinal));
        Assert.Contains(control.EvidenceResponses, value => value.Contains("\"rawClass\":\"LiveSelfieImage\"", StringComparison.Ordinal));
        Assert.Equal(8, result.SubmissionReceipt!.AcceptedSubmissions.Count);
        Assert.Equal(2, admission.Calls);
        Assert.Equal(["ChipDg2Portrait", "LiveSelfieImage"], admission.Classes);
        Assert.Equal(6, execution.Calls);
        Assert.Equal(8, authentication.Calls);
    }

    [Theory]
    [InlineData(RawExportRawClass.ChipDg2Portrait, "ChipDg2Portrait")]
    [InlineData(RawExportRawClass.LiveSelfieImage, "LiveSelfieImage")]
    public async Task AgentRequestCrossesExactServerCrt1AndUnreadBodyBoundary(
        RawExportRawClass rawClass, string expectedRawClass)
    {
        using var fixture = new AgentFixture(rawClass);
        var authentication = new ExactAuthenticator(fixture.Keys);
        var admission = new ExactAdmission(fixture.Bytes, expectedRawClass);
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddCurrentSiteQualificationForRawIngressTests();
        builder.Services.AddSingleton<ICaptureRuntimeRequestAuthenticator>(authentication);
        builder.Services.AddSingleton<ICaptureRuntimeRawIngressAdmission>(admission);
        await using var app = builder.Build();
        app.MapCaptureRuntimeRawIngressEndpoints();
        await app.StartAsync();
        using var control = new RejectingHandler();
        using var client = new CaptureRuntimeHttpClient(new("https://localhost"), fixture.Journal,
            fixture.Keys, fixture.Clock, control, rawTransport: app.GetTestServer().CreateHandler());

        var result = await client.SubmitRawExportSourceAsync(fixture.Metadata, fixture.Lease);

        Assert.Equal("RAW_EXPORT_SOURCE_AVAILABLE", result.OutcomeCode);
        Assert.Equal(admission.SourceArtifactId.ToString("D"), result.SourceArtifactId);
        Assert.Equal(1, authentication.Calls);
        Assert.Equal(1, admission.Calls);
        Assert.Equal(fixture.Bytes.Length, admission.ObservedLength);
    }

    private sealed class ExactAuthenticator(KeyStore keys, params string[] roles) : ICaptureRuntimeRequestAuthenticator
    {
        public int Calls { get; private set; }
        public Task<SessionOperationResult<AuthenticatedCaptureRuntimeContext>> AuthenticateAsync(
            CaptureRuntimeSignedRequest request, CancellationToken cancellationToken = default)
        {
            Calls++;
            Assert.Contains(request.RequiredRole, roles.Length == 0 ? ["RawIngress"] : roles);
            Assert.True(keys.Verify(request.ExactSignedPreimage.Span, request.Signature));
            Assert.EndsWith("\n", System.Text.Encoding.UTF8.GetString(request.ExactSignedPreimage.Span), StringComparison.Ordinal);
            return Task.FromResult(SessionOperationResult<AuthenticatedCaptureRuntimeContext>.Success(new(
                Guid.Parse("11111111-1111-4111-8111-111111111111"),
                Guid.Parse("22222222-2222-4222-8222-222222222222"), request.CredentialId,
                request.CredentialGeneration, keys.Thumbprint, Guid.Parse("33333333-3333-4333-8333-333333333333"),
                1, 1, 1, 1, request.SignedAtUtc, request.Nonce, SHA256.HashData(request.ExactSignedPreimage.Span))));
        }
    }

    private sealed class OutcomeAdmission(
        CaptureRuntimeRawIngressOutcome outcome, Guid sourceId, DateTimeOffset retryAt)
        : ICaptureRuntimeRawIngressAdmission
    {
        public int Calls { get; private set; }

        public ValueTask<CaptureRuntimeRawIngressAdmissionResult> AdmitAsync(
            CaptureRuntimeRawIngressAdmissionContext context, Stream body, CancellationToken cancellationToken)
        {
            Calls++;
            Assert.Equal("ChipDg2Portrait", context.RawClass);
            return ValueTask.FromResult(outcome switch
            {
                CaptureRuntimeRawIngressOutcome.Available or CaptureRuntimeRawIngressOutcome.AlreadyAvailable =>
                    new CaptureRuntimeRawIngressAdmissionResult(outcome, sourceId, "Available", "Available", null),
                CaptureRuntimeRawIngressOutcome.EvaluationInProgress =>
                    new CaptureRuntimeRawIngressAdmissionResult(outcome, null, null, null, retryAt),
                _ => new CaptureRuntimeRawIngressAdmissionResult(outcome, null, null, null, null)
            });
        }
    }

    private sealed class ExactAdmission(byte[] expectedBody, string expectedRawClass)
        : ICaptureRuntimeRawIngressAdmission
    {
        public Guid SourceArtifactId { get; } = Guid.Parse("44444444-4444-4444-8444-444444444444");
        public int Calls { get; private set; }
        public int ObservedLength { get; private set; }
        public async ValueTask<CaptureRuntimeRawIngressAdmissionResult> AdmitAsync(
            CaptureRuntimeRawIngressAdmissionContext context, Stream body, CancellationToken cancellationToken)
        {
            Calls++;
            Assert.Equal(expectedRawClass, context.RawClass);
            Assert.Equal("image/jpeg", context.MediaType);
            Assert.Equal(expectedBody.Length, context.ClaimedPlaintextLength);
            using var bytes = new MemoryStream();
            await body.CopyToAsync(bytes, cancellationToken);
            Assert.Equal(expectedBody, bytes.ToArray());
            ObservedLength = checked((int)bytes.Length);
            return new(CaptureRuntimeRawIngressOutcome.Available, SourceArtifactId,
                "Available", "Available", null);
        }
    }

    private sealed class IntegratedExecution(Guid sessionId) : ICaptureRuntimeExecutionService
    {
        private Guid nfcArtifact;
        private Guid selfieArtifact;
        public int Calls { get; private set; }

        public Task<SessionOperationResult<CaptureArtifactSubmissionResponseDto>> AppendCaptureArtifactAsync(
            AuthenticatedCaptureRuntimeContext actor, Guid bindingId, CaptureRuntimeCaptureArtifactRequest request,
            Guid idempotencyKey, CancellationToken cancellationToken = default)
        {
            Calls++;
            var artifact = Guid.NewGuid();
            if (request.Payload.ArtifactType == CaptureArtifactTypeDto.NfcReadArtifact) nfcArtifact = artifact;
            if (request.Payload.ArtifactType == CaptureArtifactTypeDto.SelfieImage) selfieArtifact = artifact;
            return Task.FromResult(SessionOperationResult<CaptureArtifactSubmissionResponseDto>.Success(new(
                artifact.ToString("N"), sessionId.ToString("N"), request.Payload.ArtifactHash,
                true, "EvidenceCollecting", "correlation")));
        }

        public Task<SessionOperationResult<EvidenceResultSubmissionResponseDto>> AppendEvidenceResultAsync(
            AuthenticatedCaptureRuntimeContext actor, Guid verificationSessionId, CaptureRuntimeEvidenceResultRequest request,
            Guid idempotencyKey, CancellationToken cancellationToken = default)
        {
            Calls++;
            RawCaptureAcceptanceDto? raw = request.Payload.ResultType switch
            {
                EvidenceResultTypeDto.NfcValidation => new(nfcArtifact, Guid.NewGuid(), 1, "ChipDg2Portrait"),
                EvidenceResultTypeDto.FaceMatch => new(selfieArtifact, Guid.NewGuid(), 1, "LiveSelfieImage"),
                _ => null,
            };
            return Task.FromResult(SessionOperationResult<EvidenceResultSubmissionResponseDto>.Success(new(
                Guid.NewGuid().ToString("N"), true, "EvidenceCollecting", null, RawCaptureAcceptance: raw)));
        }

        public Task<SessionOperationResult<CaptureCapabilityResponse>> IssueOrReplaceCapabilityAsync(
            AuthenticatedClientContext actor, Guid verificationSessionId, CaptureCapabilityRequest request,
            Guid idempotencyKey, ReadOnlyMemory<byte> exactRequestBody, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Unexpected capability route");
        public Task<SessionOperationResult<CaptureRuntimeBindingResponse>> BindAsync(
            AuthenticatedCaptureRuntimeContext actor, CaptureRuntimeBindRequest request, Guid idempotencyKey,
            ReadOnlyMemory<byte> exactRequestBody, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Unexpected bind route");
        public Task<SessionOperationResult<CaptureRuntimeBindingResponse>> ReconcileAsync(
            AuthenticatedCaptureRuntimeContext actor, CaptureRuntimeReconcileRequest request,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Unexpected reconcile route");
        public Task<SessionOperationResult<CaptureRuntimeConfigurationResponse>> ResolveConfigurationAsync(
            AuthenticatedCaptureRuntimeContext actor, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Unexpected configuration route");
    }

    private sealed class IntegratedAdmission : ICaptureRuntimeRawIngressAdmission
    {
        public int Calls { get; private set; }
        public List<string> Classes { get; } = [];
        public async ValueTask<CaptureRuntimeRawIngressAdmissionResult> AdmitAsync(
            CaptureRuntimeRawIngressAdmissionContext context, Stream body, CancellationToken cancellationToken)
        {
            Calls++;
            Classes.Add(context.RawClass);
            using var bytes = new MemoryStream();
            await body.CopyToAsync(bytes, cancellationToken);
            Assert.Equal(3, bytes.Length);
            return new(CaptureRuntimeRawIngressOutcome.Available, Guid.NewGuid(), "Available", "Available", null);
        }
    }

    private sealed class ResponseMutationHandler(HttpMessageHandler inner, int expectedStatus, bool extraField)
        : DelegatingHandler(inner)
    {
        public string? OriginalOutcomeCode { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);
            Assert.Equal(expectedStatus, (int)response.StatusCode);
            var original = await response.Content.ReadAsStringAsync(cancellationToken);
            using (var json = JsonDocument.Parse(original))
                OriginalOutcomeCode = json.RootElement.GetProperty("outcomeCode").GetString();
            var changed = extraField ? original.Insert(1, "\"unexpected\":true,") : original;
            response.Content.Dispose();
            response.Content = new StringContent(changed, System.Text.Encoding.UTF8, "application/json");
            if (!extraField)
                response.StatusCode = (HttpStatusCode)(expectedStatus == 409 ? 422 : 409);
            return response;
        }
    }

    private sealed class CapturingHandler(HttpMessageHandler inner) : DelegatingHandler(inner)
    {
        public string? EvidenceResponse { get; private set; }
        public List<string> EvidenceResponses { get; } = [];
        public string? RawResponse { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);
            if (request.RequestUri!.AbsolutePath.EndsWith("/evidence-results", StringComparison.Ordinal))
            {
                var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                EvidenceResponse = System.Text.Encoding.UTF8.GetString(bytes);
                EvidenceResponses.Add(EvidenceResponse);
                ReplaceContent(response, bytes);
            }
            else if (request.RequestUri.AbsolutePath.EndsWith("/source-ingress", StringComparison.Ordinal))
            {
                var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                RawResponse = $"{(int)response.StatusCode}:{System.Text.Encoding.UTF8.GetString(bytes)}";
                ReplaceContent(response, bytes);
            }
            return response;
        }
        private static void ReplaceContent(HttpResponseMessage response, byte[] bytes)
        {
            var replacement = new ByteArrayContent(bytes);
            foreach (var header in response.Content.Headers)
                replacement.Headers.TryAddWithoutValidation(header.Key, header.Value);
            response.Content = replacement;
        }
    }

    private sealed class SyntheticCapture(DateTimeOffset capturedAt) : ICccdReader, IFaceCamera, ICaptureAgentUxSink
    {
        private static SensitiveByteBuffer Buffer(byte value) => new(new[] { value, value, value });
        public Task WaitForCardAsync(TimeSpan timeout, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<SensitiveByteBuffer?> TryDeriveAccessCodeAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<SensitiveByteBuffer?>(Buffer(49));
        public Task<CccdChipReadResult> ReadChipAsync(CccdReadRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new CccdChipReadResult(Buffer(80), new string('a', 64), capturedAt,
                Tip74ChipAuthFlag.ResponseValid));
        public Task<FaceCaptureResult> CaptureAsync(Tip71SessionContext session, CancellationToken cancellationToken = default) =>
            Task.FromResult(new FaceCaptureResult(Buffer(81), Buffer(82), new string('b', 64),
                new string('c', 64), capturedAt));
        public Task EmitAsync(CaptureAgentUxEvent evt, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<SensitiveByteBuffer> PromptSensitiveAsync(SensitivePrompt prompt,
            CancellationToken cancellationToken = default) => Task.FromResult(Buffer(49));
    }

    private sealed class AgentFixture : IDisposable
    {
        public Clock Clock { get; } = new();
        public KeyStore Keys { get; } = new();
        public Journal Journal { get; }
        public byte[] Bytes { get; } = Enumerable.Range(1, 64).Select(value => (byte)value).ToArray();
        public RetainedRawBufferLease Lease { get; }
        public RawExportSourceIngressMetadata Metadata { get; }

        public AgentFixture(RawExportRawClass rawClass)
        {
            Journal = new(Keys, Clock);
            var configuration = new RawExportAgentConfigurationDocument(Journal.State.CaptureAgentId!.Value,
                Guid.Parse("55555555-5555-4555-8555-555555555555"), 7, Clock.UtcNow.AddMinutes(-1),
                Clock.UtcNow.AddMinutes(20), true, 300, 100, 60, 1024, 1024, 4096, 1024, 4096, 1024);
            Lease = RetainedRawBufferLease.TakeOwnership(Bytes, rawClass,
                new(new(1024, 1024, 2, 2048)), configuration, Clock.UtcNow, Clock);
            Metadata = new(7, Journal.SessionId, Guid.NewGuid(), 1, rawClass, Guid.NewGuid(),
                "image/jpeg", Bytes.Length, CaptureRuntimeWireCodec.Digest(Bytes), Clock.UtcNow,
                Lease.PlaintextRetentionStartedAtUtc, Lease.PlaintextRetentionExpiresAtUtc,
                Lease.PlaintextRetentionBudgetSeconds);
        }
        public void Dispose() { Lease.Dispose(); Keys.Dispose(); }
    }

    private sealed class Clock : TimeProvider, IMonotonicTimestampSource
    {
        public DateTimeOffset UtcNow { get; } = DateTimeOffset.Parse("2026-09-16T01:02:03.4567890Z");
        public override DateTimeOffset GetUtcNow() => UtcNow;
        long IMonotonicTimestampSource.GetTimestamp() => 0;
        TimeSpan IMonotonicTimestampSource.GetElapsedTime(long startTimestamp, long endTimestamp) => TimeSpan.Zero;
    }

    private sealed class KeyStore : ICaptureRuntimeKeyStore, IDisposable
    {
        private readonly ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        public byte[] Thumbprint => SHA256.HashData(key.ExportSubjectPublicKeyInfo());
        public CaptureRuntimePublicKey CreateReserved(Guid candidateKeyId) => Open(candidateKeyId, []);
        public CaptureRuntimePublicKey Open(Guid candidateKeyId, ReadOnlySpan<byte> expectedThumbprint)
        { var spki = key.ExportSubjectPublicKeyInfo(); return new(candidateKeyId, spki, SHA256.HashData(spki)); }
        public byte[] Sign(Guid candidateKeyId, ReadOnlySpan<byte> expectedThumbprint, ReadOnlySpan<byte> preimage) =>
            key.SignData(preimage, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        public bool Verify(ReadOnlySpan<byte> preimage, ReadOnlySpan<byte> signature) => key.VerifyData(preimage, signature,
            HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        public void DeleteConfirmedAbandoned(Guid candidateKeyId, ReadOnlySpan<byte> expectedThumbprint) { }
        public void DeleteQueuedAbandoned(Guid candidateKeyId) { }
        public void Dispose() => key.Dispose();
    }

    private sealed class Journal : ICaptureRuntimeJournal
    {
        public Guid SessionId { get; } = Guid.Parse("66666666-6666-4666-8666-666666666666");
        public CaptureRuntimeJournalState State { get; private set; }
        public Journal(KeyStore keys, Clock clock)
        {
            var candidate = Guid.Parse("77777777-7777-4777-8777-777777777777");
            var publicKey = keys.CreateReserved(candidate);
            State = new(1, "https://localhost", "synthetic", 1, "BindPending", candidate,
                Guid.Parse("88888888-8888-4888-8888-888888888888"), Guid.Parse("99999999-9999-4999-8999-999999999999"),
                Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"), 1,
                CaptureRuntimeWireCodec.Base64Url(publicKey.Spki), CaptureRuntimeWireCodec.Digest(publicKey.Spki), null, null,
                new("synthetic", SessionId, Guid.NewGuid(), Guid.NewGuid(), clock.UtcNow.AddMinutes(5),
                    clock.UtcNow.AddMinutes(5), "Bound", true, true, Guid.NewGuid(), clock.UtcNow.AddMinutes(10),
                    1, 1, 1, 1), [], []);
        }
        public ICaptureRuntimeJournalTransaction Acquire() => new Transaction(this);
        private sealed class Transaction(Journal owner) : ICaptureRuntimeJournalTransaction
        {
            public CaptureRuntimeJournalState Read() => owner.State;
            public void Write(CaptureRuntimeJournalState state, long expectedRevision)
            { Assert.Equal(owner.State.Revision, expectedRevision); owner.State = state; }
            public void Dispose() { }
        }
    }

    private sealed class RejectingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Control transport must not be used by raw ingress.");
    }

    private sealed class InjectedInternalCodeBroker(string code) : IRawIngressMetadataBroker
    {
        public int Calls { get; private set; }
        public Task<RawIngressBrokerResult> AdmitAsync(
            CaptureRuntimeRawIngressAdmissionContext context, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult<RawIngressBrokerResult>(
                new RawIngressBrokerResult.Final(new CaptureAgentFinalResult(code)));
        }
    }

    private sealed class RejectBodyPipeline : ICaptureRuntimeRawIngressBodyPipeline
    {
        public int Calls { get; private set; }
        public Task<CaptureRuntimeRawIngressAdmissionResult> ProcessAsync(
            CaptureRuntimeRawIngressAdmissionContext context, RawIngressBrokerHandoff handoff,
            Stream body, CancellationToken cancellationToken)
        {
            Calls++;
            throw new InvalidOperationException("Excluded code reached the body pipeline.");
        }
    }
}
