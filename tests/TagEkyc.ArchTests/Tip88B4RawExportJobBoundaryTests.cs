using System.Reflection;
using System.Text.RegularExpressions;
using TagEkyc.Api;
using TagEkyc.Application.Ports;
using TagEkyc.Domain;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.ArchTests;

public sealed class Tip88B4RawExportJobBoundaryTests
{
    [Fact]
    public void B4_does_not_assign_or_normalize_connection_string()
    {
        foreach (var (path, source) in ReadB4ImplementationSources())
        {
            Assert.DoesNotMatch(
                new Regex(
                    @"(?:\.|\b)ConnectionString\s*=",
                    RegexOptions.CultureInvariant),
                source);
            Assert.DoesNotContain(
                "SetConnectionString(",
                source,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                "UseNpgsql(",
                source,
                StringComparison.Ordinal);
        }
    }

    [Fact]
    public void B4_does_not_reference_enlist_or_npgsql_connection_string_builder()
    {
        foreach (var (_, source) in ReadB4ImplementationSources())
        {
            Assert.DoesNotContain(
                "NpgsqlConnectionStringBuilder",
                source,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                "Enlist",
                source,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void B4_uses_same_scoped_dbcontext_connection_and_transaction_for_B1_B2()
    {
        var source = ReadRepositorySource();

        Assert.DoesNotContain("new TagEkycDbContext", source, StringComparison.Ordinal);
        Assert.DoesNotContain("new NpgsqlConnection(", source, StringComparison.Ordinal);
        Assert.Contains("db.Database.GetDbConnection()", source, StringComparison.Ordinal);
        Assert.Contains(
            "ResolveExportEligibilityForAuthorizationAsync",
            source,
            StringComparison.Ordinal);
        Assert.Contains("ReadPolicyInputsAsync", source, StringComparison.Ordinal);
        Assert.Contains(
            "ResolveSubjectExportConsentForAuthorizationAsync",
            source,
            StringComparison.Ordinal);
        Assert.Contains(
            "Func<NpgsqlConnection, NpgsqlTransaction, CancellationToken, Task<T>> body",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void B4_does_not_create_second_dbcontext_connection_or_datasource()
    {
        var source = ReadRepositorySource();

        Assert.DoesNotContain("new TagEkycDbContext", source, StringComparison.Ordinal);
        Assert.DoesNotContain("new NpgsqlConnection(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("NpgsqlDataSource", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IDbContextFactory", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IServiceScopeFactory", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".CreateScope(", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".CreateAsyncScope(", source, StringComparison.Ordinal);
    }

    [Fact]
    public void B4_global_persistence_options_remain_unchanged()
    {
        var source = ReadRepositorySource();

        Assert.DoesNotContain("DbContextOptionsBuilder", source, StringComparison.Ordinal);
        Assert.DoesNotContain("UseNpgsql(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AddDbContext", source, StringComparison.Ordinal);
        Assert.DoesNotContain("NpgsqlConnectionStringBuilder", source, StringComparison.Ordinal);
        Assert.DoesNotMatch(
            new Regex(
                @"(?:\.|\b)ConnectionString\s*=",
                RegexOptions.CultureInvariant),
            source);
    }

    [Fact]
    public void M3_production_bind_and_direct_claim_share_session_then_permit_order()
    {
        var root = FindRepositoryRoot();
        var repository = File.ReadAllText(Path.Combine(
            root,
            "src",
            "TagEkyc.Infrastructure",
            "Persistence",
            "EfRawExportJobRepository.cs"));
        var bindStart = repository.IndexOf(
            "private async Task<RawExportJobBindResult> BindCoreAsync(",
            StringComparison.Ordinal);
        var bindEnd = repository.IndexOf(
            "private async Task<RawExportJobReadResult> ReadCoreAsync(",
            bindStart,
            StringComparison.Ordinal);
        Assert.True(bindStart >= 0 && bindEnd > bindStart);
        var bind = repository[bindStart..bindEnd];
        Assert.True(
            bind.IndexOf("LockSessionAsync(", StringComparison.Ordinal) <
            bind.IndexOf("raw_export_claim_or_read_job(", StringComparison.Ordinal));

        var migration = File.ReadAllText(Path.Combine(
            root,
            "src",
            "TagEkyc.Infrastructure",
            "Persistence",
            "Migrations",
            "20260726145547_Tip88B4RawExportJobFoundation.cs"));
        var claimStart = migration.IndexOf(
            "CREATE FUNCTION tagekyc.raw_export_claim_or_read_job(",
            StringComparison.Ordinal);
        var claimEnd = migration.IndexOf(
            "CREATE FUNCTION tagekyc.raw_export_read_job(",
            claimStart,
            StringComparison.Ordinal);
        Assert.True(claimStart >= 0 && claimEnd > claimStart);
        var claim = migration[claimStart..claimEnd];
        Assert.True(
            claim.IndexOf(
                "raw_export_lock_verification_session_for_authorization",
                StringComparison.Ordinal) <
            claim.IndexOf(
                "FROM tagekyc.raw_export_authorization_permits",
                StringComparison.Ordinal));
        Assert.Contains("FOR UPDATE", claim, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("BindAsync")]
    [InlineData("ReadAsync")]
    [InlineData("AcquireOrReclaimLeaseAsync")]
    [InlineData("RenewLeaseAsync")]
    [InlineData("RecordAttemptFailureAsync")]
    [InlineData("TerminalizeAsync")]
    public void B4_all_six_methods_own_fresh_read_committed_transactions(
        string methodName)
    {
        var source = ReadRepositorySource();
        var start = source.IndexOf($" {methodName}(", StringComparison.Ordinal);
        Assert.True(start >= 0, $"{methodName} was not found.");
        var next = source.IndexOf("\n    public Task<", start + methodName.Length, StringComparison.Ordinal);
        var body = source[start..(next < 0 ? source.Length : next)];

        Assert.Contains("return ExecuteAsync", body, StringComparison.Ordinal);
        Assert.Contains(
            "BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cancellationToken)",
            ExtractExecuteAsync(source),
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("BindAsync")]
    [InlineData("ReadAsync")]
    [InlineData("AcquireOrReclaimLeaseAsync")]
    [InlineData("RenewLeaseAsync")]
    [InlineData("RecordAttemptFailureAsync")]
    [InlineData("TerminalizeAsync")]
    public void B4_all_six_methods_final_admission_is_adjacent_to_each_open_or_begin(
        string methodName)
    {
        var source = ReadRepositorySource();
        var method = ExtractPublicMethod(source, methodName);
        Assert.Contains("return ExecuteAsync", method, StringComparison.Ordinal);
        Assert.DoesNotContain("OpenAsync(", method, StringComparison.Ordinal);
        Assert.DoesNotContain("OpenConnectionAsync(", method, StringComparison.Ordinal);

        const string adjacent =
            """
            EnsureTransactionAdmission(connection);
                        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cancellationToken);
            """;
        Assert.Contains(adjacent, ExtractExecuteAsync(source), StringComparison.Ordinal);
    }

    [Fact]
    public void B4_explicit_open_adjacency_cells_are_not_applicable()
    {
        var execute = ExtractExecuteAsync(ReadRepositorySource());

        Assert.DoesNotContain("OpenAsync(", execute, StringComparison.Ordinal);
        Assert.DoesNotContain("OpenConnectionAsync(", execute, StringComparison.Ordinal);
    }

    [Fact]
    public void M13_b4_contains_no_raw_byte_package_or_delivery_surface()
    {
        var b4Types = new[]
        {
            typeof(BindRawExportJobCommand),
            typeof(ReadRawExportJobCommand),
            typeof(AcquireOrReclaimRawExportJobLeaseCommand),
            typeof(RenewRawExportJobLeaseCommand),
            typeof(RecordRawExportJobAttemptFailureCommand),
            typeof(TerminalizeRawExportJobCommand),
            typeof(RawExportJobIdentityView),
            typeof(RawExportJobClassView),
            typeof(RawExportJobOperationalHeadView),
            typeof(RawExportJobTransitionSummary),
            typeof(RawExportJobView),
            typeof(RawExportJobBindResult),
            typeof(RawExportJobReadResult),
            typeof(RawExportJobLeaseResult),
            typeof(RawExportJobRenewResult),
            typeof(RawExportJobAttemptFailureResult),
            typeof(RawExportJobTerminalizeResult),
        };

        foreach (var type in b4Types)
        {
            Assert.DoesNotContain(
                type.GetProperties(BindingFlags.Public | BindingFlags.Instance),
                property => ContainsByteSurface(property.PropertyType) ||
                            HasForbiddenDataName(property.Name));
        }

        var port = typeof(IRawExportJobRepository);
        Assert.All(
            port.GetMethods(BindingFlags.Public | BindingFlags.Instance),
            method =>
            {
                Assert.False(ContainsByteSurface(method.ReturnType));
                Assert.DoesNotContain(
                    method.GetParameters(),
                    parameter => ContainsByteSurface(parameter.ParameterType));
            });

        var apiSurface = typeof(VerificationSessionEndpoints).Assembly.GetTypes()
            .SelectMany(type => type.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            .SelectMany(method => new[] { method.ReturnType }
                .Concat(method.GetParameters().Select(parameter => parameter.ParameterType)));
        var contractSurface = typeof(TagEkyc.Contracts.BusinessConsumer.CreateVerificationSessionRequestDto)
            .Assembly
            .GetTypes()
            .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            .Select(property => property.PropertyType);

        foreach (var type in b4Types)
        {
            Assert.DoesNotContain(apiSurface, candidate => IsOrContains(candidate, type));
            Assert.DoesNotContain(contractSurface, candidate => IsOrContains(candidate, type));
        }

        Assert.DoesNotContain(
            typeof(TagEkycDbContext)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.Name.StartsWith("RawExportJob", StringComparison.Ordinal)),
            property => HasForbiddenDataName(property.Name));
    }

    private static bool HasForbiddenDataName(string name) =>
        name.Contains("Payload", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("Bytes", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("Blob", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("Content", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("Artifact", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("Package", StringComparison.OrdinalIgnoreCase);

    private static bool ContainsByteSurface(Type type)
    {
        if (type == typeof(byte[]) ||
            typeof(Stream).IsAssignableFrom(type) ||
            typeof(ReadOnlyMemory<byte>).IsAssignableFrom(type) ||
            typeof(Memory<byte>).IsAssignableFrom(type))
        {
            return true;
        }

        return type.IsGenericType &&
               type.GetGenericArguments().Any(ContainsByteSurface);
    }

    private static bool IsOrContains(Type candidate, Type forbidden)
    {
        if (candidate == forbidden)
        {
            return true;
        }

        if (candidate.IsArray)
        {
            return IsOrContains(candidate.GetElementType()!, forbidden);
        }

        return candidate.IsGenericType &&
               candidate.GetGenericArguments().Any(argument => IsOrContains(argument, forbidden));
    }

    private static string ReadRepositorySource() =>
        File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "TagEkyc.Infrastructure",
            "Persistence",
            "EfRawExportJobRepository.cs"));

    private static IReadOnlyList<(string Path, string Source)> ReadB4ImplementationSources()
    {
        var root = FindRepositoryRoot();
        var paths = new[]
        {
            Path.Combine(
                root,
                "src",
                "TagEkyc.Infrastructure",
                "Persistence",
                "EfRawExportJobRepository.cs"),
            Path.Combine(
                root,
                "src",
                "TagEkyc.Infrastructure",
                "Persistence",
                "Migrations",
                "20260726145547_Tip88B4RawExportJobFoundation.cs"),
            Path.Combine(
                root,
                "src",
                "TagEkyc.Infrastructure",
                "Persistence",
                "Migrations",
                "20260726145547_Tip88B4RawExportJobFoundation.Designer.cs"),
        };

        return paths
            .Select(path => (path, File.ReadAllText(path)))
            .ToArray();
    }

    private static string ExtractPublicMethod(string source, string methodName)
    {
        var start = source.IndexOf($" {methodName}(", StringComparison.Ordinal);
        Assert.True(start >= 0, $"{methodName} was not found.");
        var openingBrace = source.IndexOf('{', start);
        Assert.True(openingBrace >= 0, $"{methodName} has no opening brace.");
        var depth = 0;
        for (var index = openingBrace; index < source.Length; index++)
        {
            depth += source[index] switch
            {
                '{' => 1,
                '}' => -1,
                _ => 0,
            };
            if (depth == 0)
            {
                return source[start..(index + 1)];
            }
        }

        throw new Xunit.Sdk.XunitException($"{methodName} has no matching closing brace.");
    }

    private static string ExtractExecuteAsync(string source)
    {
        var start = source.IndexOf(
            "private async Task<T> ExecuteAsync<T>(",
            StringComparison.Ordinal);
        var end = source.IndexOf(
            "private void EnsureTransactionAdmission(",
            start,
            StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start);
        return source[start..end];
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "TagEkyc.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ??
               throw new InvalidOperationException("Repository root was not found.");
    }
}
