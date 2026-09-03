using System.Reflection;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.ArchTests;

public sealed class Tip88C1C1ResolverAssemblyArchTests
{
    [Fact]
    public void C1_resolver_and_framed_verifier_are_internal_and_share_one_parser()
    {
        Assert.False(typeof(RawExportAssemblySourceResolver).IsPublic);
        Assert.False(typeof(RawExportFramedSourceVerificationService).IsPublic);
        Assert.Contains(typeof(RawExportR2CompletionVerifier).GetFields(BindingFlags.Instance | BindingFlags.NonPublic),
            field => field.FieldType == typeof(RawExportFramedSourceVerificationService));
        Assert.DoesNotContain(ConstructorParameters(typeof(RawExportR2CompletionVerifier)),
            type => type == typeof(IAttemptAeadEncryptionOperation));
    }

    [Fact]
    public void C1_public_contract_exposes_no_locator_key_plaintext_or_provider_credentials()
    {
        var contracts = new[]
        {
            typeof(RawExportAssemblyExecutionRequest), typeof(RawExportAssemblyExecutionResult),
            typeof(RawExportAssemblyHeader), typeof(RawExportAssemblyItemDescriptor),
            typeof(C2AssemblyPreparationRequest),
        };
        foreach (var property in contracts.SelectMany(type => type.GetProperties()))
        {
            foreach (var forbidden in new[] { "ObjectKey", "Locator", "Dek", "Credential", "PlaintextBytes", "PlaintextStream", "Nonce", "WrappedKey" })
                Assert.DoesNotContain(forbidden, property.Name, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void C1_orchestrator_uses_resolver_authenticator_and_c2_capability_without_writer_or_lifecycle()
    {
        var parameters = ConstructorParameters(typeof(RawExportAssemblyOrchestrator));
        Assert.Contains(typeof(RawExportAssemblySourceResolver), parameters);
        Assert.Contains(typeof(RawExportAssemblyAuthenticationService), parameters);
        Assert.Contains(typeof(IC2AssemblyPreparationProvider), parameters);
        Assert.DoesNotContain(typeof(IProvisionalObjectWriter), parameters);
        Assert.DoesNotContain(typeof(IProvisionalObjectLifecycle), parameters);
        Assert.DoesNotContain(typeof(IAttemptAeadEncryptionOperation), parameters);
    }

    [Fact]
    public void C1_source_contains_no_complete_plaintext_buffer_temp_file_or_raw_logging_edge()
    {
        var source = string.Join('\n', new[]
        {
            "RawExportAssemblySourceResolver.cs", "RawExportAssemblyOrchestrator.cs",
            "RawExportAssemblyCodec.cs", "RawExportFramedSourceVerificationService.cs",
        }.Select(name => File.ReadAllText(ProjectPath($"src/TagEkyc.Infrastructure/RawExport/{name}"))));
        foreach (var forbidden in new[]
        {
            "File.Create", "Path.GetTemp", "GetTempFileName", "ToArrayAsync",
            "ReadToEnd", "Convert.ToBase64String(plaintext", "Convert.ToHexString(plaintext", "ILogger<",
        })
            Assert.DoesNotContain(forbidden, source, StringComparison.OrdinalIgnoreCase);
    }

    private static Type[] ConstructorParameters(Type type) =>
        type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .OrderByDescending(constructor => constructor.GetParameters().Length)
            .First().GetParameters().Select(parameter => parameter.ParameterType).ToArray();

    private static string ProjectPath(string relative)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "TagEkyc.sln")))
            directory = directory.Parent;
        return Path.Combine(directory?.FullName ?? throw new InvalidOperationException("Repository root not found."), relative);
    }
}
