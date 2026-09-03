using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Infrastructure.ProtectedValues;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.ArchTests;

public sealed class Tip88C1Key1ContentCommitmentTests
{
    // NON-SECRET development fixture key.
    // UTF-8 value: 0123456789abcdef0123456789abcdef
    private const string FixtureKeyHex =
        "30313233343536373839616263646566" +
        "30313233343536373839616263646566";

    private const string PayloadHex = "0000000464656D6F";

    private const string ExpectedMacHex =
        "37A3B8A69368ECEB4943679AF63B8DC39" +
        "F14A52A894C5F2195BABEB061E99AD3";

    [Fact]
    public void KEY1_application_and_broker_surface_cannot_expose_protected_values()
    {
        var applicationAssembly =
            typeof(TagEkyc.Application.AssemblyMarker).Assembly;
        Assert.DoesNotContain(
            applicationAssembly.GetReferencedAssemblies(),
            reference => StringComparer.Ordinal.Equals(
                reference.Name,
                "TagEkyc.Infrastructure"));

        var applicationRoot = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "TagEkyc.Application");
        var forbiddenSourceTokens = new[]
        {
            "IProtectedValueResolver",
            "ProtectedValueMaterialLease",
            "TagEkyc.Infrastructure.ProtectedValues",
        };
        foreach (var path in Directory.EnumerateFiles(
                     applicationRoot,
                     "*.cs",
                     SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(path);
            foreach (var forbidden in forbiddenSourceTokens)
            {
                Assert.DoesNotContain(
                    forbidden,
                    source,
                    StringComparison.Ordinal);
            }
        }

        var protectedValueExports =
            typeof(TagEkyc.Infrastructure.AssemblyMarker)
                .Assembly
                .ExportedTypes
                .Where(type =>
                    type.Namespace?.StartsWith(
                        "TagEkyc.Infrastructure.ProtectedValues",
                        StringComparison.Ordinal) == true)
                .ToArray();
        Assert.Empty(protectedValueExports);

        var serviceType = typeof(IContentCommitmentService);
        var method = Assert.Single(serviceType.GetMethods());
        Assert.Equal("ComputeAsync", method.Name);
        Assert.Equal(
            typeof(ValueTask<ContentCommitmentResult>),
            method.ReturnType);
        Assert.Equal(
            new[]
            {
                typeof(CommitmentKeySelector),
                typeof(ReadOnlyMemory<byte>),
                typeof(CancellationToken),
            },
            method.GetParameters()
                .Select(parameter => parameter.ParameterType)
                .ToArray());

        var brokerTypes = new[]
        {
            serviceType,
            typeof(CommitmentKeySelector),
            typeof(ContentCommitmentResult),
            typeof(ContentCommitmentFailure),
        };
        var forbiddenMemberNames = new[]
        {
            "KeyBytes",
            "KeyMaterial",
            "MaterialLease",
            "Secret",
        };
        foreach (var brokerType in brokerTypes)
        {
            foreach (var member in brokerType.GetMembers(
                         BindingFlags.Public
                         | BindingFlags.Instance
                         | BindingFlags.Static
                         | BindingFlags.DeclaredOnly))
            {
                Assert.DoesNotContain(
                    forbiddenMemberNames,
                    forbidden =>
                        member.Name.Contains(
                            forbidden,
                            StringComparison.OrdinalIgnoreCase));
                Assert.DoesNotContain(
                    "ProtectedValue",
                    GetMemberType(member).FullName ?? string.Empty,
                    StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public async Task KEY1_fixture_selector_matches_independent_hmac_golden_vector()
    {
        // Independent recompute:
        // node -e "const c=require('crypto');const k=Buffer.from(
        // '3031323334353637383961626364656630313233343536373839616263646566',
        // 'hex');const p=Buffer.from('0000000464656d6f','hex');
        // console.log(c.createHmac('sha256',k).update(p).digest('hex'))"
        var configuration = new ConfigurationManager
        {
            [FixtureContentCommitmentCatalog.FixtureConfigurationPath] =
                Encoding.UTF8.GetString(
                    Convert.FromHexString(FixtureKeyHex)),
        };
        var services = new ServiceCollection();
        services.AddTagEkycContentCommitment(configuration);
        await using var provider = services.BuildServiceProvider();
        var service =
            provider.GetRequiredService<IContentCommitmentService>();

        var result = await service.ComputeAsync(
            new CommitmentKeySelector(
                FixtureContentCommitmentCatalog.FixtureKeyId,
                FixtureContentCommitmentCatalog.FixtureKeyVersion),
            Convert.FromHexString(PayloadHex),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Failure);
        Assert.Equal(
            ExpectedMacHex,
            Convert.ToHexString(result.Mac.Span));
        Assert.Equal(ContentCommitmentResult.MacLength, result.Mac.Length);
        var exposedMac = result.Mac;
        Assert.True(
            MemoryMarshal.TryGetArray(
                exposedMac,
                out ArraySegment<byte> exposedSegment));
        exposedSegment.Array![exposedSegment.Offset] ^= 1;
        Assert.Equal(
            ExpectedMacHex,
            Convert.ToHexString(result.Mac.Span));
        Assert.DoesNotContain(
            Encoding.UTF8.GetString(Convert.FromHexString(FixtureKeyHex)),
            result.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task KEY1_unknown_selector_is_typed_provider_failure_not_throw()
    {
        var configuration = new ConfigurationManager();
        var services = new ServiceCollection();
        services.AddTagEkycContentCommitment(configuration);
        await using var provider = services.BuildServiceProvider();
        var service =
            provider.GetRequiredService<IContentCommitmentService>();

        var result = await service.ComputeAsync(
            new CommitmentKeySelector("unknown-selector", 1),
            Convert.FromHexString(PayloadHex),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ContentCommitmentFailure.ProviderFailure,
            result.Failure);
        Assert.Throws<InvalidOperationException>(() => result.Mac);
    }

    [Fact]
    public async Task KEY1_compute_disposes_and_zeroes_material_lease()
    {
        var keyBuffer = Encoding.UTF8.GetBytes(
            "0123456789abcdef0123456789abcdef");
        var lease = ProtectedValueMaterialLease.CreateOwned(keyBuffer);
        var resolver = new CapturingResolver(lease);
        var service = new InProcessContentCommitmentService(resolver);

        var result = await service.ComputeAsync(
            new CommitmentKeySelector("capturing-fixture", 1),
            Convert.FromHexString(PayloadHex),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Throws<ObjectDisposedException>(() => lease.Material);
        Assert.All(keyBuffer, value => Assert.Equal((byte)0, value));
    }

    [Fact]
    public async Task KEY1_missing_fixture_configuration_is_provider_failure()
    {
        var configuration = new ConfigurationManager();
        var services = new ServiceCollection();
        services.AddTagEkycContentCommitment(configuration);
        await using var provider = services.BuildServiceProvider();
        var service =
            provider.GetRequiredService<IContentCommitmentService>();

        var result = await service.ComputeAsync(
            new CommitmentKeySelector(
                FixtureContentCommitmentCatalog.FixtureKeyId,
                FixtureContentCommitmentCatalog.FixtureKeyVersion),
            Convert.FromHexString(PayloadHex),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ContentCommitmentFailure.ProviderFailure,
            result.Failure);
    }

    [Fact]
    public async Task KEY1_resolver_rejects_material_over_the_size_cap()
    {
        var configuration = new ConfigurationManager
        {
            [FixtureContentCommitmentCatalog.FixtureConfigurationPath] =
                new string(
                    'x',
                    ProtectedValueResolverOptions
                        .DefaultMaxMaterialBytes + 1),
        };
        var services = new ServiceCollection();
        services.AddTagEkycContentCommitment(configuration);
        await using var provider = services.BuildServiceProvider();
        var service =
            provider.GetRequiredService<IContentCommitmentService>();

        var result = await service.ComputeAsync(
            new CommitmentKeySelector(
                FixtureContentCommitmentCatalog.FixtureKeyId,
                FixtureContentCommitmentCatalog.FixtureKeyVersion),
            Convert.FromHexString(PayloadHex),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ContentCommitmentFailure.ProviderFailure,
            result.Failure);
    }

    [Fact]
    public void KEY1_registry_rejects_invalid_scheme_and_dependency_cycle()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new ProtectedValueProviderRegistry(
            [
                new ProtectedValueProviderRegistration(
                    new StubProvider("INVALID!")),
            ]));

        Assert.Throws<InvalidOperationException>(() =>
            new ProtectedValueProviderRegistry(
            [
                new ProtectedValueProviderRegistration(
                    new StubProvider("alpha"),
                    ["beta"]),
                new ProtectedValueProviderRegistration(
                    new StubProvider("beta"),
                    ["alpha"]),
            ]));
    }

    private static Type GetMemberType(MemberInfo member) =>
        member switch
        {
            MethodInfo method => method.ReturnType,
            PropertyInfo property => property.PropertyType,
            FieldInfo field => field.FieldType,
            EventInfo @event => @event.EventHandlerType ?? typeof(void),
            _ => typeof(void),
        };

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null
               && !File.Exists(
                   Path.Combine(directory.FullName, "TagEkyc.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException(
                "Repository root was not found.");
    }

    private sealed class CapturingResolver : IProtectedValueResolver
    {
        private readonly ProtectedValueMaterialLease _lease;

        internal CapturingResolver(
            ProtectedValueMaterialLease lease)
        {
            _lease = lease;
        }

        public ValueTask<ProtectedValueResolution> ResolveAsync(
            ProtectedValueRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(
                ProtectedValueResolution.Found(
                    _lease,
                    new ProtectedValueTechnicalMetadata(
                        "1",
                        null,
                        null,
                        "fixture",
                        "redacted",
                        null)));
        }
    }

    private sealed class StubProvider : IProtectedValueProvider
    {
        internal StubProvider(string scheme)
        {
            Scheme = scheme;
        }

        public string Scheme { get; }

        public ValueTask<ProtectedValueProviderResolution> ResolveAsync(
            ProtectedValueDescriptor descriptor,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
