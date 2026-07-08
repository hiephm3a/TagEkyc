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
using TagEkyc.Domain;

namespace TagEkyc.UnitTests;

public sealed class Tip05ApiPipelineTests
{
    private const string BusinessKey = "localdev-business-key";
    private const string CaptureAgentKey = "localdev-capture-agent-key";
    private const string TrustedAdapterKey = "localdev-trusted-adapter-key";
    private const string UnknownKey = "localdev-unknown-key";

    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    [Fact]
    public async Task Tip05_pipeline_enforces_caller_categories_and_api_key_errors()
    {
        await using var api = await TestApiApp.CreateAsync();
        var sessionId = await CreateSessionAsync(api.Client, RequiredCheckTypeDto.CaptureQuality);
        var artifact = DeviceMetadataArtifact();
        var evidence = CaptureQualityEvidence(["00000000-0000-0000-0000-000000000001"]);

        var businessCapture = await PostJsonAsync(api.Client, $"/api/ekyc/verification-sessions/{sessionId}/capture-artifacts", BusinessKey, artifact, "tip05-business-capture");
        var businessEvidence = await PostJsonAsync(api.Client, $"/api/ekyc/verification-sessions/{sessionId}/evidence-results", BusinessKey, evidence, "tip05-business-evidence");
        var captureEvidence = await PostJsonAsync(api.Client, $"/api/ekyc/verification-sessions/{sessionId}/evidence-results", CaptureAgentKey, evidence, "tip05-capture-evidence");
        var trustedCapture = await PostJsonAsync(api.Client, $"/api/ekyc/verification-sessions/{sessionId}/capture-artifacts", TrustedAdapterKey, artifact, "tip05-trusted-capture");
        var missingKey = await PostJsonAsync(api.Client, $"/api/ekyc/verification-sessions/{sessionId}/capture-artifacts", apiKey: null, artifact, "tip05-missing-api-key");
        var unknownKey = await PostJsonAsync(api.Client, $"/api/ekyc/verification-sessions/{sessionId}/capture-artifacts", UnknownKey, artifact, "tip05-unknown-key");

        await AssertErrorAsync(businessCapture, HttpStatusCode.Forbidden, "CALLER_CATEGORY_NOT_ALLOWED");
        await AssertErrorAsync(businessEvidence, HttpStatusCode.Forbidden, "CALLER_CATEGORY_NOT_ALLOWED");
        await AssertErrorAsync(captureEvidence, HttpStatusCode.Forbidden, "CALLER_CATEGORY_NOT_ALLOWED");
        await AssertErrorAsync(trustedCapture, HttpStatusCode.Forbidden, "CALLER_CATEGORY_NOT_ALLOWED");
        await AssertErrorAsync(missingKey, HttpStatusCode.Unauthorized, "INVALID_API_KEY");
        await AssertErrorAsync(unknownKey, HttpStatusCode.Unauthorized, "INVALID_API_KEY");
    }

    [Fact]
    public async Task Tip05_pipeline_happy_chain_keeps_business_summary_non_final_and_sanitized()
    {
        await using var api = await TestApiApp.CreateAsync();
        var sessionId = await CreateSessionAsync(api.Client, RequiredCheckTypeDto.CaptureQuality);

        var artifactResponse = await PostJsonAsync(
            api.Client,
            $"/api/ekyc/verification-sessions/{sessionId}/capture-artifacts",
            CaptureAgentKey,
            DeviceMetadataArtifact(),
            "tip05-happy-artifact");
        Assert.Equal(HttpStatusCode.Created, artifactResponse.StatusCode);
        var artifactJson = await ReadJsonAsync(artifactResponse);
        Assert.Equal("InProgress", artifactJson["sessionState"]?.GetValue<string>());
        var artifactId = artifactJson["captureArtifactId"]!.GetValue<string>();
        var registry = api.App.Services.GetRequiredService<IMetadataReferenceRegistry>();
        var metadataReference = await registry.QueryAsync(new MetadataReferenceId($"capture-artifact-metadata:{artifactId}"));
        Assert.True(metadataReference.HasRegisteredMetadata());
        Assert.Equal("capture-artifact-metadata", metadataReference.Reference?.ReferenceKind);
        Assert.True(metadataReference.DeniesArtifactAccessProof());
        Assert.True(metadataReference.DeniesCompletePackageProof());
        Assert.True(metadataReference.DeniesProviderEvidenceAvailabilityProof());
        Assert.True(metadataReference.DeniesReadinessProof());

        var evidenceResponse = await PostJsonAsync(
            api.Client,
            $"/api/ekyc/verification-sessions/{sessionId}/evidence-results",
            TrustedAdapterKey,
            CaptureQualityEvidence([artifactId]),
            "tip05-happy-evidence");
        Assert.Equal(HttpStatusCode.Created, evidenceResponse.StatusCode);
        var evidenceJson = await ReadJsonAsync(evidenceResponse);
        Assert.Equal("ReadyToComplete", evidenceJson["sessionState"]?.GetValue<string>());

        using var getRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/ekyc/verification-sessions/{sessionId}");
        getRequest.Headers.Add(LocalDevApiKeyAuthenticator.HeaderName, BusinessKey);
        var summaryResponse = await api.Client.SendAsync(getRequest);
        Assert.Equal(HttpStatusCode.OK, summaryResponse.StatusCode);
        var summaryJson = await ReadJsonAsync(summaryResponse);
        Assert.Equal("ReadyToComplete", summaryJson["state"]?.GetValue<string>());
        Assert.Equal("NotAvailable", summaryJson["result"]?.GetValue<string>());
        Assert.Equal("None", summaryJson["assuranceLevel"]?.GetValue<string>());
        Assert.Null(summaryJson["evidencePackageId"]);
        Assert.Null(summaryJson["evidencePackageHash"]);
        Assert.Null(summaryJson["completedAt"]);

        var summaryText = summaryJson.ToJsonString();
        Assert.DoesNotContain("inputCaptureArtifactIds", summaryText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("payloadHash", summaryText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("vaultRef", summaryText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("engineName", summaryText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("captureAgentId", summaryText, StringComparison.OrdinalIgnoreCase);

        var captureAfterReady = await PostJsonAsync(
            api.Client,
            $"/api/ekyc/verification-sessions/{sessionId}/capture-artifacts",
            CaptureAgentKey,
            DeviceMetadataArtifact(),
            "tip05-after-ready-artifact");
        var evidenceAfterReady = await PostJsonAsync(
            api.Client,
            $"/api/ekyc/verification-sessions/{sessionId}/evidence-results",
            TrustedAdapterKey,
            CaptureQualityEvidence([artifactId]),
            "tip05-after-ready-evidence");

        await AssertErrorAsync(captureAfterReady, HttpStatusCode.Conflict, "SESSION_READY_TO_COMPLETE");
        await AssertErrorAsync(evidenceAfterReady, HttpStatusCode.Conflict, "SESSION_READY_TO_COMPLETE");
    }

    [Fact]
    public async Task Tip05_pipeline_rejects_missing_payload_hash_and_incompatible_input_artifact()
    {
        await using var api = await TestApiApp.CreateAsync();
        var captureQualitySessionId = await CreateSessionAsync(api.Client, RequiredCheckTypeDto.CaptureQuality);
        var captureQualityArtifactId = await AppendDeviceMetadataArtifactAsync(api.Client, captureQualitySessionId);

        var missingPayloadHash = await PostJsonAsync(
            api.Client,
            $"/api/ekyc/verification-sessions/{captureQualitySessionId}/evidence-results",
            TrustedAdapterKey,
            CaptureQualityEvidence([captureQualityArtifactId]) with { PayloadHash = null },
            "tip05-missing-payload");

        var nfcSessionId = await CreateSessionAsync(api.Client, RequiredCheckTypeDto.DocumentNfc);
        var metadataArtifactId = await AppendDeviceMetadataArtifactAsync(api.Client, nfcSessionId);
        var incompatible = await PostJsonAsync(
            api.Client,
            $"/api/ekyc/verification-sessions/{nfcSessionId}/evidence-results",
            TrustedAdapterKey,
            CaptureQualityEvidence([metadataArtifactId]) with { ResultType = EvidenceResultTypeDto.NfcValidation },
            "tip05-incompatible");

        await AssertErrorAsync(missingPayloadHash, HttpStatusCode.BadRequest, "INVALID_HASH_REF");
        await AssertErrorAsync(incompatible, HttpStatusCode.BadRequest, "INVALID_EVIDENCE_RESULT");
    }

    [Fact]
    public async Task Tip80SI_append_endpoints_require_well_formed_idempotency_key()
    {
        await using var api = await TestApiApp.CreateAsync();
        var sessionId = await CreateSessionAsync(api.Client, RequiredCheckTypeDto.CaptureQuality);

        var missing = await PostJsonAsync(
            api.Client,
            $"/api/ekyc/verification-sessions/{sessionId}/capture-artifacts",
            CaptureAgentKey,
            DeviceMetadataArtifact());
        var whitespace = await PostJsonAsync(
            api.Client,
            $"/api/ekyc/verification-sessions/{sessionId}/capture-artifacts",
            CaptureAgentKey,
            DeviceMetadataArtifact(),
            " ");
        var oversize = await PostJsonAsync(
            api.Client,
            $"/api/ekyc/verification-sessions/{sessionId}/capture-artifacts",
            CaptureAgentKey,
            DeviceMetadataArtifact(),
            new string('x', 257));

        await AssertErrorAsync(missing, HttpStatusCode.BadRequest, "IDEMPOTENCY_KEY_REQUIRED");
        await AssertErrorAsync(whitespace, HttpStatusCode.BadRequest, "IDEMPOTENCY_KEY_REQUIRED");
        await AssertErrorAsync(oversize, HttpStatusCode.BadRequest, "IDEMPOTENCY_KEY_INVALID_FORMAT");
    }

    [Fact]
    public async Task Tip05_pipeline_does_not_map_specialized_evidence_endpoints()
    {
        await using var api = await TestApiApp.CreateAsync();
        var sessionId = Guid.NewGuid().ToString("N");
        var specializedPaths = new[]
        {
            "capture-quality-result",
            "document-result",
            "nfc-result",
            "face-result",
            "liveness-result",
            "fingerprint-result",
        };

        foreach (var path in specializedPaths)
        {
            var response = await PostJsonAsync(
                api.Client,
                $"/api/ekyc/verification-sessions/{sessionId}/{path}",
                TrustedAdapterKey,
                new { correlationId = "corr-specialized" });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        var metadataReferenceRoutes = new[]
        {
            "/api/ekyc/metadata-references/capture-artifact-metadata:test",
            $"/api/ekyc/verification-sessions/{sessionId}/metadata-references",
        };

        foreach (var path in metadataReferenceRoutes)
        {
            using var getRequest = new HttpRequestMessage(HttpMethod.Get, path);
            getRequest.Headers.Add(LocalDevApiKeyAuthenticator.HeaderName, BusinessKey);
            var getResponse = await api.Client.SendAsync(getRequest);

            var postResponse = await PostJsonAsync(
                api.Client,
                path,
                BusinessKey,
                new { correlationId = "corr-metadata-route" });

            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, postResponse.StatusCode);
        }
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
            RequestId: "req-api-tip05",
            CorrelationId: "corr-api-tip05");

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
            $"tip05-device-metadata-{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await ReadJsonAsync(response);
        return json["captureArtifactId"]!.GetValue<string>();
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
            CorrelationId: "corr-api-tip05");

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
            CorrelationId: "corr-api-tip05");

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
            builder.Services.AddSingleton<LocalDevApiKeyStore>();
            builder.Services.AddSingleton<IApiKeyStore>(sp => sp.GetRequiredService<LocalDevApiKeyStore>());
            builder.Services.AddSingleton<LocalDevApiKeyValidator>();
            builder.Services.AddSingleton<IApiKeyAuthenticator, LocalDevApiKeyAuthenticator>();
            builder.Services.AddSingleton<LocalDevInMemoryMetadataReferenceRegistry>();
            builder.Services.AddSingleton<IMetadataReferenceRegistry>(sp => sp.GetRequiredService<LocalDevInMemoryMetadataReferenceRegistry>());
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
