using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;
using TagEkyc.Api;
using TagEkyc.Application.CaptureRuntime;
using TagEkyc.Application.Ports;
using TagEkyc.Application.RawExport;
using TagEkyc.Infrastructure.RawExport;
using TagEkyc.Infrastructure.Signing;

namespace TagEkyc.IntegrationTests;

public sealed class RawExportAssemblyReconcilerCompositionTests
{
    [Fact]
    public async Task Production_DurableWorker_registers_exact_role_scoped_reconciler_seam_without_root_reconciler()
    {
        using var secrets = new SyntheticOwnerSecrets();
        var configuration = secrets.Configuration;
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);

        // Preserve Program.cs ordering: assembly is registered before the
        // capture-runtime graph that owns the three custody role roots.
        services.AddTagEkycRawExportAssembly(configuration, isProduction: true);
        services.AddTagEkycCaptureRuntimeRawIngress(configuration, isProduction: true);

        var seam = Assert.Single(services.Where(descriptor =>
            descriptor.ServiceType == typeof(IRawExportAssemblyReconcilerScopeFactory)));
        Assert.Equal(ServiceLifetime.Singleton, seam.Lifetime);
        Assert.Equal(typeof(CaptureRuntimeAssemblyReconcilerScopeFactory),
            seam.ImplementationType);
        Assert.DoesNotContain(services, descriptor =>
            descriptor.ServiceType == typeof(IProvisionalObjectReconciler));
        Assert.DoesNotContain(services, descriptor =>
            descriptor.ImplementationType == typeof(FixtureAssemblyReconcilerScopeFactory));

        await using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateScopes = true,
            });
        Assert.IsType<CaptureRuntimeAssemblyReconcilerScopeFactory>(
            provider.GetRequiredService<IRawExportAssemblyReconcilerScopeFactory>());
    }

    [Fact]
    public void Real_Program_Production_graph_validates_DurableWorker_reconciler_edge()
    {
        using var secrets = new SyntheticOwnerSecrets(fullProgram: true);
        using var factory = new ExactProductionProgramCompositionFactory(secrets);

        var provider = factory.Services;
        var registrations = Assert.IsAssignableFrom<IReadOnlyList<ServiceDescriptor>>(
            factory.ProgramRegistrations);
        var seam = Assert.Single(registrations.Where(descriptor =>
            descriptor.ServiceType == typeof(IRawExportAssemblyReconcilerScopeFactory)));
        Assert.Equal(ServiceLifetime.Singleton, seam.Lifetime);
        Assert.Equal(typeof(CaptureRuntimeAssemblyReconcilerScopeFactory),
            seam.ImplementationType);
        Assert.Empty(registrations.Where(descriptor =>
            descriptor.ServiceType == typeof(IProvisionalObjectReconciler)));
        Assert.DoesNotContain(registrations, descriptor =>
            descriptor.ImplementationType == typeof(FixtureAssemblyReconcilerScopeFactory));
        Assert.Contains(registrations, descriptor =>
            descriptor.ServiceType == typeof(RawExportAssemblySourceResolver)
            && descriptor.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(registrations, descriptor =>
            descriptor.ServiceType == typeof(CaptureRuntimeCustodyProviderScopes)
            && descriptor.Lifetime == ServiceLifetime.Singleton);

        using var scope = provider.CreateScope();
        Assert.NotNull(scope.ServiceProvider
            .GetRequiredService<RawExportAssemblySourceResolver>());
    }

    [Fact]
    public void Production_DurableWorker_rejects_unknown_reconciler_scope_registration()
    {
        using var secrets = new SyntheticOwnerSecrets();
        var services = new ServiceCollection();
        services.AddSingleton<IRawExportAssemblyReconcilerScopeFactory,
            UnknownReconcilerScopeFactory>();

        var failure = Assert.Throws<InvalidOperationException>(() =>
            services.AddTagEkycRawExportAssembly(
                secrets.Configuration, isProduction: true));

        Assert.Equal("PROD_RAW_EXPORT_ASSEMBLY_RECONCILER_SCOPE_INVALID",
            failure.Message);
    }

    [Fact]
    public void NonProduction_FixtureProof_uses_fixture_scope_adapter_only()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{RawExportAssemblyOptions.SectionName}:Topology"] = "FixtureProof",
            })
            .Build();
        var services = new ServiceCollection();

        services.AddTagEkycRawExportAssembly(configuration, isProduction: false);

        var seam = Assert.Single(services.Where(descriptor =>
            descriptor.ServiceType == typeof(IRawExportAssemblyReconcilerScopeFactory)));
        Assert.Equal(ServiceLifetime.Scoped, seam.Lifetime);
        Assert.Equal(typeof(FixtureAssemblyReconcilerScopeFactory),
            seam.ImplementationType);
        Assert.DoesNotContain(services, descriptor =>
            descriptor.ImplementationType ==
            typeof(CaptureRuntimeAssemblyReconcilerScopeFactory));
    }

    private sealed class UnknownReconcilerScopeFactory
        : IRawExportAssemblyReconcilerScopeFactory
    {
        public ValueTask<IRawExportAssemblyReconcilerScope> OpenAsync(
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class ExactProductionProgramCompositionFactory(
        SyntheticOwnerSecrets secrets) : WebApplicationFactory<Program>
    {
        internal IReadOnlyList<ServiceDescriptor>? ProgramRegistrations { get; private set; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("environment", "Production");
            builder.UseDefaultServiceProvider(options =>
            {
                options.ValidateOnBuild = true;
                options.ValidateScopes = true;
            });
            foreach (var setting in secrets.Configuration.AsEnumerable())
            {
                if (setting.Value is not null)
                    builder.UseSetting(setting.Key, setting.Value);
            }
            builder.ConfigureServices(services =>
                ProgramRegistrations = services.ToArray());
            builder.ConfigureTestServices(services =>
            {
                // The exact Program registrations above remain intact. Only
                // the post-build startup selector is made deterministic so
                // this gate measures container composition, not a live DB.
                services.RemoveAll<ICaptureRuntimeStartupDependencyReader>();
                services.RemoveAll<ICaptureRuntimeVerifierPepperSource>();
                services.RemoveAll<IEvidenceSigner>();
                services.RemoveAll<IEs256PublicJwkSource>();
                services.RemoveAll<IHostedService>();
                services.AddSingleton<ICaptureRuntimeStartupDependencyReader,
                    PreparedStartupReader>();
                services.AddSingleton<ICaptureRuntimeVerifierPepperSource,
                    PreparedStartupPeppers>();
                services.AddSingleton<LocalDevEs256JwsEvidenceSigner>();
                services.AddSingleton<IEvidenceSigner>(sp =>
                    sp.GetRequiredService<LocalDevEs256JwsEvidenceSigner>());
                services.AddSingleton<IEs256PublicJwkSource>(sp =>
                    sp.GetRequiredService<LocalDevEs256JwsEvidenceSigner>());
            });
        }
    }

    private sealed class PreparedStartupReader : ICaptureRuntimeStartupDependencyReader
    {
        public Task<CaptureRuntimeStartupDependencies> ReadAsync(
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            Task.FromResult(new CaptureRuntimeStartupDependencies(
                "Managed",
                "Prepared",
                1,
                now.AddMinutes(-1),
                null,
                null,
                []));
    }

    private sealed class PreparedStartupPeppers : ICaptureRuntimeVerifierPepperSource
    {
        public int CurrentVersion => 1;

        public ValueTask<ICaptureRuntimeVerifierPepperLease?> TryResolveAsync(
            int version,
            CaptureRuntimeVerifierPepperDomain domain,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<ICaptureRuntimeVerifierPepperLease?>(
                version == 1 ? new Lease(domain) : null);

        private sealed class Lease(CaptureRuntimeVerifierPepperDomain domain)
            : ICaptureRuntimeVerifierPepperLease
        {
            private readonly byte[] key = new byte[32];
            public int Version => 1;
            public CaptureRuntimeVerifierPepperDomain Domain => domain;
            public ReadOnlyMemory<byte> Key => key;
            public void Dispose() => CryptographicOperations.ZeroMemory(key);
        }
    }
}

internal sealed class TestRawExportAssemblyReconcilerScopeFactory(
    IProvisionalObjectReconciler reconciler,
    IList<string>? events = null)
    : IRawExportAssemblyReconcilerScopeFactory, IProvisionalObjectReconciler
{
    public Task<ExactObjectInspection> InspectExactAsync(
        ExactObjectLocator locator,
        CancellationToken cancellationToken) =>
        reconciler.InspectExactAsync(locator, cancellationToken);

    public Task<ExactObjectRead> OpenExactReadAsync(
        ExactObjectLocator locator,
        CancellationToken cancellationToken) =>
        reconciler.OpenExactReadAsync(locator, cancellationToken);

    public ValueTask<IRawExportAssemblyReconcilerScope> OpenAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (events is null)
        {
            return ValueTask.FromResult<IRawExportAssemblyReconcilerScope>(
                new PassthroughScope(reconciler));
        }
        var state = new ScopeState(events);
        state.Record("RoleScope opened");
        return ValueTask.FromResult<IRawExportAssemblyReconcilerScope>(
            new Scope(new TrackingReconciler(reconciler, state), state));
    }

    private sealed class PassthroughScope(IProvisionalObjectReconciler reconciler)
        : IRawExportAssemblyReconcilerScope
    {
        public IProvisionalObjectReconciler Reconciler => reconciler;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class Scope(
        IProvisionalObjectReconciler reconciler,
        ScopeState state) : IRawExportAssemblyReconcilerScope
    {
        public IProvisionalObjectReconciler Reconciler => reconciler;

        public ValueTask DisposeAsync()
        {
            state.AssertExactReadsDisposed();
            state.Record("RoleScope disposed");
            state.ScopeDisposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class TrackingReconciler(
        IProvisionalObjectReconciler inner,
        ScopeState state) : IProvisionalObjectReconciler
    {
        public Task<ExactObjectInspection> InspectExactAsync(
            ExactObjectLocator locator,
            CancellationToken cancellationToken) =>
            inner.InspectExactAsync(locator, cancellationToken);

        public async Task<ExactObjectRead> OpenExactReadAsync(
            ExactObjectLocator locator,
            CancellationToken cancellationToken)
        {
            state.AssertScopeAlive();
            var exact = await inner.OpenExactReadAsync(locator, cancellationToken);
            state.ExactReads++;
            state.Record("ExactObjectRead opened");
            return new ExactObjectRead(
                new ScopeBoundStream(exact.Ciphertext, state),
                exact.CiphertextLength);
        }
    }

    private sealed class ScopeBoundStream(Stream inner, ScopeState state) : Stream
    {
        private int disposed;

        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => inner.CanSeek;
        public override bool CanWrite => false;
        public override long Length => inner.Length;
        public override long Position
        {
            get => inner.Position;
            set => inner.Position = value;
        }

        public override void Flush() => inner.Flush();
        public override Task FlushAsync(CancellationToken cancellationToken) =>
            inner.FlushAsync(cancellationToken);
        public override int Read(byte[] buffer, int offset, int count)
        {
            state.AssertScopeAlive();
            return inner.Read(buffer, offset, count);
        }
        public override int Read(Span<byte> buffer)
        {
            state.AssertScopeAlive();
            return inner.Read(buffer);
        }
        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            state.AssertScopeAlive();
            return inner.ReadAsync(buffer, cancellationToken);
        }
        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            state.AssertScopeAlive();
            return inner.ReadAsync(buffer, offset, count, cancellationToken);
        }
        public override long Seek(long offset, SeekOrigin origin) =>
            inner.Seek(offset, origin);
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0)
            {
                if (disposing) inner.Dispose();
                state.ExactReads--;
                state.Record("ExactObjectRead disposed");
            }
            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0)
            {
                await inner.DisposeAsync();
                state.ExactReads--;
                state.Record("ExactObjectRead disposed");
            }
            GC.SuppressFinalize(this);
        }
    }

    private sealed class ScopeState(IList<string>? events)
    {
        internal bool ScopeDisposed { get; set; }
        internal int ExactReads { get; set; }

        internal void AssertScopeAlive()
        {
            if (ScopeDisposed)
                throw new InvalidOperationException(
                    "ASSEMBLY_RECONCILER_SCOPE_DISPOSED_BEFORE_EXACT_READ_COMPLETED");
        }

        internal void AssertExactReadsDisposed()
        {
            if (ExactReads != 0)
                throw new InvalidOperationException(
                    "ASSEMBLY_EXACT_READ_OUTLIVED_RECONCILER_SCOPE");
        }

        internal void Record(string value) => events?.Add(value);
    }
}

internal sealed class SyntheticOwnerSecrets : IDisposable
{
    private readonly List<string> names = [];
    private readonly string? caPath;

    internal SyntheticOwnerSecrets(bool fullProgram = false)
    {
        var assemblyResolver = Secret(Connection(RawExportAssemblyRuntimeOptions.ResolverLogin));
        var assemblySealer = Secret(Connection(RawExportAssemblyRuntimeOptions.SealerLogin));
        var writer = Secret(Connection(CaptureRuntimeCustodyProviderScopes.WriterLogin));
        var reconciler = Secret(Connection(CaptureRuntimeCustodyProviderScopes.ReconcilerLogin));
        var lifecycle = Secret(Connection(CaptureRuntimeCustodyProviderScopes.LifecycleLogin));
        var assemblyKey = Secret(Convert.ToBase64String(new byte[32]));

        Configuration = new ConfigurationManager
        {
            [$"{RawExportAssemblyOptions.SectionName}:Topology"] = "DurableWorker",
            [$"{RawExportAssemblyOptions.SectionName}:PollIntervalMilliseconds"] = "100",
            [$"{RawExportAssemblyRuntimeOptions.SectionPath}:ResolverConnectionStringSecretRef"] = $"env:{assemblyResolver}",
            [$"{RawExportAssemblyRuntimeOptions.SectionPath}:SealerConnectionStringSecretRef"] = $"env:{assemblySealer}",
            [$"{RawExportAssemblyRuntimeOptions.SectionPath}:Authentication:KeyId"] = "assembly-test-key",
            [$"{RawExportAssemblyRuntimeOptions.SectionPath}:Authentication:KeyVersion"] = "1",
            [$"{RawExportAssemblyRuntimeOptions.SectionPath}:Authentication:KeySecretRef"] = $"env:{assemblyKey}",
            [$"{RawIngressBrokerOptions.SectionName}:BaseUri"] = "http://127.0.0.1:45679/",
            [$"{RawIngressBrokerOptions.SectionName}:ListenAddress"] = "127.0.0.1",
            [$"{RawIngressBrokerOptions.SectionName}:AllowedApiAddresses:0"] = "127.0.0.1",
            [$"{RawIngressBrokerOptions.SectionName}:RequestTimeoutMilliseconds"] = "1000",
            [$"{RawIngressBrokerOptions.SectionName}:EvaluationTokenTtlSeconds"] = "10",
            [$"{RawIngressBrokerOptions.SectionName}:IdempotencyLockTimeoutMilliseconds"] = "100",
            [$"{RawIngressBrokerOptions.SectionName}:EvaluationOwnerId"] = "8bf2e6a5425a42689647465139fc46b0",
            [$"{RawIngressBrokerOptions.SectionName}:CommitmentSelectorId"] = "content-test",
            [$"{RawIngressBrokerOptions.SectionName}:CommitmentSelectorVersion"] = "1",
            [$"{RawIngressBrokerOptions.SectionName}:SubjectTokenSelectorId"] = "subject-test",
            [$"{RawIngressBrokerOptions.SectionName}:SubjectTokenSelectorVersion"] = "1",
            [$"{RawIngressBrokerOptions.SectionName}:ApiReplicaCount"] = "1",
            [$"{RawIngressBrokerOptions.SectionName}:ContinuationPollIntervalMilliseconds"] = "100",
            ["TagEkyc:RawExport:RawExportCustodyMaximumPlaintextWindowBytesPerStream"] = "1048576",
        };
        AddOwner("Writer", ProvisionalObjectCapability.Writer, writer, "writer-access");
        AddOwner("Reconciler", ProvisionalObjectCapability.Reconciler, reconciler, "reconciler-access");
        AddOwner("Lifecycle", ProvisionalObjectCapability.Lifecycle, lifecycle, "lifecycle-access");
        if (fullProgram)
        {
            caPath = CreateCaCertificate();
            AddFullProgramSettings(caPath);
        }
    }

    internal ConfigurationManager Configuration { get; }

    private void AddOwner(
        string name,
        ProvisionalObjectCapability capability,
        string secretName,
        string accessKey)
    {
        var prefix = $"{CaptureRuntimeRawIngressProductionOptions.SectionPath}:{name}:";
        Configuration[prefix + "DatabaseConnectionStringSecretRef"] = $"env:{secretName}";
        prefix += "ObjectCustody:";
        Configuration[prefix + "Topology"] = "S3CompatibleDurable";
        Configuration[prefix + "Capability"] = capability.ToString();
        Configuration[prefix + "ServiceUrl"] = "http://127.0.0.1:9000/";
        Configuration[prefix + "BucketName"] = "tagekyc-raw-source";
        Configuration[prefix + "AccessKeyId"] = accessKey;
        Configuration[prefix + "SecretAccessKey"] = "synthetic-secret";
        Configuration[prefix + "AllowLoopbackHttp"] = "true";
        Configuration[prefix + "MaximumSinglePartCiphertextBytes"] = "134217728";
        Configuration[prefix + "OperationTimeoutSeconds"] = "300";
    }

    private string Secret(string value)
    {
        var name = $"TAGEKYC_ASSEMBLY_SCOPE_{Guid.NewGuid():N}";
        names.Add(name);
        Environment.SetEnvironmentVariable(name, value);
        return name;
    }

    private void AddFullProgramSettings(string certificatePath)
    {
        var application = Secret(Connection("tagekyc_application_persistence_login"));
        var online = Secret(Connection("tagekyc_capture_runtime_online_login"));
        var @operator = Secret(Connection("tagekyc_capture_runtime_operator_login"));
        var pepper = Secret(Convert.ToBase64String(new byte[32]));
        var verifierPepper = Secret(Convert.ToBase64String(new byte[32]));
        var roleId = Secret(Guid.NewGuid().ToString("D"));
        var secretId = Secret(Guid.NewGuid().ToString("D"));
        var contentRole = Secret(Guid.NewGuid().ToString("D"));
        var contentSecret = Secret(Guid.NewGuid().ToString("D"));
        var subjectRole = Secret(Guid.NewGuid().ToString("D"));
        var subjectSecret = Secret(Guid.NewGuid().ToString("D"));
        var packagePreparer = Secret(Connection(RecipientPackageDatabaseOptions.PreparerLogin));
        var packageReconciler = Secret(Connection(RecipientPackageDatabaseOptions.ReconcilerLogin));
        var packageLifecycle = Secret(Connection(RecipientPackageDatabaseOptions.LifecycleLogin));

        Configuration["TagEkyc:Persistence:Provider"] = "Postgres";
        Configuration["TagEkyc:Persistence:ConnectionStringSecretRef"] = $"env:{application}";
        Configuration["TagEkyc:ApiKeyStore:Backend"] = "Postgres";
        Configuration["TagEkyc:ApiKeyStore:PepperSecretRef"] = $"env:{pepper}";
        Configuration["TagEkyc:CaptureRuntimeDatabase:OnlineConnectionStringSecretRef"] = $"env:{online}";
        Configuration["TagEkyc:CaptureRuntimeDatabase:OperatorConnectionStringSecretRef"] = $"env:{@operator}";
        Configuration["TagEkyc:CaptureRuntimeVerifierPeppers:CurrentVersion"] = "1";
        Configuration["TagEkyc:CaptureRuntimeVerifierPeppers:Versions:0:Version"] = "1";
        Configuration["TagEkyc:CaptureRuntimeVerifierPeppers:Versions:0:SecretRef"] = $"env:{verifierPepper}";
        Configuration["TagEkyc:EvidenceSigning:Backend"] = "Pkcs11";
        Configuration["TagEkyc:Retention:RegulatedEvidenceRetentionDays"] = "30";
        Configuration["TagEkyc:DecisionThresholds:FaceMatch"] = "0.80";
        Configuration["TagEkyc:DecisionThresholds:Liveness"] = "0.80";
        Configuration[RawExportCustodyProfileState.ConfigurationPath] = "OpenBao";
        Configuration["TagEkyc:RawExport:CustodyProfile:StorageProfileId"] = "test-storage";
        Configuration["TagEkyc:RawExport:CustodyProfile:SourceEncryptionProfileId"] = "test-encryption";
        Configuration["TagEkyc:RawExport:CustodyProfile:SourceEncryptionProfileVersion"] = "1";
        Configuration["TagEkyc:RawExport:CustodyProfile:EncryptionSuiteId"] = "fixture-aead-aes256gcm-v1";
        Configuration["TagEkyc:RawExport:CustodyProfile:EncryptionFramingVersion"] = "1";
        Configuration["TagEkyc:RawExport:CustodyProfile:NonceStrategyId"] = "fixture-nonce-random96-v1";
        Configuration["TagEkyc:RawExport:CustodyProfile:ChunkSize"] = "1048576";
        Configuration["TagEkyc:RawExport:AttemptKey:Topology"] = "DurableKey";
        Configuration["TagEkyc:RawExport:AttemptKey:CsprngExpectedOwner"] = "tagekyc_admin";
        Configuration["TagEkyc:RawExport:AttemptKey:OpenBao:Address"] = "https://127.0.0.1:8200";
        Configuration["TagEkyc:RawExport:AttemptKey:OpenBao:RoleIdSecretRef"] = $"env:{roleId}";
        Configuration["TagEkyc:RawExport:AttemptKey:OpenBao:SecretIdSecretRef"] = $"env:{secretId}";
        Configuration["TagEkyc:RawExport:AttemptKey:OpenBao:CaCertificatePath"] = certificatePath;
        Configuration["TagEkyc:RawExport:AttemptKey:OpenBao:TransitMount"] = "transit";
        Configuration["TagEkyc:RawExport:AttemptKey:OpenBao:KeyName"] = "raw-export";
        Configuration["TagEkyc:RawExport:AttemptKey:OpenBao:KeyVersion"] = "1";
        Configuration["TagEkyc:RawExport:AttemptKey:OpenBao:KeyFingerprint"] = new string('a', 64);
        Configuration["TagEkyc:RawExport:AttemptKey:OpenBao:RequestTimeoutSeconds"] = "5";
        AddClaimProvider("ContentCommitment", contentRole, contentSecret,
            "content-test", "content-hmac", certificatePath);
        AddClaimProvider("SubjectRefToken", subjectRole, subjectSecret,
            "subject-test", "subject-hmac", certificatePath);
        AddRecipientPackage(packagePreparer, packageReconciler, packageLifecycle);
        Configuration["TagEkyc:RawExport:ObjectCustody:Topology"] = "Disabled";
        Configuration["TagEkyc:RawExport:RawExportCustodyMaximumConcurrentStreamsPerProducer"] = "2";
        Configuration["TagEkyc:RawExport:RawExportCustodyMaximumConcurrentStreamsPerDeployment"] = "4";
        Configuration["TagEkyc:RawExport:RawExportCustodyMaximumAggregatePlaintextWindowBytesPerDeployment"] = "4194304";
        Configuration[CustodyTimeBoundsState.SafetyMarginKey] = "2000";
        Configuration[CustodyTimeBoundsState.MaxRemainingContinuationWindowKey] = "1800";
        Configuration[CustodyTimeBoundsState.EncryptionAttemptDeadlineKey] = "900";
        Configuration[CustodyTimeBoundsState.OwnershipLeaseDurationKey] = "300";
    }

    private void AddRecipientPackage(
        string preparer,
        string reconciler,
        string lifecycle)
    {
        var root = RecipientPackageOptions.SectionPath + ":";
        Configuration[root + "Topology"] = "S3CompatibleDurable";
        Configuration[root + "ProviderConfigurationId"] = "lab-minio";
        Configuration[root + "Providers:lab-minio:ServiceUrl"] = "http://127.0.0.1:9000/";
        Configuration[root + "Providers:lab-minio:BucketName"] = "tagekyc-recipient-package";
        Configuration[root + "Providers:lab-minio:ForcePathStyle"] = "true";
        Configuration[root + "Providers:lab-minio:RegionIdentifier"] = "lab-1";
        Configuration[root + "Providers:lab-minio:AllowLoopbackHttp"] = "true";
        var capabilities = new[] { "Writer", "Reconciler", "Lifecycle", "PostureProbe" };
        for (var index = 0; index < capabilities.Length; index++)
        {
            var capability = capabilities[index];
            Configuration[$"{root}Providers:lab-minio:{capability}:AccessKeyId"] =
                $"package-{capability.ToLowerInvariant()}";
            Configuration[$"{root}Providers:lab-minio:{capability}:SecretAccessKey"] =
                $"synthetic-secret-{index}";
        }

        var database = RecipientPackageDatabaseOptions.SectionPath + ":";
        Configuration[database + "PreparerConnectionStringSecretRef"] = $"env:{preparer}";
        Configuration[database + "ReconcilerConnectionStringSecretRef"] = $"env:{reconciler}";
        Configuration[database + "LifecycleConnectionStringSecretRef"] = $"env:{lifecycle}";
    }

    private void AddClaimProvider(
        string arm,
        string role,
        string secret,
        string keyId,
        string transitKey,
        string certificatePath)
    {
        var root = $"TagEkyc:RawExport:ClaimProviders:{arm}:";
        Configuration[root + "Address"] = "https://127.0.0.1:8200";
        Configuration[root + "RoleIdSecretRef"] = $"env:{role}";
        Configuration[root + "SecretIdSecretRef"] = $"env:{secret}";
        Configuration[root + "CaCertificatePath"] = certificatePath;
        Configuration[root + "TransitMount"] = "transit";
        Configuration[root + "RequestTimeoutSeconds"] = "5";
        Configuration[root + "Keys:0:KeyId"] = keyId;
        Configuration[root + "Keys:0:KeyVersion"] = "1";
        Configuration[root + "Keys:0:TransitKeyName"] = transitKey;
        Configuration[root + "Keys:0:NotBeforeUtc"] = "2020-01-01T00:00:00Z";
        Configuration[root + "Keys:0:NotAfterUtc"] = "2100-01-01T00:00:00Z";
    }

    private static string CreateCaCertificate()
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=tagekyc-composition-test-ca",
            key,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(true, false, 0, true));
        using var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow.AddDays(1));
        var path = Path.Combine(Path.GetTempPath(),
            $"tagekyc-composition-ca-{Guid.NewGuid():N}.pem");
        File.WriteAllText(path, certificate.ExportCertificatePem());
        return path;
    }

    private static string Connection(string username) =>
        new NpgsqlConnectionStringBuilder
        {
            Host = "127.0.0.1",
            Port = 1,
            Database = "unused",
            Username = username,
            Password = "synthetic-only",
            Pooling = false,
            Timeout = 1,
        }.ConnectionString;

    public void Dispose()
    {
        foreach (var name in names)
            Environment.SetEnvironmentVariable(name, null);
        if (caPath is not null)
            File.Delete(caPath);
    }
}
