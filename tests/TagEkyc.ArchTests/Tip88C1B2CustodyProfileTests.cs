using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.ArchTests;

public sealed class Tip88C1B2CustodyProfileTests
{
    private const string ExpectedKekFingerprint =
        "f6e431575f3c2ef0a84f919017505f7ef55a417b7f4c280e3a38c809905186a2";

    [Fact]
    public void C1B2_profiles_provider_returns_exact_nonproduction_fixture_bundles()
    {
        using var provider = BuildServices(ValidSettings());
        var custody =
            provider.GetRequiredService<ICustodyProfileProvider>();

        Assert.Equal(
            new SourceEncryptionProfileBundle(
                "fixture-storage-local-v1",
                "fixture-source-encryption-v1",
                1,
                "fixture-aead-aes256gcm-v1",
                1,
                "fixture-nonce-random96-v1",
                1_048_576),
            custody.ActiveSourceEncryptionProfile);
        Assert.Equal(
            new KekReferenceBundle(
                "fixture-kek-provider-v1",
                "fixture-kek-v1",
                1,
                ExpectedKekFingerprint),
            custody.ActiveKekReference);
        Assert.Equal(
            32,
            Convert.FromHexString(
                custody.ActiveKekReference.KekFingerprint).Length);
        Assert.Equal(
            new CustodyTimeBounds(
                TimeSpan.FromMilliseconds(5_000),
                TimeSpan.FromSeconds(3_600),
                TimeSpan.FromSeconds(900),
                TimeSpan.FromSeconds(300)),
            custody.TimeBounds);
    }

    [Fact]
    public async Task C1B2_profiles_each_time_bound_accepts_exact_minimum_and_maximum()
    {
        var bounds = new[]
        {
            (
                CustodyTimeBoundsState.SafetyMarginKey,
                Minimum: "1",
                Maximum: "30000"),
            (
                CustodyTimeBoundsState.MaxRemainingContinuationWindowKey,
                Minimum: "1",
                Maximum: "3600"),
            (
                CustodyTimeBoundsState.EncryptionAttemptDeadlineKey,
                Minimum: "1",
                Maximum: "3600"),
            (
                CustodyTimeBoundsState.OwnershipLeaseDurationKey,
                Minimum: "1",
                Maximum: "3600"),
        };

        foreach (var (key, minimum, maximum) in bounds)
        {
            foreach (var accepted in new[] { minimum, maximum })
            {
                var settings = ValidSettings();
                settings[key] = accepted;
                await ValidateAsync(settings, isProduction: false);
            }
        }
    }

    [Fact]
    public async Task C1B2_profiles_each_invalid_time_bound_fails_closed_without_default()
    {
        var cases = new[]
        {
            (
                CustodyTimeBoundsState.SafetyMarginKey,
                Below: "0",
                Above: "30001"),
            (
                CustodyTimeBoundsState.MaxRemainingContinuationWindowKey,
                Below: "0",
                Above: "3601"),
            (
                CustodyTimeBoundsState.EncryptionAttemptDeadlineKey,
                Below: "0",
                Above: "3601"),
            (
                CustodyTimeBoundsState.OwnershipLeaseDurationKey,
                Below: "0",
                Above: "3601"),
        };

        foreach (var (key, below, above) in cases)
        {
            foreach (var invalid in new[] { below, above, "not-an-integer" })
            {
                var settings = ValidSettings();
                settings[key] = invalid;
                await AssertReadinessFailureAsync(
                    settings,
                    isProduction: false,
                    RawExportCustodyProfileReadinessValidator
                        .TimeBoundsInvalid);
            }

            var missing = ValidSettings();
            Assert.True(missing.Remove(key));
            await AssertReadinessFailureAsync(
                missing,
                isProduction: false,
                RawExportCustodyProfileReadinessValidator
                    .TimeBoundsInvalid);
        }

        var invalidProviderSettings = ValidSettings();
        invalidProviderSettings.Remove(
            CustodyTimeBoundsState.SafetyMarginKey);
        using var provider = BuildServices(invalidProviderSettings);
        var custody =
            provider.GetRequiredService<ICustodyProfileProvider>();
        var exception =
            Assert.Throws<RawExportCustodyProfileReadinessException>(
                () => custody.TimeBounds);
        Assert.Equal(
            RawExportCustodyProfileReadinessValidator.TimeBoundsInvalid,
            exception.Code);
    }

    [Fact]
    public async Task C1B2_profiles_readiness_precedence_and_fixture_gate_are_exact()
    {
        var missing = ValidSettings();
        missing.Remove(
            RawExportCustodyProfileState.ConfigurationPath);
        await AssertReadinessFailureAsync(
            missing,
            isProduction: false,
            RawExportCustodyProfileReadinessValidator.ProfileMissing);

        var whitespace = ValidSettings();
        whitespace[
            RawExportCustodyProfileState.ConfigurationPath] = " ";
        await AssertReadinessFailureAsync(
            whitespace,
            isProduction: false,
            RawExportCustodyProfileReadinessValidator.ProfileMissing);

        var unknown = ValidSettings();
        unknown[
            RawExportCustodyProfileState.ConfigurationPath] = "Ratified";
        await AssertReadinessFailureAsync(
            unknown,
            isProduction: false,
            RawExportCustodyProfileReadinessValidator.ProfileInvalid);

        await AssertReadinessFailureAsync(
            ValidSettings(),
            isProduction: true,
            RawExportCustodyProfileReadinessValidator.FixtureActive);
        await ValidateAsync(ValidSettings(), isProduction: false);

        var productionWithInvalidTime = ValidSettings();
        productionWithInvalidTime[
            CustodyTimeBoundsState.SafetyMarginKey] = "0";
        await AssertReadinessFailureAsync(
            productionWithInvalidTime,
            isProduction: true,
            RawExportCustodyProfileReadinessValidator.FixtureActive);
    }

    [Fact]
    public void C1B2_profiles_contract_is_opaque_and_has_no_key_operation_surface()
    {
        Assert.All(
            typeof(SourceEncryptionProfileBundle).GetProperties(),
            property => Assert.Contains(
                property.PropertyType,
                new[] { typeof(string), typeof(int) }));
        Assert.All(
            typeof(KekReferenceBundle).GetProperties(),
            property => Assert.Contains(
                property.PropertyType,
                new[] { typeof(string), typeof(int) }));
        Assert.DoesNotContain(
            typeof(ICustodyProfileProvider).GetProperties(),
            property => property.PropertyType.IsEnum);

        var declaredMethods = typeof(ICustodyProfileProvider)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public);
        Assert.All(
            declaredMethods,
            method => Assert.True(method.IsSpecialName));
        Assert.Equal(
            new[]
            {
                "get_ActiveKekReference",
                "get_ActiveSourceEncryptionProfile",
                "get_TimeBounds",
            },
            declaredMethods
                .Select(method => method.Name)
                .Order(StringComparer.Ordinal));
    }

    private static async Task ValidateAsync(
        IReadOnlyDictionary<string, string?> settings,
        bool isProduction)
    {
        var configuration = BuildConfiguration(settings);
        await new RawExportCustodyProfileReadinessValidator(
                configuration,
                isProduction)
            .ValidateAsync(CancellationToken.None);
    }

    private static async Task AssertReadinessFailureAsync(
        IReadOnlyDictionary<string, string?> settings,
        bool isProduction,
        string expectedCode)
    {
        var configuration = BuildConfiguration(settings);
        var exception =
            await Assert.ThrowsAsync<
                RawExportCustodyProfileReadinessException>(
                () => new RawExportCustodyProfileReadinessValidator(
                        configuration,
                        isProduction)
                    .ValidateAsync(CancellationToken.None));
        Assert.Equal(expectedCode, exception.Code);
        Assert.Equal(expectedCode, exception.Message);
    }

    private static ServiceProvider BuildServices(
        IReadOnlyDictionary<string, string?> settings)
    {
        var services = new ServiceCollection();
        services.AddTagEkycCustodyProfiles(
            BuildConfiguration(settings));
        return services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
    }

    private static IConfiguration BuildConfiguration(
        IReadOnlyDictionary<string, string?> settings) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

    private static Dictionary<string, string?> ValidSettings() =>
        new(StringComparer.Ordinal)
        {
            [RawExportCustodyProfileState.ConfigurationPath] = "Fixture",
            [CustodyTimeBoundsState.SafetyMarginKey] = "5000",
            [CustodyTimeBoundsState
                .MaxRemainingContinuationWindowKey] = "3600",
            [CustodyTimeBoundsState.EncryptionAttemptDeadlineKey] = "900",
            [CustodyTimeBoundsState.OwnershipLeaseDurationKey] = "300",
        };
}
