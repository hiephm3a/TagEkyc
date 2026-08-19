using System.Security.Cryptography;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using TagEkyc.Application;
using TagEkyc.Application.Ports;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Api;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class Tip88C1C3RecipientPackageDeliveryTests(PostgresPersistenceFixture postgres)
{
    [Fact]
    public async Task C324_topology_is_closed_and_profile_limits_are_compile_time_exact()
    {
        Assert.Equal(new[] { "Disabled", "S3CompatibleDurable", "Invalid" },
            Enum.GetNames<RecipientPackageDeliveryTopology>());
        Assert.Equal(2, RecipientPackageDeliveryOptions.MaximumConcurrentSpools);
        Assert.Equal(33_557_106, RecipientPackageDeliveryOptions.MaximumEncryptedPackageLength);
        Assert.Equal(TimeSpan.FromMinutes(30), RecipientPackageDeliveryOptions.AuthorizationLifetime);
        Assert.Equal(TimeSpan.FromMinutes(35), RecipientPackageDeliveryOptions.StreamLeaseDuration);
        Assert.Equal(10, RecipientPackageDeliveryReadinessValidator.Codes.Length);

        var catalogSql = (string)typeof(RecipientPackageDeliveryReadinessValidator)
            .GetField("CatalogSql", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .GetRawConstantValue()!;
        var manifestIdentifiers = System.Text.RegularExpressions.Regex
            .Matches(catalogSql, "'(?<identifier>(?:tagekyc|raw_export)[a-z0-9_]*)'")
            .Select(match => match.Groups["identifier"].Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        Assert.NotEmpty(manifestIdentifiers);
        Assert.All(manifestIdentifiers, identifier => Assert.True(
            System.Text.Encoding.UTF8.GetByteCount(identifier) <= 63,
            $"PostgreSQL identifier exceeds 63 UTF-8 bytes: {identifier}"));

        var disabled = new RecipientPackageDeliveryOptions(RecipientPackageDeliveryTopology.Disabled,
            null, null, null, true);
        await new RecipientPackageDeliveryReadinessValidator(disabled,
            new ServiceCollection().BuildServiceProvider()).ValidateAsync(default);
        var invalid = new RecipientPackageDeliveryOptions(RecipientPackageDeliveryTopology.Invalid,
            null, null, null, false);
        var invalidError = await Assert.ThrowsAsync<RecipientPackageDeliveryReadinessException>(() =>
            new RecipientPackageDeliveryReadinessValidator(invalid,
                new ServiceCollection().BuildServiceProvider()).ValidateAsync(default));
        Assert.Equal(RecipientPackageDeliveryReadinessValidator.Codes[0], invalidError.Code);

        await AssertFailClosedHostStartsAsync(new Dictionary<string, string?>());
        await AssertFailClosedHostStartsAsync(new Dictionary<string, string?>
        {
            [$"{RecipientPackageDeliveryOptions.SectionPath}:Topology"] = "Invalid",
        });

        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT
              (SELECT count(*) FROM pg_roles WHERE rolname IN ('tagekyc_raw_export_package_delivery','tagekyc_raw_export_package_delivery_login'))=2
              AND (SELECT count(*) FROM pg_auth_members m JOIN pg_roles member ON member.oid=m.member JOIN pg_roles role ON role.oid=m.roleid
                   WHERE member.rolname='tagekyc_raw_export_package_delivery_login' AND role.rolname='tagekyc_raw_export_package_delivery'
                     AND NOT m.admin_option AND m.inherit_option AND NOT m.set_option)=1
              AND (SELECT count(*) FROM information_schema.role_routine_grants
                   WHERE grantee='tagekyc_raw_export_package_delivery' AND routine_schema='tagekyc'
                     AND privilege_type='EXECUTE')=8
              AND has_schema_privilege('tagekyc_raw_export_package_delivery','tagekyc','USAGE')
            """, connection);
        Assert.True((bool)(await command.ExecuteScalarAsync())!);
        await AssertExactC3FunctionSurfaceAsync(connection);

        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var provider = minio.RecipientPackageConfiguration();
        var c2Options = new RecipientPackageOptions(
            RecipientPackageTopology.S3CompatibleDurable, provider, true);
        await using var c2Db = postgres.CreateDbContext();
        using var c2Services = new ServiceCollection()
            .AddSingleton<IRecipientPackageConnectionFactory>(new C2RoleConnectionFactory(postgres.ConnectionString))
            .AddSingleton<IRecipientPackagePostureProbe>(new C324HealthyPostureProbe())
            .BuildServiceProvider();
        var predecessor = new RecipientPackageReadinessValidator(c2Options, c2Db, c2Services);
        var deliveryOptions = new RecipientPackageDeliveryOptions(
            RecipientPackageDeliveryTopology.S3CompatibleDurable, provider,
            minio.RecipientPackageDeliveryReaderCredential(), postgres.ConnectionString, true);
        using var objectClientFactory = new RecipientPackageDeliveryObjectClientFactory(deliveryOptions);
        using var reader = new S3CompatibleRecipientPackageDeliveryReader(deliveryOptions, objectClientFactory);
        using var deliveryServices = new ServiceCollection()
            .AddSingleton<IRecipientPackageDeliveryConnectionFactory>(new DeliveryRoleConnectionFactory(postgres.ConnectionString))
            .AddSingleton(predecessor)
            .AddSingleton<IRecipientPackageDeliveryReader>(reader)
            .BuildServiceProvider();
        var readiness = new RecipientPackageDeliveryReadinessValidator(deliveryOptions, deliveryServices);
        await readiness.ValidateAsync(CancellationToken.None);
        await using (var durableHost = C3HostFactory(DurableHostConfiguration(
            provider, minio.RecipientPackageDeliveryReaderCredential(), postgres.ConnectionString)))
        {
            using var client = durableHost.CreateClient();
            var hostOptions = durableHost.Services.GetRequiredService<RecipientPackageDeliveryOptions>();
            Assert.Equal(RecipientPackageDeliveryTopology.S3CompatibleDurable, hostOptions.Topology);
            Assert.True(hostOptions.IsSyntacticallyValid, hostOptions.ToString());
            Assert.Contains(durableHost.Services.GetServices<IHostedService>(),
                service => service.GetType().Name == "RecipientPackageDeliveryHostedService");
        }
        await AssertC324CatalogMutationRejectedAsync(readiness,
            "ALTER FUNCTION tagekyc.raw_export_guard_recipient_package_delivery_event() SECURITY INVOKER",
            "ALTER FUNCTION tagekyc.raw_export_guard_recipient_package_delivery_event() SECURITY DEFINER");
        await AssertC324CatalogMutationRejectedAsync(readiness,
            "ALTER FUNCTION tagekyc.raw_export_guard_recipient_package_delivery_event() OWNER TO tagekyc",
            "ALTER FUNCTION tagekyc.raw_export_guard_recipient_package_delivery_event() OWNER TO tagekyc_raw_export_deployer");
        await AssertC324CatalogMutationRejectedAsync(readiness,
            "ALTER FUNCTION tagekyc.raw_export_guard_recipient_package_delivery_event() SET search_path = tagekyc",
            "ALTER FUNCTION tagekyc.raw_export_guard_recipient_package_delivery_event() SET search_path = pg_catalog");
        await AssertC324CatalogMutationRejectedAsync(readiness,
            "GRANT EXECUTE ON FUNCTION tagekyc.raw_export_read_recipient_package_delivery(uuid,uuid) TO tagekyc_raw_export_package_delivery WITH GRANT OPTION",
            "REVOKE GRANT OPTION FOR EXECUTE ON FUNCTION tagekyc.raw_export_read_recipient_package_delivery(uuid,uuid) FROM tagekyc_raw_export_package_delivery");
        await AssertC324CatalogMutationRejectedAsync(readiness,
            """
            CREATE FUNCTION tagekyc.raw_export_read_recipient_package_delivery(integer)
            RETURNS integer LANGUAGE sql SECURITY DEFINER SET search_path = pg_catalog AS 'SELECT 1';
            ALTER FUNCTION tagekyc.raw_export_read_recipient_package_delivery(integer) OWNER TO tagekyc_raw_export_deployer;
            REVOKE ALL ON FUNCTION tagekyc.raw_export_read_recipient_package_delivery(integer) FROM PUBLIC
            """,
            "DROP FUNCTION tagekyc.raw_export_read_recipient_package_delivery(integer)");
    }

    [Fact]
    public async Task C319_only_three_delivery_routes_are_mapped_and_no_HEAD_or_discovery_surface_exists()
    {
        var packageId = Guid.NewGuid();
        var deliveryId = Guid.NewGuid();
        var bytes = "C3-PACKAGE"u8.ToArray();
        var service = new EndpointDeliveryService(packageId, deliveryId, bytes);
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.WebHost.UseTestServer();
        builder.Services.Configure<Microsoft.AspNetCore.Routing.RouteOptions>(options =>
            options.ConstraintMap["D"] = typeof(DFormatGuidRouteConstraint));
        builder.Services.AddSingleton<IApiKeyAuthenticator>(new EndpointAuthenticator());
        builder.Services.AddSingleton<IRecipientPackageDeliveryApplicationService>(service);
        await using var app = builder.Build();
        app.MapRecipientPackageDeliveryEndpoints();
        await app.StartAsync();
        var endpoints = app.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>().ToArray();
        Assert.Equal(3, endpoints.Length);
        Assert.Equal(new[]
        {
            "GET /api/ekyc/raw-export/deliveries/{deliveryId:D}",
            "GET /api/ekyc/raw-export/deliveries/{deliveryId:D}/content",
            "POST /api/ekyc/raw-export/packages/{packageId:D}/deliveries",
        }, endpoints.Select(endpoint => $"{string.Join(',', endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods)} {endpoint.RoutePattern.RawText}")
            .OrderBy(value => value, StringComparer.Ordinal).ToArray());

        using var client = app.GetTestClient();
        using var head = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head,
            $"/api/ekyc/raw-export/deliveries/{deliveryId:D}/content"));
        Assert.Equal(System.Net.HttpStatusCode.MethodNotAllowed, head.StatusCode);
        using var rangedRequest = new HttpRequestMessage(HttpMethod.Get,
            $"/api/ekyc/raw-export/deliveries/{deliveryId:D}/content");
        rangedRequest.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 1);
        using var ranged = await client.SendAsync(rangedRequest);
        Assert.Equal((System.Net.HttpStatusCode)416, ranged.StatusCode);

        using var response = await client.GetAsync($"/api/ekyc/raw-export/deliveries/{deliveryId:D}/content");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/octet-stream", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(bytes.LongLength, response.Content.Headers.ContentLength);
        Assert.Equal($"attachment; filename=\"tagekyc-package-{packageId:D}.t88pkg\"", response.Content.Headers.ContentDisposition?.ToString());
        Assert.Equal("no-store", Assert.Single(response.Headers.CacheControl!.NoStore ? new[] { "no-store" } : []));
        Assert.Equal("no-cache", Assert.Single(response.Headers.Pragma).Name);
        Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
        Assert.Equal("none", Assert.Single(response.Headers.GetValues("Accept-Ranges")));
        Assert.False(response.Content.Headers.Contains("Content-Encoding"));
        Assert.Equal(bytes, await response.Content.ReadAsByteArrayAsync());
        Assert.True(service.Completed);
        service.FailPrepare = true;
        using var failed = await client.GetAsync($"/api/ekyc/raw-export/deliveries/{deliveryId:D}/content");
        Assert.Equal(System.Net.HttpStatusCode.ServiceUnavailable, failed.StatusCode);
        Assert.Equal("application/json", failed.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("C3-PACKAGE", await failed.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        CryptographicOperations.ZeroMemory(bytes);
    }

    [Fact]
    public async Task C314_provider_surface_is_one_exact_GET_operation_without_second_adapter_member()
    {
        var contract = typeof(RecipientPackageDeliveryOptions).Assembly
            .GetType("TagEkyc.Infrastructure.RawExport.IRecipientPackageDeliveryReader", throwOnError: true)!;
        var method = Assert.Single(contract.GetMethods(System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic));
        Assert.Equal("OpenExactAsync", method.Name);
        Assert.Equal(2, method.GetParameters().Length);

        var package = await CreateFinalizedPackageAsync();
        var fixture = await CreateDeliveryAsync(package, "c314-exact-get");
        var reader = new ScriptedReader(RecipientPackageDeliveryReadOutcome.Opened, package.Content);
        var options = new RecipientPackageDeliveryOptions(RecipientPackageDeliveryTopology.S3CompatibleDurable,
            TestProvider(), new("delivery", "secret"), postgres.ConnectionString, true);
        using var pool = new RecipientPackageDeliverySpoolPool();
        var coordinator = new RecipientPackageDeliveryCoordinator(options, fixture.Repository, reader, pool);
        var actor = new AuthenticatedClientContext(Guid.NewGuid(), package.RecipientId, "c314",
            AuthenticatedCallerCategory.BusinessConsumer,
            new HashSet<string> { "business.raw-export.package.download" }, PrincipalId: Guid.NewGuid());
        var correlation = RecipientPackageDeliveryCodec.CorrelationDigest("c314-exact-get");
        try
        {
            var result = await coordinator.PrepareContentAsync(actor, fixture.DeliveryId, correlation, default);
            Assert.True(result.IsSuccess);
            await using var lease = result.Value!;
            Assert.Equal(1, reader.OpenCount);
            Assert.Equal(RecipientPackageCodec.ObjectKey(package.PackageId), reader.Locator?.ObjectKey);
            Assert.Equal(TestProvider().BucketName, reader.Locator?.BucketName);
        }
        finally { CryptographicOperations.ZeroMemory(correlation); }
    }

    [Fact]
    public async Task C315_delivery_reader_is_a_distinct_fifth_credential()
    {
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        Assert.Equal(5, minio.RecipientPackageDistinctCredentialCount());
        var policy = minio.RecipientPackageDeliveryReaderPolicyDocument();
        Assert.Contains("s3:GetObject", policy, StringComparison.Ordinal);
        Assert.DoesNotContain("s3:ListBucket", policy, StringComparison.Ordinal);
        Assert.DoesNotContain("s3:PutObject", policy, StringComparison.Ordinal);
        Assert.DoesNotContain("s3:DeleteObject", policy, StringComparison.Ordinal);
        Assert.DoesNotContain("s3:AbortMultipartUpload", policy, StringComparison.Ordinal);

        var exactKey = $"raw-export/c2-package/v1/{Guid.NewGuid():N}";
        var wrongPrefix = $"raw-export/c1/v1/{Guid.NewGuid():N}";
        var otherBucket = await minio.CreateBucketAsync();
        await minio.PutRootObjectAsync(minio.BucketName, exactKey, "exact"u8.ToArray());
        await minio.PutRootObjectAsync(minio.BucketName, wrongPrefix, "wrong"u8.ToArray());
        await minio.PutRootObjectAsync(otherBucket, exactKey, "other"u8.ToArray());
        using var reader = minio.CreateRecipientPackageClient("delivery-reader");
        using var allowed = await reader.GetObjectAsync(minio.BucketName, exactKey);
        Assert.Equal(System.Net.HttpStatusCode.OK, allowed.HttpStatusCode);
        var crossPrefix = await Assert.ThrowsAsync<Amazon.S3.AmazonS3Exception>(
            () => reader.GetObjectAsync(minio.BucketName, wrongPrefix));
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, crossPrefix.StatusCode);
        var crossBucket = await Assert.ThrowsAsync<Amazon.S3.AmazonS3Exception>(
            () => reader.GetObjectAsync(otherBucket, exactKey));
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, crossBucket.StatusCode);
        var list = await Assert.ThrowsAsync<Amazon.S3.AmazonS3Exception>(
            () => reader.ListObjectsV2Async(new ListObjectsV2Request { BucketName = minio.BucketName }));
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, list.StatusCode);
        var put = await Assert.ThrowsAsync<Amazon.S3.AmazonS3Exception>(
            () => reader.PutObjectAsync(new PutObjectRequest { BucketName = minio.BucketName, Key = exactKey + "-put",
                InputStream = new MemoryStream("put"u8.ToArray()), AutoCloseStream = true }));
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, put.StatusCode);
        var delete = await Assert.ThrowsAsync<Amazon.S3.AmazonS3Exception>(
            () => reader.DeleteObjectAsync(minio.BucketName, exactKey));
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, delete.StatusCode);

        var provider = minio.RecipientPackageConfiguration();
        var correctOptions = new RecipientPackageDeliveryOptions(
            RecipientPackageDeliveryTopology.S3CompatibleDurable, provider,
            minio.RecipientPackageDeliveryReaderCredential(), postgres.ConnectionString, true);
        using var correctFactory = new RecipientPackageDeliveryObjectClientFactory(correctOptions);
        using var productionReader = new S3CompatibleRecipientPackageDeliveryReader(correctOptions, correctFactory);
        var endpoint = RecipientPackageCodec.ProviderEndpointFingerprint(provider);
        var binding = RecipientPackageCodec.ObjectBindingDigest(provider.ProviderConfigurationId,
            endpoint, provider.BucketName, exactKey);
        try
        {
            var productionRead = await productionReader.OpenExactAsync(new(provider.ProviderConfigurationId,
                provider.ServiceUrl, provider.RegionIdentifier, provider.ForcePathStyle,
                provider.BucketName, exactKey, binding), default);
            Assert.Equal(RecipientPackageDeliveryReadOutcome.Opened, productionRead.Outcome);
            Assert.NotNull(productionRead.Content);
            await productionRead.Content!.DisposeAsync();

            var deniedOptions = new RecipientPackageDeliveryOptions(
                RecipientPackageDeliveryTopology.S3CompatibleDurable, provider,
                minio.RecipientPackageCredentialFor("writer"), postgres.ConnectionString, true);
            using var deniedFactory = new RecipientPackageDeliveryObjectClientFactory(deniedOptions);
            using var deniedReader = new S3CompatibleRecipientPackageDeliveryReader(deniedOptions, deniedFactory);
            var deniedRead = await deniedReader.OpenExactAsync(new(provider.ProviderConfigurationId,
                provider.ServiceUrl, provider.RegionIdentifier, provider.ForcePathStyle,
                provider.BucketName, exactKey, binding), default);
            Assert.Equal(RecipientPackageDeliveryReadOutcome.Forbidden, deniedRead.Outcome);
            Assert.Null(deniedRead.Content);
        }
        finally { CryptographicOperations.ZeroMemory(endpoint); CryptographicOperations.ZeroMemory(binding); }
    }

    [Fact]
    public async Task C303_recipient_scoped_delivery_read_does_not_disclose_cross_recipient_rows()
    {
        var fixture = await CreateDeliveryAsync();
        var wrongRecipient = Guid.NewGuid();
        var before = await CountRowsAsync("raw_export_recipient_package_deliveries");
        var wrong = await fixture.Repository.ReadAsync(wrongRecipient, fixture.DeliveryId, default);
        Assert.Equal("NotFound", wrong.Outcome);
        Assert.Null(wrong.Delivery);
        var digest = RecipientPackageDeliveryCodec.IdempotencyKeyDigest("c303-cross-recipient");
        try
        {
            var created = await fixture.Repository.CreateAsync(wrongRecipient, fixture.Package.PackageId,
                RecipientPackageDeliveryCodec.DeliveryId(wrongRecipient, digest), digest,
                Guid.NewGuid(), Guid.NewGuid(), RandomNumberGenerator.GetBytes(32), default);
            Assert.Equal("NotFound", created.Outcome);
            Assert.Null(created.Delivery);
            Assert.Equal(before, await CountRowsAsync("raw_export_recipient_package_deliveries"));
        }
        finally { CryptographicOperations.ZeroMemory(digest); }
    }

    [Fact]
    public async Task C305_create_replay_conflict_precedence_and_zero_mutation_are_exact()
    {
        var package = await CreateFinalizedPackageAsync();
        var repository = DeliveryRepository();
        var idempotency = SHA256.HashData("c305"u8);
        var deliveryId = RecipientPackageDeliveryCodec.DeliveryId(package.RecipientId, idempotency);
        var beforeDeliveries = await CountRowsAsync("raw_export_recipient_package_deliveries");
        var beforeEvents = await CountRowsAsync("raw_export_recipient_package_delivery_events");
        var creatorApiKey = Guid.NewGuid(); var creatorPrincipal = Guid.NewGuid(); var creatorCorrelation = RandomNumberGenerator.GetBytes(32);
        var first = await repository.CreateAsync(package.RecipientId, package.PackageId, deliveryId, idempotency,
            creatorApiKey, creatorPrincipal, creatorCorrelation, default);
        var afterFirstDeliveries = await CountRowsAsync("raw_export_recipient_package_deliveries");
        var afterFirstEvents = await CountRowsAsync("raw_export_recipient_package_delivery_events");
        var replay = await repository.CreateAsync(package.RecipientId, package.PackageId, deliveryId, idempotency,
            Guid.NewGuid(), Guid.NewGuid(), RandomNumberGenerator.GetBytes(32), default);
        var conflict = await repository.CreateAsync(package.RecipientId, Guid.NewGuid(), deliveryId, idempotency,
            Guid.NewGuid(), Guid.NewGuid(), RandomNumberGenerator.GetBytes(32), default);
        Assert.Equal("Created", first.Outcome);
        Assert.Equal("ExistingMatch", replay.Outcome);
        Assert.Equal("IdempotencyConflict", conflict.Outcome);
        Assert.Equal(first.Delivery?.Revision, replay.Delivery?.Revision);
        Assert.Equal(beforeDeliveries + 1, afterFirstDeliveries);
        Assert.Equal(beforeEvents + 1, afterFirstEvents);
        Assert.Equal(afterFirstDeliveries, await CountRowsAsync("raw_export_recipient_package_deliveries"));
        Assert.Equal(afterFirstEvents, await CountRowsAsync("raw_export_recipient_package_delivery_events"));
        await using var db = postgres.CreateDbContext();
        var persisted = await db.RawExportRecipientPackageDeliveries.AsNoTracking().SingleAsync(item => item.DeliveryId == deliveryId);
        Assert.Equal(creatorApiKey, persisted.CreatorApiKeyId);
        Assert.Equal(creatorPrincipal, persisted.CreatorPrincipalId);
        Assert.Equal(creatorCorrelation, persisted.AuthorizationCorrelationDigest);
    }

    [Fact]
    public async Task C306_only_exact_finalized_C2_package_snapshot_is_admitted()
    {
        var delivery = await CreateDeliveryAsync();
        Assert.Equal("Created", delivery.Created.Outcome);
        Assert.Equal(delivery.Package.PackageId, delivery.Created.Delivery?.PackageId);
        await using var db = postgres.CreateDbContext();
        var package = await db.RawExportRecipientPackagePreparations.AsNoTracking()
            .SingleAsync(item => item.PackageId == delivery.Package.PackageId);
        var row = await db.RawExportRecipientPackageDeliveries.AsNoTracking()
            .SingleAsync(item => item.DeliveryId == delivery.DeliveryId);
        Assert.Equal("Finalized", package.State);
        Assert.Equal(package.Revision, row.PackageRevisionAtAuthorization);
        Assert.Equal(package.C2PreparationId, row.C2PreparationId);
        Assert.Equal(package.PackageEqualityFingerprint, row.PackageEqualityFingerprint);
        Assert.Equal(package.RecipientKeyId, row.RecipientKeyId);
        Assert.Equal(package.RecipientKeyVersion, row.RecipientKeyVersion);
        Assert.Equal(package.RecipientKeyFingerprint, row.RecipientKeyFingerprint);
        Assert.Equal(package.RecipientKeyRevision, row.RecipientKeyRevision);
        Assert.Equal(package.EncryptedPackageLength, row.EncryptedPackageLength);
        Assert.Equal(package.PackageCiphertextDigest, row.PackageCiphertextDigest);
        Assert.Equal(package.EnvelopeDigest, row.EnvelopeDigest);
        Assert.Equal(package.ObjectBindingDigest, row.ObjectBindingDigest);

        var prepared = await CreateFinalizedPackageAsync();
        await ExecuteAsync("UPDATE tagekyc.raw_export_recipient_package_preparations SET \"State\"='Prepared',\"FinalizedAtUtc\"=NULL WHERE \"PackageId\"=@id", ("id", prepared.PackageId));
        var digest = RecipientPackageDeliveryCodec.IdempotencyKeyDigest("c306-prepared");
        try
        {
            var rejected = await delivery.Repository.CreateAsync(prepared.RecipientId, prepared.PackageId,
                RecipientPackageDeliveryCodec.DeliveryId(prepared.RecipientId, digest), digest,
                Guid.NewGuid(), Guid.NewGuid(), RandomNumberGenerator.GetBytes(32), default);
            Assert.Equal("NotFound", rejected.Outcome);
            Assert.Null(rejected.Delivery);
        }
        finally { CryptographicOperations.ZeroMemory(digest); }
    }

    [Fact]
    public async Task C307_frozen_key_state_fingerprint_revision_and_window_are_revalidated()
    {
        async Task AssertRejectedAsync(string mutation)
        {
            var package = await CreateFinalizedPackageAsync();
            await ExecuteAsync(mutation, ("id", package.RecipientId));
            var digest = SHA256.HashData(Guid.NewGuid().ToByteArray());
            var result = await DeliveryRepository().CreateAsync(package.RecipientId, package.PackageId,
                RecipientPackageDeliveryCodec.DeliveryId(package.RecipientId, digest), digest,
                Guid.NewGuid(), Guid.NewGuid(), RandomNumberGenerator.GetBytes(32), default);
            Assert.Equal("Ineligible", result.Outcome);
            Assert.Equal(0L, await CountRowsAsync("raw_export_recipient_package_deliveries", package.PackageId));
        }
        await AssertRejectedAsync("UPDATE tagekyc.raw_export_recipient_key_registrations SET \"State\"='Revoked',\"RevokedAtUtc\"=clock_timestamp() WHERE \"RecipientClientApplicationId\"=@id AND \"State\"='Active'");
        await AssertRejectedAsync("UPDATE tagekyc.raw_export_recipient_key_registrations SET \"PublicKeyFingerprint\"=tagekyc_extensions.digest(\"PublicKeyFingerprint\",'sha256') WHERE \"RecipientClientApplicationId\"=@id AND \"State\"='Active'");
        await AssertRejectedAsync("UPDATE tagekyc.raw_export_recipient_key_registrations SET \"Revision\"=\"Revision\"+1 WHERE \"RecipientClientApplicationId\"=@id AND \"State\"='Active'");
        await AssertRejectedAsync("UPDATE tagekyc.raw_export_recipient_key_registrations SET \"ValidFromUtc\"=clock_timestamp()+interval '1 hour',\"ValidUntilUtc\"=clock_timestamp()+interval '2 hours' WHERE \"RecipientClientApplicationId\"=@id AND \"State\"='Active'");
        await AssertRejectedAsync("UPDATE tagekyc.raw_export_recipient_key_registrations SET \"ValidFromUtc\"=clock_timestamp()-interval '2 hours',\"ValidUntilUtc\"=clock_timestamp()-interval '1 hour' WHERE \"RecipientClientApplicationId\"=@id AND \"State\"='Active'");
    }

    [Fact]
    public async Task C308_key_package_delivery_lock_order_and_fresh_clock_are_global()
    {
        var source = MigrationSource();
        var begin = Between(source, "CREATE FUNCTION tagekyc.raw_export_begin_recipient_package_delivery_stream", "CREATE FUNCTION tagekyc.raw_export_record_recipient_package_delivery_interrupted");
        var locks = begin.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Where(statement => statement.Contains("FOR UPDATE", StringComparison.Ordinal))
            .ToArray();
        Assert.Equal(3, locks.Length);
        Assert.True(locks[0].Contains("recipient_key_registrations", StringComparison.Ordinal));
        Assert.True(locks[1].Contains("recipient_package_preparations", StringComparison.Ordinal));
        Assert.True(locks[2].Contains("recipient_package_deliveries", StringComparison.Ordinal));
        Assert.Equal(1, Count(begin, "now_utc:=pg_catalog.clock_timestamp()"));
        Assert.DoesNotContain("transaction_timestamp()", begin, StringComparison.Ordinal);
        Assert.True(begin.LastIndexOf("FOR UPDATE", StringComparison.Ordinal) < begin.IndexOf("now_utc:=pg_catalog.clock_timestamp()", StringComparison.Ordinal));
        await Task.CompletedTask;
    }

    [Fact]
    public async Task C309_seven_state_sparse_matrix_ten_transitions_and_event_guard_are_closed()
    {
        await using var db = postgres.CreateDbContext();
        var entity = db.GetService<Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel>().Model
            .FindEntityType("TagEkyc.Infrastructure.Persistence.Entities.RawExportRecipientPackageDeliveryRow")!;
        var checks = entity.GetCheckConstraints().ToArray();
        Assert.Contains(checks, item => item.Name == "ck_raw_export_recipient_package_delivery_sparse");
        var sql = string.Join(' ', checks.Select(item => item.Sql));
        Assert.Equal(7, new[] { "Authorized", "Streaming", "Interrupted", "IntegrityUnavailable", "OutcomeUnknown", "ServerStreamCompleted", "Expired" }.Count(sql.Contains));
        await using var connection = new NpgsqlConnection(postgres.ConnectionString); await connection.OpenAsync();
        await AssertExactC3FunctionSurfaceAsync(connection);

        var fixture = await CreateDeliveryAsync();
        await using var state = postgres.CreateDbContext();
        var authorizedRow = await state.RawExportRecipientPackageDeliveries.AsNoTracking()
            .SingleAsync(item => item.DeliveryId == fixture.DeliveryId);
        var authorizedEvent = await state.RawExportRecipientPackageDeliveryEvents.AsNoTracking()
            .SingleAsync(item => item.DeliveryId == fixture.DeliveryId && item.EventType == "Authorized");
        var authorizationEvidence = RecipientPackageDeliveryCodec.AuthorizationEvidence(
            authorizedRow.DeliveryId, authorizedRow.PackageId, authorizedRow.RecipientClientApplicationId,
            authorizedRow.DeliveryEqualityFingerprint, authorizedRow.CreatorApiKeyId, authorizedRow.CreatorPrincipalId,
            authorizedRow.AuthorizationCorrelationDigest, authorizedRow.AuthorizedAtUtc, authorizedRow.AuthorizationExpiresAtUtc);
        Assert.Equal(RecipientPackageDeliveryCodec.EventId(authorizedRow.DeliveryId, 1), authorizedEvent.DeliveryEventId);
        Assert.Equal(authorizationEvidence, authorizedEvent.EvidenceDigest);
        Assert.Equal(authorizedRow.CreatorApiKeyId, authorizedEvent.AuthenticatedApiKeyId);
        Assert.Equal(authorizedRow.CreatorPrincipalId, authorizedEvent.AuthenticatedPrincipalId);

        var streamApiKey = Guid.NewGuid(); var streamPrincipal = Guid.NewGuid();
        var streamCorrelation = RandomNumberGenerator.GetBytes(32);
        var started = await fixture.Repository.BeginAsync(fixture.Package.RecipientId, fixture.DeliveryId,
            streamApiKey, streamPrincipal, streamCorrelation, default);
        Assert.Equal("Started", started.Outcome);
        var streamEvidence = RecipientPackageDeliveryCodec.StreamAdmissionEvidence(
            started.Delivery!.DeliveryId, started.Delivery.PackageId, started.Delivery.RecipientClientApplicationId,
            started.Delivery.StreamAttemptCount, started.Delivery.DeliveryFence, streamApiKey, streamPrincipal,
            streamCorrelation, started.Delivery.StreamStartedAtUtc!.Value, started.Delivery.StreamLeaseExpiresAtUtc!.Value);
        var streamEvent = await state.RawExportRecipientPackageDeliveryEvents.AsNoTracking()
            .SingleAsync(item => item.DeliveryId == fixture.DeliveryId && item.EventType == "StreamingStarted");
        Assert.Equal(RecipientPackageDeliveryCodec.EventId(fixture.DeliveryId, started.Delivery.Revision), streamEvent.DeliveryEventId);
        Assert.Equal(streamEvidence, streamEvent.EvidenceDigest);
        Assert.Equal(streamApiKey, streamEvent.AuthenticatedApiKeyId);
        Assert.Equal(streamPrincipal, streamEvent.AuthenticatedPrincipalId);
        Assert.Equal(streamCorrelation, streamEvent.CorrelationDigest);

        var observations = RecipientPackageDeliveryCodec.EncodeFailureObservations(null, [], false, [], false);
        var interrupted = await fixture.Repository.InterruptAsync(fixture.DeliveryId, started.Delivery.Revision,
            started.Delivery.DeliveryFence, "ProviderUnavailable", observations, default);
        Assert.Equal("Interrupted", interrupted.Outcome);
        var interruptedRow = await state.RawExportRecipientPackageDeliveries.AsNoTracking()
            .SingleAsync(item => item.DeliveryId == fixture.DeliveryId);
        var interruptionEvidence = RecipientPackageDeliveryCodec.FailureEvidence(
            "tip-88c1-c3-delivery-interruption-v1", interruptedRow.DeliveryId, interruptedRow.PackageId,
            interruptedRow.RecipientClientApplicationId, interruptedRow.StreamAttemptCount, interruptedRow.DeliveryFence,
            interruptedRow.InterruptionKind!, null, [], false, [], false, interruptedRow.ObjectBindingDigest,
            interruptedRow.InterruptedAtUtc!.Value);
        var interruptionEvent = await state.RawExportRecipientPackageDeliveryEvents.AsNoTracking()
            .SingleAsync(item => item.DeliveryId == fixture.DeliveryId && item.EventType == "Interrupted");
        Assert.Equal(interruptionEvidence, interruptedRow.InterruptionEvidenceDigest);
        Assert.Equal(interruptionEvidence, interruptionEvent.EvidenceDigest);
        Assert.Equal(streamApiKey, interruptionEvent.AuthenticatedApiKeyId);
        Assert.Equal(streamPrincipal, interruptionEvent.AuthenticatedPrincipalId);
        Assert.Equal(streamCorrelation, interruptionEvent.CorrelationDigest);
        CryptographicOperations.ZeroMemory(observations);

        await using var invalidState = new NpgsqlCommand("UPDATE tagekyc.raw_export_recipient_package_deliveries SET \"OutcomeUnknownAtUtc\"=clock_timestamp() WHERE \"DeliveryId\"=@id", connection);
        invalidState.Parameters.AddWithValue("id", fixture.DeliveryId);
        var stateShape = await Assert.ThrowsAsync<PostgresException>(() => invalidState.ExecuteNonQueryAsync());
        Assert.Equal(PostgresErrorCodes.CheckViolation, stateShape.SqlState);
        Assert.Equal("ck_raw_export_recipient_package_delivery_sparse", stateShape.ConstraintName);

        await using var invalidEvent = new NpgsqlCommand("""
            INSERT INTO tagekyc.raw_export_recipient_package_delivery_events
              ("DeliveryEventId","DeliveryId","RecipientClientApplicationId","Revision","EventType",
               "DeliveryAttemptNumber","DeliveryFence","AuthenticatedApiKeyId","AuthenticatedPrincipalId",
               "CorrelationDigest","EvidenceDigest","OccurredAtUtc")
            VALUES (@event,@delivery,@recipient,99,'StreamingStarted',NULL,NULL,@api,@principal,@correlation,@evidence,clock_timestamp())
            """, connection);
        invalidEvent.Parameters.AddWithValue("event", Guid.NewGuid());
        invalidEvent.Parameters.AddWithValue("delivery", fixture.DeliveryId);
        invalidEvent.Parameters.AddWithValue("recipient", fixture.Package.RecipientId);
        invalidEvent.Parameters.AddWithValue("api", Guid.NewGuid());
        invalidEvent.Parameters.AddWithValue("principal", Guid.NewGuid());
        invalidEvent.Parameters.AddWithValue("correlation", RandomNumberGenerator.GetBytes(32));
        invalidEvent.Parameters.AddWithValue("evidence", RandomNumberGenerator.GetBytes(32));
        var eventShape = await Assert.ThrowsAsync<PostgresException>(() => invalidEvent.ExecuteNonQueryAsync());
        Assert.Equal(PostgresErrorCodes.CheckViolation, eventShape.SqlState);
        Assert.Equal("ck_raw_export_recipient_package_delivery_event_shape", eventShape.ConstraintName);

        await using var guard = new NpgsqlCommand("UPDATE tagekyc.raw_export_recipient_package_delivery_events SET \"EventType\"='Expired' WHERE \"DeliveryEventId\"=@id", connection);
        guard.Parameters.AddWithValue("id", interruptionEvent.DeliveryEventId);
        var guardError = await Assert.ThrowsAsync<PostgresException>(() => guard.ExecuteNonQueryAsync());
        Assert.Equal("P0001", guardError.SqlState);
        Assert.Equal("RAW_EXPORT_RECIPIENT_PACKAGE_DELIVERY_EVENT_APPEND_ONLY", guardError.MessageText);
    }

    [Fact]
    public async Task C310_two_permits_fail_fast_before_any_stream_mutation()
    {
        using var pool = new RecipientPackageDeliverySpoolPool();
        await using var one = await pool.TryAcquireAsync(default); await using var two = await pool.TryAcquireAsync(default);
        Assert.NotNull(one); Assert.NotNull(two); Assert.Null(await pool.TryAcquireAsync(default));
        var fixture = await CreateDeliveryAsync();
        var reader = new CountingReader();
        var options = new RecipientPackageDeliveryOptions(RecipientPackageDeliveryTopology.S3CompatibleDurable,
            TestProvider(), new("delivery", "secret"), postgres.ConnectionString, true);
        var coordinator = new RecipientPackageDeliveryCoordinator(options, fixture.Repository, reader, pool);
        var actor = new AuthenticatedClientContext(Guid.NewGuid(), fixture.Package.RecipientId, "c310",
            AuthenticatedCallerCategory.BusinessConsumer, new HashSet<string> { "business.raw-export.package.download" }, PrincipalId: Guid.NewGuid());
        var beforeEvents = await CountRowsAsync("raw_export_recipient_package_delivery_events");
        var rejected = await coordinator.PrepareContentAsync(actor, fixture.DeliveryId, RandomNumberGenerator.GetBytes(32), default);
        Assert.False(rejected.IsSuccess);
        Assert.Equal(RecipientPackageDeliveryErrorCodes.CapacityUnavailable, rejected.Error?.Code);
        Assert.Equal(0, reader.OpenCount);
        Assert.Equal(beforeEvents, await CountRowsAsync("raw_export_recipient_package_delivery_events"));
        Assert.Equal("Authorized", (await fixture.Repository.ReadAsync(fixture.Package.RecipientId, fixture.DeliveryId, default)).Delivery?.State);
    }

    [Fact]
    public async Task C311_concurrent_begin_has_one_attempt_fence_and_stale_CAS_cannot_write()
    {
        var fixture = await CreateDeliveryAsync();
        var actor = (Guid.NewGuid(), Guid.NewGuid(), RandomNumberGenerator.GetBytes(32));
        var results = await Task.WhenAll(
            fixture.Repository.BeginAsync(fixture.Package.RecipientId, fixture.DeliveryId, actor.Item1, actor.Item2, actor.Item3, default),
            fixture.Repository.BeginAsync(fixture.Package.RecipientId, fixture.DeliveryId, actor.Item1, actor.Item2, actor.Item3, default));
        Assert.Equal(1, results.Count(result => result.Outcome == "Started"));
        Assert.Equal(1, results.Count(result => result.Outcome == "InProgress"));
        var started = Assert.Single(results.Where(result => result.Outcome == "Started")).Delivery!;
        var staleCompletion = await fixture.Repository.CompleteAsync(fixture.DeliveryId,
            started.Revision - 1, started.DeliveryFence, fixture.Package.Length,
            fixture.Package.CiphertextDigest, default);
        var observations = RecipientPackageDeliveryCodec.EncodeFailureObservations(null, [], false, [], false);
        var staleInterruption = await fixture.Repository.InterruptAsync(fixture.DeliveryId,
            started.Revision, started.DeliveryFence + 1, "ProviderUnavailable", observations, default);
        CryptographicOperations.ZeroMemory(observations);
        Assert.Equal("StateConflict", staleCompletion.Outcome);
        Assert.Equal("StateConflict", staleInterruption.Outcome);
        var current = await fixture.Repository.ReadAsync(fixture.Package.RecipientId, fixture.DeliveryId, default);
        Assert.Equal("Streaming", current.Delivery?.State);
        Assert.Equal(started.Revision, current.Delivery?.Revision);
    }

    [Fact]
    public async Task C312_known_interruption_allows_byte_zero_retry_with_incremented_fence()
    {
        var fixture = await CreateDeliveryAsync();
        var started = await BeginAsync(fixture);
        var observations = RecipientPackageDeliveryCodec.EncodeFailureObservations(null, [], false, [], false);
        var interrupted = await fixture.Repository.InterruptAsync(fixture.DeliveryId, started.Delivery!.Revision,
            started.Delivery.DeliveryFence, "ProviderUnavailable", observations, default);
        var replay = await fixture.Repository.InterruptAsync(fixture.DeliveryId, started.Delivery.Revision,
            started.Delivery.DeliveryFence, "ProviderUnavailable", observations, default);
        CryptographicOperations.ZeroMemory(observations);
        var retried = await BeginAsync(fixture);
        Assert.Equal("Interrupted", interrupted.Outcome);
        Assert.Equal("ExistingMatch", replay.Outcome);
        Assert.Equal(interrupted.Delivery?.Revision, replay.Delivery?.Revision);
        Assert.Equal("Started", retried.Outcome);
        Assert.Equal(started.Delivery.DeliveryFence + 1, retried.Delivery?.DeliveryFence);
    }

    [Fact]
    public async Task C313_expiry_and_stale_stream_restart_terminalize_from_durable_state()
    {
        async Task AssertAdmissionExpiryAsync(bool interrupted)
        {
            var fixture = await CreateDeliveryAsync();
            Guid expectedApiKey;
            Guid expectedPrincipal;
            byte[] expectedCorrelation;
            var predecessor = interrupted ? "Interrupted" : "Authorized";
            if (interrupted)
            {
                expectedApiKey = Guid.NewGuid(); expectedPrincipal = Guid.NewGuid();
                expectedCorrelation = RandomNumberGenerator.GetBytes(32);
                var started = await fixture.Repository.BeginAsync(fixture.Package.RecipientId, fixture.DeliveryId,
                    expectedApiKey, expectedPrincipal, expectedCorrelation, default);
                Assert.Equal("Started", started.Outcome);
                var observations = RecipientPackageDeliveryCodec.EncodeFailureObservations(null, [], false, [], false);
                try
                {
                    var result = await fixture.Repository.InterruptAsync(fixture.DeliveryId, started.Delivery!.Revision,
                        started.Delivery.DeliveryFence, "ProviderUnavailable", observations, default);
                    Assert.Equal("Interrupted", result.Outcome);
                }
                finally { CryptographicOperations.ZeroMemory(observations); }
            }
            else
            {
                await using var creatorDb = postgres.CreateDbContext();
                var creator = await creatorDb.RawExportRecipientPackageDeliveries.AsNoTracking()
                    .SingleAsync(item => item.DeliveryId == fixture.DeliveryId);
                expectedApiKey = creator.CreatorApiKeyId;
                expectedPrincipal = creator.CreatorPrincipalId;
                expectedCorrelation = creator.AuthorizationCorrelationDigest;
            }

            await ExecuteAsync("WITH t AS (SELECT clock_timestamp()-interval '31 minutes' AS authorized) UPDATE tagekyc.raw_export_recipient_package_deliveries SET \"AuthorizedAtUtc\"=t.authorized,\"AuthorizationExpiresAtUtc\"=t.authorized+interval '30 minutes' FROM t WHERE \"DeliveryId\"=@id", ("id", fixture.DeliveryId));
            await using var beforeDb = postgres.CreateDbContext();
            var before = await beforeDb.RawExportRecipientPackageDeliveries.AsNoTracking()
                .SingleAsync(item => item.DeliveryId == fixture.DeliveryId);
            var beforeEventCount = await beforeDb.RawExportRecipientPackageDeliveryEvents.AsNoTracking()
                .CountAsync(item => item.DeliveryId == fixture.DeliveryId);
            Assert.Equal(predecessor, before.State);

            var reader = new CountingReader();
            using var pool = new RecipientPackageDeliverySpoolPool();
            var coordinator = new RecipientPackageDeliveryCoordinator(
                new RecipientPackageDeliveryOptions(RecipientPackageDeliveryTopology.S3CompatibleDurable,
                    TestProvider(), new("delivery", "secret"), postgres.ConnectionString, true),
                fixture.Repository, reader, pool);
            var callerApiKey = Guid.NewGuid(); var callerPrincipal = Guid.NewGuid();
            var callerCorrelation = RandomNumberGenerator.GetBytes(32);
            var actor = new AuthenticatedClientContext(callerApiKey, fixture.Package.RecipientId, "c313",
                AuthenticatedCallerCategory.BusinessConsumer,
                new HashSet<string> { "business.raw-export.package.download" }, PrincipalId: callerPrincipal);
            var dbClockBefore = await ReadDatabaseClockAsync();
            SessionOperationResult<RecipientPackageDeliveryContentLease> admission;
            try
            {
                admission = await coordinator.PrepareContentAsync(
                    actor, fixture.DeliveryId, callerCorrelation, default);
            }
            finally { CryptographicOperations.ZeroMemory(callerCorrelation); }
            var dbClockAfter = await ReadDatabaseClockAsync();
            Assert.False(admission.IsSuccess);
            Assert.Equal(RecipientPackageDeliveryErrorCodes.Expired, admission.Error?.Code);
            Assert.Equal(0, reader.OpenCount);

            await using var afterDb = postgres.CreateDbContext();
            var expired = await afterDb.RawExportRecipientPackageDeliveries.AsNoTracking()
                .SingleAsync(item => item.DeliveryId == fixture.DeliveryId);
            var expiredEvents = await afterDb.RawExportRecipientPackageDeliveryEvents.AsNoTracking()
                .Where(item => item.DeliveryId == fixture.DeliveryId && item.EventType == "Expired")
                .ToArrayAsync();
            var expiredEvent = Assert.Single(expiredEvents);
            Assert.Equal("Expired", expired.State);
            Assert.Equal(before.Revision + 1, expired.Revision);
            Assert.NotNull(expired.ExpiredAtUtc);
            Assert.InRange(expired.ExpiredAtUtc!.Value, dbClockBefore, dbClockAfter);
            Assert.Equal(before.StreamAttemptCount, expired.StreamAttemptCount);
            Assert.Equal(before.DeliveryFence, expired.DeliveryFence);
            Assert.Equal(beforeEventCount + 1, await afterDb.RawExportRecipientPackageDeliveryEvents.AsNoTracking()
                .CountAsync(item => item.DeliveryId == fixture.DeliveryId));
            Assert.Equal(expectedApiKey, expiredEvent.AuthenticatedApiKeyId);
            Assert.Equal(expectedPrincipal, expiredEvent.AuthenticatedPrincipalId);
            Assert.Equal(expectedCorrelation, expiredEvent.CorrelationDigest);
            Assert.NotEqual(callerApiKey, expiredEvent.AuthenticatedApiKeyId);
            Assert.NotEqual(callerPrincipal, expiredEvent.AuthenticatedPrincipalId);
            Assert.Equal(expired.ExpiredAtUtc, expiredEvent.OccurredAtUtc);
            Assert.Equal(RecipientPackageDeliveryCodec.ExpiryEvidence(
                expired.DeliveryId, expired.PackageId, expired.RecipientClientApplicationId, predecessor,
                expectedApiKey, expectedPrincipal, expectedCorrelation,
                expired.AuthorizationExpiresAtUtc, expired.ExpiredAtUtc.Value), expiredEvent.EvidenceDigest);
            Assert.Equal(RecipientPackageDeliveryCodec.EventId(expired.DeliveryId, expired.Revision),
                expiredEvent.DeliveryEventId);

            var replay = await fixture.Repository.BeginAsync(fixture.Package.RecipientId, fixture.DeliveryId,
                Guid.NewGuid(), Guid.NewGuid(), RandomNumberGenerator.GetBytes(32), default);
            Assert.Equal("Expired", replay.Outcome);
            Assert.Equal("Expired", replay.Delivery?.State);
            Assert.Equal(expired.Revision, replay.Delivery?.Revision);
            Assert.Equal(1, await afterDb.RawExportRecipientPackageDeliveryEvents.AsNoTracking()
                .CountAsync(item => item.DeliveryId == fixture.DeliveryId && item.EventType == "Expired"));
        }

        await AssertAdmissionExpiryAsync(interrupted: false);
        await AssertAdmissionExpiryAsync(interrupted: true);

        var fixture = await CreateDeliveryAsync();
        await ExecuteAsync("WITH t AS (SELECT clock_timestamp()-interval '31 minutes' AS authorized) UPDATE tagekyc.raw_export_recipient_package_deliveries SET \"AuthorizedAtUtc\"=t.authorized,\"AuthorizationExpiresAtUtc\"=t.authorized+interval '30 minutes' FROM t WHERE \"DeliveryId\"=@id", ("id", fixture.DeliveryId));
        var reconciled = await fixture.Repository.ReconcileNextAsync(default);
        Assert.Equal("Expired", reconciled.Outcome);
        await using var db = postgres.CreateDbContext();
        var expired = await db.RawExportRecipientPackageDeliveries.AsNoTracking()
            .SingleAsync(item => item.DeliveryId == fixture.DeliveryId);
        var expiredEvent = await db.RawExportRecipientPackageDeliveryEvents.AsNoTracking()
            .SingleAsync(item => item.DeliveryId == fixture.DeliveryId && item.EventType == "Expired");
        Assert.Equal(RecipientPackageDeliveryCodec.ExpiryEvidence(
            expired.DeliveryId, expired.PackageId, expired.RecipientClientApplicationId, "Authorized",
            expired.CreatorApiKeyId, expired.CreatorPrincipalId, expired.AuthorizationCorrelationDigest,
            expired.AuthorizationExpiresAtUtc, expired.ExpiredAtUtc!.Value), expiredEvent.EvidenceDigest);
        Assert.Equal(RecipientPackageDeliveryCodec.EventId(expired.DeliveryId, expired.Revision), expiredEvent.DeliveryEventId);

        var interruptedFixture = await CreateDeliveryAsync();
        var interruptedApiKey = Guid.NewGuid(); var interruptedPrincipal = Guid.NewGuid();
        var interruptedCorrelation = RandomNumberGenerator.GetBytes(32);
        var interruptedStart = await interruptedFixture.Repository.BeginAsync(
            interruptedFixture.Package.RecipientId, interruptedFixture.DeliveryId,
            interruptedApiKey, interruptedPrincipal, interruptedCorrelation, default);
        var interruptionEvidence = RecipientPackageDeliveryCodec.EncodeFailureObservations(null, [], false, [], false);
        try
        {
            var interruption = await interruptedFixture.Repository.InterruptAsync(
                interruptedFixture.DeliveryId, interruptedStart.Delivery!.Revision,
                interruptedStart.Delivery.DeliveryFence, "ProviderUnavailable", interruptionEvidence, default);
            Assert.Equal("Interrupted", interruption.Outcome);
        }
        finally { CryptographicOperations.ZeroMemory(interruptionEvidence); }
        await ExecuteAsync("WITH t AS (SELECT clock_timestamp()-interval '31 minutes' AS authorized) UPDATE tagekyc.raw_export_recipient_package_deliveries SET \"AuthorizedAtUtc\"=t.authorized,\"AuthorizationExpiresAtUtc\"=t.authorized+interval '30 minutes' FROM t WHERE \"DeliveryId\"=@id", ("id", interruptedFixture.DeliveryId));
        var interruptedReconciled = await interruptedFixture.Repository.ReconcileNextAsync(default);
        Assert.Equal("Expired", interruptedReconciled.Outcome);
        await using var interruptedDb = postgres.CreateDbContext();
        var interruptedExpired = await interruptedDb.RawExportRecipientPackageDeliveries.AsNoTracking()
            .SingleAsync(item => item.DeliveryId == interruptedFixture.DeliveryId);
        var interruptedEvent = await interruptedDb.RawExportRecipientPackageDeliveryEvents.AsNoTracking()
            .SingleAsync(item => item.DeliveryId == interruptedFixture.DeliveryId && item.EventType == "Expired");
        Assert.Equal(interruptedApiKey, interruptedEvent.AuthenticatedApiKeyId);
        Assert.Equal(interruptedPrincipal, interruptedEvent.AuthenticatedPrincipalId);
        Assert.Equal(interruptedCorrelation, interruptedEvent.CorrelationDigest);
        Assert.Equal(RecipientPackageDeliveryCodec.ExpiryEvidence(
            interruptedExpired.DeliveryId, interruptedExpired.PackageId,
            interruptedExpired.RecipientClientApplicationId, "Interrupted", interruptedApiKey,
            interruptedPrincipal, interruptedCorrelation, interruptedExpired.AuthorizationExpiresAtUtc,
            interruptedExpired.ExpiredAtUtc!.Value), interruptedEvent.EvidenceDigest);

        var staleFixture = await CreateDeliveryAsync();
        var apiKey = Guid.NewGuid(); var principal = Guid.NewGuid(); var correlation = RandomNumberGenerator.GetBytes(32);
        var started = await staleFixture.Repository.BeginAsync(staleFixture.Package.RecipientId, staleFixture.DeliveryId,
            apiKey, principal, correlation, default);
        Assert.Equal("Started", started.Outcome);
        await ExecuteAsync("WITH t AS (SELECT clock_timestamp()-interval '36 minutes' AS started) UPDATE tagekyc.raw_export_recipient_package_deliveries SET \"StreamStartedAtUtc\"=t.started,\"StreamLeaseExpiresAtUtc\"=t.started+interval '35 minutes' FROM t WHERE \"DeliveryId\"=@id", ("id", staleFixture.DeliveryId));
        var unknownResult = await staleFixture.Repository.ReconcileNextAsync(default);
        Assert.Equal("OutcomeUnknown", unknownResult.Outcome);
        var unknown = await db.RawExportRecipientPackageDeliveries.AsNoTracking()
            .SingleAsync(item => item.DeliveryId == staleFixture.DeliveryId);
        var unknownEvidence = RecipientPackageDeliveryCodec.OutcomeUnknownEvidence(
            unknown.DeliveryId, unknown.PackageId, unknown.RecipientClientApplicationId,
            unknown.StreamAttemptCount, unknown.DeliveryFence, unknown.StreamApiKeyId!.Value,
            unknown.StreamPrincipalId!.Value, unknown.StreamCorrelationDigest!, unknown.StreamStartedAtUtc!.Value,
            unknown.StreamLeaseExpiresAtUtc!.Value, unknown.OutcomeUnknownAtUtc!.Value);
        var unknownEvent = await db.RawExportRecipientPackageDeliveryEvents.AsNoTracking()
            .SingleAsync(item => item.DeliveryId == staleFixture.DeliveryId && item.EventType == "OutcomeUnknown");
        Assert.Equal(unknownEvidence, unknownEvent.EvidenceDigest);
        Assert.Equal(apiKey, unknownEvent.AuthenticatedApiKeyId);
        Assert.Equal(principal, unknownEvent.AuthenticatedPrincipalId);
        Assert.Equal(correlation, unknownEvent.CorrelationDigest);
    }

    [Fact]
    public async Task C316_bounded_encrypted_spool_rejects_extra_byte_and_zeroizes_on_dispose()
    {
        var spool = new RecipientPackageEncryptedSpool();
        var secret = Enumerable.Repeat((byte)0xA5, 1024).ToArray();
        await spool.WriteAsync(secret); spool.Seal();
        Assert.Equal(1024, spool.EncryptedLength);
        var segments = (List<byte[]>)typeof(RecipientPackageEncryptedSpool)
            .GetField("segments", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(spool)!;
        var rented = Assert.Single(segments);
        Assert.Contains((byte)0xA5, rented);
        await spool.DisposeAsync();
        Assert.All(rented, value => Assert.Equal((byte)0, value));
        CryptographicOperations.ZeroMemory(secret);

        await using var oversized = new RecipientPackageEncryptedSpool();
        Assert.Throws<InvalidOperationException>(() => oversized.Write(new byte[checked((int)RecipientPackageOptions.MaximumEncryptedPackageLength + 1)]));

        var body = Enumerable.Repeat((byte)0x5A, 64).ToArray();
        var package = await CreateFinalizedPackageAsync(body);
        var delivery = await CreateDeliveryAsync(package, "c316-early-eof");
        var truncated = package.Content[..^1];
        var options = new RecipientPackageDeliveryOptions(RecipientPackageDeliveryTopology.S3CompatibleDurable,
            TestProvider(), new("delivery", "secret"), postgres.ConnectionString, true);
        using var pool = new RecipientPackageDeliverySpoolPool();
        var coordinator = new RecipientPackageDeliveryCoordinator(options, delivery.Repository,
            new ScriptedReader(RecipientPackageDeliveryReadOutcome.Opened, truncated), pool);
        var actor = new AuthenticatedClientContext(Guid.NewGuid(), package.RecipientId, "c316",
            AuthenticatedCallerCategory.BusinessConsumer,
            new HashSet<string> { "business.raw-export.package.download" }, PrincipalId: Guid.NewGuid());
        var correlation = RecipientPackageDeliveryCodec.CorrelationDigest("c316-early-eof");
        try
        {
            var rejected = await coordinator.PrepareContentAsync(actor, delivery.DeliveryId, correlation, default);
            Assert.False(rejected.IsSuccess);
            Assert.Equal(RecipientPackageDeliveryErrorCodes.IntegrityUnavailable, rejected.Error?.Code);
            await AssertIntegrityFailureKindAsync(delivery.DeliveryId, "CiphertextMismatch");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(body);
            CryptographicOperations.ZeroMemory(correlation);
            CryptographicOperations.ZeroMemory(truncated);
        }
    }

    [Fact]
    public async Task C317_real_C2_finalized_metadata_is_consumed_without_test_substitute()
    {
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        var providerConfiguration = minio.RecipientPackageConfiguration();
        var c2Options = new RecipientPackageOptions(RecipientPackageTopology.S3CompatibleDurable, providerConfiguration, true);
        S3CompatibleRecipientPackageProvider? c2Store = null;
        try
        {
            var execution = await new Tip88C1C1ResolverAssemblyTests(postgres).ExecuteWithRealC2ProviderAsync(
                async (_, recipientId) =>
                {
                    await InsertActiveKeyAsync(recipientId, $"c317-{Guid.NewGuid():N}");
                    var factory = new RecipientPackageObjectClientFactory(c2Options);
                    c2Store = new S3CompatibleRecipientPackageProvider(c2Options, factory);
                    return new RecipientPackagePreparationProvider(
                        c2Options,
                        new RecipientPackageRepository(new C2RoleConnectionFactory(postgres.ConnectionString)),
                        new RawExportAssemblyRepository(new AssemblyRoleConnectionFactory(postgres.ConnectionString)),
                        new RecipientPackageCryptoService(), c2Store, c2Store, c2Store);
                });
            Assert.Equal(RawExportAssemblyExecutionOutcome.Sealed, execution.Result.Outcome);
            Assert.NotNull(execution.Result.C2PreparationId);
            await using var db = postgres.CreateDbContext();
            var package = await db.RawExportRecipientPackagePreparations.AsNoTracking()
                .SingleAsync(row => row.C2PreparationId == execution.Result.C2PreparationId);
            Assert.Equal("Finalized", package.State);
            Assert.NotNull(package.EncryptedPackageLength);
            Assert.NotNull(package.PackageCiphertextDigest);
            Assert.NotNull(package.EnvelopeDigest);

            await minio.RestartAsync();

            var deliveryOptions = new RecipientPackageDeliveryOptions(
                RecipientPackageDeliveryTopology.S3CompatibleDurable, providerConfiguration,
                minio.RecipientPackageDeliveryReaderCredential(), postgres.ConnectionString, true);
            using var deliveryFactory = new RecipientPackageDeliveryObjectClientFactory(deliveryOptions);
            using var deliveryReader = new S3CompatibleRecipientPackageDeliveryReader(deliveryOptions, deliveryFactory);
            using var pool = new RecipientPackageDeliverySpoolPool();
            var repository = DeliveryRepository();
            var coordinator = new RecipientPackageDeliveryCoordinator(deliveryOptions, repository, deliveryReader, pool);
            var actor = new AuthenticatedClientContext(
                Guid.NewGuid(), execution.RecipientClientApplicationId, "c317",
                AuthenticatedCallerCategory.BusinessConsumer,
                new HashSet<string>(StringComparer.Ordinal) { "business.raw-export.package.download" },
                PrincipalId: Guid.NewGuid());
            var correlation = RecipientPackageDeliveryCodec.CorrelationDigest("c317-real-c2-through-c3");
            try
            {
                var created = await coordinator.CreateAsync(actor, package.PackageId, $"c317-{Guid.NewGuid():N}", correlation, default);
                Assert.True(created.IsSuccess);
                var leaseResult = await coordinator.PrepareContentAsync(actor, created.Value!.Delivery.DeliveryId, correlation, default);
                Assert.True(leaseResult.IsSuccess);
                await using var lease = leaseResult.Value!;
                using var copied = new MemoryStream();
                await lease.Content.CopyToAsync(copied);
                var bytes = copied.ToArray();
                var digest = SHA256.HashData(bytes);
                try { await lease.CompleteAsync(bytes.LongLength, digest, default); }
                finally { CryptographicOperations.ZeroMemory(digest); }
                var final = await repository.ReadAsync(actor.ClientApplicationId, created.Value.Delivery.DeliveryId, default);
                Assert.Equal("ServerStreamCompleted", final.Delivery?.State);
                Assert.Equal(package.EncryptedPackageLength, final.Delivery?.EncryptedPackageLength);
                Assert.Equal(package.PackageCiphertextDigest, final.Delivery?.PackageCiphertextDigest);
                Assert.Equal(package.EnvelopeDigest, final.Delivery?.EnvelopeDigest);
                Assert.Equal(package.ObjectBindingDigest, final.Delivery?.ObjectBindingDigest);

                var last = bytes.Length - 1;
                bytes[last] ^= 0x01;
                await minio.PutRootObjectAsync(package.BucketName, package.ObjectKey, bytes);
                var tamperedDelivery = await coordinator.CreateAsync(actor, package.PackageId,
                    $"c317-ciphertext-{Guid.NewGuid():N}", correlation, default);
                Assert.True(tamperedDelivery.IsSuccess);
                var tamperedRead = await coordinator.PrepareContentAsync(actor,
                    tamperedDelivery.Value!.Delivery.DeliveryId, correlation, default);
                Assert.False(tamperedRead.IsSuccess);
                Assert.Equal(RecipientPackageDeliveryErrorCodes.IntegrityUnavailable, tamperedRead.Error?.Code);
                await AssertIntegrityFailureKindAsync(tamperedDelivery.Value.Delivery.DeliveryId, "CiphertextMismatch");

                bytes[last] ^= 0x01;
                await minio.PutRootObjectAsync(package.BucketName, package.ObjectKey, bytes);
                var originalEnvelope = package.EnvelopeDigest!.ToArray();
                var mismatchedEnvelope = SHA256.HashData("C317-M26"u8);
                try
                {
                    await ExecuteAsync("UPDATE tagekyc.raw_export_recipient_package_preparations SET \"EnvelopeDigest\"=@digest WHERE \"PackageId\"=@id",
                        ("digest", mismatchedEnvelope), ("id", package.PackageId));
                    var mismatchDelivery = await coordinator.CreateAsync(actor, package.PackageId,
                        $"c317-envelope-{Guid.NewGuid():N}", correlation, default);
                    Assert.True(mismatchDelivery.IsSuccess);
                    var mismatchRead = await coordinator.PrepareContentAsync(actor,
                        mismatchDelivery.Value!.Delivery.DeliveryId, correlation, default);
                    Assert.False(mismatchRead.IsSuccess);
                    Assert.Equal(RecipientPackageDeliveryErrorCodes.IntegrityUnavailable, mismatchRead.Error?.Code);
                    await AssertIntegrityFailureKindAsync(mismatchDelivery.Value.Delivery.DeliveryId, "EnvelopeMismatch");
                }
                finally
                {
                    await ExecuteAsync("UPDATE tagekyc.raw_export_recipient_package_preparations SET \"EnvelopeDigest\"=@digest WHERE \"PackageId\"=@id",
                        ("digest", originalEnvelope), ("id", package.PackageId));
                    CryptographicOperations.ZeroMemory(originalEnvelope);
                    CryptographicOperations.ZeroMemory(mismatchedEnvelope);
                    CryptographicOperations.ZeroMemory(bytes);
                }
            }
            finally { CryptographicOperations.ZeroMemory(correlation); }
        }
        finally { c2Store?.Dispose(); }
    }

    [Fact]
    public async Task C318_integrity_failure_is_delivery_local_and_new_delivery_reads_fresh()
    {
        var package = await CreateFinalizedPackageAsync();
        var a = await CreateDeliveryAsync(package, "c318-a");
        var options = new RecipientPackageDeliveryOptions(RecipientPackageDeliveryTopology.S3CompatibleDurable,
            TestProvider(), new("delivery", "secret"), postgres.ConnectionString, true);
        using var firstPool = new RecipientPackageDeliverySpoolPool();
        var absent = new ScriptedReader(RecipientPackageDeliveryReadOutcome.PositivelyAbsent);
        var firstCoordinator = new RecipientPackageDeliveryCoordinator(options, a.Repository, absent, firstPool);
        var actor = new AuthenticatedClientContext(Guid.NewGuid(), package.RecipientId, "c318",
            AuthenticatedCallerCategory.BusinessConsumer,
            new HashSet<string> { "business.raw-export.package.download" }, PrincipalId: Guid.NewGuid());
        var correlation = RecipientPackageDeliveryCodec.CorrelationDigest("c318-a");
        var failed = await firstCoordinator.PrepareContentAsync(actor, a.DeliveryId, correlation, default);
        Assert.False(failed.IsSuccess);
        Assert.Equal(RecipientPackageDeliveryErrorCodes.IntegrityUnavailable, failed.Error?.Code);
        await AssertIntegrityFailureKindAsync(a.DeliveryId, "ObjectAbsent");
        Assert.Equal("IntegrityUnavailable", (await a.Repository.ReadAsync(package.RecipientId, a.DeliveryId, default)).Delivery?.State);

        var b = await CreateDeliveryAsync(package, "c318-b");
        using var secondPool = new RecipientPackageDeliverySpoolPool();
        var fresh = new ScriptedReader(RecipientPackageDeliveryReadOutcome.Opened, package.Content);
        var secondCoordinator = new RecipientPackageDeliveryCoordinator(options, b.Repository, fresh, secondPool);
        var recovered = await secondCoordinator.PrepareContentAsync(actor, b.DeliveryId, correlation, default);
        Assert.True(recovered.IsSuccess);
        await using var lease = recovered.Value!;
        using var sink = new MemoryStream();
        await lease.Content.CopyToAsync(sink);
        var digest = SHA256.HashData(sink.ToArray());
        try { await lease.CompleteAsync(sink.Length, digest, default); }
        finally { CryptographicOperations.ZeroMemory(digest); CryptographicOperations.ZeroMemory(correlation); }
        Assert.NotEqual(a.DeliveryId, b.DeliveryId);
        Assert.Equal(1, absent.OpenCount);
        Assert.Equal(1, fresh.OpenCount);
        Assert.Equal("ServerStreamCompleted", (await b.Repository.ReadAsync(package.RecipientId, b.DeliveryId, default)).Delivery?.State);
    }

    [Fact]
    public async Task C320_copy_completion_and_late_lease_have_distinct_terminal_outcomes()
    {
        var cancelledFixture = await CreateDeliveryAsync();
        var cancelledStart = await BeginAsync(cancelledFixture);
        var cancellationObservations = RecipientPackageDeliveryCodec.EncodeFailureObservations(
            0, [], false, [], false);
        var cancelled = await cancelledFixture.Repository.InterruptAsync(cancelledFixture.DeliveryId,
            cancelledStart.Delivery!.Revision, cancelledStart.Delivery.DeliveryFence,
            "CopyCancelled", cancellationObservations, default);
        CryptographicOperations.ZeroMemory(cancellationObservations);
        Assert.Equal("Interrupted", cancelled.Outcome);

        var graceFixture = await CreateDeliveryAsync();
        var graceStart = await BeginAsync(graceFixture);
        await ExecuteAsync("WITH t AS (SELECT clock_timestamp()-interval '31 minutes' AS authorized) UPDATE tagekyc.raw_export_recipient_package_deliveries SET \"AuthorizedAtUtc\"=t.authorized,\"AuthorizationExpiresAtUtc\"=t.authorized+interval '30 minutes' FROM t WHERE \"DeliveryId\"=@id", ("id", graceFixture.DeliveryId));
        var completed = await graceFixture.Repository.CompleteAsync(graceFixture.DeliveryId, graceStart.Delivery!.Revision,
            graceStart.Delivery.DeliveryFence, graceFixture.Package.Length, graceFixture.Package.CiphertextDigest, default);
        Assert.Equal("Completed", completed.Outcome);
        Assert.NotNull(completed.Delivery?.DeliveryReceiptDigest);

        var lateFixture = await CreateDeliveryAsync();
        var lateStart = await BeginAsync(lateFixture);
        await ExecuteAsync("WITH t AS (SELECT clock_timestamp()-interval '36 minutes' AS started) UPDATE tagekyc.raw_export_recipient_package_deliveries SET \"StreamStartedAtUtc\"=t.started,\"StreamLeaseExpiresAtUtc\"=t.started+interval '35 minutes' FROM t WHERE \"DeliveryId\"=@id", ("id", lateFixture.DeliveryId));
        var late = await lateFixture.Repository.CompleteAsync(lateFixture.DeliveryId, lateStart.Delivery!.Revision,
            lateStart.Delivery.DeliveryFence, lateFixture.Package.Length, lateFixture.Package.CiphertextDigest, default);
        Assert.Equal("OutcomeUnknown", late.Outcome);
        Assert.Null(late.Delivery?.DeliveryReceiptDigest);
    }

    [Fact]
    public async Task C321_receipt_absolute_vector_sql_csharp_and_stream_actor_are_exact()
    {
        var vectorFields = new[]
        {
            "11111111111111111111111111111111", "22222222222222222222222222222222",
            "33333333333333333333333333333333", "2", "7",
            "000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f", "123456",
            "2026-08-18T01:02:03.0040000Z", "2026-08-18T01:03:04.0050000Z",
            "2026-08-18T01:04:05.0060000Z", "44444444444444444444444444444444",
            "55555555555555555555555555555555",
        };
        var preimage = C1HashCanonical.EncodeLengthPrefixedPayload("tip-88c1-c3-delivery-receipt-v1", vectorFields);
        Assert.Equal(399, preimage.Length);
        Assert.Equal("0000001F7469702D383863312D63332D64656C69766572792D726563656970742D7631000000203131313131313131313131313131313131313131313131313131313131313131000000203232323232323232323232323232323232323232323232323232323232323232000000203333333333333333333333333333333333333333333333333333333333333333000000013200000001370000004030303031303230333034303530363037303830393061306230633064306530663130313131323133313431353136313731383139316131623163316431653166000000063132333435360000001C323032362D30382D31385430313A30323A30332E303034303030305A0000001C323032362D30382D31385430313A30333A30342E303035303030305A0000001C323032362D30382D31385430313A30343A30352E303036303030305A000000203434343434343434343434343434343434343434343434343434343434343434000000203535353535353535353535353535353535353535353535353535353535353535", Convert.ToHexString(preimage));
        Assert.Equal("9E1D054F0D722918E51C1512B1F17BCCA24C709355C2F48AE01BD9F450F32881",
            Convert.ToHexString(SHA256.HashData(preimage)));

        var fixture = await CreateDeliveryAsync();
        var apiKeyId = Guid.NewGuid(); var principalId = Guid.NewGuid();
        var correlation = RandomNumberGenerator.GetBytes(32);
        var started = await fixture.Repository.BeginAsync(fixture.Package.RecipientId, fixture.DeliveryId,
            apiKeyId, principalId, correlation, default);
        Assert.Equal("Started", started.Outcome);
        var completed = await fixture.Repository.CompleteAsync(fixture.DeliveryId, started.Delivery!.Revision,
            started.Delivery.DeliveryFence, fixture.Package.Length, fixture.Package.CiphertextDigest, default);
        Assert.Equal("Completed", completed.Outcome);
        var row = completed.Delivery!;
        var expected = RecipientPackageDeliveryCodec.ReceiptDigest(row.DeliveryId, row.PackageId,
            row.RecipientClientApplicationId, row.StreamAttemptCount, row.DeliveryFence,
            row.PackageCiphertextDigest, row.EncryptedPackageLength, row.AuthorizedAtUtc,
            row.StreamStartedAtUtc!.Value, row.ServerStreamCompletedAtUtc!.Value, apiKeyId, principalId);
        Assert.Equal(expected, row.DeliveryReceiptDigest);
        await using var db = postgres.CreateDbContext();
        var persistedEvent = await db.RawExportRecipientPackageDeliveryEvents.AsNoTracking()
            .SingleAsync(item => item.DeliveryId == row.DeliveryId && item.EventType == "ServerStreamCompleted");
        Assert.Equal(RecipientPackageDeliveryCodec.EventId(row.DeliveryId, row.Revision), persistedEvent.DeliveryEventId);
        Assert.Equal(apiKeyId, persistedEvent.AuthenticatedApiKeyId);
        Assert.Equal(principalId, persistedEvent.AuthenticatedPrincipalId);
        Assert.Equal(correlation, persistedEvent.CorrelationDigest);
        Assert.Equal(expected, persistedEvent.EvidenceDigest);
    }

    [Fact]
    public async Task C322_status_and_create_replay_remain_historical_after_key_revoke()
    {
        var package = await CreateFinalizedPackageAsync();
        var authorized = await CreateDeliveryAsync(package, "c322-authorized");
        var streaming = await CreateDeliveryAsync(package, "c322-streaming");
        await BeginAsync(streaming);
        var interrupted = await CreateDeliveryAsync(package, "c322-interrupted");
        var interruptedStart = await BeginAsync(interrupted);
        var observations = RecipientPackageDeliveryCodec.EncodeFailureObservations(null, [], false, [], false);
        await interrupted.Repository.InterruptAsync(interrupted.DeliveryId, interruptedStart.Delivery!.Revision,
            interruptedStart.Delivery.DeliveryFence, "ProviderUnavailable", observations, default);
        var integrity = await CreateDeliveryAsync(package, "c322-integrity");
        var integrityStart = await BeginAsync(integrity);
        await integrity.Repository.IntegrityUnavailableAsync(integrity.DeliveryId, integrityStart.Delivery!.Revision,
            integrityStart.Delivery.DeliveryFence, "ObjectAbsent", observations, default);
        CryptographicOperations.ZeroMemory(observations);
        var unknown = await CreateDeliveryAsync(package, "c322-unknown");
        var unknownStart = await BeginAsync(unknown);
        await ExecuteAsync("WITH t AS (SELECT clock_timestamp()-interval '36 minutes' AS started) UPDATE tagekyc.raw_export_recipient_package_deliveries SET \"StreamStartedAtUtc\"=t.started,\"StreamLeaseExpiresAtUtc\"=t.started+interval '35 minutes' FROM t WHERE \"DeliveryId\"=@id", ("id", unknown.DeliveryId));
        await unknown.Repository.ReconcileNextAsync(default);
        var completed = await CreateDeliveryAsync(package, "c322-completed");
        var completedStart = await BeginAsync(completed);
        await completed.Repository.CompleteAsync(completed.DeliveryId, completedStart.Delivery!.Revision,
            completedStart.Delivery.DeliveryFence, package.Length, package.CiphertextDigest, default);
        var expired = await CreateDeliveryAsync(package, "c322-expired");
        await ExecuteAsync("WITH t AS (SELECT clock_timestamp()-interval '31 minutes' AS authorized) UPDATE tagekyc.raw_export_recipient_package_deliveries SET \"AuthorizedAtUtc\"=t.authorized,\"AuthorizationExpiresAtUtc\"=t.authorized+interval '30 minutes' FROM t WHERE \"DeliveryId\"=@id", ("id", expired.DeliveryId));
        await expired.Repository.ReconcileNextAsync(default);

        await ExecuteAsync("UPDATE tagekyc.raw_export_recipient_key_registrations SET \"State\"='Revoked',\"Revision\"=\"Revision\"+1,\"RevokedAtUtc\"=clock_timestamp() WHERE \"RecipientClientApplicationId\"=@id", ("id", package.RecipientId));
        var expected = new Dictionary<Guid, string>
        {
            [authorized.DeliveryId] = "Authorized", [streaming.DeliveryId] = "Streaming",
            [interrupted.DeliveryId] = "Interrupted", [integrity.DeliveryId] = "IntegrityUnavailable",
            [unknown.DeliveryId] = "OutcomeUnknown", [completed.DeliveryId] = "ServerStreamCompleted",
            [expired.DeliveryId] = "Expired",
        };
        foreach (var pair in expected)
        {
            var read = await authorized.Repository.ReadAsync(package.RecipientId, pair.Key, default);
            Assert.Equal("ExistingMatch", read.Outcome);
            Assert.Equal(pair.Value, read.Delivery?.State);
        }
        var digest = RecipientPackageDeliveryCodec.IdempotencyKeyDigest("c322-authorized");
        try
        {
            var replay = await authorized.Repository.CreateAsync(package.RecipientId, package.PackageId,
                authorized.DeliveryId, digest, Guid.NewGuid(), Guid.NewGuid(), RandomNumberGenerator.GetBytes(32), default);
            Assert.Equal("ExistingMatch", replay.Outcome);
            Assert.Equal("Authorized", replay.Delivery?.State);
        }
        finally { CryptographicOperations.ZeroMemory(digest); }
    }

    [Fact]
    public async Task C325_apply_down_reapply_pending_model_and_snapshot_tripwires_are_clean()
    {
        await using var isolated = await postgres.CreateDisposableCurrentDatabaseAsync("c3_c325");
        await using var db = isolated.CreateDbContext(); var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20260818120000_Tip88C1C2RecipientPackage");
        await migrator.MigrateAsync();
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.Equal("20260819120000_Tip88C1C3AuthenticatedPackageDelivery", (await db.Database.GetAppliedMigrationsAsync()).Last());
    }

    [Fact]
    public async Task C326_proof_census_hygiene_and_allowlist_are_exact()
    {
        var files = new[]
        {
            "tests/TagEkyc.UnitTests/Tip88C1C3RecipientPackageDeliveryCodecTests.cs",
            "tests/TagEkyc.UnitTests/Tip88C1C3RecipientPackageDeliveryApplicationTests.cs",
            "tests/TagEkyc.ArchTests/Tip88C1C3RecipientPackageDeliveryArchTests.cs",
            "tests/TagEkyc.IntegrationTests/Tip88C1C3RecipientPackageDeliveryTests.cs",
        };
        var source = string.Join('\n', files.Select(path => File.ReadAllText(ProjectPath(path))));
        Assert.Equal(26, Enumerable.Range(301, 26).Count(id => Count(source, $"C{id}_") == 1));
        Assert.DoesNotContain("Skip" + " =", source, StringComparison.Ordinal);
        await postgres.AssertLatestMigrationAsync("C3 proof census");
    }

    private async Task<DeliveryFixture> CreateDeliveryAsync(FinalizedPackage? package = null, string? key = null)
    {
        package ??= await CreateFinalizedPackageAsync();
        key ??= $"delivery-{Guid.NewGuid():N}";
        var digest = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(key));
        var deliveryId = RecipientPackageDeliveryCodec.DeliveryId(package.RecipientId, digest);
        var repository = DeliveryRepository();
        var result = await repository.CreateAsync(package.RecipientId, package.PackageId, deliveryId, digest,
            Guid.NewGuid(), Guid.NewGuid(), RandomNumberGenerator.GetBytes(32), default);
        return new(repository, package, deliveryId, result);
    }

    private Task<RecipientPackageDeliveryMutation> BeginAsync(DeliveryFixture fixture) => fixture.Repository.BeginAsync(
        fixture.Package.RecipientId, fixture.DeliveryId, Guid.NewGuid(), Guid.NewGuid(), RandomNumberGenerator.GetBytes(32), default);

    private static async Task AssertExactC3FunctionSurfaceAsync(NpgsqlConnection connection)
    {
        var expected = new (string Name, string Arguments)[]
        {
            ("raw_export_begin_recipient_package_delivery_stream", "uuid, uuid, uuid, uuid, bytea"),
            ("raw_export_complete_recipient_package_delivery", "uuid, bigint, bigint, bigint, bytea"),
            ("raw_export_create_recipient_package_delivery", "uuid, uuid, uuid, bytea, uuid, uuid, bytea"),
            ("raw_export_guard_recipient_package_delivery_event", ""),
            ("raw_export_probe_recipient_package_delivery_content", "uuid, uuid"),
            ("raw_export_read_recipient_package_delivery", "uuid, uuid"),
            ("raw_export_reconcile_next_recipient_package_delivery", ""),
            ("raw_export_record_recipient_package_delivery_interrupted", "uuid, bigint, bigint, text, bytea"),
            ("raw_export_record_recipient_package_integrity_unavailable", "uuid, bigint, bigint, text, bytea"),
        };
        await using var command = new NpgsqlCommand("""
            SELECT p.proname, pg_catalog.oidvectortypes(p.proargtypes)
            FROM pg_catalog.pg_proc p
            JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
            WHERE n.nspname='tagekyc' AND p.proname=ANY(@names)
            ORDER BY p.proname, pg_catalog.oidvectortypes(p.proargtypes)
            """, connection);
        command.Parameters.AddWithValue("names", expected.Select(item => item.Name).ToArray());
        var actual = new List<(string Name, string Arguments)>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) actual.Add((reader.GetString(0), reader.GetString(1)));
        Assert.Equal(expected.OrderBy(item => item.Name, StringComparer.Ordinal).ThenBy(item => item.Arguments, StringComparer.Ordinal), actual);
    }

    private async Task AssertC324CatalogMutationRejectedAsync(
        RecipientPackageDeliveryReadinessValidator readiness,
        string mutationSql,
        string restoreSql)
    {
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        try
        {
            await using (var mutation = new NpgsqlCommand(mutationSql, connection))
                await mutation.ExecuteNonQueryAsync();
            var error = await Assert.ThrowsAsync<RecipientPackageDeliveryReadinessException>(
                () => readiness.ValidateAsync(CancellationToken.None));
            Assert.Equal(RecipientPackageDeliveryReadinessValidator.Codes[2], error.Code);
        }
        finally
        {
            await using var restore = new NpgsqlCommand(restoreSql, connection);
            await restore.ExecuteNonQueryAsync();
        }
        await readiness.ValidateAsync(CancellationToken.None);
    }

    private static async Task AssertFailClosedHostStartsAsync(Dictionary<string, string?> configuration)
    {
        await using var factory = C3HostFactory(configuration);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"/api/ekyc/raw-export/packages/{Guid.NewGuid():D}/deliveries");
        request.Headers.Add("X-TagEkyc-Api-Key", "localdev-recipient-package-delivery-key");
        request.Headers.Add("Idempotency-Key", $"c324-{Guid.NewGuid():N}");
        using var response = await client.SendAsync(request);
        Assert.Equal(System.Net.HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.DoesNotContain(factory.Services.GetServices<IHostedService>(),
            service => service.GetType().Name == "RecipientPackageDeliveryHostedService");
    }

    private static WebApplicationFactory<Program> C3HostFactory(
        IReadOnlyDictionary<string, string?> configuration) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            foreach (var setting in configuration)
                builder.UseSetting(setting.Key, setting.Value);
        });

    private static Dictionary<string, string?> DurableHostConfiguration(
        RecipientPackageProviderConfiguration provider,
        RecipientPackageCredential deliveryReader,
        string databaseConnectionString)
    {
        var c2 = RecipientPackageOptions.SectionPath;
        var providerPath = $"{c2}:Providers:{provider.ProviderConfigurationId}";
        var c3 = RecipientPackageDeliveryOptions.SectionPath;
        return new Dictionary<string, string?>
        {
            [$"{c2}:Topology"] = "S3CompatibleDurable",
            [$"{c2}:ProviderConfigurationId"] = provider.ProviderConfigurationId,
            [$"{providerPath}:ServiceUrl"] = provider.ServiceUrl.ToString(),
            [$"{providerPath}:BucketName"] = provider.BucketName,
            [$"{providerPath}:ForcePathStyle"] = provider.ForcePathStyle.ToString(),
            [$"{providerPath}:RegionIdentifier"] = provider.RegionIdentifier,
            [$"{providerPath}:AllowLoopbackHttp"] = provider.AllowLoopbackHttp.ToString(),
            [$"{providerPath}:Writer:AccessKeyId"] = provider.Writer.AccessKeyId,
            [$"{providerPath}:Writer:SecretAccessKey"] = provider.Writer.SecretAccessKey,
            [$"{providerPath}:Reconciler:AccessKeyId"] = provider.Reconciler.AccessKeyId,
            [$"{providerPath}:Reconciler:SecretAccessKey"] = provider.Reconciler.SecretAccessKey,
            [$"{providerPath}:Lifecycle:AccessKeyId"] = provider.Lifecycle.AccessKeyId,
            [$"{providerPath}:Lifecycle:SecretAccessKey"] = provider.Lifecycle.SecretAccessKey,
            [$"{providerPath}:PostureProbe:AccessKeyId"] = provider.PostureProbe.AccessKeyId,
            [$"{providerPath}:PostureProbe:SecretAccessKey"] = provider.PostureProbe.SecretAccessKey,
            [$"{c3}:Topology"] = "S3CompatibleDurable",
            [$"{c3}:ProviderConfigurationId"] = provider.ProviderConfigurationId,
            [$"{c3}:DeliveryReader:AccessKeyId"] = deliveryReader.AccessKeyId,
            [$"{c3}:DeliveryReader:SecretAccessKey"] = deliveryReader.SecretAccessKey,
            [$"{c3}:DatabaseConnectionString"] = databaseConnectionString,
        };
    }

    private async Task<FinalizedPackage> CreateFinalizedPackageAsync(byte[]? packageBody = null)
    {
        var lineage = await new Tip88C1C1ResolverAssemblyTests(postgres).CreateC2RecipientPackageLineageAsync();
        await ExecuteAsync("UPDATE tagekyc.raw_export_recipient_key_registrations SET \"State\"='Revoked',\"Revision\"=\"Revision\"+1,\"RevokedAtUtc\"=clock_timestamp() WHERE \"RecipientClientApplicationId\"=@id AND \"State\"='Active'", ("id", lineage.Request.RecipientClientApplicationId));
        using var rsa = RSA.Create(3072); var spki = rsa.ExportSubjectPublicKeyInfo(); var keyId = $"c3-{Guid.NewGuid():N}"; var fingerprint = SHA256.HashData(spki);
        await ExecuteAsync("INSERT INTO tagekyc.raw_export_recipient_key_registrations(\"RecipientClientApplicationId\",\"RecipientKeyId\",\"RecipientKeyVersion\",\"PublicKeyAlgorithm\",\"PublicKeySpki\",\"PublicKeyFingerprint\",\"ValidFromUtc\",\"ValidUntilUtc\",\"State\",\"Revision\",\"RegisteredAtUtc\") VALUES(@recipient,@key,1,'RSA-OAEP-256',@spki,@fp,clock_timestamp()-interval '1 minute',clock_timestamp()+interval '1 hour','Active',1,clock_timestamp())",
            ("recipient", lineage.Request.RecipientClientApplicationId), ("key", keyId), ("spki", spki), ("fp", fingerprint));
        var equality = RecipientPackageCodec.PackageEqualityFingerprint(lineage.Request.C2PreparationId, lineage.Request.AssemblyId,
            lineage.Request.AssemblyFingerprint, lineage.Request.ManifestDigest, lineage.Request.AssemblyDigest,
            lineage.Request.AssemblyAuthenticationValue, lineage.Request.RecipientClientApplicationId, keyId, 1, fingerprint, lineage.Request.CompleteAssemblyLength);
        var packageId = RecipientPackageCodec.PackageId(lineage.Request.C2PreparationId, equality); var provider = TestProvider();
        var endpoint = RecipientPackageCodec.ProviderEndpointFingerprint(provider); var objectKey = RecipientPackageCodec.ObjectKey(packageId);
        var binding = RecipientPackageCodec.ObjectBindingDigest(provider.ProviderConfigurationId, endpoint, provider.BucketName, objectKey);
        var repository = new RecipientPackageRepository(new C2RoleConnectionFactory(postgres.ConnectionString));
        var reserve = await repository.ReserveAsync(new(lineage.Request.C2PreparationId, packageId, lineage.Request.AssemblyId,
            lineage.JobId, lineage.AttemptId, lineage.FencingToken, lineage.Request.AssemblyFingerprint, lineage.Request.ManifestDigest,
            lineage.Request.AssemblyDigest, lineage.Request.AssemblyAuthenticationValue, lineage.Request.CompleteAssemblyLength,
            lineage.Request.RecipientClientApplicationId, keyId, 1, fingerprint, 1, equality, RandomNumberGenerator.GetBytes(32),
            RecipientPackageProviderConfiguration.ProviderKind, provider.ProviderConfigurationId, endpoint, provider.BucketName,
            objectKey, binding, RecipientPackageOptions.PackageProfile), default);
        var envelopeBytes = System.Text.Encoding.ASCII.GetBytes("TIP-88C1-C2-PACKAGE-V1\0\0\0\u0002{}");
        var packageBytes = envelopeBytes.Concat(packageBody ?? []).ToArray();
        var envelope = SHA256.HashData(envelopeBytes); var ciphertext = SHA256.HashData(packageBytes); var length = packageBytes.LongLength;
        var begun = await repository.BeginPutAsync(lineage.Request.C2PreparationId, reserve.RowRevision!.Value, envelope, length, ciphertext, default);
        var prepared = await repository.RecordPreparedAsync(RecipientPackageDatabaseCapability.Reconciler, lineage.Request.C2PreparationId,
            begun.RowRevision!.Value, RandomNumberGenerator.GetBytes(32), length, ciphertext, RandomNumberGenerator.GetBytes(32), RandomNumberGenerator.GetBytes(32), default);
        var finalized = await repository.FinalizeAsync(lineage.Request.C2PreparationId, prepared.RowRevision!.Value, lineage.Request.AssemblyFingerprint, default);
        Assert.Equal("Finalized", finalized.Outcome);
        return new(packageId, lineage.Request.RecipientClientApplicationId, length, ciphertext, 4, packageBytes);
    }

    private async Task AssertIntegrityFailureKindAsync(Guid deliveryId, string expected)
    {
        await using var db = postgres.CreateDbContext();
        var delivery = await db.RawExportRecipientPackageDeliveries.AsNoTracking()
            .SingleAsync(row => row.DeliveryId == deliveryId);
        Assert.Equal("IntegrityUnavailable", delivery.State);
        Assert.Equal(expected, delivery.IntegrityFailureKind);
    }

    private async Task InsertActiveKeyAsync(Guid recipientId, string keyId)
    {
        using var rsa = RSA.Create(3072);
        var spki = rsa.ExportSubjectPublicKeyInfo();
        var fingerprint = SHA256.HashData(spki);
        try
        {
            await ExecuteAsync("UPDATE tagekyc.raw_export_recipient_key_registrations SET \"State\"='Revoked',\"Revision\"=\"Revision\"+1,\"RevokedAtUtc\"=clock_timestamp() WHERE \"RecipientClientApplicationId\"=@id AND \"State\"='Active'", ("id", recipientId));
            await ExecuteAsync("INSERT INTO tagekyc.raw_export_recipient_key_registrations(\"RecipientClientApplicationId\",\"RecipientKeyId\",\"RecipientKeyVersion\",\"PublicKeyAlgorithm\",\"PublicKeySpki\",\"PublicKeyFingerprint\",\"ValidFromUtc\",\"ValidUntilUtc\",\"State\",\"Revision\",\"RegisteredAtUtc\") VALUES(@recipient,@key,1,'RSA-OAEP-256',@spki,@fp,clock_timestamp()-interval '1 minute',clock_timestamp()+interval '1 hour','Active',1,clock_timestamp())",
                ("recipient", recipientId), ("key", keyId), ("spki", spki), ("fp", fingerprint));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(spki);
            CryptographicOperations.ZeroMemory(fingerprint);
        }
    }

    private async Task<DateTimeOffset> ReadDatabaseClockAsync()
    {
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT clock_timestamp();", connection);
        var value = (DateTime)(await command.ExecuteScalarAsync())!;
        return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }

    private RecipientPackageDeliveryRepository DeliveryRepository() => new(new DeliveryRoleConnectionFactory(postgres.ConnectionString));
    private async Task ExecuteAsync(string sql, params (string Name, object Value)[] values)
    { await using var connection = new NpgsqlConnection(postgres.ConnectionString); await connection.OpenAsync(); await using var command = new NpgsqlCommand(sql, connection); foreach (var value in values) command.Parameters.AddWithValue(value.Name, value.Value); await command.ExecuteNonQueryAsync(); }
    private async Task<long> CountRowsAsync(string table, Guid? packageId = null)
    {
        Assert.Contains(table, new[] { "raw_export_recipient_package_deliveries", "raw_export_recipient_package_delivery_events" });
        await using var connection = new NpgsqlConnection(postgres.ConnectionString); await connection.OpenAsync();
        var predicate = packageId.HasValue && table == "raw_export_recipient_package_deliveries" ? " WHERE \"PackageId\"=@package" : string.Empty;
        await using var command = new NpgsqlCommand($"SELECT count(*) FROM tagekyc.{table}{predicate}", connection);
        if (packageId.HasValue && predicate.Length > 0) command.Parameters.AddWithValue("package", packageId.Value);
        return (long)(await command.ExecuteScalarAsync())!;
    }
    private static RecipientPackageProviderConfiguration TestProvider() => new("c2-integration-minio-v1", new Uri("http://127.0.0.1:9000/"), "tagekyc-c2-integration", true, "us-east-1", new("writer", "secret"), new("reader", "secret"), new("lifecycle", "secret"), new("posture", "secret"), true);
    private static string MigrationSource() => File.ReadAllText(ProjectPath("src/TagEkyc.Infrastructure/Persistence/Migrations/20260819120000_Tip88C1C3AuthenticatedPackageDelivery.cs"));
    private static string Between(string text,string start,string end){var a=text.IndexOf(start,StringComparison.Ordinal);var b=text.IndexOf(end,a,StringComparison.Ordinal);return text[a..b];}
    private static int Count(string text,string token)=>(text.Length-text.Replace(token,string.Empty,StringComparison.Ordinal).Length)/token.Length;
    private static string ProjectPath(string relative){var d=new DirectoryInfo(AppContext.BaseDirectory);while(d is not null&&!File.Exists(Path.Combine(d.FullName,"TagEkyc.sln")))d=d.Parent;return Path.Combine(d?.FullName??throw new InvalidOperationException(),relative);}
    private sealed record FinalizedPackage(Guid PackageId,Guid RecipientId,long Length,byte[] CiphertextDigest,long PackageRevision,byte[] Content);
    private sealed record DeliveryFixture(RecipientPackageDeliveryRepository Repository,FinalizedPackage Package,Guid DeliveryId,RecipientPackageDeliveryMutation Created);
    private sealed class DeliveryRoleConnectionFactory(string connectionString):IRecipientPackageDeliveryConnectionFactory{public async Task<NpgsqlConnection> OpenAsync(CancellationToken token){var c=new NpgsqlConnection(connectionString);await c.OpenAsync(token);await new NpgsqlCommand("SET ROLE tagekyc_raw_export_package_delivery",c).ExecuteNonQueryAsync(token);return c;}}
    private sealed class CountingReader : IRecipientPackageDeliveryReader
    {
        public int OpenCount { get; private set; }
        public Task<RecipientPackageDeliveryReadResult> OpenExactAsync(RecipientPackageDeliveryLocator locator, CancellationToken cancellationToken)
        { OpenCount++; return Task.FromResult(new RecipientPackageDeliveryReadResult(RecipientPackageDeliveryReadOutcome.Unavailable, null)); }
    }
    private sealed class ScriptedReader(RecipientPackageDeliveryReadOutcome outcome, byte[]? content = null) : IRecipientPackageDeliveryReader
    {
        public int OpenCount { get; private set; }
        public RecipientPackageDeliveryLocator? Locator { get; private set; }
        public Task<RecipientPackageDeliveryReadResult> OpenExactAsync(RecipientPackageDeliveryLocator locator, CancellationToken cancellationToken)
        {
            OpenCount++;
            Locator = locator;
            Stream? stream = outcome == RecipientPackageDeliveryReadOutcome.Opened
                ? new MemoryStream(content ?? throw new InvalidOperationException("C3_CONTENT_REQUIRED"), writable: false)
                : null;
            return Task.FromResult(new RecipientPackageDeliveryReadResult(outcome, stream));
        }
    }
    private sealed class C2RoleConnectionFactory(string connectionString):IRecipientPackageConnectionFactory{public async Task<NpgsqlConnection> OpenAsync(RecipientPackageDatabaseCapability capability,CancellationToken token){var c=new NpgsqlConnection(connectionString);await c.OpenAsync(token);var role=capability switch{RecipientPackageDatabaseCapability.Preparer=>"tagekyc_raw_export_package_preparer",RecipientPackageDatabaseCapability.Reconciler=>"tagekyc_raw_export_package_reconciler",_=>"tagekyc_raw_export_package_lifecycle"};await new NpgsqlCommand($"SET ROLE {role}",c).ExecuteNonQueryAsync(token);return c;}}
    private sealed class C324HealthyPostureProbe : IRecipientPackagePostureProbe
    {
        public Task<RecipientPackageBucketPosture> InspectAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new RecipientPackageBucketPosture(true, true, true, true, true));
    }
    private sealed class EndpointAuthenticator : IApiKeyAuthenticator
    {
        public Task<SessionOperationResult<AuthenticatedClientContext>> AuthenticateAsync(HttpContext httpContext, string? requiredScope = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(SessionOperationResult<AuthenticatedClientContext>.Success(new(
                Guid.NewGuid(), Guid.NewGuid(), "c319", AuthenticatedCallerCategory.BusinessConsumer,
                new HashSet<string> { "business.raw-export.package.download" }, PrincipalId: Guid.NewGuid())));
    }
    private sealed class EndpointDeliveryService(Guid packageId, Guid deliveryId, byte[] bytes) : IRecipientPackageDeliveryApplicationService
    {
        public bool Completed { get; private set; }
        public bool FailPrepare { get; set; }
        private RecipientPackageDeliveryDto Dto => new(deliveryId, packageId, "Authorized",
            DateTimeOffset.Parse("2026-08-18T01:00:00Z"), DateTimeOffset.Parse("2026-08-18T01:30:00Z"),
            0, null, bytes.LongLength, RecipientPackageDeliveryCodec.Base64Url(SHA256.HashData(bytes)), null);
        public Task<SessionOperationResult<RecipientPackageDeliveryCreation>> CreateAsync(AuthenticatedClientContext actor, Guid requestedPackageId, string? idempotencyKey, string? traceIdentifier, CancellationToken cancellationToken) =>
            Task.FromResult(SessionOperationResult<RecipientPackageDeliveryCreation>.Success(new(Dto, true)));
        public Task<SessionOperationResult<RecipientPackageDeliveryDto>> ReadAsync(AuthenticatedClientContext actor, Guid requestedDeliveryId, CancellationToken cancellationToken) =>
            Task.FromResult(SessionOperationResult<RecipientPackageDeliveryDto>.Success(Dto));
        public Task<SessionOperationResult<RecipientPackageDeliveryContentLease>> PrepareContentAsync(AuthenticatedClientContext actor, Guid requestedDeliveryId, string? traceIdentifier, CancellationToken cancellationToken) =>
            Task.FromResult(FailPrepare
                ? SessionOperationResult<RecipientPackageDeliveryContentLease>.Failure(
                    RecipientPackageDeliveryErrorCodes.IntegrityUnavailable, "Verified package content is unavailable.", 503)
                : SessionOperationResult<RecipientPackageDeliveryContentLease>.Success(new(
                    new MemoryStream(bytes, writable: false), Dto,
                    (length, digest, token) => { Completed = length == bytes.LongLength && CryptographicOperations.FixedTimeEquals(digest, SHA256.HashData(bytes)); return Task.CompletedTask; },
                    (kind, length, digest, token) => Task.CompletedTask)));
    }
    private sealed class AssemblyRoleConnectionFactory(string connectionString) : IRawExportAssemblyConnectionFactory
    {
        public async Task<NpgsqlConnection> OpenAsync(RawExportAssemblyDatabaseCapability capability, CancellationToken token)
        {
            var role = capability switch
            {
                RawExportAssemblyDatabaseCapability.Resolver => "tagekyc_raw_export_assembly_resolver",
                RawExportAssemblyDatabaseCapability.Sealer => "tagekyc_raw_export_assembly_sealer",
                _ => throw new ArgumentOutOfRangeException(nameof(capability)),
            };
            var builder = new NpgsqlConnectionStringBuilder(connectionString) { Options = $"-c role={role}", Pooling = false };
            var connection = new NpgsqlConnection(builder.ConnectionString);
            await connection.OpenAsync(token);
            return connection;
        }
    }
}
