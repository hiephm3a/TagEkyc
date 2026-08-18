using Amazon.Runtime;
using Amazon.S3;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed class RecipientPackageObjectClientFactory(RecipientPackageOptions options) : IDisposable
{
    private readonly List<IAmazonS3> owned = [];

    internal IAmazonS3 CreateWriter() => Create(Provider.Writer);
    internal IAmazonS3 CreateReconciler() => Create(Provider.Reconciler);
    internal IAmazonS3 CreateLifecycle() => Create(Provider.Lifecycle);
    internal IAmazonS3 CreatePostureProbe() => Create(Provider.PostureProbe);

    private RecipientPackageProviderConfiguration Provider =>
        options.IsSyntacticallyValid
        && options.Topology == RecipientPackageTopology.S3CompatibleDurable
        && options.Provider is not null
            ? options.Provider
            : throw new InvalidOperationException("PROD_RAW_EXPORT_RECIPIENT_PACKAGE_CONFIG_INVALID");

    private IAmazonS3 Create(RecipientPackageCredential credential)
    {
        var provider = Provider;
        var config = new AmazonS3Config
        {
            ServiceURL = provider.ServiceUrl.AbsoluteUri.TrimEnd('/'),
            ForcePathStyle = provider.ForcePathStyle,
            AuthenticationRegion = provider.RegionIdentifier,
            MaxErrorRetry = 0,
            Timeout = RecipientPackageOptions.OperationTimeout,
        };
        var client = new AmazonS3Client(
            new BasicAWSCredentials(credential.AccessKeyId, credential.SecretAccessKey), config);
        owned.Add(client);
        return client;
    }

    public void Dispose()
    {
        foreach (var client in owned) client.Dispose();
        owned.Clear();
    }
}
