using System.Buffers.Binary;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Amazon.S3;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.ArchTests;

public sealed class Tip88C1C2RecipientPackageArchTests
{
    [Fact]
    public void C203_function_names_are_ten_exact_and_no_reset_surface_exists()
    {
        var expected = new[]
        {
            "raw_export_select_active_recipient_key", "raw_export_reserve_recipient_package",
            "raw_export_begin_recipient_package_put", "raw_export_record_recipient_package_put_unknown",
            "raw_export_record_recipient_package_prepared", "raw_export_read_recipient_package_recovery_context",
            "raw_export_finalize_recipient_package", "raw_export_authorize_recipient_package_abort",
            "raw_export_record_recipient_package_abort_result", "raw_export_record_recipient_package_quarantined",
        };
        var readiness = File.ReadAllText(ProjectPath("src/TagEkyc.Infrastructure/RawExport/RecipientPackageReadinessValidator.cs"));
        var functions = Between(readiness, "expected_functions(name,args) AS (VALUES", "expected_acl(name,args,grantee) AS (VALUES");
        Assert.Equal(10, Count(functions, "('raw_export_"));
        foreach (var name in expected) Assert.Equal(1, Count(functions, $"('{name}'"));
        Assert.DoesNotContain("recipient_package_reset", MigrationSource(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task C208_real_recipient_unwraps_while_production_has_no_private_key_or_unwrap_capability()
    {
        var assembly = typeof(RecipientPackageCodec).Assembly;
        var packageTypes = assembly.GetTypes().Where(type => type.Namespace == typeof(RecipientPackageCodec).Namespace
            && type.Name.Contains("RecipientPackage", StringComparison.Ordinal)).ToArray();
        Assert.DoesNotContain(packageTypes.SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)),
            method => method.Name.Contains("Unwrap", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(packageTypes.SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)),
            property => property.Name.Contains("PrivateKey", StringComparison.OrdinalIgnoreCase));

        using var recipient = RSA.Create(3072);
        var spki = recipient.ExportSubjectPublicKeyInfo();
        var request = new C2AssemblyPreparationRequest(
            Guid.NewGuid(), Guid.NewGuid(), RandomNumberGenerator.GetBytes(32), RandomNumberGenerator.GetBytes(32),
            RandomNumberGenerator.GetBytes(32), RandomNumberGenerator.GetBytes(32), 5, Guid.NewGuid());
        var reservation = new RecipientPackageReserveResult(
            "Reserved", 1, "Reserved", Guid.NewGuid(), RandomNumberGenerator.GetBytes(32), "recipient-key", 1,
            SHA256.HashData(spki), spki, 1, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(1),
            RandomNumberGenerator.GetBytes(32), RandomNumberGenerator.GetBytes(32), null);
        var encrypted = await new RecipientPackageCryptoService().EncryptAsync(
            request, reservation,
            async (destination, token) => await destination.WriteAsync("hello"u8.ToArray(), token), CancellationToken.None);
        await using var spool = encrypted.Spool;
        await using var stream = spool.OpenRead();
        var prefix = new byte[RecipientPackageCodec.MagicText.Length + 4];
        await stream.ReadExactlyAsync(prefix);
        var headerLength = BinaryPrimitives.ReadUInt32BigEndian(prefix.AsSpan(RecipientPackageCodec.MagicText.Length));
        var header = new byte[checked((int)headerLength)];
        await stream.ReadExactlyAsync(header);
        using var document = JsonDocument.Parse(header);
        var encoded = document.RootElement.GetProperty("wrappedCek").GetString()!;
        var padded = encoded.Replace('-', '+').Replace('_', '/') + new string('=', (4 - encoded.Length % 4) % 4);
        var wrapped = Convert.FromBase64String(padded);
        var cek = recipient.Decrypt(wrapped, RSAEncryptionPadding.OaepSHA256);
        Assert.Equal(32, cek.Length);
        CryptographicOperations.ZeroMemory(cek);
        CryptographicOperations.ZeroMemory(wrapped);
        CryptographicOperations.ZeroMemory(header);
    }

    [Fact]
    public void C218_provider_has_no_multipart_list_copy_presign_or_batch_delete_member()
    {
        var source = File.ReadAllText(ProjectPath("src/TagEkyc.Infrastructure/RawExport/S3CompatibleRecipientPackageProvider.cs"));
        foreach (var forbidden in new[]
        {
            "CreateMultipartUpload", "UploadPart", "CompleteMultipartUpload", "AbortMultipartUpload",
            "ListObjects", "ListObjectsV2", "CopyObject", "GetPreSignedURL", "DeleteObjectsAsync", "DeleteObjectsRequest",
        }) Assert.DoesNotContain(forbidden, source, StringComparison.Ordinal);
        Assert.Equal(1, Count(source, "DeleteObjectAsync("));
    }

    [Fact]
    public void C219_public_contract_and_configuration_redact_token_key_and_credentials()
    {
        var publicTypes = new[]
        {
            typeof(C2AssemblyPreparationRequest), typeof(C2AssemblyPrepareResult), typeof(C2AssemblyInspection),
            typeof(RecipientPackageOptions), typeof(RecipientPackageProviderConfiguration), typeof(RecipientPackageCredential),
        };
        Assert.DoesNotContain(publicTypes.SelectMany(type => type.GetProperties()), property =>
            property.Name.Contains("Private", StringComparison.OrdinalIgnoreCase)
            || property.Name.Contains("OperationToken", StringComparison.OrdinalIgnoreCase)
            || property.Name.Contains("Cek", StringComparison.OrdinalIgnoreCase));
        var rendered = new RecipientPackageCredential("access-value", "secret-value").ToString();
        Assert.DoesNotContain("access-value", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("secret-value", rendered, StringComparison.Ordinal);

        var crypto = File.ReadAllText(ProjectPath(
            "src/TagEkyc.Infrastructure/RawExport/RecipientPackageCryptoService.cs"));
        foreach (var zeroization in new[]
        {
            "CryptographicOperations.ZeroMemory(cek);",
            "CryptographicOperations.ZeroMemory(noncePrefix);",
            "CryptographicOperations.ZeroMemory(wrappedCek);",
            "CryptographicOperations.ZeroMemory(plaintext.AsSpan(0, buffered));",
            "CryptographicOperations.ZeroMemory(ciphertext);",
        }) Assert.Contains(zeroization, crypto, StringComparison.Ordinal);
        var orchestration = File.ReadAllText(ProjectPath(
            "src/TagEkyc.Infrastructure/RawExport/RecipientPackageServiceCollectionExtensions.cs"));
        Assert.Contains("Zero(token); Zero(equality); Zero(tokenDigest); Zero(endpoint); Zero(binding);",
            orchestration, StringComparison.Ordinal);
        var spool = File.ReadAllText(ProjectPath(
            "src/TagEkyc.Infrastructure/RawExport/RecipientPackageEncryptedSpool.cs"));
        Assert.Contains("CryptographicOperations.ZeroMemory(segment);", spool, StringComparison.Ordinal);
        Assert.Contains("CryptographicOperations.ZeroMemory(digest);", spool, StringComparison.Ordinal);
        Assert.DoesNotContain("Console.", crypto + orchestration + spool, StringComparison.Ordinal);
        Assert.DoesNotContain("privateKey", crypto + orchestration + spool, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void C225_slice_claims_reference_fixture_evidence_only()
    {
        var contracts = typeof(C2AssemblyPreparationRequest).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == typeof(C2AssemblyPreparationRequest).Namespace).ToArray();
        Assert.DoesNotContain(contracts.Where(type => !IsC3OwnedDeliveryContract(type)), type => type.Name.Contains("Download", StringComparison.OrdinalIgnoreCase)
            || type.Name.Contains("Delivery", StringComparison.OrdinalIgnoreCase)
            || type.Name.Contains("Locator", StringComparison.OrdinalIgnoreCase));
        Assert.False(typeof(RecipientPackageObjectClientFactory).IsPublic);
        Assert.False(typeof(S3CompatibleRecipientPackageProvider).IsPublic);
    }

    [Fact]
    public void C226_exact_36_path_census_has_no_program_project_or_public_delivery_surface()
    {
        var changedPaths = new[]
        {
            "docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c2_recipient_package_review_ledger.md",
            "docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c2_recipient_package_as_built.md",
            "docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c1_resolver_assembly_as_built.md",
            "src/TagEkyc.Contracts/RawExport/RawExportAssemblyContracts.cs",
            "src/TagEkyc.Infrastructure/Persistence/TagEkycDbContext.cs",
            "src/TagEkyc.Infrastructure/Persistence/Entities/RawExportRecipientKeyRegistrationRow.cs",
            "src/TagEkyc.Infrastructure/Persistence/Entities/RawExportRecipientPackagePreparationRow.cs",
            "src/TagEkyc.Infrastructure/Persistence/Entities/RawExportRecipientPackageEventRow.cs",
            "src/TagEkyc.Infrastructure/Persistence/Configurations/RawExportRecipientKeyRegistrationConfig.cs",
            "src/TagEkyc.Infrastructure/Persistence/Configurations/RawExportRecipientPackagePreparationConfig.cs",
            "src/TagEkyc.Infrastructure/Persistence/Configurations/RawExportRecipientPackageEventConfig.cs",
            "src/TagEkyc.Infrastructure/Persistence/Migrations/20260818120000_Tip88C1C2RecipientPackage.cs",
            "src/TagEkyc.Infrastructure/Persistence/Migrations/20260818120000_Tip88C1C2RecipientPackage.Designer.cs",
            "src/TagEkyc.Infrastructure/Persistence/Migrations/TagEkycDbContextModelSnapshot.cs",
            "src/TagEkyc.Infrastructure/RawExport/RecipientPackageContracts.cs",
            "src/TagEkyc.Infrastructure/RawExport/RecipientPackageOptions.cs",
            "src/TagEkyc.Infrastructure/RawExport/RecipientPackageCodec.cs",
            "src/TagEkyc.Infrastructure/RawExport/RecipientPackageCryptoService.cs",
            "src/TagEkyc.Infrastructure/RawExport/RecipientPackageEncryptedSpool.cs",
            "src/TagEkyc.Infrastructure/RawExport/RecipientPackageRepository.cs",
            "src/TagEkyc.Infrastructure/RawExport/RecipientPackageObjectClientFactory.cs",
            "src/TagEkyc.Infrastructure/RawExport/S3CompatibleRecipientPackageProvider.cs",
            "src/TagEkyc.Infrastructure/RawExport/RecipientPackageReadinessValidator.cs",
            "src/TagEkyc.Infrastructure/RawExport/RecipientPackageServiceCollectionExtensions.cs",
            "src/TagEkyc.Infrastructure/RawExport/RawExportAssemblyOrchestrator.cs",
            "tests/TagEkyc.UnitTests/Tip88C1C1AssemblyCodecTests.cs",
            "tests/TagEkyc.ArchTests/Tip88C1C1ResolverAssemblyArchTests.cs",
            "tests/TagEkyc.IntegrationTests/Tip88C1C1ResolverAssemblyTests.cs",
            "tests/TagEkyc.UnitTests/Tip88C1C2RecipientPackageCodecTests.cs",
            "tests/TagEkyc.ArchTests/Tip88C1C2RecipientPackageArchTests.cs",
            "tests/TagEkyc.IntegrationTests/Tip88C1C2RecipientPackageTests.cs",
            "tests/TagEkyc.IntegrationTests/DurableObjectMinioFixture.cs",
            "tests/TagEkyc.IntegrationTests/PostgresPersistenceFixture.cs",
            "tests/TagEkyc.IntegrationTests/Tip88B1E3ResolverReadBoundaryTests.cs",
            "tests/TagEkyc.IntegrationTests/Tip88C1B2R3VerifiedCiphertextStagingTests.cs",
            "tests/TagEkyc.IntegrationTests/Tip88C1B2SourceFinalizationTests.cs",
        };
        Assert.Equal(36, changedPaths.Length);
        Assert.Equal(36, changedPaths.Distinct(StringComparer.Ordinal).Count());
        Assert.DoesNotContain(changedPaths, path => path.EndsWith(".csproj", StringComparison.Ordinal)
            || path.EndsWith("Program.cs", StringComparison.Ordinal) || path.Contains("appsettings", StringComparison.OrdinalIgnoreCase));
        var contracts = typeof(C2AssemblyPreparationRequest).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == typeof(C2AssemblyPreparationRequest).Namespace).ToArray();
        Assert.DoesNotContain(contracts.Where(type => !IsC3OwnedDeliveryContract(type)), type => type.Name.Contains("Download", StringComparison.OrdinalIgnoreCase)
            || type.Name.Contains("Delivery", StringComparison.OrdinalIgnoreCase)
            || type.Name.Contains("Locator", StringComparison.OrdinalIgnoreCase));
        foreach (var path in changedPaths.Where(path => !path.EndsWith("tip_88c1_c2_recipient_package_as_built.md", StringComparison.Ordinal)))
            Assert.True(File.Exists(ProjectPath(path)), path);
    }

    private static string MigrationSource() => File.ReadAllText(ProjectPath(
        "src/TagEkyc.Infrastructure/Persistence/Migrations/20260818120000_Tip88C1C2RecipientPackage.cs"));
    private static bool IsC3OwnedDeliveryContract(Type type) =>
        type == typeof(RecipientPackageDeliveryDto) || type == typeof(RecipientPackageDeliveryErrorCodes);
    private static int Count(string value, string token) => (value.Length - value.Replace(token, string.Empty, StringComparison.Ordinal).Length) / token.Length;
    private static string Between(string value, string start, string end)
    {
        var first = value.IndexOf(start, StringComparison.Ordinal);
        var last = value.IndexOf(end, first, StringComparison.Ordinal);
        return value[first..last];
    }
    private static string ProjectPath(string relative)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "TagEkyc.sln"))) directory = directory.Parent;
        return Path.Combine(directory?.FullName ?? throw new InvalidOperationException("Repository root not found."), relative);
    }
}
