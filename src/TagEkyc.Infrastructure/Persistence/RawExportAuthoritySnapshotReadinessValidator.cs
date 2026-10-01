using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace TagEkyc.Infrastructure.Persistence;

public sealed class RawExportAuthoritySnapshotReadinessException(string code)
    : InvalidOperationException(code)
{
    public string Code { get; } = code;
}

public sealed record RawExportAuthoritySnapshotProfileState(string? Profile, bool IsProduction)
{
    public const string ConfigurationPath = "TagEkyc:RawExport:AuthoritySnapshot:Profile";

    public static RawExportAuthoritySnapshotProfileState Resolve(
        IConfiguration configuration,
        bool isProduction)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return new(configuration[ConfigurationPath], isProduction);
    }
}

public sealed class RawExportAuthoritySnapshotReadinessValidator(
    RawExportAuthoritySnapshotProfileState state,
    TagEkycDbContext dbContext)
{
    public const string ProfileMissing = "PROD_RAW_EXPORT_AUTHORITY_SNAPSHOT_PROFILE_MISSING";
    public const string ProfileInvalid = "PROD_RAW_EXPORT_AUTHORITY_SNAPSHOT_PROFILE_INVALID";
    public const string FixtureActive = "PROD_RAW_EXPORT_AUTHORITY_SNAPSHOT_FIXTURE_ACTIVE";
    public const string ProductionProducerInvalid = "PROD_RAW_EXPORT_AUTHORITY_SNAPSHOT_PRODUCER_INVALID";

    // Production readiness deliberately verifies the controlled producers, not
    // business rows.  The application login has no SELECT on the authority or
    // A3 retention tables; granting it just for readiness would break the
    // least-privilege boundary that these SECURITY DEFINER functions enforce.
    private static readonly ProductionFunction[] ProductionFunctions =
    [
        new(
            "tagekyc.raw_export_begin_production_source_ingress_with_authority(uuid,uuid,text,text,text,uuid,uuid,uuid,integer,text,text,bigint,text,timestamp with time zone,timestamp with time zone,timestamp with time zone,integer,text,integer,uuid,integer,integer)",
            "18eb81e48e0afcb33e7bda88cb2939e3ea5f11232495e1838b862203e4c87591",
            SecurityDefiner: true,
            BrokerExecute: true),
        new(
            "tagekyc.raw_export_begin_source_ingress_with_authority_core(uuid,uuid,text,text,text,uuid,uuid,uuid,integer,text,text,bigint,text,timestamp with time zone,timestamp with time zone,timestamp with time zone,integer,text,integer,uuid,integer,integer,text)",
            "ff4f5c9841e39c6e5c95d2fffe1edac11cfa40a4e6565cc09642240879b954b3",
            SecurityDefiner: true,
            BrokerExecute: false),
        new(
            "tagekyc.raw_export_append_retained_authority_snapshot(uuid,uuid,text,uuid,bigint)",
            "a5bc6a7ac200a4ce40fbedeaa1d3663c9de0c0574da1305efad5680822544046",
            SecurityDefiner: true,
            BrokerExecute: true),
        new(
            "tagekyc.raw_export_append_authority_snapshot(uuid,uuid,uuid,text,uuid,integer,text,text,text,integer,uuid,integer,text,text,timestamp with time zone,text,text,text,timestamp with time zone,timestamp with time zone)",
            "da8e2dbac6555f2d7a00284728ebfb91785d8b22224b878561cc131e6c195767",
            SecurityDefiner: true,
            BrokerExecute: false),
        new(
            "tagekyc.raw_export_withdraw_authority_snapshot(uuid,uuid,uuid,text,bigint,uuid)",
            "6e4065aa9a614f8e0b1c41266b8467d013426670824f17068828bdd8b1cf17b6",
            SecurityDefiner: true,
            BrokerExecute: false),
        new(
            "tagekyc.raw_export_revoke_authority_snapshot(uuid,uuid,uuid,text,bigint,uuid)",
            "351aa273b10f0ecf7062507f9c6aa5f82f1369a90d5d65496521bc5ce678d03a",
            SecurityDefiner: true,
            BrokerExecute: false),
    ];

    public async Task ValidateAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(state.Profile))
            throw new RawExportAuthoritySnapshotReadinessException(ProfileMissing);

        var isFixture = string.Equals(state.Profile, "Fixture", StringComparison.Ordinal);
        var isProduction = string.Equals(state.Profile, "Production", StringComparison.Ordinal);
        if (!isFixture && !isProduction)
            throw new RawExportAuthoritySnapshotReadinessException(ProfileInvalid);
        if (state.IsProduction && isFixture)
            throw new RawExportAuthoritySnapshotReadinessException(FixtureActive);
        if (!state.IsProduction && isProduction)
            throw new RawExportAuthoritySnapshotReadinessException(ProfileInvalid);
        if (!state.IsProduction) return;

        if (!await HasProductionEvidenceProducersAsync(cancellationToken)
            || !await HasAuthorityMutationBoundaryAsync(cancellationToken))
            throw new RawExportAuthoritySnapshotReadinessException(ProductionProducerInvalid);
    }

    private async Task<bool> HasProductionEvidenceProducersAsync(CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(cancellationToken);

        foreach (var expected in ProductionFunctions)
        {
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT p.prosecdef,
                       owner_role.rolname,
                       p.proconfig = ARRAY['search_path=pg_catalog']::text[],
                       pg_catalog.has_function_privilege('public', p.oid, 'EXECUTE'),
                       pg_catalog.has_function_privilege('tagekyc_runtime', p.oid, 'EXECUTE'),
                       pg_catalog.has_function_privilege('tagekyc_raw_export_claim_broker', p.oid, 'EXECUTE'),
                       p.prosrc
                FROM pg_catalog.pg_proc AS p
                JOIN pg_catalog.pg_roles AS owner_role ON owner_role.oid = p.proowner
                WHERE p.oid = pg_catalog.to_regprocedure(@signature);
                """;
            command.Parameters.Add(new NpgsqlParameter("signature", expected.Signature));
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)
                || reader.GetBoolean(0) != expected.SecurityDefiner
                || !string.Equals(reader.GetString(1), "tagekyc_raw_export_deployer", StringComparison.Ordinal)
                || !reader.GetBoolean(2)
                || reader.GetBoolean(3)
                || reader.GetBoolean(4)
                || reader.GetBoolean(5) != expected.BrokerExecute)
            {
                return false;
            }

            var normalizedSource = reader.GetString(6).Replace("\r\n", "\n", StringComparison.Ordinal);
            var sourceSha256 = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(normalizedSource)));
            if (!string.Equals(
                    sourceSha256,
                    expected.NormalizedSourceSha256,
                    StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    private async Task<bool> HasAuthorityMutationBoundaryAsync(CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
              NOT pg_catalog.has_table_privilege(
                    'public', 'tagekyc.raw_export_authority_snapshots',
                    'SELECT,INSERT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER')
              AND NOT pg_catalog.has_table_privilege(
                    'tagekyc_runtime', 'tagekyc.raw_export_authority_snapshots',
                    'SELECT,INSERT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER')
              AND NOT pg_catalog.has_table_privilege(
                    'tagekyc_raw_export_claim_broker', 'tagekyc.raw_export_authority_snapshots',
                    'SELECT,INSERT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER');
            """;
        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }

    private sealed record ProductionFunction(
        string Signature,
        string NormalizedSourceSha256,
        bool SecurityDefiner,
        bool BrokerExecute);
}
