using System.Reflection;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.ArchTests;

public sealed class Tip88C1B2R3VerifiedCiphertextStagingArchTests
{
    [Fact]
    public void R313_R3_transaction_allows_only_PostgreSQL_and_staged_fingerprint_SHA256_edges()
    {
        var serviceConstructor = typeof(RawExportR3StagingService)
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single();
        Assert.Equal([typeof(TagEkycDbContext)], serviceConstructor.GetParameters().Select(value => value.ParameterType));

        var repositoryConstructor = typeof(RawExportR3Repository)
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single();
        Assert.Equal([typeof(TagEkycDbContext)], repositoryConstructor.GetParameters().Select(value => value.ParameterType));

        var production = string.Join('\n', R3ProductionFiles().Select(File.ReadAllText));
        foreach (var forbidden in new[]
        {
            "AmazonS3", "Minio", "HttpClient", "IProvisionalObject", "IAttemptKey",
            "IAttemptAead", "IContentCommitment", "HMAC", "AesGcm", "MemoryStream",
            "Stream plaintext", "IRawSource",
            "Credential", "ObjectKey", "WrappedKey", "AddTagEkyc", "IServiceCollection",
        })
            Assert.DoesNotContain(forbidden, production, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("Npgsql", File.ReadAllText(ProjectPath(
            "src/TagEkyc.Infrastructure/RawExport/RawExportR3Repository.cs")), StringComparison.Ordinal);
        Assert.Contains("C1HashCanonical.Compute", File.ReadAllText(ProjectPath(
            "src/TagEkyc.Infrastructure/RawExport/RawExportR3StagedCiphertextFingerprintCodec.cs")), StringComparison.Ordinal);
        Assert.Contains("SHA256.HashData", File.ReadAllText(ProjectPath(
            "src/TagEkyc.Infrastructure/RawExport/C1HashCanonical.cs")), StringComparison.Ordinal);
    }

    [Fact]
    public void R315_Staged_has_no_locator_resolver_Available_R4_R5_or_delivery_surface()
    {
        foreach (var type in R3Types())
            Assert.False(type.IsPublic || type.IsNestedPublic, type.FullName);

        var r3 = string.Join('\n', R3ProductionFiles().Select(File.ReadAllText));
        foreach (var forbidden in new[]
        {
            "Available", "CommittedLocator", "RawExportR4", "RawExportR5",
            "Delivery", "PackageAssembly", "MapGet", "MapPost", "ControllerBase",
            "GetObject", "PutObject", "DeleteObject",
        })
            Assert.DoesNotContain(forbidden, r3, StringComparison.OrdinalIgnoreCase);

        var program = File.ReadAllText(ProjectPath("src/TagEkyc.Api/Program.cs"));
        Assert.DoesNotContain("RawExportR3", program, StringComparison.Ordinal);
    }

    private static Type[] R3Types() =>
    [
        typeof(RawExportR3StageCommand), typeof(RawExportR3StageDisposition),
        typeof(RawExportR3StageResult), typeof(RawExportR3FingerprintInput),
        typeof(RawExportR3StagedCiphertextFingerprintCodec), typeof(RawExportR3Repository),
        typeof(RawExportR3StagingService),
    ];

    private static string[] R3ProductionFiles() =>
    [
        ProjectPath("src/TagEkyc.Infrastructure/RawExport/RawExportR3Contracts.cs"),
        ProjectPath("src/TagEkyc.Infrastructure/RawExport/RawExportR3Repository.cs"),
        ProjectPath("src/TagEkyc.Infrastructure/RawExport/RawExportR3StagedCiphertextFingerprintCodec.cs"),
        ProjectPath("src/TagEkyc.Infrastructure/RawExport/RawExportR3StagingService.cs"),
    ];

    private static string ProjectPath(string relative)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relative.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(candidate))
                return candidate;
            directory = directory.Parent;
        }
        throw new FileNotFoundException(relative);
    }
}
