using Microsoft.EntityFrameworkCore;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88C1C6BA1AclTests(PostgresPersistenceFixture postgres)
{
    [Fact]
    public async Task DatabaseAcl_ExactOwnersGranteesNoTableDmlAndSafeDown()
    {
        await using var db = postgres.CreateDbContext();
        await A1CatalogueProof.AssertOwnersAndRolesAsync(db);
        var leaks = await db.Database.SqlQueryRaw<string>($"""
            SELECT role_name || ':' || c.relname AS "Value"
            FROM (VALUES ('tagekyc_capture_runtime_authenticator'),('tagekyc_capture_runtime_operator'),
              ('tagekyc_capture_runtime_application'),('tagekyc_runtime')) roles(role_name)
            CROSS JOIN pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            WHERE {A1CatalogueProof.TablePredicate} AND c.relkind='r'
              AND pg_catalog.has_table_privilege(role_name,c.oid,'SELECT,INSERT,UPDATE,DELETE')
            """).ToListAsync();
        Assert.Empty(leaks);
    }

    [Fact]
    public async Task Foundation_functions_are_security_definer_deployer_owned_and_not_public()
    {
        await using var db = postgres.CreateDbContext();
        await A1CatalogueProof.AssertOwnersAndRolesAsync(db);
        await A1CatalogueProof.AssertGrantsAsync(db);
        // c6ba_roles_are_canonical and c6ba_reject_row_mutation are SECURITY INVOKER;
        // functions are SECURITY DEFINER. Ownership and ACL still cover all 47.
        var violations = await db.Database.SqlQueryRaw<string>(A1CatalogueProof.FunctionCte + """
            SELECT identity AS "Value" FROM a1
            WHERE identity NOT IN ('tagekyc.c6ba_roles_are_canonical(text[])','tagekyc.c6ba_reject_row_mutation()')
              AND (NOT prosecdef OR NOT ('search_path=pg_catalog'=ANY(COALESCE(proconfig,ARRAY[]::text[]))))
            """).ToListAsync();
        Assert.Empty(violations);
    }

    [Fact]
    public async Task Public_entrypoints_have_an_explicit_single_capability_role_or_the_frozen_dual_runtime_grant()
    {
        await using var db = postgres.CreateDbContext();
        await A1CatalogueProof.AssertGrantsAsync(db);
    }

    [Fact]
    public async Task Stage1_internal_helpers_are_callable_only_by_their_exact_roles()
    {
        await using var db = postgres.CreateDbContext();
        await A1CatalogueProof.AssertGrantsAsync(db);
    }

    [Fact]
    public async Task Owner_role_identity_and_acl_negative_controls_are_discriminating()
    {
        await using var db = postgres.CreateDbContext();
        await A1CatalogueProof.AssertOwnersAndRolesAsync(db);
        await A1CatalogueProof.AssertGrantsAsync(db);
        Assert.ThrowsAny<Exception>(() => A1CatalogueProof.Exact(
            A1CatalogueProof.Tables.Skip(1), A1CatalogueProof.Tables));
        var mutations = new[]
        {
            "ALTER TABLE tagekyc.capture_runtime_cutover_state OWNER TO CURRENT_USER",
            "ALTER TABLE tagekyc.capture_runtime_cutover_state OWNER TO tagekyc_capture_runtime_application",
            "ALTER FUNCTION tagekyc.c6ba_roles_are_canonical(text[]) OWNER TO CURRENT_USER",
            "ALTER ROLE tagekyc_capture_runtime_application LOGIN",
            "ALTER ROLE tagekyc_capture_runtime_application SUPERUSER",
            "ALTER ROLE tagekyc_capture_runtime_application CREATEDB",
            "ALTER ROLE tagekyc_capture_runtime_application CREATEROLE",
            "ALTER ROLE tagekyc_capture_runtime_application REPLICATION",
            "ALTER ROLE tagekyc_capture_runtime_application BYPASSRLS",
            "ALTER ROLE tagekyc_capture_runtime_application NOINHERIT",
            "ALTER ROLE tagekyc_capture_runtime_application PASSWORD 'synthetic-negative-control-only'",
            "GRANT tagekyc_runtime TO tagekyc_capture_runtime_application",
            "GRANT tagekyc_capture_runtime_application TO tagekyc_runtime",
            "GRANT tagekyc_capture_runtime_authenticator TO tagekyc_runtime",
            "GRANT tagekyc_capture_runtime_operator TO tagekyc_runtime"
        };
        foreach (var mutation in mutations)
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            await db.Database.ExecuteSqlRawAsync(mutation);
            await Assert.ThrowsAnyAsync<Exception>(() => A1CatalogueProof.AssertOwnersAndRolesAsync(db));
            await transaction.RollbackAsync();
            await A1CatalogueProof.AssertOwnersAndRolesAsync(db);
        }
        foreach (var role in new[] { "PUBLIC", "tagekyc_raw_export_claim_broker" })
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            await db.Database.ExecuteSqlRawAsync($"GRANT EXECUTE ON FUNCTION tagekyc.capture_runtime_read_cutover_state(text,timestamptz) TO {role}");
            await Assert.ThrowsAnyAsync<Exception>(() => A1CatalogueProof.AssertGrantsAsync(db));
            await transaction.RollbackAsync();
            await A1CatalogueProof.AssertGrantsAsync(db);
        }
        await using (var transaction = await db.Database.BeginTransactionAsync())
        {
            await db.Database.ExecuteSqlRawAsync("""
                CREATE FUNCTION tagekyc.c6ba_roles_are_canonical(integer) RETURNS boolean
                LANGUAGE sql AS 'SELECT true';
                ALTER FUNCTION tagekyc.c6ba_roles_are_canonical(integer) OWNER TO tagekyc_raw_export_deployer;
                REVOKE ALL ON FUNCTION tagekyc.c6ba_roles_are_canonical(integer) FROM PUBLIC;
                """);
            await Assert.ThrowsAnyAsync<Exception>(() => A1CatalogueProof.AssertOwnersAndRolesAsync(db));
            await Assert.ThrowsAnyAsync<Exception>(() => A1CatalogueProof.AssertGrantsAsync(db));
            await transaction.RollbackAsync();
        }
        await using (var transaction = await db.Database.BeginTransactionAsync())
        {
            await db.Database.ExecuteSqlRawAsync("ALTER FUNCTION tagekyc.c6ba_roles_are_canonical(text[]) RENAME TO c6ba_missing_identity_negative_control");
            await Assert.ThrowsAnyAsync<Exception>(() => A1CatalogueProof.AssertOwnersAndRolesAsync(db));
            await Assert.ThrowsAnyAsync<Exception>(() => A1CatalogueProof.AssertGrantsAsync(db));
            await transaction.RollbackAsync();
        }
        await using (var transaction = await db.Database.BeginTransactionAsync())
        {
            var error = await Assert.ThrowsAsync<Npgsql.PostgresException>(() =>
                db.Database.ExecuteSqlRawAsync("DROP ROLE tagekyc_capture_runtime_application"));
            Assert.Equal("2BP01", error.SqlState);
            await transaction.RollbackAsync();
        }
        await A1CatalogueProof.AssertOwnersAndRolesAsync(db);
        await A1CatalogueProof.AssertGrantsAsync(db);
    }

    [Fact]
    public async Task CapabilityRolePreflight_RejectsBothMembershipDirectionsWithoutRepair()
    {
        var migration = new TagEkyc.Infrastructure.Persistence.Migrations.Tip88C1C6BA1Foundation();
        var sql = ((Microsoft.EntityFrameworkCore.Migrations.Operations.SqlOperation)migration.UpOperations[0]).Sql;
        var start = sql.IndexOf("DO $capability_roles$", StringComparison.Ordinal);
        var end = sql.IndexOf("$capability_roles$;", start, StringComparison.Ordinal) + "$capability_roles$;".Length;
        Assert.True(start >= 0 && end > start);
        var actualPreflight = sql[start..end];
        await using var db = postgres.CreateDbContext();
        await db.Database.ExecuteSqlRawAsync(actualPreflight);
        foreach (var role in A1CatalogueProof.Roles)
        foreach (var reverse in new[] { false, true })
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            var grant = reverse ? $"GRANT {role} TO tagekyc_runtime" : $"GRANT tagekyc_runtime TO {role}";
            await db.Database.ExecuteSqlRawAsync(grant);
            var error = await Assert.ThrowsAsync<Npgsql.PostgresException>(() => db.Database.ExecuteSqlRawAsync(actualPreflight));
            Assert.Equal("P0001", error.SqlState);
            Assert.Equal($"TIP88C1C6BA_CAPABILITY_ROLE_INVALID: {role}", error.MessageText);
            await transaction.RollbackAsync();
        }
        await A1CatalogueProof.AssertOwnersAndRolesAsync(db);
    }
}
