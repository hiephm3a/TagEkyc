using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.IntegrationTests;

public sealed class RawExportAssemblyPostSealRecoveryMigrationTests
{
    private const string Predecessor = "20260925090000_RawExportAssemblyRetainedModeWorkSource";
    private const string Current = "20260926120000_RawExportAssemblyPostSealRecovery";

    [Fact]
    public async Task Post_seal_recovery_apply_down_reapply_preserves_catalog_security_and_guards()
    {
        await using var isolated = await IsolatedMigrationPostgres.CreateAsync();
        await using var db = isolated.CreateDbContext();
        var migrator = db.GetService<IMigrator>();

        var installed = await ReadCatalogAsync(db);
        Assert.Contains("raw_export_claim_next_post_seal_recovery", installed, StringComparison.Ordinal);
        Assert.Contains("'EncryptedExportPacket','EncryptedRawVaultRetained'", installed, StringComparison.Ordinal);
        Assert.Contains("'ExternalExportOnlyNoRetain','EncryptedExportPacket','EncryptedRawVaultRetained'", installed, StringComparison.Ordinal);
        Assert.Contains("tagekyc_raw_export_deployer|false|search_path=pg_catalog", installed, StringComparison.Ordinal);
        Assert.Contains("tagekyc_raw_export_deployer|true|search_path=pg_catalog", installed, StringComparison.Ordinal);
        Assert.Contains("NON_OWNER_TABLE_GRANTS=0", installed, StringComparison.Ordinal);
        Assert.Contains("INVALID_FUNCTION_GRANTS=0", installed, StringComparison.Ordinal);
        Assert.Contains("LEGACY_FINALIZE_SEALER_EXECUTE=false", installed, StringComparison.Ordinal);
        await AssertDirectMutationRejectedAsync(db);

        await migrator.MigrateAsync(Predecessor);
        Assert.Equal(Predecessor, (await db.Database.GetAppliedMigrationsAsync()).Last());
        Assert.Equal("TABLE=false;FUNCTIONS=0;TRIGGER=false;PREPARATION_INDEX=false;LEGACY_FINALIZE_SEALER_EXECUTE=true",
            await ReadAbsentStateAsync(db));

        await migrator.MigrateAsync(Current);
        Assert.Equal(Current, (await db.Database.GetAppliedMigrationsAsync()).Last());
        Assert.Equal(installed, await ReadCatalogAsync(db));
        await AssertDirectMutationRejectedAsync(db);
        Assert.False(db.Database.HasPendingModelChanges());
    }

    private static async Task<string> ReadCatalogAsync(TagEkycDbContext db)
    {
        await db.Database.OpenConnectionAsync();
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = """
            WITH function_rows AS (
              SELECT p.proname||'|'||pg_catalog.oidvectortypes(p.proargtypes)||'|'||
                     pg_catalog.pg_get_userbyid(p.proowner)||'|'||p.prosecdef::text||'|'||
                     COALESCE(pg_catalog.array_to_string(p.proconfig,','),'')||'|'||
                     COALESCE(p.proacl::text,'')||'|'||pg_catalog.pg_get_functiondef(p.oid) AS value
              FROM pg_catalog.pg_proc p
              JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
              WHERE n.nspname='tagekyc' AND p.proname IN (
                'enforce_raw_export_post_seal_recovery_mutation',
                'raw_export_claim_next_post_seal_recovery',
                'raw_export_claim_exact_post_seal_recovery',
                'raw_export_defer_post_seal_recovery',
                'raw_export_record_claimed_assembly_finalized')),
            table_rows AS (
              SELECT 'TABLE|'||pg_catalog.pg_get_userbyid(c.relowner)||'|'||COALESCE(c.relacl::text,'') AS value
              FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
              WHERE n.nspname='tagekyc' AND c.relname='raw_export_assembly_post_seal_recovery_claims'
              UNION ALL
              SELECT 'CONSTRAINT|'||c.conname||'|'||pg_catalog.pg_get_constraintdef(c.oid)
              FROM pg_catalog.pg_constraint c
              WHERE c.conrelid='tagekyc.raw_export_assembly_post_seal_recovery_claims'::regclass
              UNION ALL
              SELECT 'INDEX|'||i.indexname||'|'||i.indexdef FROM pg_catalog.pg_indexes i
              WHERE i.schemaname='tagekyc' AND i.tablename IN (
                'raw_export_assembly_post_seal_recovery_claims','raw_export_assembly_preparation_dispositions')
                AND i.indexname IN (
                  'raw_export_assembly_post_seal_recovery_claims_pkey',
                  'raw_export_assembly_post_seal_recovery_claims_job_key',
                  'ix_raw_export_post_seal_recovery_eligibility',
                  'ix_raw_export_preparation_post_seal_discovery')
              UNION ALL
              SELECT 'TRIGGER|'||t.tgname||'|'||pg_catalog.pg_get_triggerdef(t.oid)
              FROM pg_catalog.pg_trigger t
              WHERE t.tgrelid='tagekyc.raw_export_assembly_post_seal_recovery_claims'::regclass
                AND NOT t.tgisinternal),
            security_rows AS (
              SELECT 'NON_OWNER_TABLE_GRANTS='||pg_catalog.count(*)::text AS value
              FROM pg_catalog.pg_class c
              JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
              CROSS JOIN LATERAL pg_catalog.aclexplode(COALESCE(c.relacl,pg_catalog.acldefault('r',c.relowner))) a
              WHERE n.nspname='tagekyc' AND c.relname='raw_export_assembly_post_seal_recovery_claims'
                AND a.grantee<>c.relowner
              UNION ALL
              SELECT 'INVALID_FUNCTION_GRANTS='||pg_catalog.count(*)::text
              FROM pg_catalog.pg_proc p
              JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
              CROSS JOIN LATERAL pg_catalog.aclexplode(COALESCE(p.proacl,pg_catalog.acldefault('f',p.proowner))) a
              WHERE n.nspname='tagekyc' AND p.proname IN (
                'raw_export_claim_next_post_seal_recovery',
                'raw_export_claim_exact_post_seal_recovery',
                'raw_export_defer_post_seal_recovery',
                'raw_export_record_claimed_assembly_finalized')
                AND (a.privilege_type<>'EXECUTE'
                  OR a.grantee NOT IN (p.proowner,
                    (SELECT oid FROM pg_catalog.pg_roles WHERE rolname='tagekyc_raw_export_assembly_sealer'))
                  OR a.grantor<>p.proowner OR a.is_grantable)
              UNION ALL
              SELECT 'LEGACY_FINALIZE_SEALER_EXECUTE='||pg_catalog.has_function_privilege(
                'tagekyc_raw_export_assembly_sealer',
                'tagekyc.raw_export_record_assembly_finalized(uuid,bigint,bytea)',
                'EXECUTE')::text
            )
            SELECT pg_catalog.string_agg(value,E'\n' ORDER BY value)
            FROM (SELECT value FROM function_rows UNION ALL SELECT value FROM table_rows
                  UNION ALL SELECT value FROM security_rows) all_rows
            """;
        return ((string?)await command.ExecuteScalarAsync())!
            .Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    private static async Task<string> ReadAbsentStateAsync(TagEkycDbContext db)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = """
            SELECT 'TABLE='||(pg_catalog.to_regclass('tagekyc.raw_export_assembly_post_seal_recovery_claims') IS NOT NULL)::text||
                   ';FUNCTIONS='||(SELECT pg_catalog.count(*) FROM pg_catalog.pg_proc p
                     JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
                     WHERE n.nspname='tagekyc' AND p.proname IN (
                       'enforce_raw_export_post_seal_recovery_mutation',
                       'raw_export_claim_next_post_seal_recovery',
                       'raw_export_claim_exact_post_seal_recovery',
                       'raw_export_defer_post_seal_recovery',
                       'raw_export_record_claimed_assembly_finalized'))::text||
                   ';TRIGGER='||(EXISTS(SELECT 1 FROM pg_catalog.pg_trigger WHERE tgname='tr_raw_export_post_seal_recovery_mutation'))::text||
                   ';PREPARATION_INDEX='||(pg_catalog.to_regclass('tagekyc.ix_raw_export_preparation_post_seal_discovery') IS NOT NULL)::text||
                   ';LEGACY_FINALIZE_SEALER_EXECUTE='||pg_catalog.has_function_privilege(
                     'tagekyc_raw_export_assembly_sealer',
                     'tagekyc.raw_export_record_assembly_finalized(uuid,bigint,bytea)',
                     'EXECUTE')::text
            """;
        return (string)(await command.ExecuteScalarAsync())!;
    }

    private static async Task AssertDirectMutationRejectedAsync(TagEkycDbContext db)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = """
            SET LOCAL ROLE tagekyc_raw_export_deployer;
            INSERT INTO tagekyc.raw_export_assembly_post_seal_recovery_claims(
              "C2PreparationId","JobId","ClaimOwnerId","ClaimGeneration","ClaimExpiresAtUtc",
              "FailureCount","RetryNotBeforeUtc","LastOutcome","CreatedAtUtc","UpdatedAtUtc",
              "CompletedAtUtc","SchemaVersion")
            VALUES(pg_catalog.gen_random_uuid(),pg_catalog.gen_random_uuid(),NULL,1,NULL,
              0,NULL,NULL,pg_catalog.clock_timestamp(),pg_catalog.clock_timestamp(),NULL,1)
            """;
        var error = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
        Assert.Equal("RAW_EXPORT_POST_SEAL_RECOVERY_MUTATION_UNSUPPORTED", error.MessageText);
        await transaction.RollbackAsync();
    }
}
