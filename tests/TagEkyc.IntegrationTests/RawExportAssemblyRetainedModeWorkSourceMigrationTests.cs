using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace TagEkyc.IntegrationTests;

public sealed class RawExportAssemblyRetainedModeWorkSourceMigrationTests
{
    private const string Predecessor = "20260924130000_RawExportLegacyConsentClassFence";
    private const string Current = "20260925090000_RawExportAssemblyRetainedModeWorkSource";
    private const string FunctionName = "raw_export_next_assembly_candidate";
    private const string PacketMode = "i.\"ExportMode\"='EncryptedExportPacket'";
    private const string RetainedPredicate =
        "i.\"ExportMode\" IN ('EncryptedExportPacket','EncryptedRawVaultRetained')";
    private const string RetainedMode = "'EncryptedRawVaultRetained'";

    [Fact]
    public async Task Retained_mode_work_source_down_and_reapply_preserve_exact_function_and_security_metadata()
    {
        await using var isolated = await IsolatedMigrationPostgres.CreateAsync();
        await using var db = isolated.CreateDbContext();
        var migrator = db.GetService<IMigrator>();

        var installed = await ReadFunctionAsync(db);
        Assert.Contains(RetainedPredicate, installed.Definition, StringComparison.Ordinal);
        Assert.Contains(RetainedMode, installed.Definition, StringComparison.Ordinal);
        Assert.Equal("tagekyc_raw_export_deployer", installed.Owner);
        Assert.True(installed.SecurityDefiner);
        Assert.Contains("search_path=pg_catalog", installed.Configuration, StringComparison.Ordinal);
        Assert.NotEmpty(installed.Acl);

        await migrator.MigrateAsync(Predecessor);
        Assert.Equal(Predecessor, (await db.Database.GetAppliedMigrationsAsync()).Last());
        var reverted = await ReadFunctionAsync(db);
        Assert.Contains(PacketMode, reverted.Definition, StringComparison.Ordinal);
        Assert.DoesNotContain(RetainedMode, reverted.Definition, StringComparison.Ordinal);
        Assert.Equal(installed.SecurityMetadata, reverted.SecurityMetadata);

        await migrator.MigrateAsync(Current);
        Assert.Equal(Current, (await db.Database.GetAppliedMigrationsAsync()).Last());
        var reapplied = await ReadFunctionAsync(db);
        Assert.Equal(installed, reapplied);
        Assert.False(db.Database.HasPendingModelChanges());
    }

    private static async Task<FunctionState> ReadFunctionAsync(
        TagEkyc.Infrastructure.Persistence.TagEkycDbContext db)
    {
        await db.Database.OpenConnectionAsync();
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = """
            SELECT pg_catalog.pg_get_functiondef(p.oid),
                   pg_catalog.pg_get_userbyid(p.proowner),
                   p.prosecdef,
                   COALESCE(pg_catalog.array_to_string(p.proconfig, ','), ''),
                   COALESCE(p.proacl::text, '')
            FROM pg_catalog.pg_proc p
            JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
            WHERE n.nspname='tagekyc'
              AND p.proname='raw_export_next_assembly_candidate'
            """;
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync(), $"{FunctionName} must exist.");
        var result = new FunctionState(
            reader.GetString(0).Replace("\r\n", "\n", StringComparison.Ordinal),
            reader.GetString(1),
            reader.GetBoolean(2),
            reader.GetString(3),
            reader.GetString(4));
        Assert.False(await reader.ReadAsync(), $"{FunctionName} must be unique.");
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
