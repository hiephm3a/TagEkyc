using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using TagEkyc.Contracts.BusinessConsumer;
using TagEkyc.Contracts.Common;
using TagEkyc.Contracts.InternalAudit.Manifest;
using TagEkyc.Contracts.SignFlowProfile;
using TagEkyc.Contracts.TrustedAdapter;

namespace TagEkyc.ContractTests;

public sealed class ProjectionBoundaryTests
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    [Fact]
    public void Business_consumer_contracts_do_not_expose_internal_or_raw_fields()
    {
        var forbiddenTerms = new[]
        {
            "VaultRef",
            "Raw",
            "Biometric",
            "Template",
            "Plaintext",
            "ApiKey",
            "PayloadHash",
            "PolicySnapshot",
            "Retention",
            "LegalHold",
            "Purge",
            "DeletionEligibility",
            "AccessAudit",
        };
        var businessTypes = typeof(CreateVerificationSessionRequestDto).Assembly
            .GetTypes()
            .Where(type => type.Namespace == "TagEkyc.Contracts.BusinessConsumer")
            .ToArray();

        Assert.NotEmpty(businessTypes);

        foreach (var property in businessTypes.SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public)))
        {
            Assert.DoesNotContain(
                forbiddenTerms,
                term => property.Name.Contains(term, StringComparison.OrdinalIgnoreCase) &&
                    !IsAllowedLedgerPayloadHash(property, term));
            if (property.Name.Contains("ClientApplicationId", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Equal(typeof(VerificationCompletedEventDto), property.DeclaringType);
            }

            Assert.DoesNotContain("InternalAudit", property.PropertyType.FullName ?? string.Empty, StringComparison.Ordinal);
            Assert.DoesNotContain("TrustedAdapter", property.PropertyType.FullName ?? string.Empty, StringComparison.Ordinal);
        }
    }

    private static bool IsAllowedLedgerPayloadHash(PropertyInfo property, string term) =>
        string.Equals(term, "PayloadHash", StringComparison.OrdinalIgnoreCase) &&
        string.Equals(property.Name, "PayloadHash", StringComparison.Ordinal) &&
        property.DeclaringType is not null &&
        (property.DeclaringType == typeof(EvidenceLedgerRequiredCheckDto) ||
            property.DeclaringType == typeof(EvidenceLedgerEvidenceResultDto));

    [Fact]
    public void Client_application_id_is_limited_to_completion_notification_event()
    {
        Assert.Contains(
            typeof(VerificationCompletedEventDto).GetProperties(BindingFlags.Instance | BindingFlags.Public),
            property => property.Name == "ClientApplicationId");

        var defaultBusinessReadDtos = new[]
        {
            typeof(CompleteVerificationSessionResponseDto),
            typeof(VerificationSessionSummaryDto),
            typeof(EvidencePackageSummaryDto),
        };

        foreach (var dto in defaultBusinessReadDtos)
        {
            var propertyNames = dto
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Select(property => property.Name)
                .ToArray();

            Assert.DoesNotContain("ClientApplicationId", propertyNames);
        }
    }

    [Fact]
    public void Create_session_request_does_not_accept_authenticated_caller_fields()
    {
        var propertyNames = typeof(CreateVerificationSessionRequestDto)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => property.Name)
            .ToArray();

        Assert.DoesNotContain("ClientApplicationId", propertyNames);
        Assert.DoesNotContain("ApiKey", propertyNames);
        Assert.DoesNotContain("CallerCategory", propertyNames);
        Assert.DoesNotContain("Scopes", propertyNames);
    }

    [Fact]
    public void Evidence_result_submission_lives_only_in_trusted_adapter_projection()
    {
        Assert.Equal(
            "TagEkyc.Contracts.TrustedAdapter",
            typeof(EvidenceResultSubmissionRequestDto).Namespace);

        var businessTypes = typeof(CreateVerificationSessionRequestDto).Assembly
            .GetTypes()
            .Where(type => type.Namespace == "TagEkyc.Contracts.BusinessConsumer")
            .Select(type => type.Name);

        Assert.DoesNotContain(businessTypes, name => name.Contains("EvidenceResultSubmission", StringComparison.Ordinal));
    }

    [Fact]
    public void Internal_manifest_is_the_only_projection_with_vault_refs()
    {
        Assert.Equal(
            "TagEkyc.Contracts.InternalAudit.Manifest",
            typeof(ManifestVaultRefDto).Namespace);

        var nonManifestProperties = typeof(ManifestVaultRefDto).Assembly
            .GetTypes()
            .Where(type => type.Namespace is not null)
            .Where(type => type.Namespace != "TagEkyc.Contracts.InternalAudit.Manifest")
            .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public));

        Assert.DoesNotContain(nonManifestProperties, property => property.Name.Contains("VaultRef", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Signflow_profile_contract_keeps_required_checks_narrow()
    {
        Assert.Equal(
            [
                RequiredCheckTypeDto.CaptureQuality,
                RequiredCheckTypeDto.DocumentNfc,
                RequiredCheckTypeDto.FaceMatch,
                RequiredCheckTypeDto.Liveness,
            ],
            SignFlowS1RequiredChecks.Values);
    }

    [Fact]
    public void Signature_status_supports_placeholder_and_signed_without_exposing_signature_value()
    {
        var package = new EvidencePackageSummaryDto(
            "ep_01",
            "vs_01",
            "0.1",
            "sha256:package",
            "sha256:manifest",
            VerificationResultDto.Passed,
            AssuranceLevelDto.Medium,
            [],
            SignaturePlaceholderStatusDto.PlaceholderUnverified,
            "req-contract",
            "corr-contract",
            DateTimeOffset.UtcNow);

        Assert.Equal(SignaturePlaceholderStatusDto.PlaceholderUnverified, package.EvidencePackageSignatureStatus);
        Assert.True(Enum.IsDefined(SignaturePlaceholderStatusDto.Signed));
    }

    [Fact]
    public void Evidence_package_summary_does_not_expose_tip65_internal_hash_metadata()
    {
        var propertyNames = typeof(EvidencePackageSummaryDto)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => property.Name)
            .ToArray();

        Assert.Contains("PackageVersion", propertyNames);
        Assert.DoesNotContain("CanonicalizationScheme", propertyNames);
        Assert.DoesNotContain("HashAlgorithm", propertyNames);
    }

    [Fact]
    public void Verification_profile_uses_profile_only_snake_upper_wire_converter()
    {
        var profileJson = JsonSerializer.Serialize(VerificationProfileDto.ChallengeBoundEkycProfile, JsonOptions);
        var stateJson = JsonSerializer.Serialize(VerificationSessionStateDto.Created, JsonOptions);

        Assert.Equal("\"CHALLENGE_BOUND_EKYC_PROFILE\"", profileJson);
        Assert.Equal("\"Created\"", stateJson);
    }

    [Fact]
    public void Create_session_request_accepts_legacy_profile_and_field_keys()
    {
        var json = """
            {
              "externalSessionId": "legacy-session",
              "subjectRef": "subject-ref",
              "purpose": "SIGNING_AUTH",
              "profile": "TRANSACTION_BOUND_EKYC_PROFILE",
              "requiredChecks": [
                { "checkType": "CaptureQuality", "required": true, "minimumConfidence": null }
              ],
              "expiresAt": "2030-01-01T00:00:00Z",
              "externalTransactionId": "legacy-client-ref",
              "bindingNonceHash": "legacy opaque challenge"
            }
            """;

        var request = JsonSerializer.Deserialize<CreateVerificationSessionRequestDto>(json, JsonOptions);

        Assert.NotNull(request);
        Assert.Equal(VerificationProfileDto.ChallengeBoundEkycProfile, request!.Profile);
        Assert.Equal("legacy-client-ref", request.ClientReference);
        Assert.Equal("legacy opaque challenge", request.Challenge);
        Assert.False(request.HasConflictingChallengeFields);
    }

    [Fact]
    public void Create_session_request_marks_conflicting_new_and_legacy_field_keys()
    {
        var json = """
            {
              "subjectRef": "subject-ref",
              "purpose": "SIGNING_AUTH",
              "profile": "ChallengeBoundEkycProfile",
              "requiredChecks": [
                { "checkType": "CaptureQuality", "required": true, "minimumConfidence": null }
              ],
              "expiresAt": "2030-01-01T00:00:00Z",
              "clientReference": "new-client-ref",
              "externalTransactionId": "old-client-ref",
              "challenge": "new challenge",
              "bindingNonceHash": "old challenge"
            }
            """;

        var request = JsonSerializer.Deserialize<CreateVerificationSessionRequestDto>(json, JsonOptions);

        Assert.NotNull(request);
        Assert.True(request!.HasConflictingChallengeFields);
        Assert.Equal("new-client-ref", request.ClientReference);
        Assert.Equal("new challenge", request.Challenge);
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new VerificationProfileDtoJsonConverter());
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
