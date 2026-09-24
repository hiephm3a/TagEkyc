using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace TagEkyc.IntegrationTests;

public sealed class RawExportDeliveryCredentialMigrationTests
{
    private const string Predecessor = "20260923090000_RawExportAssemblyDurableWorkSource";
    private const string DownloadOnlyProfile = "C3C4RecipientV1";
    private const string DeliveryOperatorProfile = "RawExportDeliveryRecipientV2";
    private const string DownloadOnlyScopes =
        "[\"business.raw-export.package.download\", \"business.raw-export.package.references.read\"]";
    private const string DeliveryOperatorScopes =
        "[\"business.raw-export.authorize\", \"business.raw-export.job.manage\", \"business.raw-export.package.download\", \"business.raw-export.package.references.read\"]";

    [Fact]
    public async Task Delivery_credential_successor_preserves_existing_least_privilege_and_requires_explicit_full_profile()
    {
        await using var isolated = await IsolatedMigrationPostgres.CreateAsync();
        await using var db = isolated.CreateDbContext();
        var migrator = db.GetService<IMigrator>();

        Assert.Contains(DeliveryOperatorProfile, await ReadProfileConstraintAsync(db), StringComparison.Ordinal);
        Assert.Contains(DeliveryOperatorProfile, await ReadIssueFunctionAsync(db, 12), StringComparison.Ordinal);

        await migrator.MigrateAsync(Predecessor);
        Assert.DoesNotContain(DeliveryOperatorProfile, await ReadProfileConstraintAsync(db), StringComparison.Ordinal);

        var legacyRecipient = Guid.NewGuid();
        var legacyKey = Guid.NewGuid();
        await EnrollAsync(db, legacyRecipient, Guid.NewGuid(), profile: null);
        await IssueAsync(db, legacyRecipient, legacyKey, profileVersion: null);
        Assert.Equal((DownloadOnlyProfile, DownloadOnlyScopes),
            await ReadCredentialProfileAsync(db, legacyRecipient, legacyKey));

        await migrator.MigrateAsync();
        Assert.Equal((DownloadOnlyProfile, DownloadOnlyScopes),
            await ReadCredentialProfileAsync(db, legacyRecipient, legacyKey));

        var deliveryRecipient = Guid.NewGuid();
        var deliveryKey = Guid.NewGuid();
        await EnrollAsync(db, deliveryRecipient, Guid.NewGuid(), DeliveryOperatorProfile);
        await IssueAsync(db, deliveryRecipient, deliveryKey, profileVersion: 1);
        Assert.Equal((DeliveryOperatorProfile, DeliveryOperatorScopes),
            await ReadCredentialProfileAsync(db, deliveryRecipient, deliveryKey));
        Assert.False(db.Database.HasPendingModelChanges());
    }

    private static async Task EnrollAsync(
        TagEkyc.Infrastructure.Persistence.TagEkycDbContext db,
        Guid recipient,
        Guid principal,
        string? profile)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        if (command.Connection!.State != System.Data.ConnectionState.Open)
            await command.Connection.OpenAsync();
        command.CommandText = profile is null
            ? "SELECT * FROM tagekyc.raw_export_enroll_managed_recipient(@o,@a,@m,@r,@i,@e,@p,@principal)"
            : "SELECT * FROM tagekyc.raw_export_enroll_managed_recipient(@o,@a,@m,@r,@i,@e,@p,@principal,@profile)";
        Add(command, "o", Guid.NewGuid());
        Add(command, "a", Guid.NewGuid());
        Add(command, "m", Guid.NewGuid());
        Add(command, "r", recipient);
        Add(command, "i", RandomBytes());
        Add(command, "e", RandomBytes());
        Add(command, "p", RandomBytes());
        Add(command, "principal", principal);
        if (profile is not null) Add(command, "profile", profile);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task IssueAsync(
        TagEkyc.Infrastructure.Persistence.TagEkycDbContext db,
        Guid recipient,
        Guid apiKey,
        int? profileVersion)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        if (command.Connection!.State != System.Data.ConnectionState.Open)
            await command.Connection.OpenAsync();
        command.CommandText = profileVersion is null
            ? "SELECT * FROM tagekyc.raw_export_issue_recipient_credential(@o,@a,@m,@r,@i,@e,@p,@key,@prefix,@hash,@expires)"
            : "SELECT * FROM tagekyc.raw_export_issue_recipient_credential(@o,@a,@m,@r,@i,@e,@p,@key,@prefix,@hash,@expires,@version)";
        Add(command, "o", Guid.NewGuid());
        Add(command, "a", Guid.NewGuid());
        Add(command, "m", Guid.NewGuid());
        Add(command, "r", recipient);
        Add(command, "i", RandomBytes());
        Add(command, "e", RandomBytes());
        Add(command, "p", RandomBytes());
        Add(command, "key", apiKey);
        Add(command, "prefix", Guid.NewGuid().ToString("N")[..16]);
        Add(command, "hash", RandomBytes());
        Add(command, "expires", DateTimeOffset.UtcNow.AddHours(1));
        if (profileVersion is not null) Add(command, "version", profileVersion.Value);
        await command.ExecuteNonQueryAsync();
    }

    private static void Add(System.Data.Common.DbCommand command, string name, object value) =>
        command.Parameters.Add(new NpgsqlParameter(name, value));

    private static byte[] RandomBytes() => System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);

    private static async Task<string> ReadProfileConstraintAsync(
        TagEkyc.Infrastructure.Persistence.TagEkycDbContext db) =>
        await db.Database.SqlQueryRaw<string>("""
            SELECT pg_catalog.pg_get_constraintdef(c.oid) AS "Value"
            FROM pg_catalog.pg_constraint c
            WHERE c.conrelid='tagekyc.raw_export_managed_recipient_policies'::regclass
              AND c.conname='ck_raw_export_managed_recipient_policy_shape'
            """).SingleAsync();

    private static async Task<string> ReadIssueFunctionAsync(
        TagEkyc.Infrastructure.Persistence.TagEkycDbContext db,
        int argumentCount) =>
        await db.Database.SqlQueryRaw<string>($"""
            SELECT pg_catalog.pg_get_functiondef(p.oid) AS "Value"
            FROM pg_catalog.pg_proc p
            JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
            WHERE n.nspname='tagekyc'
              AND p.proname='raw_export_issue_recipient_credential'
              AND p.pronargs={argumentCount}
            """).SingleAsync();

    private static async Task<(string Profile, string Scopes)> ReadCredentialProfileAsync(
        TagEkyc.Infrastructure.Persistence.TagEkycDbContext db,
        Guid recipient,
        Guid apiKey)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        if (command.Connection!.State != System.Data.ConnectionState.Open)
            await command.Connection.OpenAsync();
        command.CommandText = """
            SELECT p."ActivationProfile", a."ScopesJson"::text
            FROM tagekyc.raw_export_managed_recipient_policies p
            JOIN tagekyc.api_keys a ON a."ClientApplicationId"=p."RecipientClientApplicationId"
            WHERE p."RecipientClientApplicationId"=@recipient AND a."ApiKeyId"=@apiKey
            """;
        Add(command, "recipient", recipient);
        Add(command, "apiKey", apiKey);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return (reader.GetString(0), reader.GetString(1));
    }
}
