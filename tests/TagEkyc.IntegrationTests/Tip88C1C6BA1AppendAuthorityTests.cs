using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88C1C6BA1AppendAuthorityTests(PostgresPersistenceFixture postgres)
{
    private const string Identity = "tagekyc.capture_runtime_validate_append_authority(uuid,uuid,uuid,bigint,uuid,text,timestamptz)";

    [Fact]
    public async Task Exact47Manifest_RejectsOldOverloadAndMissingNewIdentity()
    {
        await using var db = postgres.CreateDbContext();
        await A1CatalogueProof.AssertOwnersAndRolesAsync(db);
        await A1CatalogueProof.AssertGrantsAsync(db);
        foreach (var mutation in new[]
        {
            """
            CREATE FUNCTION tagekyc.capture_runtime_validate_append_authority(uuid,uuid,uuid,bigint,uuid,uuid,text,timestamptz)
            RETURNS boolean LANGUAGE sql AS 'SELECT false';
            ALTER FUNCTION tagekyc.capture_runtime_validate_append_authority(uuid,uuid,uuid,bigint,uuid,uuid,text,timestamptz) OWNER TO tagekyc_raw_export_deployer;
            REVOKE ALL ON FUNCTION tagekyc.capture_runtime_validate_append_authority(uuid,uuid,uuid,bigint,uuid,uuid,text,timestamptz) FROM PUBLIC;
            """,
            $"ALTER FUNCTION {Identity} RENAME TO a1_missing_append_identity_negative_control"
        })
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            await db.Database.ExecuteSqlRawAsync(mutation);
            await Assert.ThrowsAnyAsync<Exception>(() => A1CatalogueProof.AssertOwnersAndRolesAsync(db));
            await Assert.ThrowsAnyAsync<Exception>(() => A1CatalogueProof.AssertGrantsAsync(db));
            await transaction.RollbackAsync();
        }
        await A1CatalogueProof.AssertOwnersAndRolesAsync(db);
        await A1CatalogueProof.AssertGrantsAsync(db);
    }

    [Theory]
    [InlineData("VerificationSessionId", "discovered_session_id")]
    [InlineData("CaptureCapabilityId", "discovered_capability_id")]
    public async Task BothDiscoveredLockKeys_AreRechecked_WithExecutableRedControl(string column, string local)
    {
        await using var db = postgres.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var binding = await SeedBindingAsync(db);
        Assert.Equal(1, await CountAsync(db, binding));
        var original = await db.Database.SqlQueryRaw<string>($"SELECT pg_catalog.pg_get_functiondef('{Identity}'::regprocedure) AS \"Value\"").SingleAsync();
        var assignment = $"{local}:=v_b.\"{column}\";";
        Assert.Contains(assignment, original, StringComparison.Ordinal);
        var injected = original.Replace(assignment, $"{local}:=pg_catalog.gen_random_uuid();", StringComparison.Ordinal);
        // Authorized mutation seam: corrupt only the discovered local lock key. Production
        // tables, immutability triggers and ACL remain intact. No concurrent illegal UPDATE.
        await db.Database.ExecuteSqlRawAsync(injected);
        Assert.Equal(0, await CountAsync(db, binding));
        var predicate = $" OR v_b.\"{column}\" IS DISTINCT FROM {local}";
        Assert.Contains(predicate, injected, StringComparison.Ordinal);
        var broken = injected.Replace(predicate, "", StringComparison.Ordinal);
        await db.Database.ExecuteSqlRawAsync(broken);
        // RED control: removing just this predicate admits the same corrupt discovery.
        Assert.Equal(1, await CountAsync(db, binding));
        await db.Database.ExecuteSqlRawAsync(original);
        Assert.Equal(1, await CountAsync(db, binding));
        await transaction.RollbackAsync();
        await A1CatalogueProof.AssertOwnersAndRolesAsync(db);
        await A1CatalogueProof.AssertGrantsAsync(db);
    }

    [Fact]
    public async Task SevenArgumentHelper_DerivesSession_AndHasNoOldOverloadOrTableGrant()
    {
        await using var db = postgres.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var binding = await SeedBindingAsync(db);
        await db.Database.ExecuteSqlRawAsync("SET LOCAL ROLE tagekyc_runtime");
        var session = await db.Database.SqlQueryRaw<Guid>(Call(binding, "verification_session_id")).SingleAsync();
        Assert.Equal(Guid.Parse("90000000-0000-4000-8000-000000000001"), session);
        Assert.Equal(0, await CountAsync(db, Guid.NewGuid()));
        Assert.False(await db.Database.SqlQueryRaw<bool>("SELECT pg_catalog.has_table_privilege(current_user,'tagekyc.capture_execution_bindings','SELECT') AS \"Value\"").SingleAsync());
        Assert.True(await db.Database.SqlQueryRaw<bool>($"SELECT pg_catalog.has_function_privilege(current_user,'{Identity}','EXECUTE') AS \"Value\"").SingleAsync());
        Assert.True(await db.Database.SqlQueryRaw<bool>("SELECT pg_catalog.to_regprocedure('tagekyc.capture_runtime_validate_append_authority(uuid,uuid,uuid,bigint,uuid,uuid,text,timestamptz)') IS NULL AS \"Value\"").SingleAsync());
        await db.Database.ExecuteSqlRawAsync("RESET ROLE");
        await A1CatalogueProof.AssertOwnersAndRolesAsync(db);
        await A1CatalogueProof.AssertGrantsAsync(db);
        await transaction.RollbackAsync();
    }

    private static Task<int> CountAsync(DbContext db, Guid binding) =>
        db.Database.SqlQueryRaw<int>(Call(binding, "count(*)::integer")).SingleAsync();
    private static string Call(Guid binding, string projection) => $"""
        SELECT {projection} AS "Value" FROM tagekyc.capture_runtime_validate_append_authority(
        '40000000-0000-4000-8000-000000000001','50000000-0000-4000-8000-000000000001',
        '60000000-0000-4000-8000-000000000001',1,'{binding:D}','CaptureObservation',now())
        """;

    internal static async Task<Guid> SeedBindingAsync(DbContext db, int platformPepperVersion = 1, int capabilityPepperVersion = 1, byte[]? verifierSpki = null)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "TagEkyc.sln"))) root = root.Parent;
        Assert.NotNull(root);
        var source = File.ReadAllText(Path.Combine(root.FullName, "docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c6b_a1_postgresql16_runtime_proof_companion.md"));
        var start = source.IndexOf("INSERT INTO tagekyc.platform_operator_credentials", StringComparison.Ordinal);
        var end = source.IndexOf("-- Publish newer heads", start, StringComparison.Ordinal);
        Assert.True(start > 0 && end > start);
        var seed = source[start..end].Replace("ARRAY['Configuration']", "ARRAY['Bind','CaptureObservation','TrustedEvidence']", StringComparison.Ordinal);
        seed = seed.Replace("repeat('11',32),'hex'),1,", $"repeat('11',32),'hex'),{platformPepperVersion},", StringComparison.Ordinal);
        if (verifierSpki is not null)
            seed = seed.Replace("decode(repeat('04',91),'hex')", $"decode('{Convert.ToHexString(verifierSpki)}','hex')", StringComparison.Ordinal)
                .Replace("decode(repeat('22',32),'hex')", $"decode('{Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(verifierSpki))}','hex')", StringComparison.Ordinal);
        await db.Database.ExecuteSqlRawAsync(seed);
        db.Set<TagEkyc.Infrastructure.Persistence.Entities.VerificationSessionRow>().Add(new()
        {
            Id = Guid.Parse("90000000-0000-4000-8000-000000000001"),
            ClientApplicationId = Guid.Parse("91000000-0000-4000-8000-000000000001"),
            SubjectRef = "synthetic-subject", Profile = "STANDARD_EKYC_PROFILE", Purpose = "SyntheticProof",
            State = "Pending", ExpiresAt = DateTimeOffset.UtcNow.AddDays(1), CreatedAt = DateTimeOffset.UtcNow,
            RequestId = "r", CorrelationId = "c", BindingNonceHash = "synthetic-challenge"
        });
        await db.SaveChangesAsync();
        var issue = await db.Database.SqlQueryRaw<string>($"""
            SELECT result_code AS "Value" FROM tagekyc.capture_runtime_issue_or_replace_capability(
            '91000000-0000-4000-8000-000000000001','90000000-0000-4000-8000-000000000001','Issue',NULL,NULL,
            '92000000-0000-4000-8000-000000000001','93000000-0000-4000-8000-000000000001','mnopqrstuvwx',
            decode(repeat('11',32),'hex'),{capabilityPepperVersion},decode(repeat('22',32),'hex'),now())
            """).SingleAsync();
        Assert.Equal("CREATED", issue);
        var binding = await db.Database.SqlQueryRaw<Guid>("""
            SELECT binding_id AS "Value" FROM tagekyc.capture_runtime_bind_capability(
            '40000000-0000-4000-8000-000000000001','50000000-0000-4000-8000-000000000001',
            '60000000-0000-4000-8000-000000000001',1,'93000000-0000-4000-8000-000000000001',true,
            '94000000-0000-4000-8000-000000000001',decode(repeat('33',32),'hex'),now())
            """).SingleAsync();
        await db.Database.ExecuteSqlRawAsync("SET CONSTRAINTS ALL IMMEDIATE");
        return binding;
    }
}
