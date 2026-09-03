using Amazon.Runtime;
using Amazon.S3;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed class RecipientPackageDeliveryObjectClientFactory(RecipientPackageDeliveryOptions options) : IDisposable
{
    private IAmazonS3? client;

    internal IAmazonS3 Create()
    {
        if (!options.IsSyntacticallyValid || options.Provider is null || options.DeliveryReader is null)
            throw new InvalidOperationException("PROD_RAW_EXPORT_PACKAGE_DELIVERY_CONFIG_INVALID");
        var provider = options.Provider;
        var config = new AmazonS3Config
        {
            ServiceURL = provider.ServiceUrl.AbsoluteUri.TrimEnd('/'),
            ForcePathStyle = provider.ForcePathStyle,
            AuthenticationRegion = provider.RegionIdentifier,
            MaxErrorRetry = 0,
            Timeout = RecipientPackageDeliveryOptions.StreamOperationLimit,
        };
        client = new AmazonS3Client(
            new BasicAWSCredentials(options.DeliveryReader.AccessKeyId, options.DeliveryReader.SecretAccessKey),
            config);
        return client;
    }

    public void Dispose()
    {
        client?.Dispose();
        client = null;
    }
}
