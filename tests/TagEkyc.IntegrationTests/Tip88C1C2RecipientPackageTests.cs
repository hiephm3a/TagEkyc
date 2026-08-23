using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88C1C2RecipientPackageTests(PostgresPersistenceFixture postgres)
{
    [Fact]
    public async Task C201_additive_model_migration_tripwires_and_three_tables_are_exact()
    {
        var snapshotPath = ProjectPath("src/TagEkyc.Infrastructure/Persistence/Migrations/TagEkycDbContextModelSnapshot.cs");
        var snapshotHash = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(snapshotPath)));
        Assert.Equal("5F8653C3D679DBA8EC3D933192BCB4B61E180BB3243953DCED60AAD4E87E8E3C", snapshotHash);
        var pinPaths = new[]
        {
            "tests/TagEkyc.IntegrationTests/Tip88B1E3ResolverReadBoundaryTests.cs",
            "tests/TagEkyc.IntegrationTests/Tip88C1B2R3VerifiedCiphertextStagingTests.cs",
        };
        foreach (var path in pinPaths) Assert.Contains(snapshotHash, await File.ReadAllTextAsync(ProjectPath(path)), StringComparison.Ordinal);
        await using (var downDb = postgres.CreateDbContext())
        {
            var migrator = downDb.Database.GetService<IMigrator>();
            await migrator.MigrateAsync("20260815120000_Tip88C1C1ResolverAssembly");
        }
        await using (var downConnection = new NpgsqlConnection(postgres.ConnectionString))
        {
            await downConnection.OpenAsync();
            await using var downCommand = new NpgsqlCommand("""
                SELECT
                  (SELECT pg_catalog.count(*) FROM pg_catalog.pg_class c
                   JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
                   WHERE n.nspname='tagekyc' AND c.relname IN
                     ('raw_export_recipient_key_registrations','raw_export_recipient_package_preparations','raw_export_recipient_package_events')) AS tables,
                  (SELECT pg_catalog.count(*) FROM pg_catalog.pg_roles
                   WHERE rolname IN ('tagekyc_raw_export_package_preparer_login','tagekyc_raw_export_package_reconciler_login','tagekyc_raw_export_package_lifecycle_login')) AS logins,
                  (SELECT pg_catalog.count(*) FROM pg_catalog.pg_roles
                   WHERE rolname IN ('tagekyc_raw_export_package_preparer','tagekyc_raw_export_package_reconciler','tagekyc_raw_export_package_lifecycle')) AS capabilities
                """, downConnection);
            await using var downReader = await downCommand.ExecuteReaderAsync();
            Assert.True(await downReader.ReadAsync());
            Assert.Equal(0L, downReader.GetInt64(0));
            Assert.Equal(3L, downReader.GetInt64(1));
            Assert.Equal(0L, downReader.GetInt64(2));
        }
        await using (var upDb = postgres.CreateDbContext())
        {
            var migrator = upDb.Database.GetService<IMigrator>();
            await migrator.MigrateAsync();
        }

        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT pg_catalog.count(*) FROM pg_catalog.pg_class c
            JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            WHERE n.nspname='tagekyc' AND c.relkind='r' AND c.relname IN
              ('raw_export_recipient_key_registrations','raw_export_recipient_package_preparations','raw_export_recipient_package_events')
            """, connection);
        Assert.Equal(3L, (long)(await command.ExecuteScalarAsync())!);
        await using var db = postgres.CreateDbContext();
        Assert.NotNull(db.Model.FindEntityType("TagEkyc.Infrastructure.Persistence.Entities.RawExportRecipientKeyRegistrationRow"));
        Assert.NotNull(db.Model.FindEntityType("TagEkyc.Infrastructure.Persistence.Entities.RawExportRecipientPackagePreparationRow"));
        Assert.NotNull(db.Model.FindEntityType("TagEkyc.Infrastructure.Persistence.Entities.RawExportRecipientPackageEventRow"));
        var migration = MigrationSource();
        Assert.DoesNotContain("DropTable(", migration[..migration.IndexOf("protected override void Down", StringComparison.Ordinal)], StringComparison.Ordinal);
        Assert.Contains("fk_raw_export_recipient_package_attempt_fence", migration, StringComparison.Ordinal);
    }

    [Fact]
    public async Task C202_roles_memberships_table_function_owners_and_acl_are_exact()
    {
        var catalogField = typeof(RecipientPackageReadinessValidator).GetField(
            "CatalogSql", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(catalogField);
        var catalogSql = Assert.IsType<string>(catalogField!.GetRawConstantValue());
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(catalogSql, connection);
        var catalogOk = Assert.IsType<bool>(await command.ExecuteScalarAsync());
        string? catalogFailures = null;
        if (!catalogOk)
        {
            var diagnosticSql = catalogSql.Replace(
                """
                SELECT r.ok AND m.ok AND t.ok AND f.ok AND a.ok AND s.ok
                  AND pg_catalog.has_schema_privilege('tagekyc_raw_export_package_preparer','tagekyc','USAGE')
                  AND pg_catalog.has_schema_privilege('tagekyc_raw_export_package_reconciler','tagekyc','USAGE')
                  AND pg_catalog.has_schema_privilege('tagekyc_raw_export_package_lifecycle','tagekyc','USAGE')
                FROM role_ok r CROSS JOIN member_ok m CROSS JOIN table_ok t CROSS JOIN function_ok f
                  CROSS JOIN acl_ok a CROSS JOIN surface_ok s
                """,
                """
                SELECT pg_catalog.concat_ws(',',
                  CASE WHEN NOT r.ok THEN 'roles' END,
                  CASE WHEN NOT m.ok THEN 'memberships' END,
                  CASE WHEN NOT t.ok THEN 'table-owner-acl' END,
                  CASE WHEN NOT f.ok THEN 'function-signature-owner-config-acl' END,
                  CASE WHEN NOT a.ok THEN 'function-execute-acl' END,
                  CASE WHEN NOT s.ok THEN 'function-surface' END,
                  CASE
                    WHEN (SELECT pg_catalog.count(*) FROM pg_catalog.pg_roles
                          WHERE rolname IN ('tagekyc_raw_export_package_preparer',
                            'tagekyc_raw_export_package_reconciler','tagekyc_raw_export_package_lifecycle'))<>3
                      THEN 'schema-usage-roles-absent'
                    WHEN NOT (
                      pg_catalog.has_schema_privilege('tagekyc_raw_export_package_preparer','tagekyc','USAGE')
                      AND pg_catalog.has_schema_privilege('tagekyc_raw_export_package_reconciler','tagekyc','USAGE')
                      AND pg_catalog.has_schema_privilege('tagekyc_raw_export_package_lifecycle','tagekyc','USAGE'))
                      THEN 'schema-usage'
                  END)
                FROM role_ok r CROSS JOIN member_ok m CROSS JOIN table_ok t CROSS JOIN function_ok f
                  CROSS JOIN acl_ok a CROSS JOIN surface_ok s
                """,
                StringComparison.Ordinal);
            Assert.NotEqual(catalogSql, diagnosticSql);
            await using var diagnostic = new NpgsqlCommand(diagnosticSql, connection);
            catalogFailures = Assert.IsType<string>(await diagnostic.ExecuteScalarAsync());
        }
        Assert.True(catalogOk, $"C202_CATALOG_FAILURES={catalogFailures}");

        await using var counts = new NpgsqlCommand("""
            SELECT
              (SELECT pg_catalog.count(*) FROM pg_catalog.pg_roles
               WHERE rolname IN (
                 'tagekyc_raw_export_package_preparer','tagekyc_raw_export_package_reconciler','tagekyc_raw_export_package_lifecycle',
                 'tagekyc_raw_export_package_preparer_login','tagekyc_raw_export_package_reconciler_login','tagekyc_raw_export_package_lifecycle_login')),
              (SELECT pg_catalog.count(*) FROM pg_catalog.pg_proc p
               JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
               WHERE n.nspname='tagekyc' AND p.proname IN (
                 'raw_export_reserve_recipient_package','raw_export_begin_recipient_package_put',
                 'raw_export_record_recipient_package_put_unknown','raw_export_record_recipient_package_prepared',
                 'raw_export_read_recipient_package_recovery_context','raw_export_finalize_recipient_package',
                 'raw_export_authorize_recipient_package_abort','raw_export_record_recipient_package_abort_result',
                 'raw_export_record_recipient_package_quarantined')),
              (SELECT pg_catalog.count(*) FROM pg_catalog.pg_class c
               JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
               WHERE n.nspname='tagekyc' AND c.relkind='r' AND c.relname IN
                 ('raw_export_recipient_key_registrations','raw_export_recipient_package_preparations','raw_export_recipient_package_events'))
            """, connection);
        await using var reader = await counts.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(6L, reader.GetInt64(0));
        Assert.Equal(9L, reader.GetInt64(1));
        Assert.Equal(3L, reader.GetInt64(2));
    }

    [Fact]
    public async Task C204_C1_contract_hands_off_exact_server_derived_recipient()
    {
        var property = typeof(C2AssemblyPreparationRequest).GetProperty("RecipientClientApplicationId");
        Assert.NotNull(property);
        Assert.Equal(typeof(Guid), property!.PropertyType);

        var repository = new RecipientPackageRepository(new RoleConnectionFactory(postgres.ConnectionString));
        TestObjectStore? objectStore = null;
        var execution = await new Tip88C1C1ResolverAssemblyTests(postgres).ExecuteWithRealC2ProviderAsync(
            async (jobId, recipientId) =>
            {
                using var recipientKey = RSA.Create(3072);
                await InsertRecipientKeyAsync(
                    recipientId,
                    $"c204-{Guid.NewGuid():N}",
                    recipientKey.ExportSubjectPublicKeyInfo());
                objectStore = new TestObjectStore(_ => new(
                    RecipientPackageInspectionOutcome.PositivelyAbsent,
                    null,
                    null,
                    null,
                    null))
                {
                    PutOutcome = RecipientPackagePutOutcome.Created,
                    BeforePut = async () =>
                    {
                        await using var db = postgres.CreateDbContext();
                        var preparation = await db.RawExportAssemblyPreparationDispositions.AsNoTracking()
                            .SingleAsync(row => row.JobId == jobId);
                        Assert.Equal("Preparing", preparation.Disposition);
                        Assert.Equal(0, await db.RawExportAssemblyIdentities.AsNoTracking()
                            .CountAsync(row => row.JobId == jobId));
                    },
                };
                return new RecipientPackagePreparationProvider(
                    new RecipientPackageOptions(RecipientPackageTopology.S3CompatibleDurable, TestProvider(), true),
                    repository,
                    new RawExportAssemblyRepository(new AssemblyRoleConnectionFactory(postgres.ConnectionString)),
                    new RecipientPackageCryptoService(),
                    objectStore,
                    objectStore,
                    objectStore);
            });

        Assert.Equal(RawExportAssemblyExecutionOutcome.Sealed, execution.Result.Outcome);
        Assert.NotNull(execution.Result.C2PreparationId);
        Assert.NotNull(objectStore);
        Assert.Equal(1, objectStore!.PutCount);
        await using var assertion = postgres.CreateDbContext();
        var c1 = await assertion.RawExportAssemblyPreparationDispositions.AsNoTracking()
            .SingleAsync(row => row.C2PreparationId == execution.Result.C2PreparationId);
        var c2 = await assertion.RawExportRecipientPackagePreparations.AsNoTracking()
            .SingleAsync(row => row.C2PreparationId == execution.Result.C2PreparationId);
        Assert.Equal("Finalized", c1.Disposition);
        Assert.Equal("Finalized", c2.State);
        Assert.Equal(execution.JobId, c2.JobId);
        Assert.Equal(execution.AttemptId, c2.AttemptId);
        Assert.Equal(execution.FencingToken, c2.FencingToken);
        Assert.Equal(execution.RecipientClientApplicationId, c2.RecipientClientApplicationId);
        Assert.Equal(
            ["SnapshotFrozen", "PutStarted", "Prepared", "Finalized"],
            await assertion.RawExportRecipientPackageEvents.AsNoTracking()
                .Where(row => row.C2PreparationId == execution.Result.C2PreparationId)
                .OrderBy(row => row.EventRevision)
                .Select(row => row.EventKind)
                .ToArrayAsync());
    }

    [Fact]
    public void C206_envelope_frame_and_completion_absolute_vector_is_byte_exact()
    {
        var equality = Convert.FromHexString("556411f661e7d2f0a32334a6666f0a9873cdcdb5c9e5cc9f8be5267ba09df1a8");
        var header = new RecipientPackageHeader(
            Guid.ParseExact(new string('3', 32), "N"), Bytes("66", 32), Bytes("44", 32),
            Guid.ParseExact(new string('1', 32), "N"), 5, Enumerable.Range(1, 8).Select(v => (byte)v).ToArray(),
            equality, Guid.Parse("ad8b18c9-0119-5c71-bf0b-208e69cd6cba"), Bytes("cc", 32),
            Guid.ParseExact(new string('7', 32), "N"), Bytes("88", 32), "recipient-rsa-2026-v1", 1,
            Enumerable.Range(0, 256).Select(v => (byte)v).Concat(Enumerable.Range(0, 128).Select(v => (byte)v)).ToArray());
        var envelope = RecipientPackageCodec.Envelope(header, out var digest);
        Assert.Equal(1580, envelope.Length);
        Assert.Equal("413E5EC59723A40B1325C775F31A48095FC24575F5350E8054BCCDF9A36DE609", Convert.ToHexString(digest));
        var cek = Enumerable.Range(0, 32).Select(v => (byte)v).ToArray();
        using var aes = new AesGcm(cek, 16);
        var ciphertext = new byte[5]; var tag = new byte[16];
        aes.Encrypt(Convert.FromHexString("010203040506070800000000"), "hello"u8, ciphertext, tag,
            RecipientPackageCodec.FrameAad(digest, 0, 5));
        Assert.Equal("E27A42DCE2", Convert.ToHexString(ciphertext));
        Assert.Equal("501E8236A7BF630DE24DBB71AD220BE5", Convert.ToHexString(tag));
        var completion = new byte[16];
        aes.Encrypt(Convert.FromHexString("0102030405060708FFFFFFFF"), ReadOnlySpan<byte>.Empty, Span<byte>.Empty, completion,
            RecipientPackageCodec.CompletionAad(digest, 1, 5));
        Assert.Equal("0FB61C1BBCD0DFF0CCCF60B366EA4CF7", Convert.ToHexString(completion));
    }

    [Fact]
    public async Task C210_single_part_conditional_create_uses_final_key_without_prefix_rewrite()
    {
        Assert.DoesNotContain("Prefix", typeof(RecipientPackageProviderConfiguration).GetProperties().Select(p => p.Name));
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var configuration = minio.RecipientPackageConfiguration();
        var options = new RecipientPackageOptions(RecipientPackageTopology.S3CompatibleDurable, configuration, true);
        using var factory = new RecipientPackageObjectClientFactory(options);
        using var provider = new S3CompatibleRecipientPackageProvider(options, factory);
        var packageId = Guid.NewGuid();
        var objectKey = RecipientPackageCodec.ObjectKey(packageId);
        var endpoint = RecipientPackageCodec.ProviderEndpointFingerprint(configuration);
        var binding = RecipientPackageCodec.ObjectBindingDigest(
            configuration.ProviderConfigurationId, endpoint, configuration.BucketName, objectKey);
        var locator = new RecipientPackageLocator(
            RecipientPackageProviderConfiguration.ProviderKind,
            configuration.ProviderConfigurationId,
            endpoint,
            configuration.BucketName,
            objectKey,
            binding);
        var packageBytes = Encoding.ASCII.GetBytes(RecipientPackageCodec.MagicText)
            .Concat(new byte[] { 0, 0, 0, 2, (byte)'{', (byte)'}' }).ToArray();

        await using var firstSpool = new RecipientPackageEncryptedSpool();
        await firstSpool.WriteAsync(packageBytes);
        firstSpool.Seal();
        var first = await provider.PutIfAbsentAsync(locator, firstSpool, CancellationToken.None);
        Assert.Equal(RecipientPackagePutOutcome.Created, first.Outcome);
        Assert.Equal(packageBytes.Length, first.ContentLength);

        await using var replaySpool = new RecipientPackageEncryptedSpool();
        await replaySpool.WriteAsync(packageBytes);
        replaySpool.Seal();
        var replay = await provider.PutIfAbsentAsync(locator, replaySpool, CancellationToken.None);
        Assert.Equal(RecipientPackagePutOutcome.ConditionalConflict, replay.Outcome);
        var inspected = await provider.InspectAsync(locator, CancellationToken.None);
        Assert.Equal(RecipientPackageInspectionOutcome.Present, inspected.Outcome);
        Assert.Equal(packageBytes.Length, inspected.ContentLength);
        Assert.Equal(SHA256.HashData(packageBytes), inspected.PackageCiphertextDigest);
        Assert.Equal(SHA256.HashData(packageBytes), inspected.EnvelopeDigest);
        Assert.Equal($"raw-export/c2-package/v1/{packageId:N}", objectKey);
        Assert.Equal(RecipientPackageDeleteOutcome.DeletedAcknowledged,
            await provider.DeleteAsync(locator, CancellationToken.None));

        var driftFixture = await CreateReservedPackageAsync();
        var driftEnvelope = RandomNumberGenerator.GetBytes(32);
        var driftCiphertext = RandomNumberGenerator.GetBytes(32);
        var driftArmed = await driftFixture.Repository.BeginPutAsync(
            driftFixture.Request.C2PreparationId,
            driftFixture.First.RowRevision!.Value,
            driftEnvelope,
            2_048,
            driftCiphertext,
            CancellationToken.None);
        Assert.Equal("PutInFlight", driftArmed.Outcome);
        var providerB = TestProvider() with { ProviderConfigurationId = "c2-integration-minio-v2" };
        var providerBStore = new TestObjectStore(_ => throw new InvalidOperationException("PROVIDER_B_MUST_NOT_BE_CALLED"));
        var restartedWithDrift = CreatePreparationProvider(driftFixture, providerBStore, providerB);
        var driftResult = await restartedWithDrift.PrepareAsync(
            driftFixture.PreparationRequest,
            (_, _) => throw new InvalidOperationException("ASSEMBLY_MUST_NOT_BE_REOPENED"),
            CancellationToken.None);
        Assert.Equal(C2AssemblyPrepareOutcome.Conflict, driftResult.Outcome);
        Assert.Equal(0, providerBStore.PutCount);
        Assert.Equal(0, providerBStore.InspectCount);
        Assert.Equal(0, providerBStore.DeleteCount);
        var quarantined = await driftFixture.Repository.ReadAsync(
            RecipientPackageDatabaseCapability.Reconciler,
            driftFixture.Request.C2PreparationId,
            CancellationToken.None);
        Assert.NotNull(quarantined);
        Assert.Equal("Quarantined", quarantined!.State);
        Assert.NotNull(quarantined.QuarantineEvidenceDigest);
    }

    [Fact]
    public async Task C211_durable_replay_and_equality_conflict_precede_generic_revision_rejection()
    {
        var fixture = await CreateReservedPackageAsync();
        Assert.Equal("Reserved", fixture.First.Outcome);
        Assert.Equal(1, fixture.First.RowRevision);

        var replay = await fixture.Repository.ReserveAsync(fixture.Request, CancellationToken.None);
        Assert.Equal("ExistingMatch", replay.Outcome);
        Assert.Equal(fixture.First.RowRevision, replay.RowRevision);
        Assert.Equal(fixture.First.ProviderOperationTokenDigest, replay.ProviderOperationTokenDigest);

        var alteredEquality = fixture.Request.PackageEqualityFingerprint.ToArray();
        alteredEquality[0] ^= 1;
        var conflict = await fixture.Repository.ReserveAsync(
            fixture.Request with { PackageEqualityFingerprint = alteredEquality }, CancellationToken.None);
        Assert.Equal("Conflict", conflict.Outcome);

        await using var db = postgres.CreateDbContext();
        Assert.Equal(1, await db.RawExportRecipientPackagePreparations.CountAsync(
            row => row.C2PreparationId == fixture.Request.C2PreparationId));
        Assert.Equal(1, await db.RawExportRecipientPackageEvents.CountAsync(
            row => row.C2PreparationId == fixture.Request.C2PreparationId));
    }

    [Fact]
    public async Task C212_key_selection_revoke_and_reserve_guard_are_durable()
    {
        var fixture = await CreateReservedPackageAsync();
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using (var revoke = new NpgsqlCommand("""
            UPDATE tagekyc.raw_export_recipient_key_registrations
            SET "State"='Revoked',"Revision"=2,"RevokedAtUtc"=pg_catalog.clock_timestamp()
            WHERE "RecipientClientApplicationId"=@recipient AND "RecipientKeyId"=@keyId AND "RecipientKeyVersion"=@keyVersion
            """, connection))
        {
            revoke.Parameters.AddWithValue("recipient", fixture.Request.RecipientClientApplicationId);
            revoke.Parameters.AddWithValue("keyId", fixture.Request.RecipientKeyId);
            revoke.Parameters.AddWithValue("keyVersion", fixture.Request.RecipientKeyVersion);
            await revoke.ExecuteNonQueryAsync();
        }
        var replay = await fixture.Repository.ReserveAsync(fixture.Request, CancellationToken.None);
        Assert.Equal("ExistingMatch", replay.Outcome);
        Assert.Equal(fixture.Request.RecipientKeyId, replay.RecipientKeyId);

        var secondLineage = await new Tip88C1C1ResolverAssemblyTests(postgres).CreateC2RecipientPackageLineageAsync();
        var notFrozen = BuildReserveRequest(secondLineage, fixture.Key, RandomNumberGenerator.GetBytes(32));
        var unavailable = await fixture.Repository.ReserveAsync(notFrozen, CancellationToken.None);
        Assert.Equal("Unavailable", unavailable.Outcome);
        await using var db = postgres.CreateDbContext();
        Assert.Equal(0, await db.RawExportRecipientPackagePreparations.CountAsync(
            row => row.C2PreparationId == secondLineage.Request.C2PreparationId));

        var mismatchedLineage = await new Tip88C1C1ResolverAssemblyTests(postgres).CreateC2RecipientPackageLineageAsync();
        using var mismatchedKey = RSA.Create(3072);
        var mismatchedSpki = mismatchedKey.ExportSubjectPublicKeyInfo();
        var wrongFingerprint = SHA256.HashData(mismatchedSpki);
        wrongFingerprint[0] ^= 1;
        await InsertRecipientKeyAsync(
            mismatchedLineage.Request.RecipientClientApplicationId,
            $"c212-mismatch-{Guid.NewGuid():N}",
            mismatchedSpki,
            wrongFingerprint);
        var mismatchStore = new TestObjectStore(_ => throw new InvalidOperationException("PROVIDER_MUST_NOT_BE_CALLED"));
        var mismatchProvider = CreatePreparationProvider(
            new RecipientPackageRepository(new RoleConnectionFactory(postgres.ConnectionString)),
            mismatchStore);
        var mismatch = await mismatchProvider.PrepareAsync(
            mismatchedLineage.Request,
            (_, _) => throw new InvalidOperationException("ASSEMBLY_MUST_NOT_BE_OPENED"),
            CancellationToken.None);
        Assert.Equal(C2AssemblyPrepareOutcome.Unavailable, mismatch.Outcome);
        Assert.Equal(0, mismatchStore.PutCount);
        Assert.Equal(0, mismatchStore.InspectCount);
        Assert.Equal(0, await db.RawExportRecipientPackagePreparations.CountAsync(
            row => row.C2PreparationId == mismatchedLineage.Request.C2PreparationId));
    }

    [Fact]
    public async Task C213_prepared_finalize_and_exact_finalized_replay_are_guarded()
    {
        var fixture = await CreateReservedPackageAsync();
        var envelope = RandomNumberGenerator.GetBytes(32);
        var ciphertext = RandomNumberGenerator.GetBytes(32);
        var armed = await fixture.Repository.BeginPutAsync(
            fixture.Request.C2PreparationId, fixture.First.RowRevision!.Value,
            envelope, 1_024, ciphertext, CancellationToken.None);
        Assert.Equal("PutInFlight", armed.Outcome);

        var prepared = await fixture.Repository.RecordPreparedAsync(
            RecipientPackageDatabaseCapability.Preparer,
            fixture.Request.C2PreparationId, armed.RowRevision!.Value,
            RandomNumberGenerator.GetBytes(32), 1_024, ciphertext,
            RandomNumberGenerator.GetBytes(32), RandomNumberGenerator.GetBytes(32),
            CancellationToken.None);
        Assert.Equal("Prepared", prepared.Outcome);

        var finalized = await fixture.Repository.FinalizeAsync(
            fixture.Request.C2PreparationId, prepared.RowRevision!.Value,
            fixture.Request.AssemblyFingerprint, CancellationToken.None);
        Assert.Equal("Finalized", finalized.Outcome);
        Assert.Equal(4, finalized.RowRevision);

        var replay = await fixture.Repository.FinalizeAsync(
            fixture.Request.C2PreparationId, prepared.RowRevision.Value,
            fixture.Request.AssemblyFingerprint, CancellationToken.None);
        Assert.Equal("ExistingMatch", replay.Outcome);
        Assert.Equal(finalized.RowRevision, replay.RowRevision);

        var wrongAssembly = fixture.Request.AssemblyFingerprint.ToArray();
        wrongAssembly[0] ^= 1;
        var conflict = await fixture.Repository.FinalizeAsync(
            fixture.Request.C2PreparationId, prepared.RowRevision.Value,
            wrongAssembly, CancellationToken.None);
        Assert.Equal("Conflict", conflict.Outcome);

        var row = await fixture.Repository.ReadAsync(
            RecipientPackageDatabaseCapability.Preparer,
            fixture.Request.C2PreparationId, CancellationToken.None);
        Assert.NotNull(row);
        Assert.Equal("Finalized", row!.State);
        Assert.Equal(4, row.RowRevision);
        Assert.Equal(fixture.Request.AssemblyFingerprint, row.AssemblyFingerprint);
    }

    [Fact]
    public async Task C214_abort_does_not_admit_armed_put_and_requires_positive_absence_completion()
    {
        var safe = await CreateReservedPackageAsync();
        var absent = new TestObjectStore(_ => new(
            RecipientPackageInspectionOutcome.PositivelyAbsent, null, null, null, null));
        var safeProvider = CreatePreparationProvider(safe, absent);
        var authorization = RandomNumberGenerator.GetBytes(32);
        var aborted = await safeProvider.AbortAsync(
            safe.Request.C2PreparationId, authorization, CancellationToken.None);
        Assert.Equal(C2AssemblyAbortOutcome.Aborted, aborted.Outcome);
        Assert.Equal(2, absent.InspectCount);
        Assert.Equal(0, absent.DeleteCount);
        Assert.Equal(0, absent.PutCount);
        var abortedRow = await safe.Repository.ReadAsync(
            RecipientPackageDatabaseCapability.Lifecycle, safe.Request.C2PreparationId, CancellationToken.None);
        Assert.NotNull(abortedRow);
        Assert.Equal("Aborted", abortedRow!.State);
        Assert.NotNull(abortedRow.PositiveAbsenceEvidenceDigest);

        var armedFixture = await CreateReservedPackageAsync();
        var armed = await armedFixture.Repository.BeginPutAsync(
            armedFixture.Request.C2PreparationId, armedFixture.First.RowRevision!.Value,
            RandomNumberGenerator.GetBytes(32), 1_024, RandomNumberGenerator.GetBytes(32),
            CancellationToken.None);
        Assert.Equal("PutInFlight", armed.Outcome);
        var forbiddenStore = new TestObjectStore(_ => throw new InvalidOperationException("OBJECT_IO_MUST_NOT_RUN"));
        var armedProvider = CreatePreparationProvider(armedFixture, forbiddenStore);
        var rejected = await armedProvider.AbortAsync(
            armedFixture.Request.C2PreparationId, authorization, CancellationToken.None);
        Assert.Equal(C2AssemblyAbortOutcome.Conflict, rejected.Outcome);
        Assert.Equal(0, forbiddenStore.InspectCount);
        Assert.Equal(0, forbiddenStore.DeleteCount);
        var armedRow = await armedFixture.Repository.ReadAsync(
            RecipientPackageDatabaseCapability.Lifecycle, armedFixture.Request.C2PreparationId, CancellationToken.None);
        Assert.Equal("PutInFlight", armedRow!.State);
        Assert.Null(armedRow.AbortAuthorizationDigest);

        var authorizedFixture = await CreateReservedPackageAsync();
        var authorized = await authorizedFixture.Repository.AuthorizeAbortAsync(
            authorizedFixture.Request.C2PreparationId,
            authorizedFixture.First.RowRevision!.Value,
            authorization,
            CancellationToken.None);
        Assert.Equal("AbortAuthorized", authorized.Outcome);
        var authorizedRow = await authorizedFixture.Repository.ReadAsync(
            RecipientPackageDatabaseCapability.Lifecycle,
            authorizedFixture.Request.C2PreparationId, CancellationToken.None);
        Assert.Equal("AbortAuthorized", authorizedRow!.State);

        var cleanupFixture = await CreateReservedPackageAsync();
        var cleanupEnvelope = RandomNumberGenerator.GetBytes(32);
        var cleanupCiphertext = RandomNumberGenerator.GetBytes(32);
        var cleanupArmed = await cleanupFixture.Repository.BeginPutAsync(
            cleanupFixture.Request.C2PreparationId, cleanupFixture.First.RowRevision!.Value,
            cleanupEnvelope, 2_048, cleanupCiphertext, CancellationToken.None);
        var cleanupPrepared = await cleanupFixture.Repository.RecordPreparedAsync(
            RecipientPackageDatabaseCapability.Preparer,
            cleanupFixture.Request.C2PreparationId, cleanupArmed.RowRevision!.Value,
            RandomNumberGenerator.GetBytes(32), 2_048, cleanupCiphertext,
            RandomNumberGenerator.GetBytes(32), RandomNumberGenerator.GetBytes(32),
            CancellationToken.None);
        Assert.Equal("Prepared", cleanupPrepared.Outcome);
        var cleanupStore = new TestObjectStore(_ => new(
            RecipientPackageInspectionOutcome.Present, 2_048, cleanupCiphertext, cleanupEnvelope, "etag-cleanup"))
        {
            DeleteOutcome = RecipientPackageDeleteOutcome.OutcomeUnknown,
        };
        var cleanupProvider = CreatePreparationProvider(cleanupFixture, cleanupStore);
        var cleanup = await cleanupProvider.AbortAsync(
            cleanupFixture.Request.C2PreparationId, authorization, CancellationToken.None);
        Assert.Equal(C2AssemblyAbortOutcome.OutcomeUnknown, cleanup.Outcome);
        Assert.Equal(1, cleanupStore.InspectCount);
        Assert.Equal(1, cleanupStore.DeleteCount);
        var cleanupRow = await cleanupFixture.Repository.ReadAsync(
            RecipientPackageDatabaseCapability.Lifecycle,
            cleanupFixture.Request.C2PreparationId, CancellationToken.None);
        Assert.Equal("CleanupPending", cleanupRow!.State);
        Assert.NotNull(cleanupRow.CleanupProgressEvidenceDigest);
    }

    [Fact]
    public async Task C215_conflicting_object_evidence_has_quarantine_not_overwrite_or_delete_transition()
    {
        var fixture = await CreateReservedPackageAsync();
        var envelope = RandomNumberGenerator.GetBytes(32);
        var ciphertext = RandomNumberGenerator.GetBytes(32);
        var armed = await fixture.Repository.BeginPutAsync(
            fixture.Request.C2PreparationId, fixture.First.RowRevision!.Value,
            envelope, 2_048, ciphertext, CancellationToken.None);
        Assert.Equal("PutInFlight", armed.Outcome);
        var wrongDigest = ciphertext.ToArray();
        wrongDigest[0] ^= 1;
        var store = new TestObjectStore(_ => new(
            RecipientPackageInspectionOutcome.Present, 2_048, wrongDigest, envelope, "etag-conflict"));
        var provider = CreatePreparationProvider(fixture, store);

        var result = await provider.PrepareAsync(
            fixture.PreparationRequest,
            (_, _) => throw new InvalidOperationException("ASSEMBLY_MUST_NOT_BE_REOPENED"),
            CancellationToken.None);
        Assert.Equal(C2AssemblyPrepareOutcome.Conflict, result.Outcome);
        Assert.Equal(1, store.InspectCount);
        Assert.Equal(0, store.PutCount);
        Assert.Equal(0, store.DeleteCount);
        var row = await fixture.Repository.ReadAsync(
            RecipientPackageDatabaseCapability.Reconciler,
            fixture.Request.C2PreparationId, CancellationToken.None);
        Assert.NotNull(row);
        Assert.Equal("Quarantined", row!.State);
        Assert.NotNull(row.QuarantineEvidenceDigest);
        Assert.Equal(ciphertext, row.PackageCiphertextDigest);
    }

    [Fact]
    public async Task C216_armed_put_has_no_reset_or_fresh_CEK_transition()
    {
        var fixture = await CreateReservedPackageAsync();
        var envelope = RandomNumberGenerator.GetBytes(32);
        var ciphertext = RandomNumberGenerator.GetBytes(32);
        var armed = await fixture.Repository.BeginPutAsync(
            fixture.Request.C2PreparationId, fixture.First.RowRevision!.Value,
            envelope, 4_096, ciphertext, CancellationToken.None);
        Assert.Equal("PutInFlight", armed.Outcome);
        var tokenDigest = fixture.First.ProviderOperationTokenDigest!.ToArray();
        var exactStore = new TestObjectStore(_ => new(
            RecipientPackageInspectionOutcome.Present, 4_096, ciphertext, envelope, "etag-restart"));

        var restarted = CreatePreparationProvider(fixture, exactStore);
        var recovered = await restarted.PrepareAsync(
            fixture.PreparationRequest,
            (_, _) => throw new InvalidOperationException("ASSEMBLY_MUST_NOT_BE_REOPENED"),
            CancellationToken.None);
        Assert.Equal(C2AssemblyPrepareOutcome.Prepared, recovered.Outcome);
        Assert.Equal(1, exactStore.InspectCount);
        Assert.Equal(0, exactStore.PutCount);
        var recoveredRow = await fixture.Repository.ReadAsync(
            RecipientPackageDatabaseCapability.Reconciler,
            fixture.Request.C2PreparationId, CancellationToken.None);
        Assert.Equal("Prepared", recoveredRow!.State);
        Assert.Equal(tokenDigest, recoveredRow.ProviderOperationTokenDigest);
        Assert.Equal(ciphertext, recoveredRow.PackageCiphertextDigest);
        Assert.Equal(envelope, recoveredRow.EnvelopeDigest);

        var uncertainFixture = await CreateReservedPackageAsync();
        var uncertainEnvelope = RandomNumberGenerator.GetBytes(32);
        var uncertainCiphertext = RandomNumberGenerator.GetBytes(32);
        await uncertainFixture.Repository.BeginPutAsync(
            uncertainFixture.Request.C2PreparationId, uncertainFixture.First.RowRevision!.Value,
            uncertainEnvelope, 8_192, uncertainCiphertext, CancellationToken.None);
        var absentStore = new TestObjectStore(_ => new(
            RecipientPackageInspectionOutcome.PositivelyAbsent, null, null, null, null));
        var firstRestart = CreatePreparationProvider(uncertainFixture, absentStore);
        var unknown = await firstRestart.PrepareAsync(
            uncertainFixture.PreparationRequest,
            (_, _) => throw new InvalidOperationException("ASSEMBLY_MUST_NOT_BE_REOPENED"),
            CancellationToken.None);
        Assert.Equal(C2AssemblyPrepareOutcome.OutcomeUnknown, unknown.Outcome);
        var secondRestart = CreatePreparationProvider(uncertainFixture, absentStore);
        var stillUnknown = await secondRestart.PrepareAsync(
            uncertainFixture.PreparationRequest,
            (_, _) => throw new InvalidOperationException("ASSEMBLY_MUST_NOT_BE_REOPENED"),
            CancellationToken.None);
        Assert.Equal(C2AssemblyPrepareOutcome.OutcomeUnknown, stillUnknown.Outcome);
        Assert.Equal(0, absentStore.PutCount);
        var uncertainRow = await uncertainFixture.Repository.ReadAsync(
            RecipientPackageDatabaseCapability.Reconciler,
            uncertainFixture.Request.C2PreparationId, CancellationToken.None);
        Assert.Equal("PutOutcomeUnknown", uncertainRow!.State);
        Assert.Equal(uncertainFixture.First.ProviderOperationTokenDigest, uncertainRow.ProviderOperationTokenDigest);
        var abort = await secondRestart.AbortAsync(
            uncertainFixture.Request.C2PreparationId, RandomNumberGenerator.GetBytes(32), CancellationToken.None);
        Assert.Equal(C2AssemblyAbortOutcome.Conflict, abort.Outcome);
    }

    [Fact]
    public async Task C217_four_provider_credentials_and_clients_enforce_exact_allow_deny_matrix()
    {
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var configuration = minio.RecipientPackageConfiguration();
        Assert.Equal(4, new[]
        {
            configuration.Writer.AccessKeyId,
            configuration.Reconciler.AccessKeyId,
            configuration.Lifecycle.AccessKeyId,
            configuration.PostureProbe.AccessKeyId,
        }.Distinct(StringComparer.Ordinal).Count());

        using var writer = minio.CreateRecipientPackageClient("writer");
        using var reconciler = minio.CreateRecipientPackageClient("reconciler");
        using var lifecycle = minio.CreateRecipientPackageClient("lifecycle");
        using var posture = minio.CreateRecipientPackageClient("posture");
        var objectKey = RecipientPackageCodec.ObjectKey(Guid.NewGuid());
        var bytes = RandomNumberGenerator.GetBytes(64);

        await writer.PutObjectAsync(new PutObjectRequest
        {
            BucketName = minio.BucketName,
            Key = objectKey,
            InputStream = new MemoryStream(bytes, writable: false),
            AutoCloseStream = true,
        });
        await AssertDeniedAsync(() => writer.GetObjectMetadataAsync(minio.BucketName, objectKey));
        await AssertDeniedAsync(() => writer.PutObjectAsync(new PutObjectRequest
        {
            BucketName = minio.BucketName,
            Key = "raw-export/not-c2/forbidden",
            InputStream = new MemoryStream(bytes, writable: false),
            AutoCloseStream = true,
        }));

        var reconcilerRead = await reconciler.GetObjectMetadataAsync(minio.BucketName, objectKey);
        Assert.Equal(bytes.Length, reconcilerRead.ContentLength);
        await AssertDeniedAsync(() => reconciler.PutObjectAsync(new PutObjectRequest
        {
            BucketName = minio.BucketName,
            Key = RecipientPackageCodec.ObjectKey(Guid.NewGuid()),
            InputStream = new MemoryStream(bytes, writable: false),
            AutoCloseStream = true,
        }));
        await AssertDeniedAsync(() => reconciler.DeleteObjectAsync(minio.BucketName, objectKey));

        var lifecycleRead = await lifecycle.GetObjectMetadataAsync(minio.BucketName, objectKey);
        Assert.Equal(bytes.Length, lifecycleRead.ContentLength);
        await AssertDeniedAsync(() => lifecycle.PutObjectAsync(new PutObjectRequest
        {
            BucketName = minio.BucketName,
            Key = RecipientPackageCodec.ObjectKey(Guid.NewGuid()),
            InputStream = new MemoryStream(bytes, writable: false),
            AutoCloseStream = true,
        }));

        var versioning = await posture.GetBucketVersioningAsync(new GetBucketVersioningRequest
        {
            BucketName = minio.BucketName,
        });
        Assert.NotNull(versioning);
        await AssertDeniedAsync(() => posture.GetObjectMetadataAsync(minio.BucketName, objectKey));
        await AssertDeniedAsync(() => posture.DeleteObjectAsync(minio.BucketName, objectKey));

        var deleted = await lifecycle.DeleteObjectAsync(minio.BucketName, objectKey);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, deleted.HttpStatusCode);
        await AssertDeniedAsync(() => lifecycle.GetObjectMetadataAsync(minio.BucketName, "raw-export/not-c2/forbidden"));
    }

    [Fact]
    public async Task C220_all_landed_C1_tests_remain_active_and_two_pass_read_boundary_is_unchanged()
    {
        var paths = new[]
        {
            "tests/TagEkyc.IntegrationTests/Tip88C1C1ResolverAssemblyTests.cs",
            "tests/TagEkyc.ArchTests/Tip88C1C1ResolverAssemblyArchTests.cs",
            "tests/TagEkyc.UnitTests/Tip88C1C1AssemblyCodecTests.cs",
        };
        var tests = string.Join('\n', paths.Select(path => File.ReadAllText(ProjectPath(path))));
        Assert.Equal(31, Count(tests, "[Fact]"));
        Assert.DoesNotContain("Skip =", tests, StringComparison.Ordinal);
        await new Tip88C1C1ResolverAssemblyTests(postgres)
            .C119_two_pass_streaming_has_no_complete_plaintext_buffer_or_temp_file();
    }

    [Fact]
    public void C221_all_persisted_states_have_closed_provider_outcome_mapping()
    {
        Assert.Equal(C2AssemblyInspectionOutcome.Preparing, RecipientPackageOutcomeMapper.Inspection("Reserved"));
        Assert.Equal(C2AssemblyInspectionOutcome.Preparing, RecipientPackageOutcomeMapper.Inspection("PutInFlight"));
        Assert.Equal(C2AssemblyInspectionOutcome.Preparing, RecipientPackageOutcomeMapper.Inspection("PutOutcomeUnknown"));
        Assert.Equal(C2AssemblyInspectionOutcome.Prepared, RecipientPackageOutcomeMapper.Inspection("Prepared"));
        Assert.Equal(C2AssemblyInspectionOutcome.Finalized, RecipientPackageOutcomeMapper.Inspection("Finalized"));
        Assert.Equal(C2AssemblyInspectionOutcome.Preparing, RecipientPackageOutcomeMapper.Inspection("AbortAuthorized"));
        Assert.Equal(C2AssemblyInspectionOutcome.Preparing, RecipientPackageOutcomeMapper.Inspection("CleanupPending"));
        Assert.Equal(C2AssemblyInspectionOutcome.Aborted, RecipientPackageOutcomeMapper.Inspection("Aborted"));
        Assert.Equal(C2AssemblyInspectionOutcome.Conflict, RecipientPackageOutcomeMapper.Inspection("Quarantined"));
        Assert.Equal(C2AssemblyInspectionOutcome.OutcomeUnknown, RecipientPackageOutcomeMapper.Inspection("future"));
    }

    [Fact]
    public async Task C222_global_key_then_package_lock_order_has_no_reverse_edge()
    {
        var sql = MigrationSource();
        var reserve = Between(sql, "CREATE FUNCTION tagekyc.raw_export_reserve_recipient_package", "CREATE FUNCTION tagekyc.raw_export_begin_recipient_package_put");
        Assert.True(reserve.IndexOf("key_registrations x", StringComparison.Ordinal)
            < reserve.IndexOf("package_preparations x", StringComparison.Ordinal));
        Assert.DoesNotContain("package_preparations x", reserve[..reserve.IndexOf("key_registrations x", StringComparison.Ordinal)], StringComparison.Ordinal);

        var candidate = await CreatePackageCandidateAsync();
        var contenders = await Task.WhenAll(
            candidate.Repository.ReserveAsync(candidate.Request, CancellationToken.None),
            candidate.Repository.ReserveAsync(candidate.Request, CancellationToken.None));
        Assert.Equal(1, contenders.Count(result => result.Outcome == "Reserved"));
        Assert.Equal(1, contenders.Count(result => result.Outcome == "ExistingMatch"));
        Assert.All(contenders, result => Assert.Equal(1, result.RowRevision));
        Assert.Single(contenders.Select(result => Convert.ToHexString(result.ProviderOperationTokenDigest!))
            .Distinct(StringComparer.Ordinal));
        await using var db = postgres.CreateDbContext();
        Assert.Equal(1, await db.RawExportRecipientPackagePreparations.CountAsync(
            row => row.C2PreparationId == candidate.Request.C2PreparationId));
        Assert.Equal(1, await db.RawExportRecipientPackageEvents.CountAsync(
            row => row.C2PreparationId == candidate.Request.C2PreparationId));
    }

    [Fact]
    public async Task C223_readiness_first_code_order_is_exact_and_lock_precedes_versioning()
    {
        Assert.Equal(9, RecipientPackageReadinessValidator.Codes.Length);
        var durable = new RecipientPackageOptions(
            RecipientPackageTopology.S3CompatibleDurable, TestProvider(), true);
        await using var invalidDb = postgres.CreateDbContext();
        var emptyServices = new ServiceCollection().BuildServiceProvider();
        await AssertReadinessCodeAsync(
            RecipientPackageReadinessValidator.Codes[0],
            new RecipientPackageReadinessValidator(
                new RecipientPackageOptions(RecipientPackageTopology.Invalid, null, false), invalidDb, emptyServices));
        await AssertReadinessCodeAsync(
            RecipientPackageReadinessValidator.Codes[1],
            new RecipientPackageReadinessValidator(durable, invalidDb, emptyServices));

        var cases = new[]
        {
            (Code: 3, Probe: new FakePostureProbe(null)),
            (Code: 4, Probe: new FakePostureProbe(new(false, true, true, true, true))),
            (Code: 5, Probe: new FakePostureProbe(new(true, false, false, false, false))),
            (Code: 6, Probe: new FakePostureProbe(new(true, true, false, false, false))),
            (Code: 7, Probe: new FakePostureProbe(new(true, true, true, false, false))),
            (Code: 8, Probe: new FakePostureProbe(new(true, true, true, true, false))),
        };
        foreach (var scenario in cases)
        {
            await using var db = postgres.CreateDbContext();
            using var services = new ServiceCollection()
                .AddSingleton<IRecipientPackageConnectionFactory>(new RoleConnectionFactory(postgres.ConnectionString))
                .AddSingleton<IRecipientPackagePostureProbe>(scenario.Probe)
                .BuildServiceProvider();
            await AssertReadinessCodeAsync(
                RecipientPackageReadinessValidator.Codes[scenario.Code],
                new RecipientPackageReadinessValidator(durable, db, services));
        }

        await using var healthyDb = postgres.CreateDbContext();
        using var healthyServices = new ServiceCollection()
            .AddSingleton<IRecipientPackageConnectionFactory>(new RoleConnectionFactory(postgres.ConnectionString))
            .AddSingleton<IRecipientPackagePostureProbe>(new FakePostureProbe(new(true, true, true, true, true)))
            .BuildServiceProvider();
        await new RecipientPackageReadinessValidator(durable, healthyDb, healthyServices)
            .ValidateAsync(CancellationToken.None);
    }

    [Fact]
    public async Task C224_recipient_parser_rejects_truncation_duplication_reorder_tags_completion_and_assembly_digest()
    {
        var plaintext = RandomNumberGenerator.GetBytes(RecipientPackageOptions.FramePlaintextBytes + 17);
        using var recipient = RSA.Create(3072);
        var package = await EncryptRecipientPackageAsync(plaintext, SHA256.HashData(plaintext), recipient);
        Assert.Equal(plaintext, ParseRecipientPackage(package, recipient));

        var envelopeLength = EnvelopeLength(package);
        var firstLength = FrameLength(package, envelopeLength);
        var secondOffset = envelopeLength + firstLength;
        var secondLength = FrameLength(package, secondOffset);
        var completionOffset = secondOffset + secondLength;

        var truncated = package[..^1];
        Assert.ThrowsAny<Exception>(() => ParseRecipientPackage(truncated, recipient));

        var duplicated = package[..envelopeLength]
            .Concat(package.AsSpan(envelopeLength, firstLength).ToArray())
            .Concat(package.AsSpan(envelopeLength, firstLength).ToArray())
            .Concat(package.AsSpan(secondOffset).ToArray()).ToArray();
        Assert.ThrowsAny<Exception>(() => ParseRecipientPackage(duplicated, recipient));

        var reordered = package[..envelopeLength]
            .Concat(package.AsSpan(secondOffset, secondLength).ToArray())
            .Concat(package.AsSpan(envelopeLength, firstLength).ToArray())
            .Concat(package.AsSpan(completionOffset).ToArray()).ToArray();
        Assert.ThrowsAny<Exception>(() => ParseRecipientPackage(reordered, recipient));

        var badTag = package.ToArray();
        badTag[envelopeLength + firstLength - 1] ^= 1;
        Assert.ThrowsAny<Exception>(() => ParseRecipientPackage(badTag, recipient));

        var badCompletion = package.ToArray();
        badCompletion[^1] ^= 1;
        Assert.ThrowsAny<Exception>(() => ParseRecipientPackage(badCompletion, recipient));

        var wrongAssemblyDigest = SHA256.HashData(plaintext);
        wrongAssemblyDigest[0] ^= 1;
        var wrongAssemblyPackage = await EncryptRecipientPackageAsync(plaintext, wrongAssemblyDigest, recipient);
        var digestException = Assert.Throws<InvalidDataException>(
            () => ParseRecipientPackage(wrongAssemblyPackage, recipient));
        Assert.Equal("ASSEMBLY_DIGEST_MISMATCH", digestException.Message);

        var badMagic = package.ToArray();
        badMagic[0] ^= 1;
        Assert.Throws<InvalidDataException>(() => ParseRecipientPackage(badMagic, recipient));
        var oversized = Encoding.ASCII.GetBytes(RecipientPackageCodec.MagicText).Concat(new byte[4]).ToArray();
        BinaryPrimitives.WriteUInt32BigEndian(
            oversized.AsSpan(RecipientPackageCodec.MagicText.Length, 4),
            RecipientPackageOptions.MaximumHeaderLength + 1);
        Assert.Throws<InvalidDataException>(() => ParseRecipientPackage(oversized, recipient));
    }

    private static string MigrationSource() => File.ReadAllText(ProjectPath(
        "src/TagEkyc.Infrastructure/Persistence/Migrations/20260818120000_Tip88C1C2RecipientPackage.cs"));
    private static string Between(string value, string start, string end)
    {
        var first = value.IndexOf(start, StringComparison.Ordinal);
        var last = value.IndexOf(end, first, StringComparison.Ordinal);
        return value[first..last];
    }
    private static int Count(string value, string token) =>
        (value.Length - value.Replace(token, string.Empty, StringComparison.Ordinal).Length) / token.Length;
    private static byte[] Bytes(string value, int count) => Convert.FromHexString(string.Concat(Enumerable.Repeat(value, count)));
    private static async Task<byte[]> EncryptRecipientPackageAsync(
        byte[] plaintext,
        byte[] assemblyDigest,
        RSA recipient)
    {
        var spki = recipient.ExportSubjectPublicKeyInfo();
        var request = new C2AssemblyPreparationRequest(
            Guid.NewGuid(), Guid.NewGuid(), RandomNumberGenerator.GetBytes(32), RandomNumberGenerator.GetBytes(32),
            assemblyDigest, RandomNumberGenerator.GetBytes(32), plaintext.Length, Guid.NewGuid());
        var reservation = new RecipientPackageReserveResult(
            "Reserved", 1, "Reserved", Guid.NewGuid(), RandomNumberGenerator.GetBytes(32), "recipient-key", 1,
            SHA256.HashData(spki), spki, 1, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1),
            RandomNumberGenerator.GetBytes(32), RandomNumberGenerator.GetBytes(32), null);
        var encrypted = await new RecipientPackageCryptoService().EncryptAsync(
            request,
            reservation,
            async (destination, token) => await destination.WriteAsync(plaintext, token),
            CancellationToken.None);
        await using var spool = encrypted.Spool;
        await using var source = spool.OpenRead();
        using var output = new MemoryStream();
        await source.CopyToAsync(output);
        return output.ToArray();
    }

    private static byte[] ParseRecipientPackage(byte[] package, RSA recipient)
    {
        var magic = Encoding.ASCII.GetBytes(RecipientPackageCodec.MagicText);
        if (package.Length < magic.Length + 4 || !package.AsSpan(0, magic.Length).SequenceEqual(magic))
            throw new InvalidDataException("PACKAGE_MAGIC_INVALID");
        var headerLength = checked((int)BinaryPrimitives.ReadUInt32BigEndian(package.AsSpan(magic.Length, 4)));
        if (headerLength is < 1 or > RecipientPackageOptions.MaximumHeaderLength
            || package.Length < magic.Length + 4 + headerLength)
            throw new InvalidDataException("PACKAGE_HEADER_INVALID");
        var envelopeLength = magic.Length + 4 + headerLength;
        var envelopeDigest = SHA256.HashData(package.AsSpan(0, envelopeLength));
        using var document = JsonDocument.Parse(package.AsMemory(magic.Length + 4, headerLength));
        var root = document.RootElement;
        var noncePrefix = DecodeBase64Url(root.GetProperty("noncePrefix").GetString()!);
        var wrappedCek = DecodeBase64Url(root.GetProperty("wrappedCek").GetString()!);
        var assemblyDigest = Convert.FromHexString(root.GetProperty("assemblyDigest").GetString()!);
        var expectedLength = root.GetProperty("completeAssemblyLength").GetInt64();
        var cek = recipient.Decrypt(wrappedCek, RSAEncryptionPadding.OaepSHA256);
        var nonce = new byte[12];
        try
        {
            using var aes = new AesGcm(cek, 16);
            using var plaintext = new MemoryStream();
            var offset = envelopeLength;
            var expectedOrdinal = 0;
            while (true)
            {
                if (package.Length - offset < 4) throw new InvalidDataException("PACKAGE_TRUNCATED");
                var ordinal = BinaryPrimitives.ReadUInt32BigEndian(package.AsSpan(offset, 4));
                if (ordinal == uint.MaxValue)
                {
                    if (package.Length - offset != 32) throw new InvalidDataException("COMPLETION_INVALID");
                    var completeLength = checked((long)BinaryPrimitives.ReadUInt64BigEndian(package.AsSpan(offset + 4, 8)));
                    var frameCount = checked((int)BinaryPrimitives.ReadUInt32BigEndian(package.AsSpan(offset + 12, 4)));
                    if (frameCount != expectedOrdinal || completeLength != plaintext.Length || completeLength != expectedLength)
                        throw new InvalidDataException("COMPLETION_COUNTS_INVALID");
                    nonce.AsSpan().Clear();
                    noncePrefix.AsSpan().CopyTo(nonce);
                    BinaryPrimitives.WriteUInt32BigEndian(nonce.AsSpan(8), uint.MaxValue);
                    var aad = RecipientPackageCodec.CompletionAad(envelopeDigest, frameCount, completeLength);
                    aes.Decrypt(nonce, ReadOnlySpan<byte>.Empty, package.AsSpan(offset + 16, 16), Span<byte>.Empty, aad);
                    var clear = plaintext.ToArray();
                    if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(clear), assemblyDigest))
                        throw new InvalidDataException("ASSEMBLY_DIGEST_MISMATCH");
                    return clear;
                }
                if (ordinal != expectedOrdinal || package.Length - offset < 8)
                    throw new InvalidDataException("FRAME_ORDINAL_INVALID");
                var length = checked((int)BinaryPrimitives.ReadUInt32BigEndian(package.AsSpan(offset + 4, 4)));
                if (length is < 1 or > RecipientPackageOptions.FramePlaintextBytes
                    || package.Length - offset < 8 + length + 16)
                    throw new InvalidDataException("FRAME_LENGTH_INVALID");
                var clearFrame = new byte[length];
                nonce.AsSpan().Clear();
                noncePrefix.AsSpan().CopyTo(nonce);
                BinaryPrimitives.WriteUInt32BigEndian(nonce.AsSpan(8), ordinal);
                var frameAad = RecipientPackageCodec.FrameAad(envelopeDigest, expectedOrdinal, length);
                aes.Decrypt(nonce, package.AsSpan(offset + 8, length),
                    package.AsSpan(offset + 8 + length, 16), clearFrame, frameAad);
                plaintext.Write(clearFrame);
                CryptographicOperations.ZeroMemory(clearFrame);
                offset += 8 + length + 16;
                expectedOrdinal++;
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(cek);
            CryptographicOperations.ZeroMemory(wrappedCek);
            CryptographicOperations.ZeroMemory(noncePrefix);
            CryptographicOperations.ZeroMemory(nonce);
            CryptographicOperations.ZeroMemory(envelopeDigest);
        }
    }

    private static int EnvelopeLength(byte[] package) => RecipientPackageCodec.MagicText.Length + 4
        + checked((int)BinaryPrimitives.ReadUInt32BigEndian(
            package.AsSpan(RecipientPackageCodec.MagicText.Length, 4)));

    private static int FrameLength(byte[] package, int offset) => 8
        + checked((int)BinaryPrimitives.ReadUInt32BigEndian(package.AsSpan(offset + 4, 4))) + 16;

    private static byte[] DecodeBase64Url(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/')
            + new string('=', (4 - value.Length % 4) % 4);
        return Convert.FromBase64String(padded);
    }
    private static async Task AssertDeniedAsync(Func<Task> action)
    {
        var exception = await Assert.ThrowsAsync<AmazonS3Exception>(action);
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, exception.StatusCode);
    }
    private static async Task AssertReadinessCodeAsync(
        string expected,
        RecipientPackageReadinessValidator validator)
    {
        var exception = await Assert.ThrowsAsync<RecipientPackageReadinessException>(
            () => validator.ValidateAsync(CancellationToken.None));
        Assert.Equal(expected, exception.Code);
    }
    private async Task<ReservedPackageFixture> CreateReservedPackageAsync()
    {
        var candidate = await CreatePackageCandidateAsync();
        var first = await candidate.Repository.ReserveAsync(candidate.Request, CancellationToken.None);
        return new(candidate.Repository, candidate.PreparationRequest, candidate.Request, first, candidate.Key);
    }

    private async Task<PackageCandidateFixture> CreatePackageCandidateAsync()
    {
        var lineage = await new Tip88C1C1ResolverAssemblyTests(postgres).CreateC2RecipientPackageLineageAsync();
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using (var revokeExisting = new NpgsqlCommand("""
            UPDATE tagekyc.raw_export_recipient_key_registrations
            SET "State"='Revoked',"Revision"="Revision"+1,"RevokedAtUtc"=pg_catalog.clock_timestamp()
            WHERE "RecipientClientApplicationId"=@recipient AND "State"='Active'
            """, connection))
        {
            revokeExisting.Parameters.AddWithValue("recipient", lineage.Request.RecipientClientApplicationId);
            await revokeExisting.ExecuteNonQueryAsync();
        }
        using var rsa = RSA.Create(3072);
        var key = new RecipientKeyFixture(
            $"key-{Guid.NewGuid():N}", 1, rsa.ExportSubjectPublicKeyInfo(), 1);
        key = key with { Fingerprint = SHA256.HashData(key.Spki) };
        await using (var insert = new NpgsqlCommand("""
            INSERT INTO tagekyc.raw_export_recipient_key_registrations(
              "RecipientClientApplicationId","RecipientKeyId","RecipientKeyVersion","PublicKeyAlgorithm","PublicKeySpki",
              "PublicKeyFingerprint","ValidFromUtc","ValidUntilUtc","State","Revision","RegisteredAtUtc")
            VALUES(@recipient,@keyId,@keyVersion,'RSA-OAEP-256',@spki,@fingerprint,pg_catalog.clock_timestamp()-interval '1 minute',
              pg_catalog.clock_timestamp()+interval '1 hour','Active',@revision,pg_catalog.clock_timestamp())
            """, connection))
        {
            insert.Parameters.AddWithValue("recipient", lineage.Request.RecipientClientApplicationId);
            insert.Parameters.AddWithValue("keyId", key.KeyId);
            insert.Parameters.AddWithValue("keyVersion", key.KeyVersion);
            insert.Parameters.AddWithValue("spki", key.Spki);
            insert.Parameters.AddWithValue("fingerprint", key.Fingerprint);
            insert.Parameters.AddWithValue("revision", key.Revision);
            await insert.ExecuteNonQueryAsync();
        }
        var repository = new RecipientPackageRepository(new RoleConnectionFactory(postgres.ConnectionString));
        var request = BuildReserveRequest(lineage, key, RandomNumberGenerator.GetBytes(32));
        return new(repository, lineage.Request, request, key);
    }

    private static RecipientPackageReserveRequest BuildReserveRequest(
        C2RecipientPackageLineageFixture lineage,
        RecipientKeyFixture key,
        byte[] token)
    {
        var request = lineage.Request;
        var equality = RecipientPackageCodec.PackageEqualityFingerprint(
            request.C2PreparationId, request.AssemblyId, request.AssemblyFingerprint, request.ManifestDigest,
            request.AssemblyDigest, request.AssemblyAuthenticationValue, request.RecipientClientApplicationId,
            key.KeyId, key.KeyVersion, key.Fingerprint, request.CompleteAssemblyLength);
        var packageId = RecipientPackageCodec.PackageId(request.C2PreparationId, equality);
        var provider = TestProvider();
        var endpoint = RecipientPackageCodec.ProviderEndpointFingerprint(provider);
        var objectKey = RecipientPackageCodec.ObjectKey(packageId);
        var binding = RecipientPackageCodec.ObjectBindingDigest(
            provider.ProviderConfigurationId, endpoint, provider.BucketName, objectKey);
        return new(
            request.C2PreparationId, packageId, request.AssemblyId,
            lineage.JobId, lineage.AttemptId, lineage.FencingToken,
            request.AssemblyFingerprint, request.ManifestDigest, request.AssemblyDigest,
            request.AssemblyAuthenticationValue, request.CompleteAssemblyLength,
            request.RecipientClientApplicationId, key.KeyId, key.KeyVersion, key.Fingerprint, key.Revision,
            equality, RecipientPackageCodec.ProviderOperationTokenDigest(token),
            RecipientPackageProviderConfiguration.ProviderKind, provider.ProviderConfigurationId,
            endpoint, provider.BucketName, objectKey, binding, RecipientPackageOptions.PackageProfile);
    }

    private static RecipientPackageProviderConfiguration TestProvider() => new(
        "c2-integration-minio-v1", new Uri("http://127.0.0.1:9000/"), "tagekyc-c2-integration",
        true, "us-east-1", new("writer", "secret"), new("reader", "secret"),
        new("lifecycle", "secret"), new("posture", "secret"), true);

    private RecipientPackagePreparationProvider CreatePreparationProvider(
        ReservedPackageFixture fixture,
        TestObjectStore store,
        RecipientPackageProviderConfiguration? provider = null) =>
        CreatePreparationProvider(fixture.Repository, store, provider);

    private RecipientPackagePreparationProvider CreatePreparationProvider(
        RecipientPackageRepository repository,
        TestObjectStore store,
        RecipientPackageProviderConfiguration? provider = null) => new(
            new RecipientPackageOptions(
                RecipientPackageTopology.S3CompatibleDurable,
                provider ?? TestProvider(),
                true),
            repository,
            new RawExportAssemblyRepository(new AssemblyRoleConnectionFactory(postgres.ConnectionString)),
            new RecipientPackageCryptoService(),
            store,
            store,
            store);

    private async Task InsertRecipientKeyAsync(
        Guid recipientId,
        string keyId,
        byte[] spki,
        byte[]? fingerprint = null)
    {
        fingerprint ??= SHA256.HashData(spki);
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using (var revoke = new NpgsqlCommand("""
            UPDATE tagekyc.raw_export_recipient_key_registrations
            SET "State"='Revoked',"Revision"="Revision"+1,"RevokedAtUtc"=pg_catalog.clock_timestamp()
            WHERE "RecipientClientApplicationId"=@recipient AND "State"='Active'
            """, connection))
        {
            revoke.Parameters.AddWithValue("recipient", recipientId);
            await revoke.ExecuteNonQueryAsync();
        }
        await using var insert = new NpgsqlCommand("""
            INSERT INTO tagekyc.raw_export_recipient_key_registrations(
              "RecipientClientApplicationId","RecipientKeyId","RecipientKeyVersion","PublicKeyAlgorithm","PublicKeySpki",
              "PublicKeyFingerprint","ValidFromUtc","ValidUntilUtc","State","Revision","RegisteredAtUtc")
            VALUES(@recipient,@keyId,1,'RSA-OAEP-256',@spki,@fingerprint,pg_catalog.clock_timestamp()-interval '1 minute',
              pg_catalog.clock_timestamp()+interval '1 hour','Active',1,pg_catalog.clock_timestamp())
            """, connection);
        insert.Parameters.AddWithValue("recipient", recipientId);
        insert.Parameters.AddWithValue("keyId", keyId);
        insert.Parameters.AddWithValue("spki", spki);
        insert.Parameters.AddWithValue("fingerprint", fingerprint);
        await insert.ExecuteNonQueryAsync();
    }

    private sealed class RoleConnectionFactory(string baseConnectionString) : IRecipientPackageConnectionFactory
    {
        public async Task<NpgsqlConnection> OpenAsync(
            RecipientPackageDatabaseCapability capability,
            CancellationToken cancellationToken)
        {
            var role = capability switch
            {
                RecipientPackageDatabaseCapability.Preparer => "tagekyc_raw_export_package_preparer",
                RecipientPackageDatabaseCapability.Reconciler => "tagekyc_raw_export_package_reconciler",
                RecipientPackageDatabaseCapability.Lifecycle => "tagekyc_raw_export_package_lifecycle",
                _ => throw new ArgumentOutOfRangeException(nameof(capability)),
            };
            var builder = new NpgsqlConnectionStringBuilder(baseConnectionString)
            {
                Options = $"-c role={role}",
                Pooling = false,
            };
            var connection = new NpgsqlConnection(builder.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
    }

    private sealed class AssemblyRoleConnectionFactory(string baseConnectionString) : IRawExportAssemblyConnectionFactory
    {
        public async Task<NpgsqlConnection> OpenAsync(
            RawExportAssemblyDatabaseCapability capability,
            CancellationToken cancellationToken)
        {
            var role = capability switch
            {
                RawExportAssemblyDatabaseCapability.Resolver => "tagekyc_raw_export_assembly_resolver",
                RawExportAssemblyDatabaseCapability.Sealer => "tagekyc_raw_export_assembly_sealer",
                _ => throw new ArgumentOutOfRangeException(nameof(capability)),
            };
            var builder = new NpgsqlConnectionStringBuilder(baseConnectionString)
            {
                Options = $"-c role={role}",
                Pooling = false,
            };
            var connection = new NpgsqlConnection(builder.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
    }

    private sealed class TestObjectStore(
        Func<int, RecipientPackageInspection> inspect) :
        IRecipientPackageObjectWriter,
        IRecipientPackageObjectReader,
        IRecipientPackageObjectLifecycle
    {
        public int InspectCount { get; private set; }
        public int PutCount { get; private set; }
        public int DeleteCount { get; private set; }
        public RecipientPackagePutOutcome PutOutcome { get; init; } =
            RecipientPackagePutOutcome.OutcomeUnknown;
        public Func<Task>? BeforePut { get; init; }
        public RecipientPackageDeleteOutcome DeleteOutcome { get; init; } =
            RecipientPackageDeleteOutcome.DeletedAcknowledged;

        public Task<RecipientPackageInspection> InspectAsync(
            RecipientPackageLocator locator,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            InspectCount++;
            return Task.FromResult(inspect(InspectCount));
        }

        public Task<Stream> OpenReadAsync(
            RecipientPackageLocator locator,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("OPEN_READ_NOT_EXPECTED");

        public async Task<RecipientPackagePutResult> PutIfAbsentAsync(
            RecipientPackageLocator locator,
            RecipientPackageEncryptedSpool spool,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (BeforePut is not null) await BeforePut();
            PutCount++;
            return new RecipientPackagePutResult(
                PutOutcome,
                PutOutcome == RecipientPackagePutOutcome.Created ? 201 : null,
                PutOutcome == RecipientPackagePutOutcome.Created ? "etag-created" : null,
                PutOutcome == RecipientPackagePutOutcome.Created ? spool.Length : null);
        }

        public Task<RecipientPackageDeleteOutcome> DeleteAsync(
            RecipientPackageLocator locator,
            CancellationToken cancellationToken)
        {
            DeleteCount++;
            return Task.FromResult(DeleteOutcome);
        }
    }

    private sealed class FakePostureProbe(RecipientPackageBucketPosture? posture) : IRecipientPackagePostureProbe
    {
        public Task<RecipientPackageBucketPosture> InspectAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return posture is null
                ? Task.FromException<RecipientPackageBucketPosture>(new HttpRequestException("PROVIDER_UNAVAILABLE"))
                : Task.FromResult(posture);
        }
    }

    private sealed record RecipientKeyFixture(
        string KeyId,
        int KeyVersion,
        byte[] Spki,
        long Revision,
        byte[] Fingerprint = null!);

    private sealed record ReservedPackageFixture(
        RecipientPackageRepository Repository,
        C2AssemblyPreparationRequest PreparationRequest,
        RecipientPackageReserveRequest Request,
        RecipientPackageReserveResult First,
        RecipientKeyFixture Key);
    private sealed record PackageCandidateFixture(
        RecipientPackageRepository Repository,
        C2AssemblyPreparationRequest PreparationRequest,
        RecipientPackageReserveRequest Request,
        RecipientKeyFixture Key);
    private static string ProjectPath(string relative)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "TagEkyc.sln"))) directory = directory.Parent;
        return Path.Combine(directory?.FullName ?? throw new InvalidOperationException("Repository root not found."), relative);
    }
}
