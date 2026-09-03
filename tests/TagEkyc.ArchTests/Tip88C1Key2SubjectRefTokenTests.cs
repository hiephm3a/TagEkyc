using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Infrastructure.ProtectedValues;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.ArchTests;

public sealed class Tip88C1Key2SubjectRefTokenTests
{
    // NON-SECRET development fixture key.
    // UTF-8 value: fedcba9876543210fedcba9876543210
    private const string SubjectFixtureKeyHex =
        "66656463626139383736353433323130" +
        "66656463626139383736353433323130";

    private const string CommitmentFixtureKeyHex =
        "30313233343536373839616263646566" +
        "30313233343536373839616263646566";

    private const string PayloadHex = "0000000464656D6F";

    private const string ExpectedSubjectTokenHex =
        "B9643CE6382558106D53D5463EEB6C90" +
        "033EF90620D5C3E3AEC240198BA0D02A";

    private const string ExpectedCommitmentHex =
        "37A3B8A69368ECEB4943679AF63B8DC39" +
        "F14A52A894C5F2195BABEB061E99AD3";

    [Fact]
    public void KEY2_application_and_public_seam_cannot_expose_protected_values()
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

        Assert.Empty(
            typeof(TagEkyc.Infrastructure.AssemblyMarker)
                .Assembly
                .ExportedTypes
                .Where(type =>
                    type.Namespace?.StartsWith(
                        "TagEkyc.Infrastructure.ProtectedValues",
                        StringComparison.Ordinal) == true));

        var infrastructureRoot = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "TagEkyc.Infrastructure");
        var subjectTokenKeyBridges = Directory.EnumerateFiles(
                infrastructureRoot,
                "*.cs",
                SearchOption.AllDirectories)
            .Where(path =>
            {
                var source = File.ReadAllText(path);
                return source.Contains(
                           "ISubjectRefTokenService",
                           StringComparison.Ordinal)
                       && source.Contains(
                           "MaterialLease",
                           StringComparison.Ordinal);
            })
            .Select(Path.GetFileName)
            .ToArray();
        Assert.Equal(
            new[] { "InProcessSubjectRefTokenService.cs" },
            subjectTokenKeyBridges);

        var serviceType = typeof(ISubjectRefTokenService);
        var method = Assert.Single(serviceType.GetMethods());
        Assert.Equal("ComputeAsync", method.Name);
        Assert.Equal(
            typeof(ValueTask<SubjectRefTokenResult>),
            method.ReturnType);
        Assert.Equal(
            new[]
            {
                typeof(SubjectTokenKeySelector),
                typeof(ReadOnlyMemory<byte>),
                typeof(CancellationToken),
            },
            method.GetParameters()
                .Select(parameter => parameter.ParameterType)
                .ToArray());

        foreach (var brokerType in new[]
                 {
                     serviceType,
                     typeof(SubjectTokenKeySelector),
                     typeof(SubjectRefTokenResult),
                     typeof(SubjectRefTokenFailure),
                 })
        {
            foreach (var member in brokerType.GetMembers(
                         BindingFlags.Public
                         | BindingFlags.Instance
                         | BindingFlags.Static
                         | BindingFlags.DeclaredOnly))
            {
                Assert.DoesNotContain(
                    new[]
                    {
                        "KeyBytes",
                        "KeyMaterial",
                        "MaterialLease",
                        "Secret",
                    },
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
    public async Task KEY2_fixture_matches_independent_hmac_golden_vector()
    {
        // Independent recompute:
        // node -e "const c=require('crypto');const k=Buffer.from(
        // '6665646362613938373635343332313066656463626139383736353433323130',
        // 'hex');const p=Buffer.from('0000000464656d6f','hex');
        // console.log(c.createHmac('sha256',k).update(p).digest('hex'))"
        var configuration = CreateConfiguration();
        var services = new ServiceCollection();
        services.AddTagEkycSubjectRefToken(configuration);
        await using var provider = services.BuildServiceProvider();
        var service =
            provider.GetRequiredService<ISubjectRefTokenService>();

        var result = await service.ComputeAsync(
            new SubjectTokenKeySelector(
                FixtureSubjectTokenCatalog.FixtureKeyId,
                FixtureSubjectTokenCatalog.FixtureKeyVersion),
            Convert.FromHexString(PayloadHex),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Failure);
        Assert.Equal(
            ExpectedSubjectTokenHex,
            Convert.ToHexString(result.Token.Span));
        Assert.Equal(
            SubjectRefTokenResult.TokenLength,
            result.Token.Length);
        var exposedToken = result.Token;
        Assert.True(
            MemoryMarshal.TryGetArray(
                exposedToken,
                out ArraySegment<byte> exposedSegment));
        exposedSegment.Array![exposedSegment.Offset] ^= 1;
        Assert.Equal(
            ExpectedSubjectTokenHex,
            Convert.ToHexString(result.Token.Span));
        Assert.DoesNotContain(
            Encoding.UTF8.GetString(
                Convert.FromHexString(SubjectFixtureKeyHex)),
            result.ToString(),
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task KEY2_and_KEY1_are_capability_separated_in_both_DI_orders(
        bool commitmentFirst)
    {
        var configuration = CreateConfiguration();
        var services = new ServiceCollection();
        if (commitmentFirst)
        {
            services.AddTagEkycContentCommitment(configuration);
            services.AddTagEkycSubjectRefToken(configuration);
        }
        else
        {
            services.AddTagEkycSubjectRefToken(configuration);
            services.AddTagEkycContentCommitment(configuration);
        }

        await using var provider = services.BuildServiceProvider();
        var subjectService =
            provider.GetRequiredService<ISubjectRefTokenService>();
        var commitmentService =
            provider.GetRequiredService<IContentCommitmentService>();
        var payload = Convert.FromHexString(PayloadHex);

        var subject = await subjectService.ComputeAsync(
            new SubjectTokenKeySelector(
                FixtureSubjectTokenCatalog.FixtureKeyId,
                FixtureSubjectTokenCatalog.FixtureKeyVersion),
            payload,
            CancellationToken.None);
        var commitment = await commitmentService.ComputeAsync(
            new CommitmentKeySelector(
                FixtureContentCommitmentCatalog.FixtureKeyId,
                FixtureContentCommitmentCatalog.FixtureKeyVersion),
            payload,
            CancellationToken.None);

        Assert.Equal(
            ExpectedSubjectTokenHex,
            Convert.ToHexString(subject.Token.Span));
        Assert.Equal(
            ExpectedCommitmentHex,
            Convert.ToHexString(commitment.Mac.Span));
        Assert.NotEqual(subject.Token.ToArray(), commitment.Mac.ToArray());
        Assert.NotEqual(SubjectFixtureKeyHex, CommitmentFixtureKeyHex);
        Assert.NotEqual(
            FixtureSubjectTokenCatalog.FixtureConfigurationPath,
            FixtureContentCommitmentCatalog.FixtureConfigurationPath);
        Assert.NotEqual(
            FixtureSubjectTokenCatalog.CreateRequest(
                FixtureSubjectTokenCatalog.FixtureKeyId,
                FixtureSubjectTokenCatalog.FixtureKeyVersion).Purpose,
            FixtureContentCommitmentCatalog.CreateRequest(
                FixtureContentCommitmentCatalog.FixtureKeyId,
                FixtureContentCommitmentCatalog.FixtureKeyVersion).Purpose);
        Assert.False(
            typeof(CommitmentKeySelector).IsAssignableFrom(
                typeof(SubjectTokenKeySelector)));
        Assert.False(
            typeof(SubjectTokenKeySelector).IsAssignableFrom(
                typeof(CommitmentKeySelector)));

        var root = FindRepositoryRoot();
        var subjectSource = File.ReadAllText(Path.Combine(
            root,
            "src",
            "TagEkyc.Infrastructure",
            "RawExport",
            "InProcessSubjectRefTokenService.cs"));
        var commitmentSource = File.ReadAllText(Path.Combine(
            root,
            "src",
            "TagEkyc.Infrastructure",
            "RawExport",
            "InProcessContentCommitmentService.cs"));
        Assert.DoesNotContain(
            "FixtureContentCommitmentCatalog",
            subjectSource,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "TAG-EKYC:RAW-EXPORT:SUBJECT-TOKEN",
            subjectSource,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "StableDataScopeId",
            subjectSource,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "ControllerIdentity",
            subjectSource,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "FixtureSubjectTokenCatalog",
            commitmentSource,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task KEY2_compute_disposes_and_zeroes_material_lease()
    {
        var keyBuffer = Encoding.UTF8.GetBytes(
            "fedcba9876543210fedcba9876543210");
        var lease = ProtectedValueMaterialLease.CreateOwned(keyBuffer);
        var service = new InProcessSubjectRefTokenService(
            new CapturingResolver(lease));

        var result = await service.ComputeAsync(
            new SubjectTokenKeySelector("capturing-subject-fixture", 1),
            Convert.FromHexString(PayloadHex),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Throws<ObjectDisposedException>(() => lease.Material);
        Assert.All(keyBuffer, value => Assert.Equal((byte)0, value));
    }

    [Fact]
    public async Task KEY2_unknown_selector_is_typed_provider_failure_not_throw()
    {
        var services = new ServiceCollection();
        services.AddTagEkycSubjectRefToken(new ConfigurationManager());
        await using var provider = services.BuildServiceProvider();
        var service =
            provider.GetRequiredService<ISubjectRefTokenService>();

        var result = await service.ComputeAsync(
            new SubjectTokenKeySelector("unknown-subject-selector", 1),
            Convert.FromHexString(PayloadHex),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            SubjectRefTokenFailure.ProviderFailure,
            result.Failure);
        Assert.Throws<InvalidOperationException>(() => result.Token);
    }

    [Fact]
    public async Task KEY2_missing_fixture_configuration_is_provider_failure()
    {
        var services = new ServiceCollection();
        services.AddTagEkycSubjectRefToken(new ConfigurationManager());
        await using var provider = services.BuildServiceProvider();
        var service =
            provider.GetRequiredService<ISubjectRefTokenService>();

        var result = await service.ComputeAsync(
            new SubjectTokenKeySelector(
                FixtureSubjectTokenCatalog.FixtureKeyId,
                FixtureSubjectTokenCatalog.FixtureKeyVersion),
            Convert.FromHexString(PayloadHex),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            SubjectRefTokenFailure.ProviderFailure,
            result.Failure);
    }

    private static ConfigurationManager CreateConfiguration() =>
        new()
        {
            [FixtureSubjectTokenCatalog.FixtureConfigurationPath] =
                Encoding.UTF8.GetString(
                    Convert.FromHexString(SubjectFixtureKeyHex)),
            [FixtureContentCommitmentCatalog.FixtureConfigurationPath] =
                Encoding.UTF8.GetString(
                    Convert.FromHexString(CommitmentFixtureKeyHex)),
        };

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
}
