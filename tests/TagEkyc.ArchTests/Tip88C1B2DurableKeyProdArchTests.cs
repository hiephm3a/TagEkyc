using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.ArchTests;

public sealed class Tip88C1B2DurableKeyProdArchTests
{
    private static void AssertAeadCapabilitiesAreDistinctAndNonAssignable()
    {
        Assert.False(typeof(IAttemptAeadEncryptionOperation)
            .IsAssignableFrom(typeof(IAttemptAeadVerificationOperation)));
        Assert.False(typeof(IAttemptAeadVerificationOperation)
            .IsAssignableFrom(typeof(IAttemptAeadEncryptionOperation)));
        Assert.NotEqual(
            typeof(AttemptAeadEncryptionOperationService),
            typeof(AttemptAeadVerificationOperationService));
        Assert.DoesNotContain(
            typeof(AttemptAeadEncryptionOperationService).GetInterfaces(),
            type => type == typeof(IAttemptAeadVerificationOperation));
        Assert.DoesNotContain(
            typeof(AttemptAeadVerificationOperationService).GetInterfaces(),
            type => type == typeof(IAttemptAeadEncryptionOperation));
    }

    private static void AssertPublicContractsDoNotExposeDekLeaseOrProviderHandles()
    {
        var publicMembers = new[]
            {
                typeof(IAttemptKeyReservationProvisioningOperation),
                typeof(AttemptKeyProvisioningRequest),
                typeof(AttemptKeyProvisioningResult),
                typeof(IAttemptAeadEncryptionOperation),
                typeof(IAttemptAeadVerificationOperation),
                typeof(AttemptAeadChunkRequest),
                typeof(AttemptAeadChunkResult),
            }
            .SelectMany(type => type.GetMembers(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static));

        Assert.DoesNotContain(publicMembers, member => member switch
        {
            PropertyInfo property => IsForbidden(property.PropertyType),
            FieldInfo field => IsForbidden(field.FieldType),
            MethodInfo method => IsForbidden(method.ReturnType),
            _ => false,
        });
    }

    [Fact]
    public void DKPROD_47_Csprng_CallerTokenPath_Rejected()
    {
        var value = new string('A', 43);
        var token = new ProviderOperationToken(value);
        Assert.DoesNotContain(value, token.ToString(), StringComparison.Ordinal);
        Assert.Contains("REDACTED", token.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown")]
    public void DKPROD_38_Readiness_Topology_FailClosed_NoSilentDurable(string? configured)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [DurableKeyTopologyOptions.ConfigurationPath] = configured,
            })
            .Build();

        Assert.Equal(
            DurableKeyTopology.Invalid,
            DurableKeyTopologyOptions.Resolve(configuration).Topology);
    }

    [Fact]
    public void DKPROD_39_Readiness_QualifiedDurable_NotHitByFixtureCodes()
    {
        AssertAeadCapabilitiesAreDistinctAndNonAssignable();
        AssertPublicContractsDoNotExposeDekLeaseOrProviderHandles();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [DurableKeyTopologyOptions.ConfigurationPath] = "DurableKey",
                [DurableKeyCustodyOptions.CsprngExpectedOwnerPath] = "fixture-owner",
            })
            .Build();
        var services = new ServiceCollection();
        services.AddTagEkycDurableKeyCustody(configuration);

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IAttemptAeadEncryptionOperation));
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IAttemptAeadVerificationOperation));
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IAttemptKeyReservationProvisioningOperation));
        Assert.DoesNotContain(services, descriptor =>
            descriptor.ServiceType == typeof(IAttemptKeyProvider));
    }

    [Fact]
    public void DKPROD_48_RetryConfig_FixedProfileConformance()
    {
        var absent = DurableKeyCustodyOptions.Resolve(
            new ConfigurationBuilder().AddInMemoryCollection().Build());
        Assert.True(absent.IsValid);
        DurableKeyProviderReadinessValidator.ValidateRetryConfiguration(absent);
        AssertFixedOperationalProfile(absent);

        var exact = new Dictionary<string, string?>
        {
            [TimingKey("PreparationLeaseDuration")] = "00:15:00",
            [TimingKey("ResolutionInitialRetryDelay")] = "00:00:30",
            [TimingKey("ResolutionRetryMultiplier")] = "2.0",
            [TimingKey("ResolutionMaxBackoff")] = "00:30:00",
            [TimingKey("ResolutionDeadline")] = "1.00:00:00",
            [TimingKey("ResolutionMaxAttemptCount")] = "0",
            [TimingKey("CleanupInitialRetryDelay")] = "00:01:00",
            [TimingKey("CleanupRetryMultiplier")] = "2.0",
            [TimingKey("CleanupMaxBackoff")] = "01:00:00",
            [TimingKey("CleanupDeadline")] = "7.00:00:00",
            [TimingKey("BoundedAeadOperationDuration")] = "00:00:30",
        };
        var exactOptions = ResolveTiming(exact);
        Assert.True(exactOptions.IsValid);
        DurableKeyProviderReadinessValidator.ValidateRetryConfiguration(exactOptions);
        AssertFixedOperationalProfile(exactOptions);

        var malformed = ResolveTiming(new Dictionary<string, string?>
        {
            [TimingKey("PreparationLeaseDuration")] = "not-a-duration",
        });
        Assert.False(malformed.IsValid);
        AssertRetryConfigError(malformed);
        AssertFixedOperationalProfile(malformed);

        var nonFixedValues = new Dictionary<string, string?>
        {
            ["PreparationLeaseDuration"] = "00:16:00",
            ["ResolutionInitialRetryDelay"] = "00:00:31",
            ["ResolutionRetryMultiplier"] = "3.0",
            ["ResolutionMaxBackoff"] = "00:31:00",
            ["ResolutionDeadline"] = "2.00:00:00",
            ["ResolutionMaxAttemptCount"] = "1",
            ["CleanupInitialRetryDelay"] = "00:02:00",
            ["CleanupRetryMultiplier"] = "3.0",
            ["CleanupMaxBackoff"] = "02:00:00",
            ["CleanupDeadline"] = "8.00:00:00",
            ["BoundedAeadOperationDuration"] = "00:00:31",
        };
        foreach (var (name, value) in nonFixedValues)
        {
            var options = ResolveTiming(new Dictionary<string, string?>
            {
                [TimingKey(name)] = value,
            });
            Assert.False(options.IsValid);
            AssertRetryConfigError(options);
            AssertFixedOperationalProfile(options);
        }
    }

    private static void AssertRetryConfigError(DurableKeyCustodyOptions options) =>
        Assert.Equal(
            "PROD_RAW_EXPORT_KEY_RETRY_CONFIG_INVALID",
            Assert.Throws<DurableKeyReadinessException>(() =>
                    DurableKeyProviderReadinessValidator.ValidateRetryConfiguration(options))
                .Code);

    private static DurableKeyCustodyOptions ResolveTiming(
        Dictionary<string, string?> values) =>
        DurableKeyCustodyOptions.Resolve(
            new ConfigurationBuilder().AddInMemoryCollection(values).Build());

    private static string TimingKey(string name) =>
        $"{DurableKeyCustodyOptions.SectionPath}:{name}";

    private static void AssertFixedOperationalProfile(DurableKeyCustodyOptions options)
    {
        Assert.Equal(DurableKeyCustodyOptions.FixedPreparationLeaseDuration, options.PreparationLeaseDuration);
        Assert.Equal(DurableKeyCustodyOptions.FixedResolutionInitialRetryDelay, options.ResolutionInitialRetryDelay);
        Assert.Equal(DurableKeyCustodyOptions.FixedResolutionRetryMultiplier, options.ResolutionRetryMultiplier);
        Assert.Equal(DurableKeyCustodyOptions.FixedResolutionMaxBackoff, options.ResolutionMaxBackoff);
        Assert.Equal(DurableKeyCustodyOptions.FixedResolutionDeadline, options.ResolutionDeadline);
        Assert.Equal(DurableKeyCustodyOptions.FixedResolutionMaxAttemptCount, options.ResolutionMaxAttemptCount);
        Assert.Equal(DurableKeyCustodyOptions.FixedCleanupInitialRetryDelay, options.CleanupInitialRetryDelay);
        Assert.Equal(DurableKeyCustodyOptions.FixedCleanupRetryMultiplier, options.CleanupRetryMultiplier);
        Assert.Equal(DurableKeyCustodyOptions.FixedCleanupMaxBackoff, options.CleanupMaxBackoff);
        Assert.Equal(DurableKeyCustodyOptions.FixedCleanupDeadline, options.CleanupDeadline);
        Assert.Equal(DurableKeyCustodyOptions.FixedBoundedAeadOperationDuration, options.BoundedAeadOperationDuration);
    }

    private static bool IsForbidden(Type type) =>
        type.Name.Contains("DekLease", StringComparison.Ordinal)
        || type.Name.Contains("ProviderOperationToken", StringComparison.Ordinal)
        || type == typeof(Span<byte>)
        || type == typeof(ReadOnlySpan<byte>);
}
