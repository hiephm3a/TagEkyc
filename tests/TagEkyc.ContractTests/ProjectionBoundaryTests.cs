using System.Reflection;
using TagEkyc.Contracts.BusinessConsumer;
using TagEkyc.Contracts.Common;
using TagEkyc.Contracts.InternalAudit.Manifest;
using TagEkyc.Contracts.SignFlowProfile;
using TagEkyc.Contracts.TrustedAdapter;

namespace TagEkyc.ContractTests;

public sealed class ProjectionBoundaryTests
{
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
        };
        var businessTypes = typeof(CreateVerificationSessionRequestDto).Assembly
            .GetTypes()
            .Where(type => type.Namespace == "TagEkyc.Contracts.BusinessConsumer")
            .ToArray();

        Assert.NotEmpty(businessTypes);

        foreach (var property in businessTypes.SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public)))
        {
            Assert.DoesNotContain(forbiddenTerms, term => property.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
            if (property.Name.Contains("ClientApplicationId", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Equal(typeof(VerificationCompletedEventDto), property.DeclaringType);
            }

            Assert.DoesNotContain("InternalAudit", property.PropertyType.FullName ?? string.Empty, StringComparison.Ordinal);
            Assert.DoesNotContain("TrustedAdapter", property.PropertyType.FullName ?? string.Empty, StringComparison.Ordinal);
        }
    }

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
    public void Signature_status_is_placeholder_unverified()
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
    }
}
