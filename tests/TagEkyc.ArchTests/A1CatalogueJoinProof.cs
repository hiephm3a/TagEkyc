using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using TagEkyc.Infrastructure.Persistence.Migrations;

namespace TagEkyc.ArchTests;

internal static class A1CatalogueJoinProof
{
    public static void Verify(string root)
    {
        const string folder = "docs/tips/tip_88c1_secure_raw_source_sealed_assembly/";
        var master = File.ReadAllText(Path.Combine(root, folder + "tip_88c1_c6b_a1_operation_master_r27_metadata_v1.md"));
        var parent = File.ReadAllText(Path.Combine(root, folder + "tip_88c1_c6b_a1_server_identity_control_dispatch_r27_metadata_v1.md"));
        var ddl = File.ReadAllText(Path.Combine(root, folder + "tip_88c1_c6b_a1_literal_ddl_master.md"));
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        typeof(Tip88C1C6BA1Foundation).GetMethod("Up", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(new Tip88C1C6BA1Foundation(), [builder]);
        var sql = string.Join('\n', builder.Operations.OfType<SqlOperation>().Select(op => op.Sql));
        var catalogue = typeof(Tip88C1C6BA1Foundation).Assembly.GetType("TagEkyc.Infrastructure.CaptureRuntime.CaptureRuntimeStartupCatalogue", true)!;
        string[] Rows(string name) => (string[])catalogue.GetField(name, BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        var functions = Rows("Functions"); var tables = Rows("Tables"); var grants = Rows("Grants");
        Assert.Equal(47, functions.Length); Assert.Equal(27, tables.Length);
        Equal(functions, Regex.Matches(sql, @"CREATE FUNCTION\s+(tagekyc\.\w+)\(([\s\S]*?)\)\s+RETURNS", RegexOptions.IgnoreCase)
            .Select(m => Signature(m.Groups[1].Value, m.Groups[2].Value, true)));
        IEnumerable<string> Tables(string text) => Regex.Matches(text, @"CREATE TABLE\s+tagekyc\.(\w+)\s*\(", RegexOptions.IgnoreCase).Select(m => m.Groups[1].Value);
        Equal(tables, Tables(sql)); Equal(tables, Tables(ddl));
        Equal(functions, grants.Select(row => row.Split('|')[0]));
        var owners = Regex.Matches(sql, @"ALTER FUNCTION\s+(tagekyc\.\w+)\(([^)]*)\)\s+OWNER TO\s+(\w+)", RegexOptions.IgnoreCase)
            .Select(m => (Key: Signature(m.Groups[1].Value, m.Groups[2].Value, false), Owner: m.Groups[3].Value)).ToArray();
        Equal(functions, owners.Select(row => row.Key));
        Assert.All(owners, row => Assert.Equal("tagekyc_raw_export_deployer", row.Owner));
        var edges = Regex.Matches(sql, @"GRANT EXECUTE ON FUNCTION\s+([^;]+?)\s+TO\s+([^;]+);", RegexOptions.IgnoreCase)
            .SelectMany(grant => Regex.Matches(grant.Groups[1].Value, @"(tagekyc\.\w+)\(([^)]*)\)")
                .SelectMany(function => grant.Groups[2].Value.Split(',').Select(role =>
                    Signature(function.Groups[1].Value, function.Groups[2].Value, false) + "|" + role.Trim()))).ToArray();
        Equal(grants.SelectMany(row => row.Split('|')[1].Split(',', StringSplitOptions.RemoveEmptyEntries).Select(role => row.Split('|')[0] + "|" + role)), edges);
        var operations = Regex.Matches(master, @"(?m)^### (R\d{2}[ab]?) —[^\r\n]*[\s\S]*?(?=^### |^## |\z)")
            .ToDictionary(m => m.Groups[1].Value, m => m.Value, StringComparer.Ordinal);
        string[] expected = ["R01","R02","R03","R04","R05","R06","R07","R08","R09","R10","R11","R12","R13a","R13b","R14","R15","R16","R17","R18","R19","R20a","R20b","R21","R22","R23a","R23b","R24","R25","R26","R27a","R27b","R28a","R28b"];
        Equal(expected, operations.Keys);
        var parentOperations = Regex.Matches(parent, @"(?m)^\| R(\d{2})(?:–R(\d{2}))?\s")
            .SelectMany(match => Enumerable.Range(int.Parse(match.Groups[1].Value),
                (match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : int.Parse(match.Groups[1].Value))
                - int.Parse(match.Groups[1].Value) + 1).Select(id => $"R{id:00}"));
        Equal(operations.Keys.Select(id => id[..3]).Distinct(StringComparer.Ordinal), parentOperations);
        var names = functions.Select(value => value[..value.IndexOf('(')]).ToHashSet(StringComparer.Ordinal);
        foreach (var (operation, block) in operations)
        {
            foreach (Match reference in Regex.Matches(block, @"tagekyc\.((?:capture_runtime_|c6ba_)[a-z_]+)\("))
                Assert.Contains("tagekyc." + reference.Groups[1].Value, names);
        }
        // Real independent relationship mutations, using the production sets:
        Assert.False(Same(functions, functions.Skip(1)));
        Assert.False(Same(functions, functions.Append("tagekyc.orphan()")));
        Assert.False(Same(functions, functions.Append(functions[0])));
        Assert.False(Same(edges, edges.Skip(1).Append(edges[0] + "_wrong_role")));
        Assert.False(Same(tables, tables.Skip(1).Append("orphan_table")));
        var mapping = Regex.Matches(master, @"(?m)^\| (A1-\d{2}) \| `([^`]+)` \| `([^`]+)` \| ([^\r\n]+) \|")
            .Select(m => (Id: m.Groups[1].Value, Path: m.Groups[2].Value, Method: m.Groups[3].Value)).ToArray();
        var proofIds = Regex.Matches(parent, @"(?m)^\| (A1-\d{2}) `").Select(m => m.Groups[1].Value).ToArray();
        Assert.Equal(34, mapping.Length);
        Equal(Enumerable.Range(1, 34).Select(i => $"A1-{i:00}"), mapping.Select(row => row.Id));
        Equal(proofIds, mapping.Select(row => row.Id));
        bool Declares(string path, string method)
        {
            if (!path.StartsWith("tests/TagEkyc.", StringComparison.Ordinal) || path.Contains("..", StringComparison.Ordinal)) return false;
            var full = Path.Combine(root, path);
            if (!File.Exists(full)) return false;
            return Regex.IsMatch(File.ReadAllText(full),
                @"\bpublic\s+(?:async\s+)?(?:Task(?:<[^>]+>)?|ValueTask|void)\s+" + Regex.Escape(method) + @"\s*\(");
        }
        Assert.All(mapping, row => Assert.True(Declares(row.Path, row.Method), $"Unresolved proof owner: {row.Id} {row.Path} {row.Method}"));
        Assert.False(Same(proofIds, mapping.Skip(1).Select(row => row.Id)));
        Assert.False(Declares(mapping[0].Path + ".missing", mapping[0].Method));
        Assert.False(Declares(mapping[0].Path, mapping[0].Method + "_MissingMutation"));
        // This verifies declaration joins, not execution or all required assertions.
    }
    private static string Signature(string name, string arguments, bool named)
    {
        var types = arguments.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(argument =>
        {
            var type = Regex.Replace(argument.Trim(), @"\s+", " ");
            if (named) type = Regex.Replace(type, @"^\w+\s+", "");
            type = Regex.Replace(type, @"\s+DEFAULT\s+.*$", "", RegexOptions.IgnoreCase);
            return type switch { "timestamptz" => "timestamp with time zone", "int" or "int4" => "integer", "int8" => "bigint", "bool" => "boolean", _ => type };
        });
        return name + "(" + string.Join(", ", types) + ")";
    }
    private static bool Same(IEnumerable<string> expected, IEnumerable<string> actual)
    {
        var left = expected.ToArray(); var right = actual.ToArray();
        return left.Distinct(StringComparer.Ordinal).Count() == left.Length && right.Distinct(StringComparer.Ordinal).Count() == right.Length
            && left.Order(StringComparer.Ordinal).SequenceEqual(right.Order(StringComparer.Ordinal));
    }
    private static void Equal(IEnumerable<string> expected, IEnumerable<string> actual)
    {
        var left = expected.ToArray(); var right = actual.ToArray();
        Assert.True(Same(left, right), "Missing: " + string.Join("; ", left.Except(right))
            + "\nExtra: " + string.Join("; ", right.Except(left))
            + "\nDuplicates: " + string.Join("; ", left.Concat(right).GroupBy(value => value)
                .Where(group => group.Count() > 2).Select(group => group.Key)));
    }
}
