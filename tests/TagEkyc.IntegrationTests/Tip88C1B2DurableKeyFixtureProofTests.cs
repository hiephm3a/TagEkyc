using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88C1B2DurableKeyFixtureProofTests(PostgresPersistenceFixture postgres)
    : IAsyncLifetime
{
    private const string Migration = "20260803151824_Tip88C1B2DurableKeyFixtureProof";
    private const string PreviousMigration = "20260802105416_Tip88C1B2DurableKeyProd";
    private const string WrapRole = "tagekyc_raw_export_fixture_kek_wrap_executor";
    private const string LookupRole = "tagekyc_raw_export_fixture_kek_lookup_executor";
    private const string Table = "tagekyc.raw_export_fixture_kek_wrap_journal";

    public Task InitializeAsync() => postgres.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task FixtureWrap_ProbeMissingThenCreate_PersistsSqlDerivedDigest()
    {
        await using var logins = await FixtureLogins.CreateAsync(postgres.ConnectionString);
        var token = Token();
        var context = Convert.FromHexString(
            "37108746b02b4a73070aafdb116ceeb15ba5b18399286b92fe38c40fc86c7793");
        var nonce = Convert.FromHexString("0102030405060708090a0b0c");
        var ciphertext = Convert.FromHexString(
            "202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f");
        var tag = Convert.FromHexString("404142434445464748494a4b4c4d4e4f");
        var expectedDigest = Convert.FromHexString(
            "607a19029096528f90db3fef24c5ba2229bf3cf86a5972932b00622503889d30");

        await using var wrapConnection = new NpgsqlConnection(logins.WrapConnectionString);
        await wrapConnection.OpenAsync();
        await using (var probe = new NpgsqlCommand("""
            SELECT outcome,wrapped_dek_metadata_digest
            FROM tagekyc.raw_export_fixture_kek_wrap(
              @provider,@token,@context,
              NULL::bytea,NULL::bytea,NULL::bytea,NULL::text,NULL::integer)
            """, wrapConnection))
        {
            probe.Parameters.AddWithValue("provider", FixtureDurableKekCatalog.KeyProviderId);
            probe.Parameters.AddWithValue("token", token.Value);
            probe.Parameters.AddWithValue("context", context);
            await using var probeReader = await probe.ExecuteReaderAsync();
            Assert.True(await probeReader.ReadAsync());
            Assert.Equal("Missing", probeReader.GetString(0));
            Assert.True(probeReader.IsDBNull(1));
            Assert.False(await probeReader.ReadAsync());
        }
        Assert.Equal(0L, await CountAsync());

        byte[] sqlDigest;
        string resource;
        string receipt;
        await using (var create = new NpgsqlCommand("""
            SELECT outcome,wrapped_dek_ciphertext,wrapped_dek_nonce,wrapped_dek_tag,
                   wrapping_suite_id,wrapping_suite_version,
                   provider_resource_reference,provider_operation_receipt,
                   wrapped_dek_metadata_digest
            FROM tagekyc.raw_export_fixture_kek_wrap(
              @provider,@token,@context,@ciphertext,@nonce,@tag,@suite,@version)
            """, wrapConnection))
        {
            create.Parameters.AddWithValue("provider", FixtureDurableKekCatalog.KeyProviderId);
            create.Parameters.AddWithValue("token", token.Value);
            create.Parameters.AddWithValue("context", context);
            create.Parameters.AddWithValue("ciphertext", ciphertext);
            create.Parameters.AddWithValue("nonce", nonce);
            create.Parameters.AddWithValue("tag", tag);
            create.Parameters.AddWithValue("suite", FixtureDurableKekCatalog.WrappingSuiteId);
            create.Parameters.AddWithValue("version", FixtureDurableKekCatalog.WrappingSuiteVersion);
            await using var createReader = await create.ExecuteReaderAsync();
            Assert.True(await createReader.ReadAsync());
            Assert.Equal("Created", createReader.GetString(0));
            Assert.Equal(ciphertext, (byte[])createReader[1]);
            Assert.Equal(nonce, (byte[])createReader[2]);
            Assert.Equal(tag, (byte[])createReader[3]);
            Assert.Equal(FixtureDurableKekCatalog.WrappingSuiteId, createReader.GetString(4));
            Assert.Equal(FixtureDurableKekCatalog.WrappingSuiteVersion, createReader.GetInt32(5));
            resource = createReader.GetString(6);
            receipt = createReader.GetString(7);
            sqlDigest = (byte[])createReader[8];
            Assert.False(await createReader.ReadAsync());
        }

        var csharpDigest = FixtureDurableKekOperationProvider.ComputeMetadataDigest(
            context, FixtureDurableKekCatalog.WrappingSuiteId,
            FixtureDurableKekCatalog.WrappingSuiteVersion, nonce, ciphertext, tag);

        await using var admin = await OpenAdminAsync();
        await using var command = new NpgsqlCommand($"""
            SELECT "WrappedDekMetadataDigest","ProviderResourceReference",
                   "ProviderOperationReceipt",count(*) OVER()
            FROM {Table}
            WHERE "ProviderOperationToken"=@token
            """, admin);
        command.Parameters.AddWithValue("token", token.Value);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        var persistedDigest = (byte[])reader[0];
        Assert.Equal(resource, reader.GetString(1));
        Assert.Equal(receipt, reader.GetString(2));
        Assert.Equal(1L, reader.GetInt64(3));
        Assert.False(await reader.ReadAsync());

        byte[] lookupDigest;
        await using var lookupConnection = new NpgsqlConnection(logins.LookupConnectionString);
        await lookupConnection.OpenAsync();
        await using (var lookup = new NpgsqlCommand("""
            SELECT outcome,wrapped_dek_metadata_digest
            FROM tagekyc.raw_export_fixture_kek_lookup(@provider,@token,@context)
            """, lookupConnection))
        {
            lookup.Parameters.AddWithValue("provider", FixtureDurableKekCatalog.KeyProviderId);
            lookup.Parameters.AddWithValue("token", token.Value);
            lookup.Parameters.AddWithValue("context", context);
            await using var lookupReader = await lookup.ExecuteReaderAsync();
            Assert.True(await lookupReader.ReadAsync());
            Assert.Equal("Found", lookupReader.GetString(0));
            lookupDigest = (byte[])lookupReader[1];
            Assert.False(await lookupReader.ReadAsync());
        }

        Assert.Equal(sqlDigest, csharpDigest);
        Assert.Equal(sqlDigest, persistedDigest);
        Assert.Equal(sqlDigest, lookupDigest);
        Assert.Equal(expectedDigest, sqlDigest);
        Assert.Equal(expectedDigest, persistedDigest);
        Assert.Equal(expectedDigest, csharpDigest);
        Assert.Equal(expectedDigest, lookupDigest);
        Assert.Equal(1L, await CountAsync());
    }

    [Fact]
    public async Task FixtureWrap_SameTokenSameContext_ReturnsCanonicalExistingMatch()
    {
        await using var logins = await FixtureLogins.CreateAsync(postgres.ConnectionString);
        await using var wrapDb = Db(logins.WrapConnectionString);
        await using var lookupDb = Db(logins.LookupConnectionString);
        var provider = Provider(wrapDb, lookupDb);
        var token = Token();
        var context = RandomNumberGenerator.GetBytes(32);
        using var firstCandidate = AttemptDekLease.CreateOwned(RandomNumberGenerator.GetBytes(32));
        using var secondCandidate = AttemptDekLease.CreateOwned(RandomNumberGenerator.GetBytes(32));
        var first = Assert.IsType<KekWrapResult.Wrapped>(await provider.WrapDekAsync(
            Reference(), token, context, firstCandidate, CancellationToken.None));
        var second = Assert.IsType<KekWrapResult.Wrapped>(await provider.WrapDekAsync(
            Reference(), token, context, secondCandidate, CancellationToken.None));
        AssertMaterialEqual(first.Material, second.Material);
        await using (var direct = new NpgsqlConnection(logins.WrapConnectionString))
        {
            await direct.OpenAsync();
            await using var command = new NpgsqlCommand("""
                SELECT wrapped_dek_ciphertext,wrapped_dek_nonce,wrapped_dek_tag
                FROM tagekyc.raw_export_fixture_kek_wrap(
                  'fixture-kek-provider-v1',@token,@context,
                  decode(repeat('a1',32),'hex'),decode(repeat('b2',12),'hex'),
                  decode(repeat('c3',16),'hex'),'AES-256-GCM',1)
                """, direct);
            command.Parameters.AddWithValue("token", token.Value);
            command.Parameters.AddWithValue("context", context);
            await using var reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal(first.Material.Ciphertext, (byte[])reader[0]);
            Assert.Equal(first.Material.Nonce, (byte[])reader[1]);
            Assert.Equal(first.Material.Tag, (byte[])reader[2]);
        }
        Assert.Equal(1L, await CountAsync());
    }

    [Fact]
    public async Task FixtureWrap_ConcurrentDifferentCandidates_OneRowOneCanonicalResult()
    {
        await using var logins = await FixtureLogins.CreateAsync(postgres.ConnectionString);
        await using var wrapDb1 = Db(logins.WrapConnectionString);
        await using var lookupDb1 = Db(logins.LookupConnectionString);
        await using var wrapDb2 = Db(logins.WrapConnectionString);
        await using var lookupDb2 = Db(logins.LookupConnectionString);
        var token = Token();
        var context = RandomNumberGenerator.GetBytes(32);
        using var candidate1 = AttemptDekLease.CreateOwned(RandomNumberGenerator.GetBytes(32));
        using var candidate2 = AttemptDekLease.CreateOwned(RandomNumberGenerator.GetBytes(32));
        var tasks = new[]
        {
            Provider(wrapDb1, lookupDb1).WrapDekAsync(Reference(), token, context, candidate1, CancellationToken.None),
            Provider(wrapDb2, lookupDb2).WrapDekAsync(Reference(), token, context, candidate2, CancellationToken.None),
        };
        var results = await Task.WhenAll(tasks);
        var first = Assert.IsType<KekWrapResult.Wrapped>(results[0]);
        var second = Assert.IsType<KekWrapResult.Wrapped>(results[1]);
        AssertMaterialEqual(first.Material, second.Material);
        Assert.Equal(1L, await CountAsync());
    }

    [Fact]
    public async Task FixtureRecovery_FreshProcess_RecoversExactWrappedResult()
    {
        await using var logins = await FixtureLogins.CreateAsync(postgres.ConnectionString);
        var token = Token();
        var context = RandomNumberGenerator.GetBytes(32);
        KekWrappedMaterial canonical;
        await using (var wrapDb = Db(logins.WrapConnectionString))
        await using (var lookupDb = Db(logins.LookupConnectionString))
        {
            using var candidate = AttemptDekLease.CreateOwned(RandomNumberGenerator.GetBytes(32));
            canonical = Assert.IsType<KekWrapResult.Wrapped>(await Provider(wrapDb, lookupDb)
                .WrapDekAsync(Reference(), token, context, candidate, CancellationToken.None)).Material;
        }
        await using var restartedWrapDb = Db(logins.WrapConnectionString);
        await using var restartedLookupDb = Db(logins.LookupConnectionString);
        var recovered = Assert.IsType<KekProvisioningResolution.WrappedResultRecovered>(
            await Provider(restartedWrapDb, restartedLookupDb).ResolveProvisioningOperationAsync(
                token, context, CancellationToken.None));
        AssertMaterialEqual(canonical, recovered.Material);
    }

    [Fact]
    public async Task FixtureLookup_CorruptedStoredWrappedField_ReturnsCorruptNeverFound()
    {
        await using var logins = await FixtureLogins.CreateAsync(postgres.ConnectionString);
        await using var wrapDb = Db(logins.WrapConnectionString);
        await using var lookupDb = Db(logins.LookupConnectionString);
        var provider = Provider(wrapDb, lookupDb);
        var token = Token();
        var context = RandomNumberGenerator.GetBytes(32);
        using var candidate = AttemptDekLease.CreateOwned(RandomNumberGenerator.GetBytes(32));
        Assert.IsType<KekWrapResult.Wrapped>(await provider.WrapDekAsync(
            Reference(), token, context, candidate, CancellationToken.None));

        await using var admin = await OpenAdminAsync();
        await SetRoleAsync(admin, "tagekyc_raw_export_deployer");
        await ExecuteAsync(admin, $"ALTER TABLE {Table} DISABLE TRIGGER trg_raw_export_fixture_kek_wrap_guard");
        await ExecuteAsync(admin, $"""
            UPDATE {Table}
            SET "WrappedDekCiphertext"=set_byte("WrappedDekCiphertext",0,
                get_byte("WrappedDekCiphertext",0)#1)
            WHERE "ProviderOperationToken"=@token
            """, ("token", token.Value));
        await ExecuteAsync(admin, $"ALTER TABLE {Table} ENABLE TRIGGER trg_raw_export_fixture_kek_wrap_guard");

        await using (var lookup = new NpgsqlConnection(logins.LookupConnectionString))
        {
            await lookup.OpenAsync();
            await using var command = new NpgsqlCommand("""
                SELECT outcome FROM tagekyc.raw_export_fixture_kek_lookup(
                    'fixture-kek-provider-v1',@token,@context)
                """, lookup);
            command.Parameters.AddWithValue("token", token.Value);
            command.Parameters.AddWithValue("context", context);
            Assert.Equal("CorruptOrUnverifiable", await command.ExecuteScalarAsync());
        }
        Assert.IsType<KekOperationLookup.CorruptOrUnverifiable>(
            await provider.LookupByOperationTokenAsync(token, context, CancellationToken.None));
    }

    [Fact]
    public async Task FixtureAdapter_DivergentReturnedMaterialHandleOrReceipt_IsRejected()
    {
        var context = RandomNumberGenerator.GetBytes(32);
        var canonical = CanonicalResult(context);
        var variants = new[]
        {
            canonical with { Ciphertext = canonical.Ciphertext!.Select((b, i) => i == 0 ? (byte)(b ^ 1) : b).ToArray() },
            canonical with { ProviderResourceReference = "fixture-wrap:" + new string('0', 32) },
            canonical with { ProviderOperationReceipt = "fixture-receipt:" + new string('0', 64) },
        };
        foreach (var variant in variants)
        {
            var journal = new StaticJournal(variant);
            var provider = new FixtureDurableKekOperationProvider(journal, journal);
            Assert.IsType<KekOperationLookup.CorruptOrUnverifiable>(
                await provider.LookupByOperationTokenAsync(Token(), context, CancellationToken.None));
        }
    }

    [Fact]
    public async Task PositiveAbsence_OnlyIndependentProvider_LeavesFixtureJournalUntouched()
    {
        await using var logins = await FixtureLogins.CreateAsync(postgres.ConnectionString);
        await using var wrapDb = Db(logins.WrapConnectionString);
        await using var lookupDb = Db(logins.LookupConnectionString);
        var fixtureLookup = await Provider(wrapDb, lookupDb).LookupByOperationTokenAsync(
            Token(), RandomNumberGenerator.GetBytes(32), CancellationToken.None);
        Assert.IsType<KekOperationLookup.Unknown>(fixtureLookup);
        Assert.IsNotType<KekOperationLookup.PositivelyAbsent>(fixtureLookup);

        var provider = new PositiveAbsenceProvider();
        var result = Assert.IsType<KekOperationLookup.PositivelyAbsent>(
            await provider.LookupByOperationTokenAsync(Token(), RandomNumberGenerator.GetBytes(32), CancellationToken.None));
        Assert.Equal("fixture-independent-positive-absence", result.AbsenceProofReceipt);
        Assert.Equal(0L, await CountAsync());
    }

    [Fact]
    public async Task FixtureCapabilities_ReadinessAndCleanupContract_AreExact()
    {
        await using var logins = await FixtureLogins.CreateAsync(postgres.ConnectionString);
        await using var wrapDb = Db(logins.WrapConnectionString);
        await using var lookupDb = Db(logins.LookupConnectionString);
        await using var readinessDb = postgres.CreateDbContext();
        var provider = Provider(wrapDb, lookupDb);
        Assert.Equal(new DurableKekProviderCapabilities(true, false, true, true), provider.Capabilities);
        var owner = await readinessDb.Database.SqlQueryRaw<string>("""
            SELECT r.rolname AS "Value" FROM pg_catalog.pg_extension e
            JOIN pg_catalog.pg_roles r ON r.oid=e.extowner WHERE e.extname='pgcrypto'
            """).SingleAsync();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [DurableKeyCustodyOptions.CsprngExpectedOwnerPath] = owner,
        }).Build();
        var options = DurableKeyCustodyOptions.Resolve(configuration);
        var services = new ServiceCollection()
            .AddSingleton<IKekOperationProvider>(provider)
            .AddSingleton<IKekProvisioningRecoveryOperation>(provider)
            .BuildServiceProvider();
        var readiness = new DurableKeyProviderReadinessValidator(
            new DurableKeyTopologyOptions(DurableKeyTopology.DurableKey), options,
            new CsprngReadinessValidator(readinessDb, options), readinessDb, services);
        var readinessFailure = await Assert.ThrowsAsync<DurableKeyReadinessException>(
            () => readiness.ValidateAsync(CancellationToken.None));
        Assert.Equal("PROD_RAW_EXPORT_KEK_NOT_QUALIFIED", readinessFailure.Code);

        var context = RandomNumberGenerator.GetBytes(32);
        var reference = "fixture-wrap:" + Guid.NewGuid().ToString("N");
        var count = await CountAsync();
        var cleaned = Assert.IsType<KekProvisioningCleanupResult.Cleaned>(
            await provider.CleanupProvisioningOperationAsync(reference, context, CancellationToken.None));
        var expected = "fixture-cleaned:" + Convert.ToHexString(C1HashCanonical.Compute(
            "tip-88c1-fixture-kek-cleanup-v1",
            new C1HashCanonical.Scalar(reference),
            new C1HashCanonical.Scalar(Convert.ToHexString(context).ToLowerInvariant()))).ToLowerInvariant();
        Assert.Equal(expected, cleaned.Receipt);
        Assert.IsType<KekProvisioningCleanupResult.CleanupFailed>(
            await provider.CleanupProvisioningOperationAsync("bad", context, CancellationToken.None));
        Assert.IsType<KekProvisioningCleanupResult.CleanupFailed>(
            await provider.CleanupProvisioningOperationAsync(reference, new byte[31], CancellationToken.None));
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            provider.CleanupProvisioningOperationAsync(reference, context, cancelled.Token));
        Assert.Equal(count, await CountAsync());
    }

    [Fact]
    public async Task FixtureJournal_UpdateDeleteDenied_AndFixtureRoleInsertSucceeds()
    {
        await using var logins = await FixtureLogins.CreateAsync(postgres.ConnectionString);
        await using var wrapDb = Db(logins.WrapConnectionString);
        await using var lookupDb = Db(logins.LookupConnectionString);
        var token = Token();
        var context = RandomNumberGenerator.GetBytes(32);
        using var candidate = AttemptDekLease.CreateOwned(RandomNumberGenerator.GetBytes(32));
        Assert.IsType<KekWrapResult.Wrapped>(await Provider(wrapDb, lookupDb).WrapDekAsync(
            Reference(), token, context, candidate, CancellationToken.None));

        await using var admin = await OpenAdminAsync();
        await SetRoleAsync(admin, "tagekyc_raw_export_deployer");
        foreach (var sql in new[]
        {
            $"UPDATE {Table} SET \"CreatedAtUtc\"=\"CreatedAtUtc\"",
            $"DELETE FROM {Table}",
        })
        {
            var exception = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(admin, sql));
            Assert.Equal("P0001", exception.SqlState);
            Assert.Equal("RAW_EXPORT_FIXTURE_KEK_JOURNAL_APPEND_ONLY", exception.MessageText);
        }
    }

    [Fact]
    public async Task FixtureJournal_SpoofedGucAndDirectInsertDenied()
    {
        await using var logins = await FixtureLogins.CreateAsync(postgres.ConnectionString);
        await using var admin = await OpenAdminAsync();
        await SetRoleAsync(admin, "tagekyc_raw_export_deployer");
        await ExecuteAsync(admin, $"GRANT INSERT ON {Table} TO {WrapRole}");
        try
        {
            await using var caller = new NpgsqlConnection(logins.WrapConnectionString);
            await caller.OpenAsync();
            await ScalarAsync<string>(caller,
                "SELECT set_config('tagekyc.raw_export_fixture_kek_write_context','active',false)");
            var id = Guid.NewGuid();
            var exception = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(caller, $"""
                INSERT INTO {Table}("FixtureWrapId","KeyProviderId","ProviderOperationToken",
                  "AttemptKeyContextFingerprint","WrappingSuiteId","WrappingSuiteVersion",
                  "WrappedDekCiphertext","WrappedDekNonce","WrappedDekTag",
                  "ProviderResourceReference","ProviderOperationReceipt","WrappedDekMetadataDigest","CreatedAtUtc")
                VALUES(@id,'fixture-kek-provider-v1',@token,decode(repeat('01',32),'hex'),
                  'AES-256-GCM',1,decode(repeat('02',32),'hex'),decode(repeat('03',12),'hex'),
                  decode(repeat('04',16),'hex'),'fixture-wrap:'||replace(@id::text,'-',''),
                  'fixture-receipt:test',decode(repeat('05',32),'hex'),statement_timestamp())
                """, ("id", id), ("token", Token().Value)));
            Assert.Equal("P0001", exception.SqlState);
            Assert.Equal("RAW_EXPORT_FIXTURE_KEK_WRITE_CONTEXT_INVALID", exception.MessageText);
        }
        finally
        {
            await ExecuteAsync(admin, $"REVOKE INSERT ON {Table} FROM {WrapRole}");
        }
        Assert.Equal(0L, await CountAsync());
    }

    [Fact]
    public async Task FixtureFunctions_RolesMembershipAclOwnerGrantorAndOverloads_AreExact()
    {
        await using var admin = await OpenAdminAsync();
        await using (var command = new NpgsqlCommand("""
            SELECT r.rolname,r.rolcanlogin,r.rolinherit,r.rolsuper,r.rolcreatedb,
                   r.rolcreaterole,r.rolreplication,r.rolbypassrls
            FROM pg_catalog.pg_roles r WHERE r.rolname=ANY(@roles) ORDER BY r.rolname
            """, admin))
        {
            command.Parameters.AddWithValue("roles", new[] { LookupRole, WrapRole });
            await using var reader = await command.ExecuteReaderAsync();
            var count = 0;
            while (await reader.ReadAsync())
            {
                count++;
                Assert.False(reader.GetBoolean(1)); Assert.True(reader.GetBoolean(2));
                for (var i = 3; i < 8; i++) Assert.False(reader.GetBoolean(i));
            }
            Assert.Equal(2, count);
        }
        var manifest = await QueryAsync(admin, """
            SELECT p.proname,grantee.rolname,grantor.rolname,a.privilege_type
            FROM pg_catalog.pg_proc p
            JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
            CROSS JOIN LATERAL pg_catalog.aclexplode(p.proacl) a
            LEFT JOIN pg_catalog.pg_roles grantee ON grantee.oid=a.grantee
            JOIN pg_catalog.pg_roles grantor ON grantor.oid=a.grantor
            WHERE n.nspname='tagekyc' AND p.proname IN
              ('raw_export_fixture_kek_wrap','raw_export_fixture_kek_lookup')
            ORDER BY p.proname,grantee.rolname
            """);
        Assert.Equal(new[]
        {
            "raw_export_fixture_kek_lookup|tagekyc_raw_export_deployer|tagekyc_raw_export_deployer|EXECUTE",
            $"raw_export_fixture_kek_lookup|{LookupRole}|tagekyc_raw_export_deployer|EXECUTE",
            "raw_export_fixture_kek_wrap|tagekyc_raw_export_deployer|tagekyc_raw_export_deployer|EXECUTE",
            $"raw_export_fixture_kek_wrap|{WrapRole}|tagekyc_raw_export_deployer|EXECUTE",
        }, manifest);
        Assert.Equal(0L, await ScalarAsync<long>(admin, """
            SELECT count(*) FROM pg_catalog.pg_auth_members m
            JOIN pg_catalog.pg_roles r ON r.oid=m.roleid
            WHERE r.rolname IN ('tagekyc_raw_export_fixture_kek_wrap_executor',
                                'tagekyc_raw_export_fixture_kek_lookup_executor')
            """));
        Assert.Equal(0L, await ScalarAsync<long>(admin, """
            SELECT count(*) FROM pg_catalog.pg_attribute a
            WHERE a.attrelid='tagekyc.raw_export_fixture_kek_wrap_journal'::regclass
              AND a.attacl IS NOT NULL
            """));
        var tableAcl = await QueryAsync(admin, """
            SELECT COALESCE(grantee.rolname,'PUBLIC'),grantor.rolname,acl.privilege_type
            FROM pg_catalog.pg_class relation
            CROSS JOIN LATERAL pg_catalog.aclexplode(
              COALESCE(relation.relacl,pg_catalog.acldefault('r',relation.relowner))) acl
            LEFT JOIN pg_catalog.pg_roles grantee ON grantee.oid=acl.grantee
            JOIN pg_catalog.pg_roles grantor ON grantor.oid=acl.grantor
            WHERE relation.oid='tagekyc.raw_export_fixture_kek_wrap_journal'::regclass
            ORDER BY 1,3
            """);
        Assert.Equal(new[]
        {
            "tagekyc_raw_export_deployer|tagekyc_raw_export_deployer|DELETE",
            "tagekyc_raw_export_deployer|tagekyc_raw_export_deployer|INSERT",
            "tagekyc_raw_export_deployer|tagekyc_raw_export_deployer|REFERENCES",
            "tagekyc_raw_export_deployer|tagekyc_raw_export_deployer|SELECT",
            "tagekyc_raw_export_deployer|tagekyc_raw_export_deployer|TRIGGER",
            "tagekyc_raw_export_deployer|tagekyc_raw_export_deployer|TRUNCATE",
            "tagekyc_raw_export_deployer|tagekyc_raw_export_deployer|UPDATE",
        }, tableAcl);
        Assert.Empty(await QueryAsync(admin, """
            WITH forbidden(role_name) AS (VALUES
              ('tagekyc_runtime'),
              ('tagekyc_raw_export_custody_encryptor'),
              ('tagekyc_raw_export_reconciler'),
              ('tagekyc_raw_export_lifecycle'),
              ('tagekyc_raw_export_encryptor_login'),
              ('tagekyc_raw_export_reconciler_login'),
              ('tagekyc_raw_export_lifecycle_login'),
              ('tagekyc_raw_export_fixture_kek_wrap_executor'),
              ('tagekyc_raw_export_fixture_kek_lookup_executor'),
              ('tagekyc_fixture_kek_wrap_test_login'),
              ('tagekyc_fixture_kek_lookup_test_login')),
            privileges(privilege_name) AS (VALUES
              ('SELECT'),('INSERT'),('UPDATE'),('DELETE'),
              ('TRUNCATE'),('REFERENCES'),('TRIGGER'))
            SELECT forbidden.role_name,privileges.privilege_name
            FROM forbidden
            JOIN pg_catalog.pg_roles role ON role.rolname=forbidden.role_name
            CROSS JOIN privileges
            WHERE pg_catalog.has_table_privilege(
              role.oid,'tagekyc.raw_export_fixture_kek_wrap_journal',
              privileges.privilege_name)
            ORDER BY 1,2
            """));
        Assert.Equal(new[]
        {
            $"{LookupRole}|tagekyc|USAGE",
            $"{WrapRole}|tagekyc|USAGE",
        }, await QueryAsync(admin, """
            SELECT COALESCE(grantee.rolname,'PUBLIC'),grantor.rolname,acl.privilege_type
            FROM pg_catalog.pg_namespace namespace
            CROSS JOIN LATERAL pg_catalog.aclexplode(namespace.nspacl) acl
            LEFT JOIN pg_catalog.pg_roles grantee ON grantee.oid=acl.grantee
            JOIN pg_catalog.pg_roles grantor ON grantor.oid=acl.grantor
            WHERE namespace.nspname='tagekyc'
              AND (acl.grantee=0 OR grantee.rolname IN
                ('tagekyc_raw_export_fixture_kek_wrap_executor',
                 'tagekyc_raw_export_fixture_kek_lookup_executor'))
            ORDER BY 1,3
            """));
    }

    [Fact]
    public async Task FixtureIdentifiers_IntendedNamesUnder63AndRoundTripExactly()
    {
        string[] intended =
        [
            "raw_export_fixture_kek_wrap_journal",
            "pk_raw_export_fixture_kek_wrap_journal",
            "uq_raw_export_fixture_kek_provider_token",
            "uq_raw_export_fixture_kek_resource_ref",
            "ck_raw_export_fixture_kek_wrap_shape",
            "ck_raw_export_fixture_kek_wrap_text",
            "trg_raw_export_fixture_kek_wrap_guard",
            "enforce_raw_export_fixture_kek_wrap_guard",
            "raw_export_fixture_kek_wrap",
            "raw_export_fixture_kek_lookup",
            WrapRole,
            LookupRole,
        ];
        Assert.All(intended, name => Assert.InRange(System.Text.Encoding.UTF8.GetByteCount(name), 1, 63));
        await using var admin = await OpenAdminAsync();
        var found = await QueryAsync(admin, """
            SELECT relname FROM pg_catalog.pg_class WHERE relnamespace='tagekyc'::regnamespace
              AND relname='raw_export_fixture_kek_wrap_journal'
            UNION ALL SELECT conname FROM pg_catalog.pg_constraint
              WHERE conrelid='tagekyc.raw_export_fixture_kek_wrap_journal'::regclass
            UNION ALL SELECT relname FROM pg_catalog.pg_class
              WHERE relnamespace='tagekyc'::regnamespace AND relkind='i'
                AND relname IN ('uq_raw_export_fixture_kek_provider_token',
                                'uq_raw_export_fixture_kek_resource_ref')
            UNION ALL SELECT tgname FROM pg_catalog.pg_trigger
              WHERE tgrelid='tagekyc.raw_export_fixture_kek_wrap_journal'::regclass AND NOT tgisinternal
            UNION ALL SELECT proname FROM pg_catalog.pg_proc
              WHERE pronamespace='tagekyc'::regnamespace AND proname IN
                ('enforce_raw_export_fixture_kek_wrap_guard','raw_export_fixture_kek_wrap',
                 'raw_export_fixture_kek_lookup')
            UNION ALL SELECT rolname FROM pg_catalog.pg_roles WHERE rolname IN
                ('tagekyc_raw_export_fixture_kek_wrap_executor',
                 'tagekyc_raw_export_fixture_kek_lookup_executor')
            """);
        Assert.Empty(intended.Except(found, StringComparer.Ordinal));
    }

    [Fact]
    public async Task FixtureMigration_ApplyDownReapply_RestoresExactCatalogAndAcl()
    {
        await using var db = postgres.CreateDbContext();
        var migrator = db.GetService<IMigrator>();
        var before = await CatalogDigestAsync();
        await migrator.MigrateAsync(PreviousMigration);
        await using (var admin = await OpenAdminAsync())
        {
            Assert.Equal(0L, await ScalarAsync<long>(admin, """
                SELECT count(*) FROM pg_catalog.pg_class
                WHERE relnamespace='tagekyc'::regnamespace
                  AND relname='raw_export_fixture_kek_wrap_journal'
                """));
            Assert.Equal(1L, await ScalarAsync<long>(admin, """
                SELECT count(*) FROM pg_catalog.pg_proc
                WHERE oid='tagekyc.raw_export_prepare_attempt_key_reservation(uuid,uuid,uuid)'::regprocedure
                """));
        }
        await migrator.MigrateAsync(Migration);
        Assert.Equal(before, await CatalogDigestAsync());
    }

    [Fact]
    public async Task FixtureUnwrap_ExactAadRoundTrips_AndWrongContextFails()
    {
        await using var logins = await FixtureLogins.CreateAsync(postgres.ConnectionString);
        await using var wrapDb = Db(logins.WrapConnectionString);
        await using var lookupDb = Db(logins.LookupConnectionString);
        var provider = Provider(wrapDb, lookupDb);
        var context = RandomNumberGenerator.GetBytes(32);
        var plaintext = RandomNumberGenerator.GetBytes(32);
        using var candidate = AttemptDekLease.CreateOwned(plaintext.ToArray());
        var wrapped = Assert.IsType<KekWrapResult.Wrapped>(await provider.WrapDekAsync(
            Reference(), Token(), context, candidate, CancellationToken.None)).Material;
        using var recovered = await provider.UnwrapDekAsync(
            Reference(), wrapped, context, CancellationToken.None);
        Assert.Equal(plaintext, recovered.Material.ToArray());
        var failure = await Record.ExceptionAsync(() => provider.UnwrapDekAsync(
            Reference(), wrapped, RandomNumberGenerator.GetBytes(32), CancellationToken.None));
        Assert.IsAssignableFrom<CryptographicException>(failure);
    }

    [Fact]
    public async Task FixtureLookup_MissingRow_IsUnknownNeverPositiveAbsence()
    {
        await using var logins = await FixtureLogins.CreateAsync(postgres.ConnectionString);
        await using var wrapDb = Db(logins.WrapConnectionString);
        await using var lookupDb = Db(logins.LookupConnectionString);
        var result = await Provider(wrapDb, lookupDb).LookupByOperationTokenAsync(
            Token(), RandomNumberGenerator.GetBytes(32), CancellationToken.None);
        Assert.IsType<KekOperationLookup.Unknown>(result);
        Assert.IsNotType<KekOperationLookup.PositivelyAbsent>(result);
        Assert.Equal(0L, await CountAsync());
    }

    [Fact]
    public async Task FixtureWrap_ProviderOrContextMismatch_RaisesPinnedFailureWithoutMutation()
    {
        await using var logins = await FixtureLogins.CreateAsync(postgres.ConnectionString);
        await using var caller = new NpgsqlConnection(logins.WrapConnectionString);
        await caller.OpenAsync();
        var token = Token();
        var context = RandomNumberGenerator.GetBytes(32);
        var invalid = await Assert.ThrowsAsync<PostgresException>(() => ProbeAsync(
            caller, "not-fixture", token.Value, context));
        Assert.Equal("P0001", invalid.SqlState);
        Assert.Equal("RAW_EXPORT_FIXTURE_KEK_ARGUMENT_INVALID", invalid.MessageText);
        Assert.Equal(0L, await CountAsync());

        await using var wrapDb = Db(logins.WrapConnectionString);
        await using var lookupDb = Db(logins.LookupConnectionString);
        using var candidate = AttemptDekLease.CreateOwned(RandomNumberGenerator.GetBytes(32));
        Assert.IsType<KekWrapResult.Wrapped>(await Provider(wrapDb, lookupDb).WrapDekAsync(
            Reference(), token, context, candidate, CancellationToken.None));
        var mismatch = await Assert.ThrowsAsync<PostgresException>(() => ProbeAsync(
            caller, FixtureDurableKekCatalog.KeyProviderId, token.Value,
            RandomNumberGenerator.GetBytes(32)));
        Assert.Equal("P0001", mismatch.SqlState);
        Assert.Equal("RAW_EXPORT_FIXTURE_KEK_CONTEXT_MISMATCH", mismatch.MessageText);
        using var mismatchedCandidate = AttemptDekLease.CreateOwned(RandomNumberGenerator.GetBytes(32));
        Assert.IsType<KekWrapResult.CorruptOrUnverifiable>(await Provider(wrapDb, lookupDb).WrapDekAsync(
            Reference(), token, RandomNumberGenerator.GetBytes(32), mismatchedCandidate,
            CancellationToken.None));
        Assert.Equal(1L, await CountAsync());
    }

    [Fact]
    public async Task FixtureProviderIdentityTextBoundsTokensAndRedaction_AreExact()
    {
        Assert.Equal("fixture-kek-provider-v1", FixtureDurableKekCatalog.KeyProviderId);
        Assert.Throws<ArgumentException>(() => new ProviderOperationToken("short"));
        var token = Token();
        Assert.Equal("[REDACTED:provider-operation-token]", token.ToString());
        await using var logins = await FixtureLogins.CreateAsync(postgres.ConnectionString);
        await using var caller = new NpgsqlConnection(logins.LookupConnectionString);
        await caller.OpenAsync();
        var exception = await Assert.ThrowsAsync<PostgresException>(() => LookupRawAsync(
            caller, FixtureDurableKekCatalog.KeyProviderId, new string('a', 42), new byte[32]));
        Assert.Equal("RAW_EXPORT_FIXTURE_KEK_ARGUMENT_INVALID", exception.MessageText);
        await using var wrapDb = Db(logins.WrapConnectionString);
        await using var lookupDb = Db(logins.LookupConnectionString);
        Assert.Equal("FixtureDurableKekOperationProvider:<redacted>", Provider(wrapDb, lookupDb).ToString());
        var resultText = CanonicalResult(RandomNumberGenerator.GetBytes(32)).ToString();
        Assert.Equal("FixtureKekJournalResult:<redacted>", resultText);
        Assert.DoesNotContain("fixture-wrap:", resultText, StringComparison.Ordinal);
        Assert.DoesNotContain("fixture-receipt:", resultText, StringComparison.Ordinal);
    }

    [Fact]
    public void E3_ModelSnapshotDelta_IsAdditiveAndTripwireRoundTrips()
    {
        var root = RepoRoot();
        var relative = "src/TagEkyc.Infrastructure/Persistence/Migrations/TagEkycDbContextModelSnapshot.cs";
        var snapshot = File.ReadAllText(
            Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
        const string entityName =
            "TagEkyc.Infrastructure.Persistence.Entities.RawExportFixtureKekWrapJournalRow";
        var entityStartMarker = $"modelBuilder.Entity(\"{entityName}\", b =>";
        Assert.Equal(1, Count(snapshot, entityStartMarker));
        var entityStart = snapshot.IndexOf(entityStartMarker, StringComparison.Ordinal);
        var nextEntity = snapshot.IndexOf(
            "modelBuilder.Entity(\"",
            entityStart + entityStartMarker.Length,
            StringComparison.Ordinal);
        Assert.True(nextEntity > entityStart, "DK-FIXTURE entity block is not structurally bounded.");
        var block = snapshot[entityStart..nextEntity];

        using var db = postgres.CreateDbContext();
        var entity = db.GetService<Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel>()
            .Model
            .FindEntityType(entityName);
        Assert.NotNull(entity);
        Assert.Equal("raw_export_fixture_kek_wrap_journal", entity.GetTableName());
        Assert.Equal("tagekyc", entity.GetSchema());
        Assert.Contains(
            "b.ToTable(\"raw_export_fixture_kek_wrap_journal\", \"tagekyc\", t =>",
            block,
            StringComparison.Ordinal);

        var expectedProperties = new (string Name, string ColumnType, int? MaxLength)[]
        {
            ("FixtureWrapId", "uuid", null),
            ("AttemptKeyContextFingerprint", "bytea", null),
            ("CreatedAtUtc", "timestamp with time zone", null),
            ("KeyProviderId", "character varying(512)", 512),
            ("ProviderOperationReceipt", "character varying(512)", 512),
            ("ProviderOperationToken", "character varying(43)", 43),
            ("ProviderResourceReference", "character varying(512)", 512),
            ("WrappedDekCiphertext", "bytea", null),
            ("WrappedDekMetadataDigest", "bytea", null),
            ("WrappedDekNonce", "bytea", null),
            ("WrappedDekTag", "bytea", null),
            ("WrappingSuiteId", "character varying(512)", 512),
            ("WrappingSuiteVersion", "integer", null),
        };
        foreach (var expected in expectedProperties)
        {
            var property = entity.FindProperty(expected.Name);
            Assert.NotNull(property);
            Assert.False(property.IsNullable);
            Assert.Equal(expected.ColumnType, property.GetColumnType());
            Assert.Equal(expected.MaxLength, property.GetMaxLength());
            Assert.Equal(1, CountMember(block, "b.Property<", $"(\"{expected.Name}\")"));
            Assert.Contains($".HasColumnType(\"{expected.ColumnType}\")", block, StringComparison.Ordinal);
        }

        var key = entity.FindPrimaryKey();
        Assert.NotNull(key);
        Assert.Equal("pk_raw_export_fixture_kek_wrap_journal", key.GetName());
        Assert.Equal(new[] { "FixtureWrapId" }, key.Properties.Select(property => property.Name));
        Assert.Contains(
            "b.HasKey(\"FixtureWrapId\")",
            block,
            StringComparison.Ordinal);
        Assert.Contains(
            ".HasName(\"pk_raw_export_fixture_kek_wrap_journal\")",
            block,
            StringComparison.Ordinal);

        var indexes = entity.GetIndexes()
            .ToDictionary(index => index.GetDatabaseName()!, StringComparer.Ordinal);
        Assert.Equal(2, indexes.Count);
        Assert.True(indexes["uq_raw_export_fixture_kek_resource_ref"].IsUnique);
        Assert.Equal(
            new[] { "ProviderResourceReference" },
            indexes["uq_raw_export_fixture_kek_resource_ref"].Properties.Select(property => property.Name));
        Assert.True(indexes["uq_raw_export_fixture_kek_provider_token"].IsUnique);
        Assert.Equal(
            new[] { "KeyProviderId", "ProviderOperationToken" },
            indexes["uq_raw_export_fixture_kek_provider_token"].Properties.Select(property => property.Name));
        foreach (var indexName in indexes.Keys)
            Assert.Equal(1, Count(block, $".HasDatabaseName(\"{indexName}\")"));

        Assert.Empty(entity.GetForeignKeys());
        Assert.DoesNotContain(
            $"modelBuilder.Entity(\"{entityName}\", b =>{Environment.NewLine}                {{{Environment.NewLine}                    b.HasOne",
            snapshot,
            StringComparison.Ordinal);

        var constraints = entity.GetCheckConstraints()
            .ToDictionary(
                constraint => constraint.Name
                    ?? throw new InvalidOperationException("DK-FIXTURE check constraint is unnamed."),
                StringComparer.Ordinal);
        Assert.Equal(
            new[]
            {
                "ck_raw_export_fixture_kek_wrap_shape",
                "ck_raw_export_fixture_kek_wrap_text",
            },
            constraints.Keys.Order(StringComparer.Ordinal));
        foreach (var constraint in constraints.Values)
        {
            var marker = $"t.HasCheckConstraint(\"{constraint.Name}\", ";
            Assert.Equal(
                1,
                Count(block, marker));
            Assert.Contains(
                $"{marker}\"{Escape(constraint.Sql)}\");",
                block,
                StringComparison.Ordinal);
        }

        static int Count(string value, string token) =>
            value.Split(token, StringSplitOptions.None).Length - 1;

        static int CountMember(string value, string prefix, string suffix)
        {
            var count = 0;
            var cursor = 0;
            while ((cursor = value.IndexOf(prefix, cursor, StringComparison.Ordinal)) >= 0)
            {
                var end = value.IndexOf(";", cursor, StringComparison.Ordinal);
                Assert.True(end > cursor, $"Unterminated snapshot member after {prefix}.");
                if (value.IndexOf(suffix, cursor, end - cursor, StringComparison.Ordinal) >= 0)
                    count++;
                cursor = end + 1;
            }
            return count;
        }

        static string Escape(string value) => value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\t", "\\t", StringComparison.Ordinal);
    }

    private static FixtureDurableKekOperationProvider Provider(TagEkycDbContext wrapDb, TagEkycDbContext lookupDb) =>
        new(new PostgresFixtureKekJournal(wrapDb), new PostgresFixtureKekJournal(lookupDb));

    private TagEkycDbContext Db(string connectionString) => new(
        new DbContextOptionsBuilder<TagEkycDbContext>().UseNpgsql(connectionString).Options);

    private async Task<NpgsqlConnection> OpenAdminAsync()
    {
        var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    private async Task<long> CountAsync()
    {
        await using var connection = await OpenAdminAsync();
        return await ScalarAsync<long>(connection, $"SELECT count(*) FROM {Table}");
    }

    private async Task<string> CatalogDigestAsync()
    {
        await using var connection = await OpenAdminAsync();
        return await ScalarAsync<string>(connection, """
            SELECT md5(string_agg(v,'|' ORDER BY v)) FROM (
              SELECT 'p:'||p.oid::regprocedure::text||':'||r.rolname||':'||coalesce(p.proacl::text,'') v
              FROM pg_catalog.pg_proc p JOIN pg_catalog.pg_roles r ON r.oid=p.proowner
              WHERE p.pronamespace='tagekyc'::regnamespace AND p.proname LIKE 'raw_export_fixture_kek_%'
              UNION ALL SELECT 't:'||c.relname||':'||r.rolname||':'||coalesce(c.relacl::text,'')
              FROM pg_catalog.pg_class c JOIN pg_catalog.pg_roles r ON r.oid=c.relowner
              WHERE c.relnamespace='tagekyc'::regnamespace AND c.relname='raw_export_fixture_kek_wrap_journal'
            ) q
            """);
    }

    private static KekReference Reference() => new(
        FixtureDurableKekCatalog.KeyProviderId, FixtureDurableKekCatalog.KekId,
        FixtureDurableKekCatalog.KekVersion, FixtureDurableKekCatalog.KekFingerprint);

    private static ProviderOperationToken Token()
    {
        var value = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return new ProviderOperationToken(value);
    }

    private static void AssertMaterialEqual(KekWrappedMaterial expected, KekWrappedMaterial actual)
    {
        Assert.Equal(expected.Ciphertext, actual.Ciphertext);
        Assert.Equal(expected.Nonce, actual.Nonce);
        Assert.Equal(expected.Tag, actual.Tag);
        Assert.Equal(expected.SuiteId, actual.SuiteId);
        Assert.Equal(expected.SuiteVersion, actual.SuiteVersion);
        Assert.Equal(expected.ProviderResourceReference, actual.ProviderResourceReference);
        Assert.Equal(expected.Receipt, actual.Receipt);
    }

    private static FixtureKekJournalResult CanonicalResult(byte[] context)
    {
        var id = Guid.NewGuid();
        var ciphertext = RandomNumberGenerator.GetBytes(32);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var tag = RandomNumberGenerator.GetBytes(16);
        var digest = FixtureDurableKekOperationProvider.ComputeMetadataDigest(
            context, FixtureDurableKekCatalog.WrappingSuiteId,
            FixtureDurableKekCatalog.WrappingSuiteVersion, nonce, ciphertext, tag);
        var resource = "fixture-wrap:" + id.ToString("N");
        var receipt = "fixture-receipt:" + Convert.ToHexString(C1HashCanonical.Compute(
            "tip-88c1-fixture-kek-receipt-v1", new C1HashCanonical.Scalar(resource),
            new C1HashCanonical.Scalar(Convert.ToHexString(context).ToLowerInvariant()),
            new C1HashCanonical.Scalar(Convert.ToHexString(digest).ToLowerInvariant()))).ToLowerInvariant();
        return new("Found", id, ciphertext, nonce, tag,
            FixtureDurableKekCatalog.WrappingSuiteId, FixtureDurableKekCatalog.WrappingSuiteVersion,
            resource, receipt, digest);
    }

    private static async Task ProbeAsync(NpgsqlConnection connection, string provider, string token, byte[] context)
    {
        await using var command = new NpgsqlCommand("""
            SELECT * FROM tagekyc.raw_export_fixture_kek_wrap(
              @provider,@token,@context,NULL,NULL,NULL,NULL,NULL)
            """, connection);
        command.Parameters.AddWithValue("provider", provider);
        command.Parameters.AddWithValue("token", token);
        command.Parameters.AddWithValue("context", context);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task LookupRawAsync(NpgsqlConnection connection, string provider, string token, byte[] context)
    {
        await using var command = new NpgsqlCommand("""
            SELECT * FROM tagekyc.raw_export_fixture_kek_lookup(@provider,@token,@context)
            """, connection);
        command.Parameters.AddWithValue("provider", provider);
        command.Parameters.AddWithValue("token", token);
        command.Parameters.AddWithValue("context", context);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task SetRoleAsync(NpgsqlConnection connection, string role) =>
        await ExecuteAsync(connection, $"SET ROLE {role}");

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql, params (string Name, object Value)[] args)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in args) command.Parameters.AddWithValue(name, value);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<T> ScalarAsync<T>(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return (T)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<string[]> QueryAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<string>();
        while (await reader.ReadAsync())
            rows.Add(string.Join('|', Enumerable.Range(0, reader.FieldCount).Select(i => reader.GetValue(i)?.ToString())));
        return rows.ToArray();
    }

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, ".git")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException();
    }

    private static string Git(string root, string arguments)
    {
        var start = new ProcessStartInfo("git", arguments)
        {
            WorkingDirectory = root, RedirectStandardOutput = true, RedirectStandardError = true,
        };
        using var process = Process.Start(start) ?? throw new InvalidOperationException();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        Assert.Equal(0, process.ExitCode);
        return output;
    }

    private sealed class StaticJournal(FixtureKekJournalResult result)
        : IFixtureKekWrapJournal, IFixtureKekLookupJournal
    {
        public Task<FixtureKekJournalResult> ProbeOrCreateAsync(ProviderOperationToken token,
            ReadOnlyMemory<byte> contextFingerprint, KekWrappedMaterial? candidate,
            CancellationToken cancellationToken) => Task.FromResult(result);
        public Task<FixtureKekJournalResult> LookupAsync(ProviderOperationToken token,
            ReadOnlyMemory<byte> contextFingerprint, CancellationToken cancellationToken) => Task.FromResult(result);
    }

    private sealed class PositiveAbsenceProvider : IKekOperationProvider
    {
        public Task<KekOperationLookup> LookupByOperationTokenAsync(ProviderOperationToken providerOperationToken,
            ReadOnlyMemory<byte> attemptKeyContextFingerprint, CancellationToken cancellationToken) =>
            Task.FromResult<KekOperationLookup>(new KekOperationLookup.PositivelyAbsent(
                "fixture-independent-positive-absence"));
        public Task<KekWrapResult> WrapDekAsync(KekReference reference, ProviderOperationToken providerOperationToken,
            ReadOnlyMemory<byte> attemptKeyContextFingerprint, IAttemptDekCandidate candidate,
            CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<AttemptDekLease> UnwrapDekAsync(KekReference reference, KekWrappedMaterial wrapped,
            ReadOnlyMemory<byte> attemptKeyContextFingerprint, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FixtureLogins : IAsyncDisposable
    {
        private readonly string adminConnectionString;
        private FixtureLogins(string adminConnectionString, string wrapConnectionString, string lookupConnectionString)
        {
            this.adminConnectionString = adminConnectionString;
            WrapConnectionString = wrapConnectionString;
            LookupConnectionString = lookupConnectionString;
        }
        internal string WrapConnectionString { get; }
        internal string LookupConnectionString { get; }

        internal static async Task<FixtureLogins> CreateAsync(string adminConnectionString)
        {
            var password = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            await using var admin = new NpgsqlConnection(adminConnectionString);
            await admin.OpenAsync();
            await ExecuteAsync(admin, $"""
                CREATE ROLE tagekyc_fixture_kek_wrap_test_login LOGIN INHERIT NOSUPERUSER
                  NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS PASSWORD '{password}';
                CREATE ROLE tagekyc_fixture_kek_lookup_test_login LOGIN INHERIT NOSUPERUSER
                  NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS PASSWORD '{password}';
                GRANT {WrapRole} TO tagekyc_fixture_kek_wrap_test_login
                  WITH ADMIN FALSE, INHERIT TRUE, SET FALSE;
                GRANT {LookupRole} TO tagekyc_fixture_kek_lookup_test_login
                  WITH ADMIN FALSE, INHERIT TRUE, SET FALSE;
                """);
            static string Login(string source, string user, string password)
            {
                var builder = new NpgsqlConnectionStringBuilder(source)
                {
                    Username = user, Password = password, Pooling = false,
                };
                return builder.ConnectionString;
            }
            return new(adminConnectionString,
                Login(adminConnectionString, "tagekyc_fixture_kek_wrap_test_login", password),
                Login(adminConnectionString, "tagekyc_fixture_kek_lookup_test_login", password));
        }

        public async ValueTask DisposeAsync()
        {
            NpgsqlConnection.ClearAllPools();
            await using var admin = new NpgsqlConnection(adminConnectionString);
            await admin.OpenAsync();
            await ExecuteAsync(admin, $"""
                REVOKE {WrapRole} FROM tagekyc_fixture_kek_wrap_test_login;
                REVOKE {LookupRole} FROM tagekyc_fixture_kek_lookup_test_login;
                DROP ROLE tagekyc_fixture_kek_wrap_test_login;
                DROP ROLE tagekyc_fixture_kek_lookup_test_login;
                """);
        }
    }
}
