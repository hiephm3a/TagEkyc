using Microsoft.EntityFrameworkCore;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.Infrastructure.RawExport;

public sealed class DurableKeyReadinessException(string code) : InvalidOperationException(code)
{
    public string Code { get; } = code;
}

public sealed class CsprngReadinessValidator(TagEkycDbContext db, DurableKeyCustodyOptions options)
{
    public const string Code = "PROD_RAW_EXPORT_KEY_CSPRNG_UNAVAILABLE";

    public async Task ValidateAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.CsprngExpectedOwner))
            throw new DurableKeyReadinessException(Code);

        try
        {
            var connection = db.Database.GetDbConnection();
            await db.Database.OpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
            SELECT count(*) = 1
            FROM pg_catalog.pg_extension e
            JOIN pg_catalog.pg_namespace n ON n.oid=e.extnamespace
            JOIN pg_catalog.pg_roles er ON er.oid=e.extowner
            JOIN pg_catalog.pg_proc p ON p.pronamespace=n.oid AND p.proname='gen_random_bytes' AND p.pronargs=1
            JOIN pg_catalog.pg_roles pr ON pr.oid=p.proowner
            WHERE e.extname='pgcrypto' AND n.nspname='tagekyc_extensions'
              AND p.oid='tagekyc_extensions.gen_random_bytes(integer)'::regprocedure
              AND pg_catalog.pg_get_function_result(p.oid)='bytea'
              AND er.rolname=@owner AND pr.rolname=@owner
              AND NOT pg_catalog.has_function_privilege('public',p.oid,'EXECUTE')
              AND pg_catalog.has_schema_privilege('tagekyc_raw_export_deployer',n.oid,'USAGE')
              AND pg_catalog.has_function_privilege('tagekyc_raw_export_deployer',p.oid,'EXECUTE')
              AND NOT pg_catalog.has_schema_privilege('public',n.oid,'USAGE')
              AND NOT pg_catalog.has_schema_privilege('tagekyc_runtime',n.oid,'USAGE')
              AND NOT pg_catalog.has_function_privilege('tagekyc_runtime',p.oid,'EXECUTE')
              AND NOT pg_catalog.has_function_privilege('tagekyc_raw_export_custody_encryptor',p.oid,'EXECUTE')
              AND NOT pg_catalog.has_function_privilege('tagekyc_raw_export_reconciler',p.oid,'EXECUTE')
              AND NOT pg_catalog.has_function_privilege('tagekyc_raw_export_lifecycle',p.oid,'EXECUTE')
              AND NOT EXISTS (
                SELECT 1 FROM pg_catalog.aclexplode(COALESCE(p.proacl,pg_catalog.acldefault('f',p.proowner))) a
                WHERE a.grantee=0)
              AND pg_catalog.octet_length(tagekyc_extensions.gen_random_bytes(32))=32
              AND pg_catalog.length(pg_catalog.rtrim(pg_catalog.translate(
                    pg_catalog.encode(tagekyc_extensions.gen_random_bytes(32),'base64'),'+/','-_'),'='))=43
              AND pg_catalog.rtrim(pg_catalog.translate(
                    pg_catalog.encode(tagekyc_extensions.gen_random_bytes(32),'base64'),'+/','-_'),'=')
                    ~ '^[A-Za-z0-9_-]{43}$'
              AND NOT EXISTS (
                SELECT 1 FROM pg_catalog.pg_proc shadow
                JOIN pg_catalog.pg_namespace shadow_ns ON shadow_ns.oid=shadow.pronamespace
                WHERE shadow.proname='gen_random_bytes' AND shadow.pronargs=1
                  AND shadow.oid<>p.oid)
            """;
            var parameter = command.CreateParameter();
            parameter.ParameterName = "owner";
            parameter.Value = options.CsprngExpectedOwner;
            command.Parameters.Add(parameter);
            if (await command.ExecuteScalarAsync(cancellationToken) is not true)
                throw new DurableKeyReadinessException(Code);
        }
        catch (DurableKeyReadinessException)
        {
            throw;
        }
        catch
        {
            throw new DurableKeyReadinessException(Code);
        }
    }
}
