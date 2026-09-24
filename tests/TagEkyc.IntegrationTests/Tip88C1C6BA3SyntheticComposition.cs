using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.IntegrationTests;

// Explicit non-patient composition. No production host loads this assembly.
internal static class Tip88C1C6BA3SyntheticComposition
{
    internal static ConfigurationManager BrokerConfiguration(int port = 45678) => new()
    {
        [RawIngressBrokerOptions.SectionName + ":BaseUri"] = $"http://127.0.0.1:{port}/",
        [RawIngressBrokerOptions.SectionName + ":ListenAddress"] = "127.0.0.1",
        [RawIngressBrokerOptions.SectionName + ":AllowedApiAddresses:0"] = "127.0.0.1",
        [RawIngressBrokerOptions.SectionName + ":RequestTimeoutMilliseconds"] = "1000",
        [RawIngressBrokerOptions.SectionName + ":EvaluationTokenTtlSeconds"] = "10",
        [RawIngressBrokerOptions.SectionName + ":IdempotencyLockTimeoutMilliseconds"] = "100",
        [RawIngressBrokerOptions.SectionName + ":EvaluationOwnerId"] = "8bf2e6a5425a42689647465139fc46b0",
        [RawIngressBrokerOptions.SectionName + ":CommitmentSelectorId"] = "fixture-content-commitment",
        [RawIngressBrokerOptions.SectionName + ":CommitmentSelectorVersion"] = "1",
        [RawIngressBrokerOptions.SectionName + ":SubjectTokenSelectorId"] = "fixture-subject-ref-token",
        [RawIngressBrokerOptions.SectionName + ":SubjectTokenSelectorVersion"] = "1",
        [RawIngressBrokerOptions.SectionName + ":ApiReplicaCount"] = "1",
        [RawIngressBrokerOptions.SectionName + ":ContinuationPollIntervalMilliseconds"] = "100",
    };

    internal static ServiceProvider PreflightServices() => new ServiceCollection()
        .AddTagEkycContentCommitment(Configuration())
        .AddTagEkycSubjectRefToken(Configuration()).BuildServiceProvider();

    internal static ServiceProvider QualifiedBrokerServices(NpgsqlDataSource source,
        RawIngressBrokerOptions options, bool missingKey = false, bool missingProvider = false,
        bool invalidProfile = false)
    {
        var configuration = Configuration();
        if (missingKey) configuration["TagEkyc:RawExport:ContentCommitment:FixtureKeys:fixture-content-commitment:1"] = null;
        var services = new ServiceCollection().AddSingleton(source)
            .AddSingleton<ICustodyProfileProvider>(invalidProfile
                ? new QualifiedBrokerProfiles(new FixtureSourceEncryptionProfileCatalog().GetActive() with { ChunkSize = 0 },
                    new FixtureKekReferenceCatalog().GetActive(), new Profiles(false).TimeBounds)
                : new Profiles(false))
            .AddTagEkycContentCommitment(configuration);
        if (!missingProvider) services.AddTagEkycSubjectRefToken(configuration);
        return services.AddTagEkycRawIngressBrokerTransport(options).BuildServiceProvider();
    }

    // Dedicated disposable PostgreSQL fixture only. This provisions the already
    // ratified LOGIN, never a product fallback or a SET ROLE impersonation.
    internal static async Task<string> BrokerLogin(string administratorConnection)
    {
        await using var connection = new NpgsqlConnection(administratorConnection);
        await connection.OpenAsync();
        var password = Guid.NewGuid().ToString("N"); // synthetic, never emitted
        await using var sql = new NpgsqlCommand($"""
            DO $proof$ BEGIN
              IF NOT EXISTS(SELECT 1 FROM pg_roles WHERE rolname='tagekyc_raw_export_claim_broker_login') THEN
                CREATE ROLE tagekyc_raw_export_claim_broker_login LOGIN INHERIT NOSUPERUSER
                  NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
              END IF;
            END $proof$;
            ALTER ROLE tagekyc_raw_export_claim_broker_login PASSWORD '{password}';
            GRANT tagekyc_runtime,tagekyc_raw_export_claim_broker TO tagekyc_raw_export_claim_broker_login
              WITH ADMIN FALSE, INHERIT TRUE, SET FALSE;
            """, connection);
        await sql.ExecuteNonQueryAsync();
        return new NpgsqlConnectionStringBuilder(administratorConnection)
        {
            Username = QualifiedRawIngressBroker.Login, Password = password, Options = "", Pooling = false,
        }.ConnectionString;
    }

    private static ConfigurationManager Configuration() => new()
    {
        ["TagEkyc:RawExport:ContentCommitment:FixtureKeys:fixture-content-commitment:1"] = "0123456789abcdef0123456789abcdef",
        ["TagEkyc:RawExport:SubjectRefToken:FixtureKeys:fixture-subject-ref-token:1"] = "abcdef0123456789abcdef0123456789",
    };

    internal static ConfigurationManager ObjectConfiguration(ProvisionalObjectCustodyOptions options)
    {
        const string p = "TagEkyc:RawExport:ObjectCustody:";
        return new()
        {
            [p + "Topology"] = options.Topology.ToString(), [p + "Capability"] = options.Capability.ToString(),
            [p + "ServiceUrl"] = options.ServiceUrl!.AbsoluteUri, [p + "BucketName"] = options.BucketName,
            [p + "AccessKeyId"] = options.AccessKeyId, [p + "SecretAccessKey"] = options.SecretAccessKey,
            [p + "AllowLoopbackHttp"] = options.AllowLoopbackHttp.ToString(),
            [p + "MaximumSinglePartCiphertextBytes"] = options.MaximumSinglePartCiphertextBytes.ToString(System.Globalization.CultureInfo.InvariantCulture),
            [p + "OperationTimeoutSeconds"] = options.OperationTimeout.TotalSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };
    }

    internal static async Task<SyntheticCustodyLogins> CustodyLogins(string administratorConnection)
    {
        var logins = new[] { CaptureRuntimeCustodyProviderScopes.WriterLogin,
            CaptureRuntimeCustodyProviderScopes.ReconcilerLogin, CaptureRuntimeCustodyProviderScopes.LifecycleLogin };
        var result = new string[3];
        await using var connection = new NpgsqlConnection(administratorConnection);
        await connection.OpenAsync();
        await using (var check = new NpgsqlCommand("SELECT count(*) FROM pg_catalog.pg_authid WHERE rolname=ANY($1) AND rolpassword IS NULL", connection))
        {
            check.Parameters.AddWithValue(logins);
            if (await check.ExecuteScalarAsync() is not 3L) throw new InvalidOperationException("SYNTHETIC_CUSTODY_LOGIN_BASELINE_INVALID");
        }
        var owner = new SyntheticCustodyLogins(administratorConnection, logins, result);
        try
        {
            for (var i = 0; i < logins.Length; i++)
            {
                var password = Guid.NewGuid().ToString("N"); // dedicated synthetic cluster, never output
                await using var sql = new NpgsqlCommand($"ALTER ROLE {logins[i]} PASSWORD '{password}'", connection);
                await sql.ExecuteNonQueryAsync();
                result[i] = new NpgsqlConnectionStringBuilder(administratorConnection)
                { Username = logins[i], Password = password, Options = "", Pooling = false }.ConnectionString;
            }
            return owner;
        }
        catch { await owner.DisposeAsync(); throw; }
    }

    internal sealed class SyntheticCustodyLogins(string administratorConnection, string[] logins, string[] connections) : IAsyncDisposable
    {
        internal string[] Connections => connections;
        public async ValueTask DisposeAsync()
        {
            // LOGINs are cluster-wide even when the data database is isolated.
            // Restore the exact PASSWORD NULL fixture baseline for later tests.
            await using var connection = new NpgsqlConnection(administratorConnection);
            await connection.OpenAsync();
            foreach (var login in logins)
            {
                await using var sql = new NpgsqlCommand($"ALTER ROLE {login} PASSWORD NULL", connection);
                await sql.ExecuteNonQueryAsync();
            }
        }
    }

    internal static RawIngressBrokerTransactionFacade Broker(NpgsqlDataSource source,
        IContentCommitmentService commitment, ISubjectRefTokenService subject,
        bool extendedBounds = false, int configuredCommitmentVersion = 1,
        RawIngressBrokerTransactionSettings? exactSettings = null) =>
        new(source, new RetainedSourceClaimPreflight(commitment, subject), new Profiles(extendedBounds),
            exactSettings ?? new(Guid.Parse("8bf2e6a5425a42689647465139fc46b0"), extendedBounds ? 3600 : 10, extendedBounds ? 5001 : 100,
                new("fixture-content-commitment", configuredCommitmentVersion), new("fixture-subject-ref-token", 1), extendedBounds ? 6000 : 1000));

    private sealed class Profiles(bool extendedBounds) : ICustodyProfileProvider
    {
        public SourceEncryptionProfileBundle ActiveSourceEncryptionProfile => new FixtureSourceEncryptionProfileCatalog().GetActive();
        public KekReferenceBundle ActiveKekReference => new FixtureKekReferenceCatalog().GetActive();
        public CustodyTimeBounds TimeBounds => new(TimeSpan.FromMilliseconds(extendedBounds ? 7000 : 2000),
            TimeSpan.FromSeconds(120), TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(30));
    }
}
