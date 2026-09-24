using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace TagEkyc.IntegrationTests;

public sealed class RawExportLegacyConsentClassFenceMigrationTests
{
    private const string Predecessor = "20260924120000_RawExportDeliveryRecipientCredential";
    private const string Current = "20260924130000_RawExportLegacyConsentClassFence";
    private const string ClassFence =
        "resolved_consent.\"RawClass\"=c.\"RawClass\"";

    private static readonly string[] FunctionNames =
    [
        "raw_export_commit_staged_source",
        "raw_export_publish_available_source",
        "raw_export_stage_verified_source_ciphertext",
    ];

    [Fact]
    public async Task Consent_class_fence_down_and_reapply_preserve_function_security_metadata()
    {
        await using var isolated = await IsolatedMigrationPostgres.CreateAsync();
        await using var db = isolated.CreateDbContext();
        var migrator = db.GetService<IMigrator>();

        var installed = await ReadFunctionsAsync(db);
        Assert.Equal(FunctionNames, installed.Keys);
        Assert.All(installed.Values, function =>
        {
            Assert.Contains(ClassFence, function.Definition, StringComparison.Ordinal);
            Assert.Equal("tagekyc_raw_export_deployer", function.Owner);
            Assert.True(function.SecurityDefiner);
            Assert.Contains("search_path=pg_catalog", function.Configuration, StringComparison.Ordinal);
            Assert.NotEmpty(function.Acl);
        });

        await migrator.MigrateAsync(Predecessor);
        Assert.Equal(Predecessor, (await db.Database.GetAppliedMigrationsAsync()).Last());
        var reverted = await ReadFunctionsAsync(db);
        Assert.Equal(FunctionNames, reverted.Keys);
        foreach (var name in FunctionNames)
        {
            Assert.DoesNotContain(ClassFence, reverted[name].Definition, StringComparison.Ordinal);
            Assert.Equal(installed[name].SecurityMetadata, reverted[name].SecurityMetadata);
        }

        await migrator.MigrateAsync(Current);
        Assert.Equal(Current, (await db.Database.GetAppliedMigrationsAsync()).Last());
        var reapplied = await ReadFunctionsAsync(db);
        Assert.Equal(installed, reapplied);
        Assert.False(db.Database.HasPendingModelChanges());
    }

    private static async Task<SortedDictionary<string, FunctionState>> ReadFunctionsAsync(
        TagEkyc.Infrastructure.Persistence.TagEkycDbContext db)
    {
        await db.Database.OpenConnectionAsync();
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = """
            SELECT p.proname,
                   pg_catalog.pg_get_functiondef(p.oid),
                   pg_catalog.pg_get_userbyid(p.proowner),
                   p.prosecdef,
                   COALESCE(pg_catalog.array_to_string(p.proconfig, ','), ''),
                   COALESCE(p.proacl::text, '')
            FROM pg_catalog.pg_proc p
            JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
            WHERE n.nspname='tagekyc'
              AND p.proname IN (
                'raw_export_stage_verified_source_ciphertext',
                'raw_export_commit_staged_source',
                'raw_export_publish_available_source')
            ORDER BY p.proname COLLATE "C"
            """;
        await using var reader = await command.ExecuteReaderAsync();
        var result = new SortedDictionary<string, FunctionState>(StringComparer.Ordinal);
        while (await reader.ReadAsync())
        {
            result.Add(reader.GetString(0), new FunctionState(
                reader.GetString(1).Replace("\r\n", "\n", StringComparison.Ordinal),
                reader.GetString(2),
                reader.GetBoolean(3),
                reader.GetString(4),
                reader.GetString(5)));
        }

        return result;
    }

    private sealed record FunctionState(
        string Definition,
        string Owner,
        bool SecurityDefiner,
        string Configuration,
        string Acl)
    {
        public string SecurityMetadata =>
            $"{Owner}|{SecurityDefiner}|{Configuration}|{Acl}";
    }
}
