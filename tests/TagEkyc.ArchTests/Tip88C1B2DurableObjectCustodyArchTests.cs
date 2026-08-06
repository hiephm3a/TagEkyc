using System.Net;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TagEkyc.Infrastructure.RawExport;

namespace TagEkyc.ArchTests;

public sealed class Tip88C1B2DurableObjectCustodyArchTests
{
    [Fact]
    public void OA1_object_capability_interfaces_are_non_assignable()
    {
        var interfaces = new[]
        {
            typeof(IProvisionalObjectWriter), typeof(IProvisionalObjectReconciler),
            typeof(IProvisionalObjectLifecycle), typeof(IProvisionalObjectPostureProbe),
        };
        foreach (var left in interfaces)
        foreach (var right in interfaces.Where(x => x != left))
            Assert.False(left.IsAssignableFrom(right));
        Assert.Single(typeof(S3CompatibleProvisionalObjectWriter).GetInterfaces().Intersect(interfaces));
        Assert.Single(typeof(S3CompatibleProvisionalObjectReconciler).GetInterfaces().Intersect(interfaces));
        Assert.Single(typeof(S3CompatibleProvisionalObjectLifecycle).GetInterfaces().Intersect(interfaces));
        Assert.Single(typeof(S3CompatibleProvisionalObjectPostureProbe).GetInterfaces().Intersect(interfaces));
    }

    [Fact]
    public void OA2_runtime_graph_cannot_resolve_s3_or_foreign_object_capabilities()
    {
        var configuration = Configuration(ProvisionalObjectCapability.Writer);
        var services = new ServiceCollection().AddTagEkycProvisionalObjectCustody(configuration);
        var repository = Assert.Single(services.Where(x =>
            x.ServiceType == typeof(ProvisionalObjectCustodyRepository)));
        Assert.Equal(typeof(ProvisionalObjectCustodyRepository), repository.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, repository.Lifetime);
        var readiness = Assert.Single(services.Where(x =>
            x.ServiceType == typeof(ProvisionalObjectCustodyReadinessValidator)));
        Assert.Equal(typeof(ProvisionalObjectCustodyReadinessValidator), readiness.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, readiness.Lifetime);
        Assert.DoesNotContain(services, x => x.ServiceType == typeof(IAmazonS3));
        Assert.Contains(services, x => x.ServiceType == typeof(IProvisionalObjectWriter));
        Assert.DoesNotContain(services, x => x.ServiceType == typeof(IProvisionalObjectReconciler));
        Assert.DoesNotContain(services, x => x.ServiceType == typeof(IProvisionalObjectLifecycle));
        Assert.DoesNotContain(services, x => x.ServiceType == typeof(IProvisionalObjectPostureProbe));

        var disabled = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["TagEkyc:RawExport:ObjectCustody:Topology"] = "Disabled",
        }).Build();
        var disabledServices = new ServiceCollection().AddTagEkycProvisionalObjectCustody(disabled);
        Assert.DoesNotContain(disabledServices, x =>
            x.ServiceType == typeof(ProvisionalObjectCustodyRepository));
        Assert.DoesNotContain(disabledServices, x =>
            x.ServiceType == typeof(ProvisionalObjectCustodyReadinessValidator));

        var invalid = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["TagEkyc:RawExport:ObjectCustody:Topology"] = "S3CompatibleDurable",
            ["TagEkyc:RawExport:ObjectCustody:ServiceUrl"] = "http://127.0.0.1:19000",
            ["TagEkyc:RawExport:ObjectCustody:BucketName"] = "fixture-bucket",
            ["TagEkyc:RawExport:ObjectCustody:AccessKeyId"] = "fixture-access",
            ["TagEkyc:RawExport:ObjectCustody:SecretAccessKey"] = "fixture-secret",
            ["TagEkyc:RawExport:ObjectCustody:AllowLoopbackHttp"] = "true",
            ["TagEkyc:RawExport:ObjectCustody:MaximumSinglePartCiphertextBytes"] = "134217728",
            ["TagEkyc:RawExport:ObjectCustody:OperationTimeoutSeconds"] = "300",
        }).Build();
        var invalidServices = new ServiceCollection().AddTagEkycProvisionalObjectCustody(invalid);
        Assert.DoesNotContain(invalidServices, x =>
            x.ServiceType == typeof(ProvisionalObjectCustodyRepository));
        Assert.Single(invalidServices.Where(x =>
            x.ServiceType == typeof(ProvisionalObjectCustodyReadinessValidator)));
        Assert.DoesNotContain(invalidServices, x => x.ServiceType == typeof(IProvisionalObjectWriter)
            || x.ServiceType == typeof(IProvisionalObjectReconciler)
            || x.ServiceType == typeof(IProvisionalObjectLifecycle)
            || x.ServiceType == typeof(IProvisionalObjectPostureProbe));

        var source = File.ReadAllText(ProjectPath(
            "src/TagEkyc.Infrastructure/RawExport/ProvisionalObjectCustodyServiceCollectionExtensions.cs"));
        var repositoryRegistrations = System.Text.RegularExpressions.Regex.Matches(
            source,
            @"(?m)^[ \t]*services\.TryAddScoped<ProvisionalObjectCustodyRepository>\(\);[ \t]*\r?$");
        var readinessRegistrations = System.Text.RegularExpressions.Regex.Matches(
            source,
            @"(?m)^[ \t]*services\.TryAddScoped<ProvisionalObjectCustodyReadinessValidator>\(\);[ \t]*\r?$");
        Assert.Single(repositoryRegistrations.Cast<System.Text.RegularExpressions.Match>());
        Assert.Single(readinessRegistrations.Cast<System.Text.RegularExpressions.Match>());
        var disabledGuard = source.IndexOf(
            "&& options.Topology == ProvisionalObjectTopology.Disabled)",
            StringComparison.Ordinal);
        Assert.True(disabledGuard >= 0);
        var disabledReturn = source.IndexOf("return services;", disabledGuard, StringComparison.Ordinal);
        Assert.True(disabledReturn > disabledGuard);
        Assert.True(readinessRegistrations[0].Index > disabledReturn);
        var invalidGuard = source.IndexOf("if (!options.IsSyntacticallyValid", StringComparison.Ordinal);
        Assert.True(invalidGuard > readinessRegistrations[0].Index);
        var invalidReturn = source.IndexOf("return services;", invalidGuard, StringComparison.Ordinal);
        Assert.True(invalidReturn > invalidGuard);
        Assert.True(repositoryRegistrations[0].Index > invalidReturn);

        var program = File.ReadAllText(ProjectPath("src/TagEkyc.Api/Program.cs"));
        Assert.Contains("descriptor.ServiceType == typeof(ProvisionalObjectCustodyReadinessValidator)",
            program, StringComparison.Ordinal);
    }

    [Fact]
    public void OA3_multipart_batch_delete_version_copy_list_presign_and_transfer_utility_are_forbidden()
    {
        var source = ProviderSource();
        foreach (var token in new[]
        {
            "InitiateMultipartUpload", "UploadPart", "CompleteMultipartUpload", "AbortMultipartUpload",
            "CopyObject", "CopyPart", "DeleteObjects", "DeleteObjectsRequest", "ListObjects",
            "ListVersions", "GetPreSignedURL", "TransferUtility", "PutBucketVersioning",
            "PutObjectLockConfiguration",
        }) Assert.DoesNotContain(token, source, StringComparison.Ordinal);
        Assert.Contains("DeleteObjectAsync(new DeleteObjectRequest", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".DeleteObject(", source, StringComparison.Ordinal);
    }

    [Fact]
    public void OA4_object_provider_contract_has_no_arbitrary_bucket_or_key_input()
    {
        foreach (var contract in new[] { typeof(IProvisionalObjectWriter), typeof(IProvisionalObjectReconciler), typeof(IProvisionalObjectLifecycle) })
        foreach (var method in contract.GetMethods())
            Assert.DoesNotContain(method.GetParameters(), parameter => parameter.ParameterType == typeof(string));
    }

    [Fact]
    public void OA5_object_handles_credentials_and_receipts_are_redacted()
    {
        var options = ProvisionalObjectCustodyOptions.Resolve(Configuration(ProvisionalObjectCapability.Writer));
        var rendered = options.ToString();
        Assert.DoesNotContain("fixture-access", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("fixture-secret", rendered, StringComparison.Ordinal);
        Assert.Equal(2, rendered.Split("[REDACTED]", StringSplitOptions.None).Length - 1);
        Assert.DoesNotContain(typeof(ConditionalPutResult).GetProperties(), x => x.PropertyType == typeof(string));
    }

    [Fact]
    public async Task OA6_s3_sdk_version_and_every_single_put_request_property_are_pinned()
    {
        var project = File.ReadAllText(ProjectPath("src/TagEkyc.Infrastructure/TagEkyc.Infrastructure.csproj"));
        Assert.Contains("Include=\"AWSSDK.S3\" Version=\"3.7.405.4\"", project, StringComparison.Ordinal);
        using var client = new CapturingAmazonS3Client();
        using var writer = new S3CompatibleProvisionalObjectWriter(
            ProvisionalObjectCustodyOptions.Resolve(Configuration(ProvisionalObjectCapability.Writer)),
            client);
        var identity = Guid.NewGuid();
        var locator = new ExactObjectLocator(
            identity,
            $"raw-export/c1/v1/{identity:N}",
            Enumerable.Range(0, 32).Select(value => (byte)value).ToArray());
        var bytes = "captured-single-part-ciphertext"u8.ToArray();
        await using var callerStream = new MemoryStream(bytes, writable: false);

        var result = await writer.PutIfAbsentAsync(
            new ExactWriteRequest(locator, Guid.NewGuid(), bytes.Length),
            callerStream,
            CancellationToken.None);

        Assert.Equal(ConditionalPutOutcome.Created, result.Outcome);
        Assert.Equal(1, client.PutCount);
        var request = Assert.IsType<PutObjectRequest>(client.Request);
        Assert.IsType<HashingNonSeekableReadStream>(client.CapturedInputStream);
        Assert.Equal(bytes, client.CapturedBytes);
        Assert.False(client.InputCanSeek);
        Assert.Equal(bytes.Length, request.Headers.ContentLength);
        Assert.False(request.AutoResetStreamPosition);
        Assert.False(request.AutoCloseStream);
        Assert.Equal("*", request.IfNoneMatch);
        Assert.True(string.IsNullOrEmpty(request.FilePath));
        Assert.True(string.IsNullOrEmpty(request.ContentBody));
        Assert.Equal(0, writer.MaximumErrorRetry);
        Assert.True(callerStream.CanRead);
    }

    private static IConfiguration Configuration(ProvisionalObjectCapability capability) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["TagEkyc:RawExport:ObjectCustody:Topology"] = "S3CompatibleDurable",
            ["TagEkyc:RawExport:ObjectCustody:Capability"] = capability.ToString(),
            ["TagEkyc:RawExport:ObjectCustody:ServiceUrl"] = "http://127.0.0.1:19000",
            ["TagEkyc:RawExport:ObjectCustody:BucketName"] = "fixture-bucket",
            ["TagEkyc:RawExport:ObjectCustody:AccessKeyId"] = "fixture-access",
            ["TagEkyc:RawExport:ObjectCustody:SecretAccessKey"] = "fixture-secret",
            ["TagEkyc:RawExport:ObjectCustody:AllowLoopbackHttp"] = "true",
            ["TagEkyc:RawExport:ObjectCustody:MaximumSinglePartCiphertextBytes"] = "134217728",
            ["TagEkyc:RawExport:ObjectCustody:OperationTimeoutSeconds"] = "300",
        }).Build();

    private static string ProviderSource() => File.ReadAllText(ProjectPath("src/TagEkyc.Infrastructure/RawExport/S3CompatibleProvisionalObjectProvider.cs"));
    private static int Count(string value, string needle) => (value.Length - value.Replace(needle, string.Empty, StringComparison.Ordinal).Length) / needle.Length;
    private static string ProjectPath(string relative) => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../..", relative));

    private sealed class CapturingAmazonS3Client() : AmazonS3Client(
        new BasicAWSCredentials("fixture-access", "fixture-secret"),
        new AmazonS3Config
        {
            ServiceURL = "http://127.0.0.1:19000",
            ForcePathStyle = true,
            MaxErrorRetry = 0,
        })
    {
        internal int PutCount { get; private set; }
        internal PutObjectRequest? Request { get; private set; }
        internal Stream? CapturedInputStream { get; private set; }
        internal byte[] CapturedBytes { get; private set; } = [];
        internal bool InputCanSeek { get; private set; }

        public override async Task<PutObjectResponse> PutObjectAsync(
            PutObjectRequest request,
            CancellationToken cancellationToken)
        {
            PutCount++;
            Request = request;
            CapturedInputStream = request.InputStream;
            InputCanSeek = request.InputStream.CanSeek;
            await using var copy = new MemoryStream();
            await request.InputStream.CopyToAsync(copy, cancellationToken);
            CapturedBytes = copy.ToArray();
            return new PutObjectResponse { HttpStatusCode = HttpStatusCode.OK };
        }
    }
}
