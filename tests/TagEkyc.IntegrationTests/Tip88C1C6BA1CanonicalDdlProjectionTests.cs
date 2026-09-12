using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88C1C6BA1CanonicalDdlProjectionTests(PostgresPersistenceFixture postgres)
{
    private const string ExpectedSchema = "a1_expected_catalogue";

    [Fact]
    public async Task CanonicalDdl_IndependentExpectedSchema_ExactColumnsConstraintsIndexes_ThreeMutationsAreRed()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "TagEkyc.sln"))) root = root.Parent;
        Assert.NotNull(root);
        var source = await File.ReadAllTextAsync(Path.Combine(root!.FullName,
            "docs/tips/tip_88c1_secure_raw_source_sealed_assembly/tip_88c1_c6b_a1_literal_ddl_master.md"));
        // Only the finite table/index/ALTER catalogue preceding trigger functions is
        // executable here. No migration source or implementation model is an oracle.
        var boundary = source.IndexOf("CREATE FUNCTION tagekyc.c6ba_reject_row_mutation()", StringComparison.Ordinal);
        Assert.True(boundary > 0);
        var ddl = source[..boundary];
        var tableMatches = Regex.Matches(ddl, @"(?m)^CREATE TABLE tagekyc\.([a-z_]+) \(");
        var tables = tableMatches.Select(m => m.Groups[1].Value).ToArray();
        Assert.Equal(27, tables.Length);
        Assert.Equal(27, tables.Distinct(StringComparer.Ordinal).Count());
        var tableSet = tables.ToHashSet(StringComparer.Ordinal);
        var statements = Regex.Matches(ddl,
            @"(?ms)^(?:CREATE TABLE tagekyc\.[a-z_]+ \(|CREATE (?:UNIQUE )?INDEX |ALTER TABLE tagekyc\.[a-z_]+\b).*?;");
        Assert.Equal(55, statements.Count); // 27 tables, 11 indexes, 17 ALTERs (one external).
        Assert.Equal(27, statements.Count(m => m.Value.StartsWith("CREATE TABLE ", StringComparison.Ordinal)));

        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("a1_canonical_ddl");
        await using var db = isolated.CreateDbContext();
        await using var tx = await db.Database.BeginTransactionAsync();
        await ExecuteExactSql(db, "CREATE SCHEMA " + ExpectedSchema);
        await ExecuteExactSql(db, "SET LOCAL search_path = pg_catalog");
        var indexCount = 0;
        foreach (Match statement in statements)
        {
            var text = statement.Value;
            // The existing verification_sessions key is already present in the real
            // database. It remains an external dependency, never cloned or rewritten.
            if (text.StartsWith("ALTER TABLE tagekyc.verification_sessions", StringComparison.Ordinal)) continue;
            if (text.StartsWith("CREATE INDEX ", StringComparison.Ordinal) ||
                text.StartsWith("CREATE UNIQUE INDEX ", StringComparison.Ordinal)) indexCount++;
            var remapped = Regex.Replace(text, @"\btagekyc\.([a-z_]+)\b", m =>
                tableSet.Contains(m.Groups[1].Value) ? ExpectedSchema + "." + m.Groups[1].Value : m.Value);
            Assert.DoesNotContain("$function$", remapped);
            await ExecuteExactSql(db, remapped);
        }
        Assert.Equal(11, indexCount);
        // Constraint triggers also create pg_constraint rows. Reproduce all
        // canonical user triggers, keeping the existing qualified helper functions.
        // Expected tables stay empty, so their production helpers are never run.
        var triggers = Regex.Matches(source, @"(?m)^CREATE (?:CONSTRAINT )?TRIGGER [^;]+;");
        Assert.Equal(23, triggers.Count);
        foreach (Match trigger in triggers)
        {
            var target = Regex.Match(trigger.Value, @"\bON tagekyc\.([a-z_]+)\b");
            Assert.True(target.Success);
            Assert.Contains(target.Groups[1].Value, tableSet);
            var remapped = Regex.Replace(trigger.Value, @"\bON tagekyc\.([a-z_]+)\b",
                m => "ON " + ExpectedSchema + "." + m.Groups[1].Value);
            await ExecuteExactSql(db, remapped);
        }
        var expected = await Project(db, ExpectedSchema, tables);
        var actual = await Project(db, "tagekyc", tables);
        Assert.NotEmpty(expected);
        Assert.Equal(expected.Length, actual.Length);
        Assert.Equal(expected, actual);

        string[] mutations =
        [
            "ALTER TABLE tagekyc.platform_operator_credentials ADD COLUMN \"UnexpectedProjectionColumn\" integer",
            "ALTER TABLE tagekyc.platform_operator_credentials DROP CONSTRAINT \"CK_platform_operator_credentials_principal\"",
            "DROP INDEX tagekyc.\"IX_platform_operator_credentials_expiry\""
        ];
        foreach (var mutation in mutations)
        {
            await tx.CreateSavepointAsync("projection_negative");
            await ExecuteExactSql(db, mutation);
            var corrupted = await Project(db, "tagekyc", tables);
            Assert.False(expected.SequenceEqual(corrupted, StringComparer.Ordinal), mutation);
            await tx.RollbackToSavepointAsync("projection_negative");
            await tx.ReleaseSavepointAsync("projection_negative");
            Assert.Equal(expected, await Project(db, "tagekyc", tables));
        }
        await tx.RollbackAsync();
    }

    private static async Task ExecuteExactSql(TagEkycDbContext db, string sql)
    {
        // Canonical CHECK regexes contain literal braces. This is SQL text, not
        // an EF composite-format string; submit bytes without escaping/rewriting.
        await using var command = new NpgsqlCommand(sql,
            (NpgsqlConnection)db.Database.GetDbConnection(),
            (NpgsqlTransaction)db.Database.CurrentTransaction!.GetDbTransaction());
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<string[]> Project(TagEkycDbContext db, string schema, string[] tables)
    {
        const string sql = """
            SELECT jsonb_build_array('column',c.relname,a.attname,a.attnum,
                pg_catalog.format_type(a.atttypid,a.atttypmod),a.attnotnull,
                pg_catalog.pg_get_expr(d.adbin,d.adrelid),a.attidentity,a.attgenerated,
                CASE WHEN a.attcollation=0 THEN NULL ELSE cn.nspname||'.'||co.collname END)::text AS "Value"
            FROM pg_catalog.pg_class c
            JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            JOIN pg_catalog.pg_attribute a ON a.attrelid=c.oid AND a.attnum>0 AND NOT a.attisdropped
            LEFT JOIN pg_catalog.pg_attrdef d ON d.adrelid=c.oid AND d.adnum=a.attnum
            LEFT JOIN pg_catalog.pg_collation co ON co.oid=a.attcollation
            LEFT JOIN pg_catalog.pg_namespace cn ON cn.oid=co.collnamespace
            WHERE n.nspname=@schema AND c.relname=ANY(@tables) AND c.relkind='r'
            UNION ALL
            SELECT jsonb_build_array('constraint',c.relname,k.conname,k.contype,
                pg_catalog.pg_get_constraintdef(k.oid,false),k.condeferrable,k.condeferred,k.convalidated)::text
            FROM pg_catalog.pg_constraint k JOIN pg_catalog.pg_class c ON c.oid=k.conrelid
            JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            WHERE n.nspname=@schema AND c.relname=ANY(@tables)
            UNION ALL
            SELECT jsonb_build_array('index',c.relname,ic.relname,
                pg_catalog.pg_get_indexdef(i.indexrelid,0,false),i.indisunique,i.indisprimary,
                i.indisvalid,i.indisready)::text
            FROM pg_catalog.pg_index i JOIN pg_catalog.pg_class c ON c.oid=i.indrelid
            JOIN pg_catalog.pg_class ic ON ic.oid=i.indexrelid
            JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            WHERE n.nspname=@schema AND c.relname=ANY(@tables)
            UNION ALL
            SELECT jsonb_build_array('trigger',c.relname,t.tgname,
                pg_catalog.pg_get_triggerdef(t.oid,false),t.tgenabled,
                t.tgdeferrable,t.tginitdeferred)::text
            FROM pg_catalog.pg_trigger t JOIN pg_catalog.pg_class c ON c.oid=t.tgrelid
            JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            WHERE n.nspname=@schema AND c.relname=ANY(@tables) AND NOT t.tgisinternal
            """;
        var rows = await db.Database.SqlQueryRaw<string>(sql,
            new NpgsqlParameter("schema", schema), new NpgsqlParameter("tables", tables)).ToArrayAsync();
        // Schema relocation is the ONLY normalization. Definition text, names,
        // defaults, order, types, validation flags and counts must otherwise match.
        return rows.Select(row => row.Replace(ExpectedSchema + ".", "tagekyc.", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal).ToArray();
    }
}
