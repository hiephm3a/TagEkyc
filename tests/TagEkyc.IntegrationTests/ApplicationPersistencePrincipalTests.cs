using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;
using TagEkyc.Api;
using TagEkyc.Application;
using TagEkyc.Application.LocalDev;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.BusinessConsumer;
using TagEkyc.Contracts.CaptureAgent;
using TagEkyc.Contracts.Common;
using TagEkyc.Contracts.TrustedAdapter;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.Persistence.Entities;
using TagEkyc.Infrastructure.Auth;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class ApplicationPersistencePrincipalTests(PostgresPersistenceFixture postgres)
{
    private const string Current = "20261002120000_ProductionApplicationPersistencePrincipal";
    private const string Previous = "20260930082453_OpenBaoProductionKekProvider";

    [Fact]
    public async Task Exact_ordinary_login_runs_database_and_api_key_readiness_and_resolves_core_and_recipient_keys()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("application_persistence_auth");
        await using var admin = isolated.CreateDbContext();
        var login = "application_auth_" + Guid.NewGuid().ToString("N");
        var password = Guid.NewGuid().ToString("N");
        await admin.Database.ExecuteSqlRawAsync($"""
            CREATE ROLE {login} LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE
              NOREPLICATION NOBYPASSRLS INHERIT PASSWORD '{password}';
            GRANT tagekyc_runtime TO {login} WITH ADMIN FALSE, INHERIT TRUE, SET FALSE;
            GRANT tagekyc_application_persistence TO {login} WITH ADMIN FALSE, INHERIT TRUE, SET FALSE;
            """);
        try
        {
            var pepper = new ApiKeyStorePepper(SHA256.HashData("application-persistence-proof"u8.ToArray()));
            var core = await new ApiKeyProvisioningService(
                admin, pepper, new LocalDevRuntimePolicySource(), new RandomManagedApiKeyGenerator())
                .ProvisionAsync(new(
                    LocalDevRuntimePolicySource.BusinessClientId,
                    AuthenticatedCallerCategory.BusinessConsumer,
                    new HashSet<string>(StringComparer.Ordinal) { "business.session.create" }));
            var capture = await new ApiKeyProvisioningService(
                admin, pepper, new LocalDevRuntimePolicySource(), new RandomManagedApiKeyGenerator())
                .ProvisionAsync(new(
                    LocalDevRuntimePolicySource.BusinessClientId,
                    AuthenticatedCallerCategory.CaptureAgent,
                    new HashSet<string>(StringComparer.Ordinal) { "capture.artifact.append" },
                    PrincipalId: Guid.NewGuid(),
                    AllowedClientApplicationIds: new HashSet<Guid> { LocalDevRuntimePolicySource.BusinessClientId },
                    AllowedCaptureAgentIds: new HashSet<string>(StringComparer.Ordinal) { "production-capture-proof" }));
            var trusted = await new ApiKeyProvisioningService(
                admin, pepper, new LocalDevRuntimePolicySource(), new RandomManagedApiKeyGenerator())
                .ProvisionAsync(new(
                    LocalDevRuntimePolicySource.BusinessClientId,
                    AuthenticatedCallerCategory.TrustedAdapter,
                    new HashSet<string>(StringComparer.Ordinal) { "trusted.evidence.append" },
                    PrincipalId: Guid.NewGuid(),
                    AllowedClientApplicationIds: new HashSet<Guid> { LocalDevRuntimePolicySource.BusinessClientId }));

            var recipientClient = Guid.NewGuid();
            var recipientPrincipal = Guid.NewGuid();
            var recipientScopes = RecipientManagementCodec.DeliveryOperatorScopes
                .ToHashSet(StringComparer.Ordinal);
            var recipient = await new ApiKeyProvisioningService(
                admin, pepper, new ExactScopePolicyProvider(recipientClient, recipientScopes),
                new RandomManagedApiKeyGenerator())
                .ProvisionAsync(new(
                    recipientClient,
                    AuthenticatedCallerCategory.BusinessConsumer,
                    recipientScopes,
                    PrincipalId: recipientPrincipal));
            var downloadClient = Guid.NewGuid();
            var downloadPrincipal = Guid.NewGuid();
            var downloadScopes = RecipientManagementCodec.DownloadOnlyScopes
                .ToHashSet(StringComparer.Ordinal);
            var download = await new ApiKeyProvisioningService(
                admin, pepper, new ExactScopePolicyProvider(downloadClient, downloadScopes),
                new RandomManagedApiKeyGenerator())
                .ProvisionAsync(new(
                    downloadClient,
                    AuthenticatedCallerCategory.BusinessConsumer,
                    downloadScopes,
                    PrincipalId: downloadPrincipal));
            var now = DateTimeOffset.UtcNow;
            admin.RawExportManagedRecipientIdentities.Add(new()
            {
                RecipientClientApplicationId = recipientClient,
                PrincipalId = recipientPrincipal,
                State = "Active",
                Revision = 1,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            });
            admin.RawExportManagedRecipientPolicies.Add(new()
            {
                RecipientClientApplicationId = recipientClient,
                ActivationProfile = RecipientManagementCodec.DeliveryOperatorProfile,
                ActivationScopesDigest = RecipientManagementCodec.ScopeSetDigest(recipientScopes),
                State = "Active",
                Revision = 1,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            });
            admin.RawExportManagedRecipientCredentials.Add(new()
            {
                ApiKeyId = recipient.ApiKeyId,
                RecipientClientApplicationId = recipientClient,
                PrincipalId = recipientPrincipal,
                CredentialVersion = 1,
                State = "Active",
                Revision = 1,
                IssuedAtUtc = now,
            });
            admin.RawExportManagedRecipientIdentities.Add(new()
            {
                RecipientClientApplicationId = downloadClient,
                PrincipalId = downloadPrincipal,
                State = "Active",
                Revision = 1,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            });
            admin.RawExportManagedRecipientPolicies.Add(new()
            {
                RecipientClientApplicationId = downloadClient,
                ActivationProfile = RecipientManagementCodec.DownloadOnlyProfile,
                ActivationScopesDigest = RecipientManagementCodec.ScopeSetDigest(downloadScopes),
                State = "Active",
                Revision = 1,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            });
            admin.RawExportManagedRecipientCredentials.Add(new()
            {
                ApiKeyId = download.ApiKeyId,
                RecipientClientApplicationId = downloadClient,
                PrincipalId = downloadPrincipal,
                CredentialVersion = 1,
                State = "Active",
                Revision = 1,
                IssuedAtUtc = now,
            });
            await admin.SaveChangesAsync();

            var restrictedConnection = new NpgsqlConnectionStringBuilder(admin.Database.GetConnectionString())
            {
                Username = login,
                Password = password,
                Pooling = false,
            }.ConnectionString;
            await using var restricted = new TagEkycDbContext(
                new DbContextOptionsBuilder<TagEkycDbContext>().UseNpgsql(restrictedConnection).Options);
            await new PostgresProductionReadinessValidator(restricted).ValidateAsync(default);
            await new ApiKeyStoreProductionReadinessValidator(restricted, pepper).ValidateAsync(default);
            var store = new PostgresHashedApiKeyStore(restricted, pepper);
            Assert.Equal(core.ApiKeyId, (await store.FindByPresentedKeyAsync(core.PresentedKey))?.ApiKeyId);
            Assert.Equal(capture.ApiKeyId, (await store.FindByPresentedKeyAsync(capture.PresentedKey))?.ApiKeyId);
            Assert.Equal(trusted.ApiKeyId, (await store.FindByPresentedKeyAsync(trusted.PresentedKey))?.ApiKeyId);
            Assert.Equal(recipient.ApiKeyId, (await store.FindByPresentedKeyAsync(recipient.PresentedKey))?.ApiKeyId);
            Assert.Equal(download.ApiKeyId, (await store.FindByPresentedKeyAsync(download.PresentedKey))?.ApiKeyId);

            var authenticator = new C5CredentialAwareApiKeyAuthenticator(store, new LocalDevRuntimePolicySource());
            foreach (var (material, scope, category) in new[]
                     {
                         (core, "business.session.create", AuthenticatedCallerCategory.BusinessConsumer),
                         (capture, "capture.artifact.append", AuthenticatedCallerCategory.CaptureAgent),
                         (trusted, "trusted.evidence.append", AuthenticatedCallerCategory.TrustedAdapter),
                         (recipient, "business.raw-export.job.manage", AuthenticatedCallerCategory.BusinessConsumer),
                         (download, "business.raw-export.package.download", AuthenticatedCallerCategory.BusinessConsumer),
                     })
            {
                var context = new DefaultHttpContext();
                context.Request.Headers["X-TagEkyc-Api-Key"] = material.PresentedKey;
                var authentication = await authenticator.AuthenticateAsync(context, scope);
                Assert.True(authentication.IsSuccess);
                Assert.Equal(category, authentication.Value!.CallerCategory);
            }

            var deniedInsert = await Assert.ThrowsAsync<PostgresException>(async () =>
                await restricted.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO tagekyc.api_keys (\"ApiKeyId\") VALUES ({Guid.NewGuid()})"));
            Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, deniedInsert.SqlState);

            await admin.Database.ExecuteSqlRawAsync(
                "REVOKE SELECT ON public.\"__EFMigrationsHistory\" FROM tagekyc_application_persistence");
            try
            {
                var privilegeGap = await Assert.ThrowsAsync<PostgresProductionReadinessException>(
                    () => new PostgresProductionReadinessValidator(restricted).ValidateAsync(default));
                Assert.Equal(PostgresProductionReadinessValidator.PrivilegeInvalid, privilegeGap.Code);
                Assert.NotEqual(PostgresProductionReadinessValidator.MigrationHistoryMissing, privilegeGap.Code);
                Assert.NotEqual(PostgresProductionReadinessValidator.MigrationsPending, privilegeGap.Code);
            }
            finally
            {
                await admin.Database.ExecuteSqlRawAsync(
                    "GRANT SELECT ON public.\"__EFMigrationsHistory\" TO tagekyc_application_persistence");
            }
            await new PostgresProductionReadinessValidator(restricted).ValidateAsync(default);

            foreach (var table in new[]
                     {
                         "raw_export_managed_recipient_credentials",
                         "raw_export_managed_recipient_identities",
                         "raw_export_managed_recipient_policies",
                     })
            {
                Assert.False(await ScalarAsync<bool>(admin,
                    $"SELECT pg_catalog.has_table_privilege('tagekyc_application_persistence','tagekyc.{table}','SELECT,INSERT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER')"));
                Assert.False(await ScalarAsync<bool>(admin,
                    $"SELECT pg_catalog.has_any_column_privilege('tagekyc_application_persistence','tagekyc.{table}','SELECT,INSERT,UPDATE,REFERENCES')"));
                Assert.True(await ScalarAsync<bool>(admin,
                    $"SELECT pg_catalog.has_table_privilege('tagekyc_runtime','tagekyc.{table}','SELECT')"));
                Assert.True(await ScalarAsync<bool>(restricted,
                    $"SELECT pg_catalog.has_table_privilege(current_user,'tagekyc.{table}','SELECT')"));
            }
        }
        finally
        {
            await admin.Database.ExecuteSqlRawAsync($"DROP OWNED BY {login}; DROP ROLE {login};");
        }
    }

    [Fact]
    public async Task Real_program_http_flow_uses_the_restricted_ordinary_api_login()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("verification_persistence_http");
        await using var admin = isolated.CreateDbContext();
        var login = "verification_http_" + Guid.NewGuid().ToString("N");
        var password = Guid.NewGuid().ToString("N");
        await admin.Database.ExecuteSqlRawAsync($"""
            CREATE ROLE {login} LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE
              NOREPLICATION NOBYPASSRLS INHERIT PASSWORD '{password}';
            GRANT tagekyc_runtime TO {login} WITH ADMIN FALSE, INHERIT TRUE, SET FALSE;
            GRANT tagekyc_application_persistence TO {login} WITH ADMIN FALSE, INHERIT TRUE, SET FALSE;
            """);
        try
        {
            var restrictedConnection = new NpgsqlConnectionStringBuilder(admin.Database.GetConnectionString())
            {
                Username = login,
                Password = password,
                Pooling = false,
            }.ConnectionString;

            using var factory = new HistoricalPreparedWebApplicationFactory()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseSetting("TagEkyc:Persistence:Provider", "Postgres");
                    builder.UseSetting("TagEkyc:Persistence:ConnectionString", restrictedConnection);
                    builder.ConfigureTestServices(services =>
                    {
                        services.RemoveAll<IApiKeyAuthenticator>();
                        services.RemoveAll<IHostedService>();
                        services.AddSingleton<IApiKeyAuthenticator, PathAuthenticator>();
                    });
                });
            using var client = factory.CreateClient();

            using var create = await client.PostAsJsonAsync("/api/ekyc/verification-sessions",
                new CreateVerificationSessionRequestDto(
                    "restricted-http-session",
                    "subject-ref",
                    "PATIENT_REGISTRATION",
                    VerificationProfileDto.StandardEkycProfile,
                    [new RequiredCheckRequestDto(RequiredCheckTypeDto.CaptureQuality, true, null)],
                    DateTimeOffset.UtcNow.AddMinutes(30),
                    RequestId: "req-http-create",
                    CorrelationId: "corr-http-create"));
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            using var created = await JsonDocument.ParseAsync(await create.Content.ReadAsStreamAsync());
            var verificationSessionId = created.RootElement.GetProperty("verificationSessionId").GetString();
            Assert.False(string.IsNullOrWhiteSpace(verificationSessionId));

            using var artifactRequest = new HttpRequestMessage(HttpMethod.Post,
                $"/api/ekyc/verification-sessions/{verificationSessionId}/capture-artifacts")
            {
                Content = JsonContent.Create(new CaptureArtifactSubmissionRequestDto(
                    CaptureArtifactTypeDto.DeviceCaptureMetadata,
                    CaptureSourceDto.MobileSdk,
                    "ldev_capture",
                    "device-1",
                    ArtifactHash: null,
                    MetadataHash: "sha256:metadata",
                    RequestId: "req-http-artifact",
                    CorrelationId: "corr-http-artifact"))
            };
            artifactRequest.Headers.Add("Idempotency-Key", "restricted-http-artifact");
            using var artifactResponse = await client.SendAsync(artifactRequest);
            Assert.Equal(HttpStatusCode.Created, artifactResponse.StatusCode);
            using var artifact = await JsonDocument.ParseAsync(await artifactResponse.Content.ReadAsStreamAsync());
            var captureArtifactId = artifact.RootElement.GetProperty("captureArtifactId").GetString();
            Assert.False(string.IsNullOrWhiteSpace(captureArtifactId));

            using var evidenceRequest = new HttpRequestMessage(HttpMethod.Post,
                $"/api/ekyc/verification-sessions/{verificationSessionId}/evidence-results")
            {
                Content = JsonContent.Create(new EvidenceResultSubmissionRequestDto(
                    EvidenceResultTypeDto.CaptureQuality,
                    [captureArtifactId],
                    VerificationResultDto.Passed,
                    0.9m,
                    [],
                    RetryReasonCode: null,
                    SanitizedSummaryRef: "summary:capture-quality",
                    PayloadHash: "sha256:payload",
                    SignaturePlaceholderStatusDto.PlaceholderUnverified,
                    "restricted-http-engine",
                    "1",
                    RequestId: "req-http-evidence",
                    CorrelationId: "corr-http-evidence"))
            };
            evidenceRequest.Headers.Add("Idempotency-Key", "restricted-http-evidence");
            using var evidenceResponse = await client.SendAsync(evidenceRequest);
            Assert.Equal(HttpStatusCode.Created, evidenceResponse.StatusCode);

            using var complete = await client.PostAsJsonAsync(
                $"/api/ekyc/verification-sessions/{verificationSessionId}/complete",
                new CompleteVerificationSessionRequestDto(
                    RequestId: "req-http-complete",
                    CorrelationId: "corr-http-complete"));
            Assert.Equal(HttpStatusCode.OK, complete.StatusCode);
        }
        finally
        {
            await admin.Database.ExecuteSqlRawAsync($"DROP OWNED BY {login}; DROP ROLE {login};");
        }
    }

    [Fact]
    public async Task Migration_down_removes_database_privileges_and_reapply_restores_the_exact_surface()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("verification_persistence_down");
        await using var db = isolated.CreateDbContext();
        var migrator = db.GetService<IMigrator>();

        Assert.Equal(Current, (await db.Database.GetAppliedMigrationsAsync()).Last());
        await migrator.MigrateAsync(Previous);
        Assert.Equal(Previous, (await db.Database.GetAppliedMigrationsAsync()).Last());
        Assert.False(await ScalarAsync<bool>(db,
            "SELECT pg_catalog.has_table_privilege('tagekyc_application_persistence','tagekyc.verification_sessions','SELECT')"));
        Assert.False(await ScalarAsync<bool>(db,
            "SELECT pg_catalog.has_any_column_privilege('tagekyc_application_persistence','tagekyc.verification_sessions','UPDATE')"));

        await migrator.MigrateAsync(Current);
        Assert.Equal(Current, (await db.Database.GetAppliedMigrationsAsync()).Last());
        Assert.True(await ScalarAsync<bool>(db,
            "SELECT pg_catalog.has_table_privilege('tagekyc_application_persistence','tagekyc.verification_sessions','SELECT')"));
        Assert.True(await ScalarAsync<bool>(db,
            "SELECT pg_catalog.has_column_privilege('tagekyc_application_persistence','tagekyc.verification_sessions','State','UPDATE')"));
        Assert.False(await ScalarAsync<bool>(db,
            "SELECT pg_catalog.has_column_privilege('tagekyc_application_persistence','tagekyc.verification_sessions','SubjectRef','UPDATE')"));
    }

    [Fact]
    public async Task Runtime_role_is_denied_non_ratified_update_delete_and_truncate_on_sessions()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("application_runtime_denial");
        await using var admin = isolated.CreateDbContext();
        var login = "runtime_denial_" + Guid.NewGuid().ToString("N");
        var password = Guid.NewGuid().ToString("N");
        await admin.Database.ExecuteSqlRawAsync($"""
            CREATE ROLE {login} LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE
              NOREPLICATION NOBYPASSRLS INHERIT PASSWORD '{password}';
            GRANT tagekyc_runtime TO {login} WITH ADMIN FALSE, INHERIT TRUE, SET FALSE;
            """);
        try
        {
            var restrictedConnection = new NpgsqlConnectionStringBuilder(admin.Database.GetConnectionString())
            {
                Username = login,
                Password = password,
                Pooling = false,
            }.ConnectionString;
            await using var connection = new NpgsqlConnection(restrictedConnection);
            await connection.OpenAsync();
            foreach (var sql in new[]
                     {
                         "UPDATE tagekyc.verification_sessions SET \"SubjectRef\"='forbidden' WHERE false",
                         "DELETE FROM tagekyc.verification_sessions WHERE false",
                         "TRUNCATE TABLE tagekyc.verification_sessions",
                     })
            {
                await using var command = new NpgsqlCommand(sql, connection);
                var denied = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
                Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, denied.SqlState);
            }
        }
        finally
        {
            await admin.Database.ExecuteSqlRawAsync($"DROP OWNED BY {login}; DROP ROLE {login};");
        }
    }

    [Fact]
    public async Task Readiness_requires_the_exact_role_and_privilege_boundary()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("verification_persistence_acl");
        await using var admin = isolated.CreateDbContext();
        var login = "verification_acl_" + Guid.NewGuid().ToString("N");
        var rogueLogin = "verification_rogue_" + Guid.NewGuid().ToString("N");
        var password = Guid.NewGuid().ToString("N");
        await admin.Database.ExecuteSqlRawAsync($"""
            CREATE ROLE {login} LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE
              NOREPLICATION NOBYPASSRLS INHERIT PASSWORD '{password}';
            GRANT tagekyc_runtime TO {login} WITH ADMIN FALSE, INHERIT TRUE, SET FALSE;
            GRANT tagekyc_application_persistence TO {login} WITH ADMIN FALSE, INHERIT TRUE, SET FALSE;
            """);
        try
        {
            var restrictedConnection = new NpgsqlConnectionStringBuilder(admin.Database.GetConnectionString())
            {
                Username = login,
                Password = password,
                Pooling = false,
            }.ConnectionString;
            await using var restricted = new TagEkycDbContext(
                new DbContextOptionsBuilder<TagEkycDbContext>().UseNpgsql(restrictedConnection).Options);
            var validator = new ApplicationPersistenceReadinessValidator(restricted);

            await validator.ValidateAsync(CancellationToken.None);
            var databaseIdentifier = await ScalarAsync<string>(admin,
                "SELECT pg_catalog.quote_ident(current_database())");
            var databaseOwner = await ScalarAsync<string>(admin, """
                SELECT pg_catalog.quote_ident(pg_catalog.pg_get_userbyid(datdba))
                FROM pg_catalog.pg_database
                WHERE datname=current_database()
                """);
            Assert.False(await ScalarAsync<bool>(restricted,
                "SELECT pg_catalog.has_database_privilege(current_user,current_database(),'CREATE')"));
            Assert.False(await ScalarAsync<bool>(admin,
                "SELECT pg_catalog.has_table_privilege('tagekyc_runtime','tagekyc.verification_sessions','SELECT')"));
            Assert.False(await ScalarAsync<bool>(admin,
                "SELECT pg_catalog.has_table_privilege('tagekyc_application_persistence','tagekyc.raw_export_source_reservations','SELECT')"));
            Assert.False(await ScalarAsync<bool>(admin,
                "SELECT pg_catalog.has_any_column_privilege('tagekyc_application_persistence','tagekyc.raw_export_source_reservations','SELECT,INSERT,UPDATE,REFERENCES')"));

            await AssertMutationAsync(
                admin, validator,
                $"GRANT CREATE ON DATABASE {databaseIdentifier} TO {login}",
                $"REVOKE CREATE ON DATABASE {databaseIdentifier} FROM {login}",
                ApplicationPersistenceReadinessValidator.PrivilegeInvalid);

            await AssertMutationAsync(
                admin, validator,
                $"ALTER DATABASE {databaseIdentifier} OWNER TO {login}",
                $"ALTER DATABASE {databaseIdentifier} OWNER TO {databaseOwner}",
                ApplicationPersistenceReadinessValidator.PrivilegeInvalid);

            await AssertMutationAsync(
                admin, validator,
                $"REVOKE tagekyc_application_persistence FROM {login}",
                $"GRANT tagekyc_application_persistence TO {login} WITH ADMIN FALSE, INHERIT TRUE, SET FALSE",
                ApplicationPersistenceReadinessValidator.RoleInvalid);

            await AssertMutationAsync(
                admin, validator,
                $"CREATE ROLE {rogueLogin} LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS INHERIT; GRANT tagekyc_application_persistence TO {rogueLogin} WITH ADMIN FALSE, INHERIT TRUE, SET FALSE",
                $"REVOKE tagekyc_application_persistence FROM {rogueLogin}; DROP ROLE {rogueLogin}",
                ApplicationPersistenceReadinessValidator.RoleInvalid);

            await AssertMutationAsync(
                admin, validator,
                "REVOKE SELECT ON tagekyc.evidence_results FROM tagekyc_application_persistence",
                "GRANT SELECT ON tagekyc.evidence_results TO tagekyc_application_persistence",
                ApplicationPersistenceReadinessValidator.PrivilegeInvalid);

            await AssertMutationAsync(
                admin, validator,
                "GRANT DELETE ON tagekyc.audit_events TO tagekyc_application_persistence",
                "REVOKE DELETE ON tagekyc.audit_events FROM tagekyc_application_persistence",
                ApplicationPersistenceReadinessValidator.PrivilegeInvalid);

            await AssertMutationAsync(
                admin, validator,
                $"GRANT DELETE ON tagekyc.audit_events TO {login}",
                $"REVOKE DELETE ON tagekyc.audit_events FROM {login}",
                ApplicationPersistenceReadinessValidator.PrivilegeInvalid);

            await AssertMutationAsync(
                admin, validator,
                $"GRANT UPDATE (\"SubjectRef\") ON tagekyc.verification_sessions TO {login}",
                $"REVOKE UPDATE (\"SubjectRef\") ON tagekyc.verification_sessions FROM {login}",
                ApplicationPersistenceReadinessValidator.PrivilegeInvalid);

            await AssertMutationAsync(
                admin, validator,
                "GRANT REFERENCES (\"Id\") ON tagekyc.evidence_results TO tagekyc_application_persistence",
                "REVOKE REFERENCES (\"Id\") ON tagekyc.evidence_results FROM tagekyc_application_persistence",
                ApplicationPersistenceReadinessValidator.PrivilegeInvalid);

            await AssertMutationAsync(
                admin, validator,
                "GRANT SELECT ON tagekyc.verification_sessions TO tagekyc_runtime",
                "REVOKE SELECT ON tagekyc.verification_sessions FROM tagekyc_runtime",
                ApplicationPersistenceReadinessValidator.PrivilegeInvalid);

            await AssertMutationAsync(
                admin, validator,
                "GRANT SELECT (\"SourceArtifactId\") ON tagekyc.raw_export_source_reservations TO tagekyc_application_persistence",
                "REVOKE SELECT (\"SourceArtifactId\") ON tagekyc.raw_export_source_reservations FROM tagekyc_application_persistence",
                ApplicationPersistenceReadinessValidator.PrivilegeInvalid);

            await admin.Database.ExecuteSqlRawAsync("""
                CREATE VIEW tagekyc.__vp_leak_probe AS
                  SELECT "SourceArtifactId"
                  FROM tagekyc.raw_export_source_reservations;
                GRANT SELECT ON tagekyc.__vp_leak_probe TO tagekyc_application_persistence;
                """);
            try
            {
                Assert.Equal(0L, await ScalarAsync<long>(
                    restricted,
                    "SELECT count(*) FROM tagekyc.__vp_leak_probe"));
                var viewLeak = await Assert.ThrowsAsync<ApplicationPersistenceReadinessException>(
                    () => validator.ValidateAsync(CancellationToken.None));
                Assert.Equal(ApplicationPersistenceReadinessValidator.PrivilegeInvalid, viewLeak.Code);
            }
            finally
            {
                await admin.Database.ExecuteSqlRawAsync("""
                    REVOKE SELECT ON tagekyc.__vp_leak_probe FROM tagekyc_application_persistence;
                    DROP VIEW tagekyc.__vp_leak_probe;
                    """);
            }
            await validator.ValidateAsync(CancellationToken.None);

            await AssertMutationAsync(
                admin, validator,
                "GRANT UPDATE ON tagekyc.api_keys TO tagekyc_application_persistence",
                "REVOKE UPDATE ON tagekyc.api_keys FROM tagekyc_application_persistence",
                ApplicationPersistenceReadinessValidator.PrivilegeInvalid);

            await AssertMutationAsync(
                admin, validator,
                "GRANT INSERT ON public.\"__EFMigrationsHistory\" TO tagekyc_application_persistence",
                "REVOKE INSERT ON public.\"__EFMigrationsHistory\" FROM tagekyc_application_persistence",
                ApplicationPersistenceReadinessValidator.PrivilegeInvalid);

            await admin.Database.ExecuteSqlRawAsync("""
                CREATE SCHEMA __application_persistence_probe;
                CREATE TABLE __application_persistence_probe.leak(id integer);
                GRANT USAGE ON SCHEMA __application_persistence_probe TO tagekyc_application_persistence;
                GRANT SELECT ON __application_persistence_probe.leak TO tagekyc_application_persistence;
                """);
            try
            {
                var crossSchema = await Assert.ThrowsAsync<ApplicationPersistenceReadinessException>(
                    () => validator.ValidateAsync(default));
                Assert.Equal(ApplicationPersistenceReadinessValidator.PrivilegeInvalid, crossSchema.Code);
            }
            finally
            {
                await admin.Database.ExecuteSqlRawAsync("""
                    REVOKE SELECT ON __application_persistence_probe.leak FROM tagekyc_application_persistence;
                    REVOKE USAGE ON SCHEMA __application_persistence_probe FROM tagekyc_application_persistence;
                    DROP SCHEMA __application_persistence_probe CASCADE;
                    """);
            }
            await validator.ValidateAsync(default);

            await admin.Database.ExecuteSqlRawAsync("""
                CREATE VIEW public.__application_persistence_view_probe
                WITH (security_invoker=false) AS
                  SELECT "SourceArtifactId" FROM tagekyc.raw_export_source_reservations;
                GRANT SELECT ON public.__application_persistence_view_probe TO PUBLIC;
                """);
            try
            {
                Assert.Equal(0L, await ScalarAsync<long>(restricted,
                    "SELECT count(*) FROM public.__application_persistence_view_probe"));
                var publicView = await Assert.ThrowsAsync<ApplicationPersistenceReadinessException>(
                    () => validator.ValidateAsync(default));
                Assert.Equal(ApplicationPersistenceReadinessValidator.PrivilegeInvalid, publicView.Code);
            }
            finally
            {
                await admin.Database.ExecuteSqlRawAsync("""
                    REVOKE SELECT ON public.__application_persistence_view_probe FROM PUBLIC;
                    DROP VIEW public.__application_persistence_view_probe;
                    """);
            }
            await validator.ValidateAsync(default);

            var owner = await ScalarAsync<string>(admin, "SELECT current_user");
            await AssertMutationAsync(
                admin, validator,
                "CREATE SCHEMA __application_owned_probe AUTHORIZATION tagekyc_application_persistence",
                $"ALTER SCHEMA __application_owned_probe OWNER TO {owner}; DROP SCHEMA __application_owned_probe",
                ApplicationPersistenceReadinessValidator.PrivilegeInvalid);

            await AssertMutationAsync(
                admin, validator,
                "CREATE TABLE public.__application_owned_relation_probe(id integer); ALTER TABLE public.__application_owned_relation_probe OWNER TO tagekyc_application_persistence",
                $"ALTER TABLE public.__application_owned_relation_probe OWNER TO {owner}; DROP TABLE public.__application_owned_relation_probe",
                ApplicationPersistenceReadinessValidator.PrivilegeInvalid);

            await AssertMutationAsync(
                admin, validator,
                "CREATE FUNCTION public.__application_owned_function_probe() RETURNS integer LANGUAGE sql AS 'SELECT 1'; ALTER FUNCTION public.__application_owned_function_probe() OWNER TO tagekyc_application_persistence",
                $"ALTER FUNCTION public.__application_owned_function_probe() OWNER TO {owner}; DROP FUNCTION public.__application_owned_function_probe()",
                ApplicationPersistenceReadinessValidator.PrivilegeInvalid);

            await AssertMutationAsync(
                admin, validator,
                "GRANT USAGE ON SCHEMA tagekyc TO tagekyc_application_persistence WITH GRANT OPTION",
                "REVOKE GRANT OPTION FOR USAGE ON SCHEMA tagekyc FROM tagekyc_application_persistence",
                ApplicationPersistenceReadinessValidator.PrivilegeInvalid);

            await AssertMutationAsync(
                admin, validator,
                "GRANT SELECT ON tagekyc.api_keys TO tagekyc_application_persistence WITH GRANT OPTION",
                "REVOKE GRANT OPTION FOR SELECT ON tagekyc.api_keys FROM tagekyc_application_persistence",
                ApplicationPersistenceReadinessValidator.PrivilegeInvalid);

            await AssertMutationAsync(
                admin, validator,
                "GRANT UPDATE (\"State\") ON tagekyc.verification_sessions TO tagekyc_application_persistence WITH GRANT OPTION",
                "REVOKE GRANT OPTION FOR UPDATE (\"State\") ON tagekyc.verification_sessions FROM tagekyc_application_persistence",
                ApplicationPersistenceReadinessValidator.PrivilegeInvalid);

            await AssertMutationAsync(
                admin, validator,
                "GRANT EXECUTE ON FUNCTION tagekyc.raw_export_managed_recipient_scope_matches(uuid,uuid,uuid,text,bytea) TO tagekyc_application_persistence WITH GRANT OPTION",
                "REVOKE GRANT OPTION FOR EXECUTE ON FUNCTION tagekyc.raw_export_managed_recipient_scope_matches(uuid,uuid,uuid,text,bytea) FROM tagekyc_application_persistence",
                ApplicationPersistenceReadinessValidator.PrivilegeInvalid);

            var originalRecipientFunction = await ScalarAsync<string>(admin, """
                SELECT pg_catalog.pg_get_functiondef(
                  pg_catalog.to_regprocedure('tagekyc.raw_export_managed_recipient_scope_matches(uuid,uuid,uuid,text,bytea)'))
                """);
            await admin.Database.ExecuteSqlRawAsync("""
                CREATE OR REPLACE FUNCTION tagekyc.raw_export_managed_recipient_scope_matches(
                  p_api_key_id uuid,
                  p_recipient_client_application_id uuid,
                  p_principal_id uuid,
                  p_activation_profile text,
                  p_expected_scope_digest bytea)
                RETURNS boolean LANGUAGE sql SECURITY DEFINER SET search_path=pg_catalog
                AS $mutant$ SELECT false $mutant$
                """);
            try
            {
                var bodyDrift = await Assert.ThrowsAsync<ApplicationPersistenceReadinessException>(
                    () => validator.ValidateAsync(default));
                Assert.Equal(ApplicationPersistenceReadinessValidator.RecipientFunctionInvalid, bodyDrift.Code);
            }
            finally
            {
                await admin.Database.ExecuteSqlRawAsync(originalRecipientFunction);
            }
            await validator.ValidateAsync(default);

            await AssertMutationAsync(
                admin, validator,
                "GRANT tagekyc_application_persistence TO tagekyc_raw_export_claim_broker WITH ADMIN FALSE, INHERIT TRUE, SET FALSE",
                "REVOKE tagekyc_application_persistence FROM tagekyc_raw_export_claim_broker",
                ApplicationPersistenceReadinessValidator.RoleInvalid);

            await validator.ValidateAsync(CancellationToken.None);
        }
        finally
        {
            await admin.Database.ExecuteSqlRawAsync($"DROP OWNED BY {login}; DROP ROLE {login};");
        }
    }

    private static async Task AssertMutationAsync(
        TagEkycDbContext admin,
        ApplicationPersistenceReadinessValidator validator,
        string mutation,
        string restore,
        string expectedCode)
    {
        await admin.Database.ExecuteSqlRawAsync(mutation);
        try
        {
            var exception = await Assert.ThrowsAsync<ApplicationPersistenceReadinessException>(
                () => validator.ValidateAsync(CancellationToken.None));
            Assert.Equal(expectedCode, exception.Code);
            var issues = await new ApplicationPersistenceReadinessCheck(validator)
                .CheckAsync(CancellationToken.None);
            Assert.Collection(issues, issue => Assert.Equal(expectedCode, issue.Code));
        }
        finally
        {
            await admin.Database.ExecuteSqlRawAsync(restore);
        }

        await validator.ValidateAsync(CancellationToken.None);
    }

    private static async Task<T> ScalarAsync<T>(TagEkycDbContext db, string sql)
    {
        await db.Database.OpenConnectionAsync();
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        return (T)(await command.ExecuteScalarAsync() ?? throw new InvalidOperationException("Expected scalar result."));
    }

    private sealed class PathAuthenticator : IApiKeyAuthenticator
    {
        public Task<SessionOperationResult<AuthenticatedClientContext>> AuthenticateAsync(
            HttpContext context,
            string? requiredScope = null,
            CancellationToken cancellationToken = default)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            AuthenticatedClientContext caller = path.EndsWith("/capture-artifacts", StringComparison.Ordinal)
                ? new(
                    Guid.Parse("20000000-0000-0000-0000-000000000007"),
                    LocalDevRuntimePolicySource.BusinessClientId,
                    "ldev_capture",
                    AuthenticatedCallerCategory.CaptureAgent,
                    new HashSet<string> { "capture.artifact.append" },
                    new HashSet<Guid> { LocalDevRuntimePolicySource.BusinessClientId },
                    new HashSet<string> { "ldev_capture" })
                : path.EndsWith("/evidence-results", StringComparison.Ordinal)
                    ? new(
                        Guid.Parse("20000000-0000-0000-0000-000000000008"),
                        LocalDevRuntimePolicySource.BusinessClientId,
                        "ldev_adapter",
                        AuthenticatedCallerCategory.TrustedAdapter,
                        new HashSet<string> { "trusted.evidence.append" },
                        new HashSet<Guid> { LocalDevRuntimePolicySource.BusinessClientId })
                    : new(
                        Guid.Parse("20000000-0000-0000-0000-000000000001"),
                        LocalDevRuntimePolicySource.BusinessClientId,
                        "ldev_biz",
                        AuthenticatedCallerCategory.BusinessConsumer,
                        new HashSet<string>
                        {
                            "business.session.create",
                            "business.session.read",
                            "session.complete"
                        });
            return Task.FromResult(SessionOperationResult<AuthenticatedClientContext>.Success(caller));
        }
    }

    private sealed class ExactScopePolicyProvider(Guid clientApplicationId, IReadOnlySet<string> scopes)
        : ILocalDevClientPolicyProvider
    {
        private readonly LocalDevClientPolicy policy = new LocalDevRuntimePolicySource()
            .Policies.Single(value => value.ClientApplicationId == LocalDevRuntimePolicySource.BusinessClientId) with
            {
                ClientApplicationId = clientApplicationId,
                AllowedCallerScopes = scopes,
            };

        public Task<LocalDevClientPolicy?> GetPolicyAsync(
            Guid requestedClientApplicationId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<LocalDevClientPolicy?>(
                requestedClientApplicationId == clientApplicationId ? policy : null);
        }
    }
}
