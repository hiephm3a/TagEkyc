using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Infrastructure.ProtectedValues;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.IntegrationTests;

public sealed class OpenBaoProductionClaimProviderTests
{
    [Fact]
    public void Production_graph_contains_only_OpenBao_claim_providers()
    {
        var configuration = Configuration();
        configuration[RawExportCustodyProfileState.ConfigurationPath] = "OpenBao";
        var services = new ServiceCollection();
        services.AddTagEkycRawExportSourceClaimComparison(
            configuration,
            isProduction: true);
        using var provider = services.BuildServiceProvider();

        Assert.IsType<OpenBaoContentCommitmentService>(
            provider.GetRequiredService<IContentCommitmentService>());
        Assert.IsType<OpenBaoSubjectRefTokenService>(
            provider.GetRequiredService<ISubjectRefTokenService>());
        Assert.Null(provider.GetService<FixtureContentCommitmentCatalog>());
        Assert.Null(provider.GetService<FixtureSubjectTokenCatalog>());
        Assert.Null(provider.GetService<InProcessContentCommitmentService>());
        Assert.Null(provider.GetService<InProcessSubjectRefTokenService>());
    }

    [Fact]
    public async Task Missing_catalog_is_readiness_RED()
    {
        var services = new ServiceCollection();
        services.AddTagEkycProductionRawExportClaimProviders(
            new ConfigurationManager());
        using var provider = services.BuildServiceProvider();

        var failure = await Assert.ThrowsAsync<RawExportClaimProviderReadinessException>(() =>
            provider.GetRequiredService<RawExportClaimProviderReadinessValidator>()
                .ValidateAsync(CancellationToken.None));

        Assert.Equal(RawExportClaimProviderReadinessValidator.ConfigurationInvalid,
            failure.Code);
    }

    [Theory]
    [InlineData("role")]
    [InlineData("key")]
    [InlineData("kek-role")]
    public async Task Shared_AppRole_key_or_KEK_credential_is_readiness_RED(string arm)
    {
        var configuration = Configuration();
        if (arm == "role")
            configuration[$"{Root}:SubjectRefToken:RoleIdSecretRef"] =
                configuration[$"{Root}:ContentCommitment:RoleIdSecretRef"];
        else if (arm == "key")
            configuration[$"{Root}:SubjectRefToken:Keys:0:TransitKeyName"] =
                configuration[$"{Root}:ContentCommitment:Keys:0:TransitKeyName"];
        else
            configuration[$"{OpenBaoKekOptions.SectionName}:RoleIdSecretRef"] =
                configuration[$"{Root}:ContentCommitment:RoleIdSecretRef"];
        var services = new ServiceCollection();
        services.AddTagEkycProductionRawExportClaimProviders(configuration);
        using var provider = services.BuildServiceProvider();

        var failure = await Assert.ThrowsAsync<RawExportClaimProviderReadinessException>(() =>
            provider.GetRequiredService<RawExportClaimProviderReadinessValidator>()
                .ValidateAsync(CancellationToken.None));

        Assert.Equal(RawExportClaimProviderReadinessValidator.SeparationInvalid,
            failure.Code);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, true, true)]
    public void Cross_policy_observation_is_readiness_RED(
        bool contentCredentialCanUseSubjectKey,
        bool subjectCredentialCanUseContentKey,
        bool shouldFail)
    {
        if (!shouldFail)
        {
            RawExportClaimProviderReadinessValidator.RequirePolicyIsolation(
                contentCredentialCanUseSubjectKey,
                subjectCredentialCanUseContentKey);
            return;
        }

        var failure = Assert.Throws<RawExportClaimProviderReadinessException>(() =>
            RawExportClaimProviderReadinessValidator.RequirePolicyIsolation(
                contentCredentialCanUseSubjectKey,
                subjectCredentialCanUseContentKey));
        Assert.Equal(RawExportClaimProviderReadinessValidator.SeparationInvalid,
            failure.Code);
    }

    [Theory]
    [InlineData("missing-id")]
    [InlineData("wrong-version")]
    [InlineData("expired")]
    public async Task Active_selector_must_resolve_to_an_active_catalog_entry(string arm)
    {
        var configuration = Configuration();
        if (arm == "missing-id")
            configuration[$"{RawIngressBrokerOptions.SectionName}:CommitmentSelectorId"] =
                "not-configured";
        else if (arm == "wrong-version")
            configuration[$"{RawIngressBrokerOptions.SectionName}:SubjectTokenSelectorVersion"] =
                "2";
        else
        {
            configuration[$"{Root}:ContentCommitment:Keys:0:NotBeforeUtc"] =
                "2000-01-01T00:00:00Z";
            configuration[$"{Root}:ContentCommitment:Keys:0:NotAfterUtc"] =
                "2001-01-01T00:00:00Z";
        }
        var services = new ServiceCollection();
        services.AddTagEkycProductionRawExportClaimProviders(configuration);
        using var provider = services.BuildServiceProvider();

        var failure = await Assert.ThrowsAsync<RawExportClaimProviderReadinessException>(() =>
            provider.GetRequiredService<RawExportClaimProviderReadinessValidator>()
                .ValidateAsync(CancellationToken.None));

        Assert.Equal(RawExportClaimProviderReadinessValidator.ConfigurationInvalid,
            failure.Code);
    }

    [Theory]
    [InlineData("unknown", 1)]
    [InlineData("content-v1", 2)]
    public async Task Wrong_content_selector_is_ProviderFailure(
        string keyId,
        int keyVersion)
    {
        var services = new ServiceCollection();
        services.AddTagEkycProductionRawExportClaimProviders(Configuration());
        using var provider = services.BuildServiceProvider();

        var result = await provider.GetRequiredService<IContentCommitmentService>()
            .ComputeAsync(new(keyId, keyVersion), new byte[] { 1, 2, 3 }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ContentCommitmentFailure.ProviderFailure, result.Failure);
    }

    [Fact]
    public async Task Inactive_subject_selector_is_ProviderFailure()
    {
        var configuration = Configuration();
        configuration[$"{Root}:SubjectRefToken:Keys:0:NotAfterUtc"] =
            "2001-01-01T00:00:00Z";
        configuration[$"{Root}:SubjectRefToken:Keys:0:NotBeforeUtc"] =
            "2000-01-01T00:00:00Z";
        var services = new ServiceCollection();
        services.AddTagEkycProductionRawExportClaimProviders(configuration);
        using var provider = services.BuildServiceProvider();

        var result = await provider.GetRequiredService<ISubjectRefTokenService>()
            .ComputeAsync(new("subject-v1", 1), new byte[] { 4, 5, 6 }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SubjectRefTokenFailure.ProviderFailure, result.Failure);
    }

    [Fact]
    public async Task Caller_cancellation_is_propagated_before_provider_access()
    {
        var services = new ServiceCollection();
        services.AddTagEkycProductionRawExportClaimProviders(Configuration());
        using var provider = services.BuildServiceProvider();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await provider.GetRequiredService<IContentCommitmentService>()
                .ComputeAsync(new("content-v1", 1), new byte[] { 1 }, cancellation.Token));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("vault:v2:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
    [InlineData("vault:v1:not-base64")]
    [InlineData("vault:v1:AQID")]
    public void Malformed_or_wrong_version_OpenBao_HMAC_is_rejected(string? value)
    {
        Assert.Throws<JsonException>(() =>
            OpenBaoTransitHmacClient.ParseHmac(value, 1));
    }

    [Fact]
    public void Exact_OpenBao_HMAC_shape_yields_the_existing_32_byte_contract()
    {
        var expected = Enumerable.Range(0, 32).Select(value => (byte)value).ToArray();

        var actual = OpenBaoTransitHmacClient.ParseHmac(
            $"vault:v1:{Convert.ToBase64String(expected)}", 1);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("aes256-gcm96", false, false, true)]
    [InlineData("hmac", true, false, true)]
    [InlineData("hmac", false, true, true)]
    [InlineData("hmac", false, false, false)]
    public void Wrong_key_metadata_is_readiness_RED(
        string type,
        bool exportable,
        bool plaintextBackup,
        bool hasVersion)
    {
        var minimumAvailableVersion = hasVersion ? 0 : 2;
        using var document = JsonDocument.Parse(
            $"{{\"name\":\"content-hmac\",\"type\":\"{type}\",\"derived\":false," +
            $"\"exportable\":{exportable.ToString().ToLowerInvariant()}," +
            $"\"allow_plaintext_backup\":{plaintextBackup.ToString().ToLowerInvariant()}," +
            $"\"latest_version\":1,\"min_available_version\":{minimumAvailableVersion}," +
            "\"min_encryption_version\":1}");
        var binding = new OpenBaoHmacKeyBinding(
            "content-v1", 1, "content-hmac",
            DateTimeOffset.UnixEpoch, DateTimeOffset.MaxValue);

        Assert.Throws<InvalidOperationException>(() =>
            OpenBaoTransitHmacClient.ValidateMetadata(document.RootElement, binding));
    }

    [Theory]
    [InlineData("derived")]
    [InlineData("exportable")]
    [InlineData("allow_plaintext_backup")]
    public void Missing_security_metadata_is_readiness_RED(string omittedProperty)
    {
        var properties = new Dictionary<string, object>
        {
            ["name"] = "content-hmac",
            ["type"] = "hmac",
            ["derived"] = false,
            ["exportable"] = false,
            ["allow_plaintext_backup"] = false,
            ["latest_version"] = 1,
            ["min_available_version"] = 0,
            ["min_encryption_version"] = 1,
        };
        properties.Remove(omittedProperty);
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(properties));
        var binding = new OpenBaoHmacKeyBinding(
            "content-v1", 1, "content-hmac",
            DateTimeOffset.UnixEpoch, DateTimeOffset.MaxValue);

        Assert.Throws<InvalidOperationException>(() =>
            OpenBaoTransitHmacClient.ValidateMetadata(document.RootElement, binding));
    }

    internal const string Root =
        "TagEkyc:RawExport:ClaimProviders";

    internal static ConfigurationManager Configuration()
    {
        var configuration = new ConfigurationManager();
        configuration[$"{RawIngressBrokerOptions.SectionName}:CommitmentSelectorId"] =
            "content-v1";
        configuration[$"{RawIngressBrokerOptions.SectionName}:CommitmentSelectorVersion"] =
            "1";
        configuration[$"{RawIngressBrokerOptions.SectionName}:SubjectTokenSelectorId"] =
            "subject-v1";
        configuration[$"{RawIngressBrokerOptions.SectionName}:SubjectTokenSelectorVersion"] =
            "1";
        Add(configuration, "ContentCommitment", "content-v1", "content-hmac",
            "env:CONTENT_ROLE", "env:CONTENT_SECRET");
        Add(configuration, "SubjectRefToken", "subject-v1", "subject-hmac",
            "env:SUBJECT_ROLE", "env:SUBJECT_SECRET");
        return configuration;
    }

    private static void Add(
        ConfigurationManager configuration,
        string section,
        string keyId,
        string transitKey,
        string role,
        string secret)
    {
        var prefix = $"{Root}:{section}";
        configuration[$"{prefix}:Address"] = "https://127.0.0.1:8200";
        configuration[$"{prefix}:RoleIdSecretRef"] = role;
        configuration[$"{prefix}:SecretIdSecretRef"] = secret;
        configuration[$"{prefix}:TransitMount"] = "transit";
        configuration[$"{prefix}:RequestTimeoutSeconds"] = "2";
        configuration[$"{prefix}:Keys:0:KeyId"] = keyId;
        configuration[$"{prefix}:Keys:0:KeyVersion"] = "1";
        configuration[$"{prefix}:Keys:0:TransitKeyName"] = transitKey;
        configuration[$"{prefix}:Keys:0:NotBeforeUtc"] =
            "2020-01-01T00:00:00Z";
        configuration[$"{prefix}:Keys:0:NotAfterUtc"] =
            "2100-01-01T00:00:00Z";
    }
}
