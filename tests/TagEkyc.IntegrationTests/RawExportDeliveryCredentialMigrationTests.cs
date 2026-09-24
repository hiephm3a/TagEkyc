using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace TagEkyc.IntegrationTests;

public sealed class RawExportDeliveryCredentialMigrationTests
{
    private const string Predecessor = "20260923090000_RawExportAssemblyDurableWorkSource";

    [Fact]
    public async Task Delivery_credential_successor_down_and_reapply_restore_exact_profile_contract()
    {
        await using var isolated = await IsolatedMigrationPostgres.CreateAsync();
        await using var db = isolated.CreateDbContext();
        var migrator = db.GetService<IMigrator>();

        Assert.Equal("RawExportDeliveryRecipientV2", await ReadProfileConstraintAsync(db));
        Assert.Contains("RawExportDeliveryRecipientV2", await ReadIssueFunctionAsync(db),
            StringComparison.Ordinal);

        await migrator.MigrateAsync(Predecessor);
        Assert.Equal("C3C4RecipientV1", await ReadProfileConstraintAsync(db));
        Assert.Contains("C3C4RecipientV1", await ReadIssueFunctionAsync(db),
            StringComparison.Ordinal);

        await migrator.MigrateAsync();
        Assert.Equal("RawExportDeliveryRecipientV2", await ReadProfileConstraintAsync(db));
        Assert.Contains("business.raw-export.authorize", await ReadIssueFunctionAsync(db),
            StringComparison.Ordinal);
        Assert.False(db.Database.HasPendingModelChanges());
    }

    private static async Task<string> ReadProfileConstraintAsync(
        TagEkyc.Infrastructure.Persistence.TagEkycDbContext db) =>
        (await db.Database.SqlQueryRaw<string>("""
            SELECT pg_catalog.pg_get_constraintdef(c.oid) AS "Value"
            FROM pg_catalog.pg_constraint c
            WHERE c.conrelid='tagekyc.raw_export_managed_recipient_policies'::regclass
              AND c.conname='ck_raw_export_managed_recipient_policy_shape'
            """).SingleAsync()).Contains("RawExportDeliveryRecipientV2", StringComparison.Ordinal)
            ? "RawExportDeliveryRecipientV2"
            : "C3C4RecipientV1";

    private static async Task<string> ReadIssueFunctionAsync(
        TagEkyc.Infrastructure.Persistence.TagEkycDbContext db) =>
        await db.Database.SqlQueryRaw<string>("""
            SELECT pg_catalog.pg_get_functiondef(p.oid) AS "Value"
            FROM pg_catalog.pg_proc p
            JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
            WHERE n.nspname='tagekyc'
              AND p.proname='raw_export_issue_recipient_credential'
            """).SingleAsync();
}
