using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TagEkyc.Api;
using TagEkyc.Application;
using TagEkyc.Application.Ports;
using TagEkyc.Application.RawExport;
using TagEkyc.Application.VerificationSessions;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Domain;
using TagEkyc.Infrastructure.Auth;
using TagEkyc.Infrastructure.Persistence;
using TagEkyc.Infrastructure.RawExport;
using TagEkyc.RawExport.Client;

namespace TagEkyc.IntegrationTests;

[Collection(PostgresPersistenceCollection.Name)]
public sealed class RawExportDeliverySameJobEndToEndTests(PostgresPersistenceFixture postgres)
    : IAsyncLifetime
{
    private static readonly byte[] Pepper = SHA256.HashData("c5-integration-pepper"u8.ToArray());

    public Task InitializeAsync() => postgres.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Public_sdk_uses_one_job_through_durable_assembly_listing_delivery_and_decode()
    {
        await using var minio = await DurableObjectMinioFixture.StartAsync();
        using var recipientRsa = RSA.Create(3072);
        var recipient = Tip88B34AuthorizationEngineTests.ClientApplicationId;
        var principal = Tip88B34AuthorizationEngineTests.ConsumerPrincipal;
        const string recipientKeyId = "sdk-e2e-recipient-key";
        var credential = await ProvisionRecipientAsync(
            recipient, principal, recipientRsa, recipientKeyId);

        await using var setup = postgres.CreateDbContext();
        var classes = new[] { RawExportRawClass.ChipDg2Portrait, RawExportRawClass.LiveSelfieImage };
        var packetAuthority = await Tip88B4RawExportJobFoundationTests
            .CreateAuthorizedPacketPermitAsync(setup, classes);
        var sessionId = packetAuthority.SessionId;
        var policyId = packetAuthority.PolicyId;

        var portrait = "same-job-production-dg2"u8.ToArray();
        var selfie = "same-job-production-selfie"u8.ToArray();
        var sourceFactory = new Tip88C1B2R2DurableCustodyEncryptionDatabaseTests(postgres);
        var dg2 = await sourceFactory.CreateR3VerifiedSourceForExistingSessionAsync(
            portrait, minio, principal, recipient, sessionId, policyId,
            RawExportRawClass.ChipDg2Portrait);
        await MakeAvailableAsync(dg2);
        var live = await sourceFactory.CreateR3VerifiedSourceForExistingSessionAsync(
            selfie, minio, principal, recipient, sessionId, policyId,
            RawExportRawClass.LiveSelfieImage);
        await MakeAvailableAsync(live);
        await SelectAcceptanceAsync(dg2);
        await SelectAcceptanceAsync(live);

        var provider = minio.RecipientPackageConfiguration();
        var c2Options = new RecipientPackageOptions(
            RecipientPackageTopology.S3CompatibleDurable, provider, true);
        using var c2Store = new S3CompatibleRecipientPackageProvider(
            c2Options, new RecipientPackageObjectClientFactory(c2Options));

        var deliveryOptions = new RecipientPackageDeliveryOptions(
            RecipientPackageDeliveryTopology.S3CompatibleDurable,
            provider,
            minio.RecipientPackageDeliveryReaderCredential(),
            postgres.ConnectionString,
            true);
        using var deliveryFactory = new RecipientPackageDeliveryObjectClientFactory(deliveryOptions);
        using var deliveryReader = new S3CompatibleRecipientPackageDeliveryReader(deliveryOptions, deliveryFactory);
        using var deliveryPool = new RecipientPackageDeliverySpoolPool();
        var deliveryCoordinator = new RecipientPackageDeliveryCoordinator(
            deliveryOptions,
            new RecipientPackageDeliveryRepository(new DeliveryConnectionFactory(postgres.ConnectionString)),
            deliveryReader,
            deliveryPool);

        var cursorKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{RecipientPackageReferenceOptions.SectionPath}:Topology"] = "PostgresDurable",
            [$"{RecipientPackageReferenceOptions.SectionPath}:DatabaseConnectionString"] = postgres.ConnectionString,
            [$"{RecipientPackageReferenceOptions.SectionPath}:ActiveCursorKeyId"] = "sdk-e2e-cursor",
            [$"{RecipientPackageReferenceOptions.SectionPath}:ActiveCursorKeyVersion"] = "1",
            [$"{RecipientPackageReferenceOptions.SectionPath}:AcceptedCursorKeys:0:KeyId"] = "sdk-e2e-cursor",
            [$"{RecipientPackageReferenceOptions.SectionPath}:AcceptedCursorKeys:0:KeyVersion"] = "1",
            [$"{RecipientPackageReferenceOptions.SectionPath}:CursorKeys:sdk-e2e-cursor:1"] = cursorKey,
        });
        builder.Services.Configure<Microsoft.AspNetCore.Routing.RouteOptions>(options =>
            options.ConstraintMap["D"] = typeof(DFormatGuidRouteConstraint));
        builder.Services.AddTagEkycPostgresPersistence(postgres.ConnectionString);
        builder.Services.AddScoped<IApiKeyAuthenticator>(services => new ManagedAuthenticator(
            new RecipientCredentialAuthenticationPolicy(
                new PostgresHashedApiKeyStore(
                    services.GetRequiredService<TagEkycDbContext>(), new ApiKeyStorePepper(Pepper)),
                new NoGlobalPolicyProvider())));
        builder.Services.AddScoped<IRawExportControlPlaneApplicationService,
            RawExportControlPlaneApplicationService>();
        builder.Services.AddSingleton<IRecipientPackageDeliveryApplicationService>(
            new RecipientPackageDeliveryApplicationService(deliveryCoordinator));
        builder.Services.AddTagEkycRecipientPackageReference(builder.Configuration);
        await using var app = builder.Build();
        app.MapRawExportControlPlaneEndpoints();
        app.MapRecipientPackageReferenceEndpoints();
        app.MapRecipientPackageDeliveryEndpoints();
        await app.StartAsync();

        var deliveryGate = new DeliveryGateHandler(app.GetTestServer().CreateHandler());
        using var sdkHttp = new HttpClient(deliveryGate);
        var sdk = new TagEkycRawExportClient(
            sdkHttp,
            new TagEkycRawExportClientOptions(
                new Uri("https://tagekyc.test"),
                "X-TagEkyc-Api-Key",
                credential,
                policyId,
                1,
                recipient,
                recipientKeyId,
                1,
                TimeSpan.FromMilliseconds(25),
                400),
            new FixedPrivateKeySource(recipientRsa.ExportPkcs8PrivateKey()));
        var acquire = sdk.AcquireAsync(new(sessionId, Guid.NewGuid()));

        var jobId = await WaitForJobAsync(sessionId, recipient);
        await using var workDb = postgres.CreateDbContext();
        var workSource = new DurableRawExportAssemblyWorkSource(
            new AssemblyConnectionFactory(postgres.ConnectionString),
            Tip88B4RawExportJobFoundationTests.CreateJobRepository(workDb),
            new RawExportAssemblyWorkerIdentity(Guid.NewGuid()));
        var request = await WaitForWorkAsync(workSource);
        Assert.Equal(jobId, request.JobId);

        var execution = await new Tip88C1C1ResolverAssemblyTests(postgres)
            .ExecuteExistingRequestWithRealC2ProviderAsync(
                request,
                minio,
                (_, _) => Task.FromResult<IC2AssemblyPreparationProvider>(
                    new RecipientPackagePreparationProvider(
                        c2Options,
                        new RecipientPackageRepository(new PackageConnectionFactory(postgres.ConnectionString)),
                        new RawExportAssemblyRepository(new AssemblyConnectionFactory(postgres.ConnectionString)),
                        new RecipientPackageCryptoService(),
                        c2Store,
                        c2Store,
                        c2Store)));
        Assert.Equal(RawExportAssemblyExecutionOutcome.Sealed, execution.Result.Outcome);
        await workSource.RecordAsync(request, execution.Result);

        await deliveryGate.Reached.WaitAsync(TimeSpan.FromSeconds(30));
        await using var packageDb = postgres.CreateDbContext();
        var package = await packageDb.RawExportRecipientPackagePreparations.AsNoTracking()
            .SingleAsync(row => row.JobId == jobId);
        Assert.Equal(sessionId, await packageDb.RawExportJobIdentities.AsNoTracking()
            .Where(row => row.JobId == jobId)
            .Select(row => row.VerificationSessionId)
            .SingleAsync());
        Assert.Equal(jobId, package.JobId);
        Assert.Equal(recipient, package.RecipientClientApplicationId);

        using var referenceClient = app.GetTestClient();
        referenceClient.DefaultRequestHeaders.Add("X-TagEkyc-Api-Key", credential);
        using var references = await referenceClient.GetAsync(
            "/api/ekyc/raw-export/package-references?pageSize=25");
        references.EnsureSuccessStatusCode();
        var page = await references.Content.ReadFromJsonAsync<RecipientPackageReferencePageDto>();
        Assert.Contains(page!.Items, item => item.PackageId == package.PackageId);

        deliveryGate.Release();
        using var lease = await acquire.WaitAsync(TimeSpan.FromSeconds(60));
        Assert.Equal(sessionId, lease.VerificationSessionId);
        Assert.Equal(jobId, lease.JobId);
        Assert.Equal(package.PackageId, lease.PackageId);
        Assert.Equal(portrait, lease.ChipDg2Portrait.ToArray());
        Assert.Equal(selfie, lease.LiveSelfieImage.ToArray());
    }

    private async Task<string> ProvisionRecipientAsync(
        Guid recipient,
        Guid principal,
        RSA rsa,
        string recipientKeyId)
    {
        var repository = new RecipientManagementRepository(
            new ManagementConnectionFactory(postgres.ConnectionString),
            new CredentialGenerator());
        var service = new RecipientManagementApplicationService(
            repository, new RecipientPublicKeyProfileValidator());
        var actor = new AuthenticatedClientContext(
            Guid.NewGuid(), Guid.NewGuid(), "sdk-e2e-manager",
            AuthenticatedCallerCategory.OperatorAdmin,
            new HashSet<string> { RecipientManagementApplicationService.RequiredScope },
            PrincipalId: Guid.NewGuid());
        var enrolled = await service.EnrollRecipientAsync(
            actor,
            new(recipient, principal, RecipientManagementCodec.DeliveryOperatorProfile),
            $"sdk-e2e-enroll-{Guid.NewGuid():N}",
            default);
        Assert.True(enrolled.IsSuccess, enrolled.Error?.Code);
        var issued = await service.IssueCredentialAsync(
            actor,
            new(recipient, DateTimeOffset.UtcNow.AddHours(1)),
            $"sdk-e2e-credential-{Guid.NewGuid():N}",
            default);
        Assert.True(issued.IsSuccess, issued.Error?.Code);
        var spki = rsa.ExportSubjectPublicKeyInfo();
        var key = await service.EnrollKeyAsync(
            actor,
            new(
                recipient,
                recipientKeyId,
                1,
                "RSA-OAEP-256",
                Convert.ToBase64String(spki),
                Convert.ToHexString(SHA256.HashData(spki)).ToLowerInvariant(),
                DateTimeOffset.UtcNow.AddMinutes(-1),
                DateTimeOffset.UtcNow.AddHours(1)),
            $"sdk-e2e-key-{Guid.NewGuid():N}",
            default);
        Assert.True(key.IsSuccess, key.Error?.Code);
        return issued.Value!.Value.PresentedKey!;
    }

    private async Task MakeAvailableAsync(
        Tip88C1B2R2DurableCustodyEncryptionDatabaseTests.R3VerifiedSourceFixture source)
    {
        RawExportR3StageResult staged;
        await using (var stageDb = postgres.CreateDbContext())
            staged = await new RawExportR3StagingService(stageDb).StageAsync(new(
                source.ActorPrincipalId, source.AttemptId, source.ObjectCustodyId,
                source.ReservationRevision, source.EncryptionAttemptRevision,
                source.Fence, source.ObjectStateRevision));
        Assert.Equal(RawExportR3StageDisposition.Staged, staged.Disposition);
        SourceCommitResult committed;
        await using (var commitDb = postgres.CreateDbContext())
            committed = await new RawExportSourceFinalizationService(commitDb).CommitAsync(new(
                source.ActorPrincipalId, source.AttemptId, staged.ReservationRevision!.Value,
                source.EncryptionAttemptRevision, source.Fence, source.ObjectStateRevision));
        Assert.Equal(SourceCommitDisposition.Committed, committed.Disposition);
        await using var publishDb = postgres.CreateDbContext();
        var published = await new RawExportSourceFinalizationService(publishDb).PublishAsync(new(
            source.ActorPrincipalId,
            committed.SourcePublicationId!.Value,
            staged.ReservationRevision.Value,
            source.Fence));
        Assert.Equal(SourcePublishDisposition.Available, published.Disposition);
    }

    private async Task SelectAcceptanceAsync(
        Tip88C1B2R2DurableCustodyEncryptionDatabaseTests.R3VerifiedSourceFixture source)
    {
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var actor = new NpgsqlCommand(
            "SELECT pg_catalog.set_config('tagekyc.actor_principal_id',@actor,true)", connection, transaction))
        {
            actor.Parameters.AddWithValue("actor", source.ActorPrincipalId.ToString("D"));
            await actor.ExecuteNonQueryAsync();
        }
        await using (var command = new NpgsqlCommand(
            "SELECT tagekyc.raw_export_select_session_capture_acceptance(@session,@class,@acceptance)",
            connection,
            transaction))
        {
            command.Parameters.AddWithValue("session", source.VerificationSessionId);
            command.Parameters.AddWithValue("class", source.RawClass);
            command.Parameters.AddWithValue("acceptance", source.CaptureAcceptanceId);
            Assert.NotEqual(Guid.Empty, (Guid)(await command.ExecuteScalarAsync())!);
        }
        await transaction.CommitAsync();
    }

    private async Task<Guid> WaitForJobAsync(Guid sessionId, Guid recipient)
    {
        for (var attempt = 0; attempt < 400; attempt++)
        {
            await using var db = postgres.CreateDbContext();
            var job = await db.RawExportJobIdentities.AsNoTracking()
                .Where(row => row.VerificationSessionId == sessionId
                    && row.RecipientClientApplicationId == recipient)
                .Select(row => (Guid?)row.JobId)
                .SingleOrDefaultAsync();
            if (job is not null) return job.Value;
            await Task.Delay(25);
        }
        throw new TimeoutException("The public SDK did not create its raw-export job.");
    }

    private static async Task<RawExportAssemblyExecutionRequest> WaitForWorkAsync(
        DurableRawExportAssemblyWorkSource source)
    {
        for (var attempt = 0; attempt < 200; attempt++)
        {
            var request = await source.TryAcquireAsync();
            if (request is not null) return request;
            await Task.Delay(25);
        }
        throw new TimeoutException("The durable work source did not acquire the SDK-created job.");
    }

    private sealed class CredentialGenerator : IManagedCredentialMaterialGenerator
    {
        private readonly RandomManagedApiKeyGenerator generator = new();

        public ManagedCredentialMaterial Generate()
        {
            for (var attempt = 0; attempt < 32; attempt++)
            {
                var value = generator.Generate();
                if (ManagedApiKeyParser.Parse(value.PresentedKey)?.Prefix == value.Prefix)
                    return new(
                        Guid.NewGuid(),
                        value.PresentedKey,
                        value.Prefix,
                        ApiKeyHasher.Hash(Pepper, value.PresentedKey));
            }
            throw new InvalidOperationException("SDK_E2E_CREDENTIAL_GENERATION_FAILED");
        }
    }

    private sealed class FixedPrivateKeySource(byte[] privateKey) : ITagEkycRecipientPrivateKeySource
    {
        public ValueTask<TagEkycRecipientPrivateKeyLease> AcquireAsync(
            string keyId,
            int keyVersion,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new TagEkycRecipientPrivateKeyLease((byte[])privateKey.Clone()));
    }

    private sealed class ManagedAuthenticator(RecipientCredentialAuthenticationPolicy policy)
        : IApiKeyAuthenticator
    {
        public Task<SessionOperationResult<AuthenticatedClientContext>> AuthenticateAsync(
            HttpContext httpContext,
            string? requiredScope = null,
            CancellationToken cancellationToken = default) =>
            policy.AuthenticateAsync(
                httpContext.Request.Headers["X-TagEkyc-Api-Key"].ToString(),
                requiredScope,
                cancellationToken);
    }

    private sealed class NoGlobalPolicyProvider : ILocalDevClientPolicyProvider
    {
        public Task<LocalDevClientPolicy?> GetPolicyAsync(
            Guid clientApplicationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<LocalDevClientPolicy?>(null);
    }

    private sealed class DeliveryGateHandler(HttpMessageHandler inner) : DelegatingHandler(inner)
    {
        private readonly TaskCompletionSource reached = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource release = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        internal Task Reached => reached.Task;
        internal void Release() => release.TrySetResult();

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Post
                && request.RequestUri!.AbsolutePath.Contains("/packages/", StringComparison.Ordinal)
                && request.RequestUri.AbsolutePath.EndsWith("/deliveries", StringComparison.Ordinal))
            {
                reached.TrySetResult();
                await release.Task.WaitAsync(cancellationToken);
            }
            return await base.SendAsync(request, cancellationToken);
        }
    }

    private sealed class ManagementConnectionFactory(string connectionString)
        : IRecipientManagementConnectionFactory
    {
        public async Task<NpgsqlConnection> OpenAsync(CancellationToken cancellationToken)
        {
            var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await new NpgsqlCommand("SET ROLE tagekyc_raw_export_recipient_manager_login", connection)
                .ExecuteNonQueryAsync(cancellationToken);
            return connection;
        }
    }

    private sealed class PackageConnectionFactory(string connectionString)
        : IRecipientPackageConnectionFactory
    {
        public async Task<NpgsqlConnection> OpenAsync(
            RecipientPackageDatabaseCapability capability,
            CancellationToken cancellationToken)
        {
            var role = capability switch
            {
                RecipientPackageDatabaseCapability.Preparer => "tagekyc_raw_export_package_preparer",
                RecipientPackageDatabaseCapability.Reconciler => "tagekyc_raw_export_package_reconciler",
                _ => "tagekyc_raw_export_package_lifecycle",
            };
            var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await new NpgsqlCommand($"SET ROLE {role}", connection)
                .ExecuteNonQueryAsync(cancellationToken);
            return connection;
        }
    }

    private sealed class AssemblyConnectionFactory(string connectionString)
        : IRawExportAssemblyConnectionFactory
    {
        public async Task<NpgsqlConnection> OpenAsync(
            RawExportAssemblyDatabaseCapability capability,
            CancellationToken cancellationToken)
        {
            var role = capability == RawExportAssemblyDatabaseCapability.Resolver
                ? "tagekyc_raw_export_assembly_resolver"
                : "tagekyc_raw_export_assembly_sealer";
            var builder = new NpgsqlConnectionStringBuilder(connectionString)
            {
                Options = $"-c role={role}",
                Pooling = false,
            };
            var connection = new NpgsqlConnection(builder.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
    }

    private sealed class DeliveryConnectionFactory(string connectionString)
        : IRecipientPackageDeliveryConnectionFactory
    {
        public async Task<NpgsqlConnection> OpenAsync(CancellationToken cancellationToken)
        {
            var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await new NpgsqlCommand("SET ROLE tagekyc_raw_export_package_delivery", connection)
                .ExecuteNonQueryAsync(cancellationToken);
            return connection;
        }
    }
}
