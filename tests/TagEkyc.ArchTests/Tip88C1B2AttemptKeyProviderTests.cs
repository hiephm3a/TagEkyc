using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.ArchTests;

public sealed class Tip88C1B2AttemptKeyProviderTests
{
    [Fact]
    public async Task DEKKEK_same_reservation_recovers_same_dek_and_wrapped_metadata()
    {
        var (provider, store) = CreateProvider();
        var reference = ValidReference();
        Assert.Equal(0, store.Count);

        var first = await provider.CreateOrGetAttemptKeyAsync(
            reference,
            CancellationToken.None);
        Assert.True(first.IsSuccess);
        Assert.Equal(1, store.Count);
        var firstBytes = first.Dek!.Material.ToArray();
        var firstMetadata = Assert.IsType<byte[]>(first.WrappedDekMetadata);
        first.Dek.Dispose();

        var restartedProvider = new FixtureAttemptKeyProvider(
            new FixtureAttemptKekCatalog(),
            store);
        var second = await restartedProvider.CreateOrGetAttemptKeyAsync(
            reference,
            CancellationToken.None);
        Assert.True(second.IsSuccess);
        Assert.Equal(1, store.Count);
        Assert.Equal(firstBytes, second.Dek!.Material.ToArray());
        Assert.Equal(firstMetadata, second.WrappedDekMetadata);
        second.Dek.Dispose();
    }

    [Fact]
    public async Task DEKKEK_same_reservation_with_different_context_is_conflict()
    {
        var (provider, _) = CreateProvider();
        var reference = ValidReference();
        var first = await provider.CreateOrGetAttemptKeyAsync(
            reference,
            CancellationToken.None);
        first.Dek!.Dispose();

        var conflict = await provider.CreateOrGetAttemptKeyAsync(
            reference with { KekFingerprint = new string('0', 64) },
            CancellationToken.None);
        Assert.False(conflict.IsSuccess);
        Assert.Equal(
            AttemptKeyFailure.ReservationConflict,
            conflict.Failure);
    }

    [Fact]
    public async Task DEKKEK_reference_binding_rejects_unknown_fixture_reference()
    {
        var (provider, store) = CreateProvider();
        var result = await provider.CreateOrGetAttemptKeyAsync(
            ValidReference() with { KekVersion = 2 },
            CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal(
            AttemptKeyFailure.ReferenceMismatch,
            result.Failure);
        Assert.Equal(0, store.Count);
    }

    [Fact]
    public async Task DEKKEK_lease_dispose_zeroizes_and_revokes_access()
    {
        var (provider, _) = CreateProvider();
        var result = await provider.CreateOrGetAttemptKeyAsync(
            ValidReference(),
            CancellationToken.None);
        var lease = Assert.IsAssignableFrom<IAttemptDekLease>(result.Dek);
        var exposedView = lease.Material;
        Assert.Contains(exposedView.Span.ToArray(), value => value != 0);

        lease.Dispose();

        Assert.All(exposedView.Span.ToArray(), value => Assert.Equal(0, value));
        Assert.Throws<ObjectDisposedException>(() => lease.Material);
    }

    [Fact]
    public void DEKKEK_kek_is_confined_and_fixture_capabilities_are_distinct()
    {
        var publicSurfaces = typeof(IAttemptKeyProvider).Assembly
            .GetExportedTypes()
            .SelectMany(type => type.GetMembers(BindingFlags.Public
                | BindingFlags.Instance
                | BindingFlags.Static))
            .Where(member => member.Name.Contains(
                "Kek",
                StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Assert.DoesNotContain(
            publicSurfaces,
            member => member switch
            {
                PropertyInfo property when IsMaterialType(property.PropertyType) => true,
                MethodInfo method when IsMaterialType(method.ReturnType) => true,
                FieldInfo field when IsMaterialType(field.FieldType) => true,
                _ => false,
            });

        var kek = SHA256.HashData(
            Encoding.UTF8.GetBytes(
                FixtureAttemptKekCatalog.MaterialPurpose));
        var commitmentFixture =
            Encoding.UTF8.GetBytes("0123456789abcdef0123456789abcdef");
        var subjectFixture =
            Encoding.UTF8.GetBytes("abcdef0123456789abcdef0123456789");
        Assert.NotEqual(commitmentFixture, kek);
        Assert.NotEqual(subjectFixture, kek);
        Assert.NotEqual(commitmentFixture, subjectFixture);
        Assert.Equal(32, kek.Length);
        CryptographicOperations.ZeroMemory(kek);

        var forbiddenKekMaterialMembers =
            typeof(FixtureAttemptKekCatalog)
                .GetMembers(BindingFlags.Public
                    | BindingFlags.NonPublic
                    | BindingFlags.Instance
                    | BindingFlags.Static)
                .Where(member => member.Name.Contains(
                    "Kek",
                    StringComparison.OrdinalIgnoreCase)
                    || member.Name.Contains(
                        "Material",
                        StringComparison.OrdinalIgnoreCase))
                .Where(member => member switch
                {
                    PropertyInfo property when IsMaterialType(property.PropertyType) => true,
                    MethodInfo method when IsMaterialType(method.ReturnType) => true,
                    FieldInfo field when IsMaterialType(field.FieldType) => true,
                    _ => false,
                });
        Assert.Empty(forbiddenKekMaterialMembers);
    }

    [Theory]
    [InlineData(null, false, RawExportAttemptKeyReadinessValidator.ProfileMissing)]
    [InlineData("Unknown", false, RawExportAttemptKeyReadinessValidator.ProfileInvalid)]
    [InlineData("Fixture", true, RawExportAttemptKeyReadinessValidator.FixtureActive)]
    public async Task DEKKEK_readiness_fails_closed_with_pinned_codes(
        string? profile,
        bool production,
        string expected)
    {
        var configuration = Configuration(profile);
        var exception =
            await Assert.ThrowsAsync<RawExportAttemptKeyReadinessException>(
                () => new RawExportAttemptKeyReadinessValidator(
                    configuration,
                    production).ValidateAsync(CancellationToken.None));
        Assert.Equal(expected, exception.Code);
    }

    [Fact]
    public async Task DEKKEK_fixture_profile_is_usable_only_outside_production()
    {
        var configuration = Configuration("Fixture");
        await new RawExportAttemptKeyReadinessValidator(
            configuration,
            isProduction: false).ValidateAsync(CancellationToken.None);
        var services = new ServiceCollection();
        services.AddTagEkycAttemptKeyProvider(configuration);
        await using var provider = services.BuildServiceProvider();
        Assert.IsType<FixtureAttemptKeyProvider>(
            provider.GetRequiredService<IAttemptKeyProvider>());
    }

    private static bool IsMaterialType(Type type) =>
        type == typeof(byte[])
        || type == typeof(Memory<byte>)
        || type == typeof(ReadOnlyMemory<byte>)
        || type == typeof(Span<byte>)
        || type == typeof(ReadOnlySpan<byte>);

    private static (
        FixtureAttemptKeyProvider Provider,
        FixtureWrappedAttemptKeyStore Store) CreateProvider()
    {
        var store = new FixtureWrappedAttemptKeyStore();
        return (
            new FixtureAttemptKeyProvider(
                new FixtureAttemptKekCatalog(),
                store),
            store);
    }

    private static AttemptKeyReference ValidReference() =>
        new(
            Guid.NewGuid(),
            FixtureAttemptKekCatalog.KeyProviderId,
            FixtureAttemptKekCatalog.KekId,
            FixtureAttemptKekCatalog.KekVersion,
            FixtureAttemptKekCatalog.KekFingerprint);

    private static IConfiguration Configuration(string? profile)
    {
        var values = new Dictionary<string, string?>();
        if (profile is not null)
        {
            values[RawExportAttemptKeyProfileState.ConfigurationPath] = profile;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
