using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using TagEkyc.Api;
using TagEkyc.Api.LocalDev;
using TagEkyc.Application.LocalDev;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.BusinessConsumer;
using TagEkyc.Contracts.CaptureAgent;
using TagEkyc.Contracts.Common;
using TagEkyc.Contracts.TrustedAdapter;

namespace TagEkyc.UnitTests;

public sealed class Tip06ApiPipelineTests
{
    private const string BusinessKey = "localdev-business-key";
    private const string BusinessReadOnlyKey = "localdev-readonly-key";
    private const string BusinessCompleteOnlyKey = "localdev-complete-only-key";
    private const string OtherBusinessKey = "localdev-other-business-key";
    private const string CaptureAgentKey = "localdev-capture-agent-key";
    private const string TrustedAdapterKey = "localdev-trusted-adapter-key";

    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    [Fact]
    public async Task Tip06_pipeline_completes_session_and_reads_sanitized_package()
    {
        await using var api = await TestApiApp.CreateAsync();
        var sessionId = await CreateSessionAsync(api.Client, RequiredCheckTypeDto.CaptureQuality);
        var artifactId = await AppendDeviceMetadataArtifactAsync(api.Client, sessionId);
        await AppendCaptureQualityEvidenceAsync(api.Client, sessionId, artifactId);

        var completeResponse = await PostJsonAsync(
            api.Client,
            $"/api/ekyc/verification-sessions/{sessionId}/complete",
            BusinessKey,
            new CompleteVerificationSessionRequestDto(
                RequestId: "req-api-complete",
                CorrelationId: "corr-api-complete"));
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
        var completeJson = await ReadJsonAsync(completeResponse);
        Assert.Equal("Completed", completeJson["state"]?.GetValue<string>());
        Assert.Equal("Passed", completeJson["result"]?.GetValue<string>());
        Assert.Equal("req-api-complete", completeJson["requestId"]?.GetValue<string>());
        Assert.Equal("corr-api-complete", completeJson["correlationId"]?.GetValue<string>());
        Assert.False(string.IsNullOrWhiteSpace(completeJson["finalDecisionId"]?.GetValue<string>()));
        Assert.StartsWith("sha256:", completeJson["evidencePackageHash"]?.GetValue<string>(), StringComparison.Ordinal);
        Assert.StartsWith("sha256:", completeJson["manifestHash"]?.GetValue<string>(), StringComparison.Ordinal);

        using var sessionRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/ekyc/verification-sessions/{sessionId}");
        sessionRequest.Headers.Add(LocalDevApiKeyAuthenticator.HeaderName, BusinessKey);
        var sessionResponse = await api.Client.SendAsync(sessionRequest);
        Assert.Equal(HttpStatusCode.OK, sessionResponse.StatusCode);
        var sessionJson = await ReadJsonAsync(sessionResponse);
        Assert.Equal("Completed", sessionJson["state"]?.GetValue<string>());
        Assert.Equal("Passed", sessionJson["result"]?.GetValue<string>());
        Assert.Equal(completeJson["evidencePackageId"]?.GetValue<string>(), sessionJson["evidencePackageId"]?.GetValue<string>());
        Assert.Equal(completeJson["evidencePackageHash"]?.GetValue<string>(), sessionJson["evidencePackageHash"]?.GetValue<string>());
        Assert.Equal(completeJson["manifestHash"]?.GetValue<string>(), sessionJson["manifestHash"]?.GetValue<string>());
        Assert.Equal("req-api-complete", sessionJson["requestId"]?.GetValue<string>());
        Assert.Equal("corr-api-complete", sessionJson["correlationId"]?.GetValue<string>());

        var packageId = completeJson["evidencePackageId"]!.GetValue<string>();
        using var packageRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/ekyc/evidence-packages/{packageId}");
        packageRequest.Headers.Add(LocalDevApiKeyAuthenticator.HeaderName, BusinessKey);
        var packageResponse = await api.Client.SendAsync(packageRequest);
        Assert.Equal(HttpStatusCode.OK, packageResponse.StatusCode);
        var packageJson = await ReadJsonAsync(packageResponse);
        var packageText = packageJson.ToJsonString();

        Assert.Equal(packageId, packageJson["evidencePackageId"]?.GetValue<string>());
        Assert.Equal("Passed", packageJson["result"]?.GetValue<string>());
        Assert.Equal("Signed", packageJson["evidencePackageSignatureStatus"]?.GetValue<string>());
        Assert.Equal("req-api-complete", packageJson["requestId"]?.GetValue<string>());
        Assert.Equal("corr-api-complete", packageJson["correlationId"]?.GetValue<string>());
        Assert.NotNull(packageJson["evidenceRefs"]?.AsArray().Single());
        Assert.DoesNotContain("payloadHash", packageText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PayloadHash", packageText, StringComparison.Ordinal);
        Assert.DoesNotContain("vaultRef", packageText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("clientApplicationId", packageText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("InternalAudit", packageText, StringComparison.Ordinal);
        Assert.DoesNotContain("engineName", packageText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("inputCaptureArtifactIds", packageText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Tip06_pipeline_enforces_complete_and_package_authorization()
    {
        await using var api = await TestApiApp.CreateAsync();
        var sessionId = await CreateSessionAsync(api.Client, RequiredCheckTypeDto.CaptureQuality);
        var artifactId = await AppendDeviceMetadataArtifactAsync(api.Client, sessionId);
        await AppendCaptureQualityEvidenceAsync(api.Client, sessionId, artifactId);

        var wrongCategory = await PostJsonAsync(
            api.Client,
            $"/api/ekyc/verification-sessions/{sessionId}/complete",
            TrustedAdapterKey,
            new CompleteVerificationSessionRequestDto());
        var missingCompleteScope = await PostJsonAsync(
            api.Client,
            $"/api/ekyc/verification-sessions/{sessionId}/complete",
            BusinessReadOnlyKey,
            new CompleteVerificationSessionRequestDto());

        await AssertErrorAsync(wrongCategory, HttpStatusCode.Forbidden, "CALLER_CATEGORY_NOT_ALLOWED");
        await AssertErrorAsync(missingCompleteScope, HttpStatusCode.Forbidden, "MISSING_SCOPE");

        var completeResponse = await PostJsonAsync(
            api.Client,
            $"/api/ekyc/verification-sessions/{sessionId}/complete",
            BusinessKey,
            new CompleteVerificationSessionRequestDto());
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
        var completeJson = await ReadJsonAsync(completeResponse);
        var packageId = completeJson["evidencePackageId"]!.GetValue<string>();

        using var missingReadRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/ekyc/evidence-packages/{packageId}");
        missingReadRequest.Headers.Add(LocalDevApiKeyAuthenticator.HeaderName, BusinessCompleteOnlyKey);
        var missingRead = await api.Client.SendAsync(missingReadRequest);

        await AssertErrorAsync(missingRead, HttpStatusCode.Forbidden, "MISSING_SCOPE");
    }

    [Fact]
    public async Task Tip06_pipeline_failed_identity_completion_returns_sanitized_final_response()
    {
        await using var api = await TestApiApp.CreateAsync();
        var sessionId = await CreateSessionAsync(api.Client, RequiredCheckTypeDto.DocumentOcr);
        var artifactId = await AppendDocumentFrontArtifactAsync(api.Client, sessionId);
        await AppendDocumentOcrEvidenceAsync(
            api.Client,
            sessionId,
            artifactId,
            VerificationResultDto.FailedIdentity);

        var completeResponse = await PostJsonAsync(
            api.Client,
            $"/api/ekyc/verification-sessions/{sessionId}/complete",
            BusinessKey,
            new CompleteVerificationSessionRequestDto());
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
        var json = await ReadJsonAsync(completeResponse);
        var responseText = json.ToJsonString();

        Assert.Equal("Completed", json["state"]?.GetValue<string>());
        Assert.Equal("FailedIdentity", json["result"]?.GetValue<string>());
        Assert.Equal("Low", json["assuranceLevel"]?.GetValue<string>());
        Assert.False(string.IsNullOrWhiteSpace(json["finalDecisionId"]?.GetValue<string>()));
        Assert.False(string.IsNullOrWhiteSpace(json["evidencePackageId"]?.GetValue<string>()));
        Assert.StartsWith("sha256:", json["evidencePackageHash"]?.GetValue<string>(), StringComparison.Ordinal);
        Assert.StartsWith("sha256:", json["manifestHash"]?.GetValue<string>(), StringComparison.Ordinal);
        Assert.DoesNotContain("payloadHash", responseText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("vaultRef", responseText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("clientApplicationId", responseText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("InternalAudit", responseText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Tip06_pipeline_technical_terminal_completion_returns_conflict_and_creates_no_finalization_records()
    {
        await using var api = await TestApiApp.CreateAsync();
        var sessionId = await CreateSessionAsync(api.Client, RequiredCheckTypeDto.CaptureQuality);
        var sessions = api.App.Services.GetRequiredService<LocalDevInMemoryVerificationSessionRepository>();
        await sessions.SetStateAsync(Guid.Parse(sessionId), TagEkyc.Domain.VerificationSessionState.TechnicalTerminal, CancellationToken.None);

        var completeResponse = await PostJsonAsync(
            api.Client,
            $"/api/ekyc/verification-sessions/{sessionId}/complete",
            BusinessKey,
            new CompleteVerificationSessionRequestDto(ForceReview: true));

        await AssertErrorAsync(completeResponse, HttpStatusCode.Conflict, "SESSION_TERMINAL");
        Assert.Empty(api.App.Services.GetRequiredService<LocalDevInMemoryCaptureArtifactRepository>().Artifacts);
        Assert.Empty(api.App.Services.GetRequiredService<LocalDevInMemoryVerificationDecisionRepository>().Decisions);
        Assert.Empty(api.App.Services.GetRequiredService<LocalDevInMemoryEvidencePackageRepository>().Packages);
        Assert.Empty(api.App.Services.GetRequiredService<LocalDevInMemoryEvidenceManifestRepository>().Manifests);
        Assert.DoesNotContain(
            api.App.Services.GetRequiredService<LocalDevInMemoryAuditEventRepository>().Events,
            audit => audit.EventType == "VERIFICATION_COMPLETED");
    }

    [Fact]
    public async Task Tip06_pipeline_cross_client_package_read_returns_forbidden_for_known_package()
    {
        await using var api = await TestApiApp.CreateAsync();
        var sessionId = await CreateSessionAsync(api.Client, RequiredCheckTypeDto.CaptureQuality);
        var artifactId = await AppendDeviceMetadataArtifactAsync(api.Client, sessionId);
        await AppendCaptureQualityEvidenceAsync(api.Client, sessionId, artifactId);
        var completeResponse = await PostJsonAsync(
            api.Client,
            $"/api/ekyc/verification-sessions/{sessionId}/complete",
            BusinessKey,
            new CompleteVerificationSessionRequestDto());
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
        var completeJson = await ReadJsonAsync(completeResponse);
        var packageId = completeJson["evidencePackageId"]!.GetValue<string>();

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/ekyc/evidence-packages/{packageId}");
        request.Headers.Add(LocalDevApiKeyAuthenticator.HeaderName, OtherBusinessKey);
        var response = await api.Client.SendAsync(request);

        await AssertErrorAsync(response, HttpStatusCode.Forbidden, "FORBIDDEN_CLIENT_APPLICATION");
    }

    private static async Task<string> CreateSessionAsync(HttpClient client, RequiredCheckTypeDto checkType)
    {
        var request = new CreateVerificationSessionRequestDto(
            $"external-{Guid.NewGuid():N}",
            "subject-ref",
            "PATIENT_REGISTRATION",
            VerificationProfileDto.StandardEkycProfile,
            [new RequiredCheckRequestDto(checkType, Required: true, MinimumConfidence: null)],
            DateTimeOffset.UtcNow.AddMinutes(30),
            RequestId: "req-api-create",
            CorrelationId: "corr-api-create");

        var response = await PostJsonAsync(client, "/api/ekyc/verification-sessions", BusinessKey, request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await ReadJsonAsync(response);
        return json["verificationSessionId"]!.GetValue<string>();
    }

    private static async Task<string> AppendDeviceMetadataArtifactAsync(HttpClient client, string sessionId)
    {
        var response = await PostJsonAsync(
            client,
            $"/api/ekyc/verification-sessions/{sessionId}/capture-artifacts",
            CaptureAgentKey,
            DeviceMetadataArtifact(),
            $"tip06-device-metadata-{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await ReadJsonAsync(response);
        return json["captureArtifactId"]!.GetValue<string>();
    }

    private static async Task<string> AppendDocumentFrontArtifactAsync(HttpClient client, string sessionId)
    {
        var response = await PostJsonAsync(
            client,
            $"/api/ekyc/verification-sessions/{sessionId}/capture-artifacts",
            CaptureAgentKey,
            DocumentFrontArtifact(),
            $"tip06-document-front-{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await ReadJsonAsync(response);
        return json["captureArtifactId"]!.GetValue<string>();
    }

    private static async Task AppendCaptureQualityEvidenceAsync(HttpClient client, string sessionId, string artifactId)
    {
        var response = await PostJsonAsync(
            client,
            $"/api/ekyc/verification-sessions/{sessionId}/evidence-results",
            TrustedAdapterKey,
            CaptureQualityEvidence([artifactId]),
            $"tip06-capture-quality-{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task AppendDocumentOcrEvidenceAsync(
        HttpClient client,
        string sessionId,
        string artifactId,
        VerificationResultDto result)
    {
        var response = await PostJsonAsync(
            client,
            $"/api/ekyc/verification-sessions/{sessionId}/evidence-results",
            TrustedAdapterKey,
            DocumentOcrEvidence([artifactId], result),
            $"tip06-document-ocr-{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static CaptureArtifactSubmissionRequestDto DeviceMetadataArtifact() =>
        new(
            CaptureArtifactTypeDto.DeviceCaptureMetadata,
            CaptureSourceDto.MobileSdk,
            "ldev_capture",
            "device-1",
            ArtifactHash: null,
            MetadataHash: "sha256:metadata",
            RequestId: "req-artifact",
            CorrelationId: "corr-api-artifact");

    private static CaptureArtifactSubmissionRequestDto DocumentFrontArtifact() =>
        new(
            CaptureArtifactTypeDto.DocumentFrontImage,
            CaptureSourceDto.MobileSdk,
            "ldev_capture",
            "device-1",
            "sha256:document-front",
            MetadataHash: null,
            RequestId: "req-artifact",
            CorrelationId: "corr-api-artifact");

    private static EvidenceResultSubmissionRequestDto CaptureQualityEvidence(IReadOnlyList<string> artifactIds) =>
        new(
            EvidenceResultTypeDto.CaptureQuality,
            artifactIds,
            VerificationResultDto.Passed,
            Confidence: 0.9m,
            ReasonCodes: [],
            RetryReasonCode: null,
            SanitizedSummaryRef: "summary:capture-quality",
            PayloadHash: "sha256:payload",
            SignaturePlaceholderStatusDto.PlaceholderUnverified,
            "localdev-quality",
            "s1",
            RequestId: "req-evidence",
            CorrelationId: "corr-api-evidence");

    private static EvidenceResultSubmissionRequestDto DocumentOcrEvidence(
        IReadOnlyList<string> artifactIds,
        VerificationResultDto result) =>
        CaptureQualityEvidence(artifactIds) with
        {
            ResultType = EvidenceResultTypeDto.DocumentOcr,
            Result = result,
            ReasonCodes = result == VerificationResultDto.FailedIdentity ? ["DOCUMENT_DATA_MISMATCH"] : [],
            SanitizedSummaryRef = "summary:document-ocr",
        };

    private static async Task<HttpResponseMessage> PostJsonAsync(
        HttpClient client,
        string requestUri,
        string? apiKey,
        object body,
        string? idempotencyKey = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(body, options: JsonOptions),
        };

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            request.Headers.Add(LocalDevApiKeyAuthenticator.HeaderName, apiKey);
        }

        if (idempotencyKey is not null)
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        return await client.SendAsync(request);
    }

    private static async Task<JsonNode> AssertErrorAsync(
        HttpResponseMessage response,
        HttpStatusCode statusCode,
        string expectedCode)
    {
        Assert.Equal(statusCode, response.StatusCode);
        var json = await ReadJsonAsync(response);
        var error = json["error"];
        Assert.NotNull(error);
        Assert.Equal(expectedCode, error!["code"]?.GetValue<string>());
        Assert.False(string.IsNullOrWhiteSpace(error["message"]?.GetValue<string>()));
        Assert.False(string.IsNullOrWhiteSpace(error["correlationId"]?.GetValue<string>()));
        return json;
    }

    private static async Task<JsonNode> ReadJsonAsync(HttpResponseMessage response)
    {
        var json = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        Assert.NotNull(json);
        return json!;
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new VerificationProfileDtoJsonConverter());
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private sealed class TestApiApp : IAsyncDisposable
    {
        private TestApiApp(WebApplication app, HttpClient client)
        {
            App = app;
            Client = client;
        }

        public WebApplication App { get; }
        public HttpClient Client { get; }

        public static async Task<TestApiApp> CreateAsync()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.Converters.Insert(0, new VerificationProfileDtoJsonConverter());
                options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
            builder.Services.AddSingleton<LocalDevRuntimePolicySource>();
            builder.Services.AddSingleton<ILocalDevClientPolicyProvider>(sp => sp.GetRequiredService<LocalDevRuntimePolicySource>());
            builder.Services.AddSingleton<LocalDevApiKeyValidator>();
            builder.Services.AddSingleton<ILocalDevApiKeyAuthenticator, LocalDevApiKeyAuthenticator>();
            builder.Services.AddSingleton<LocalDevInMemoryVerificationSessionRepository>();
            builder.Services.AddSingleton<IVerificationSessionRepository>(sp => sp.GetRequiredService<LocalDevInMemoryVerificationSessionRepository>());
            builder.Services.AddSingleton<LocalDevInMemoryAuditEventRepository>();
            builder.Services.AddSingleton<IAuditEventRepository>(sp => sp.GetRequiredService<LocalDevInMemoryAuditEventRepository>());
            builder.Services.AddSingleton<LocalDevInMemoryCaptureArtifactRepository>();
            builder.Services.AddSingleton<ICaptureArtifactRepository>(sp => sp.GetRequiredService<LocalDevInMemoryCaptureArtifactRepository>());
            builder.Services.AddSingleton<LocalDevInMemoryEvidenceResultRepository>();
            builder.Services.AddSingleton<IEvidenceResultRepository>(sp => sp.GetRequiredService<LocalDevInMemoryEvidenceResultRepository>());
            builder.Services.AddSingleton<LocalDevInMemoryVerificationDecisionRepository>();
            builder.Services.AddSingleton<IVerificationDecisionRepository>(sp => sp.GetRequiredService<LocalDevInMemoryVerificationDecisionRepository>());
            builder.Services.AddSingleton<LocalDevInMemoryEvidencePackageRepository>();
            builder.Services.AddSingleton<IEvidencePackageRepository>(sp => sp.GetRequiredService<LocalDevInMemoryEvidencePackageRepository>());
            builder.Services.AddSingleton<LocalDevInMemoryEvidenceManifestRepository>();
            builder.Services.AddSingleton<IInternalEvidenceManifestRepository>(sp => sp.GetRequiredService<LocalDevInMemoryEvidenceManifestRepository>());
            builder.Services.AddSingleton<LocalDevInMemoryAppendIdempotencyStore>();
            builder.Services.AddSingleton<IAppendIdempotencyRepository>(sp => sp.GetRequiredService<LocalDevInMemoryAppendIdempotencyStore>());
            builder.Services.AddSingleton<IAppendIdempotencyBoundary>(sp => sp.GetRequiredService<LocalDevInMemoryAppendIdempotencyStore>());
            builder.Services.AddSingleton<LocalDevInMemoryVerificationFinalizationBoundary>();
            builder.Services.AddSingleton<IVerificationFinalizationBoundary>(sp => sp.GetRequiredService<LocalDevInMemoryVerificationFinalizationBoundary>());
            builder.Services.AddSingleton<IEvidenceSigner, TestEvidenceSigner>();
            builder.Services.AddSingleton<VerificationSessionApplicationService>();
            builder.Services.AddSingleton<IVerificationSessionCommands>(sp => sp.GetRequiredService<VerificationSessionApplicationService>());
            builder.Services.AddSingleton<IVerificationSessionQueries>(sp => sp.GetRequiredService<VerificationSessionApplicationService>());
            builder.Services.AddSingleton<VerificationEvidenceApplicationService>();
            builder.Services.AddSingleton<ICaptureArtifactCommands>(sp => sp.GetRequiredService<VerificationEvidenceApplicationService>());
            builder.Services.AddSingleton<ITrustedEvidenceResultCommands>(sp => sp.GetRequiredService<VerificationEvidenceApplicationService>());
            builder.Services.AddSingleton<VerificationCompletionApplicationService>();
            builder.Services.AddSingleton<IVerificationSessionCompletionCommands>(sp => sp.GetRequiredService<VerificationCompletionApplicationService>());
            builder.Services.AddSingleton<IEvidencePackageQueries>(sp => sp.GetRequiredService<VerificationCompletionApplicationService>());

            var app = builder.Build();
            app.MapVerificationSessionEndpoints();
            await app.StartAsync();

            return new TestApiApp(app, app.GetTestClient());
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await App.DisposeAsync();
        }
    }
}
