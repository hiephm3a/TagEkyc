using System.Net;
using Amazon.S3;
using Amazon.S3.Model;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed class S3CompatibleRecipientPackageDeliveryReader(
    RecipientPackageDeliveryOptions options,
    RecipientPackageDeliveryObjectClientFactory factory) : IRecipientPackageDeliveryReader, IDisposable
{
    private readonly IAmazonS3 client = factory.Create();

    public async Task<RecipientPackageDeliveryReadResult> OpenExactAsync(
        RecipientPackageDeliveryLocator locator,
        CancellationToken cancellationToken)
    {
        if (options.Provider is null
            || !string.Equals(locator.ProviderConfigurationId, options.Provider.ProviderConfigurationId, StringComparison.Ordinal)
            || !string.Equals(locator.BucketName, options.Provider.BucketName, StringComparison.Ordinal)
            || !locator.ObjectKey.StartsWith("raw-export/c2-package/v1/", StringComparison.Ordinal))
            return new(RecipientPackageDeliveryReadOutcome.Unavailable, null);
        try
        {
            var response = await client.GetObjectAsync(new GetObjectRequest
            {
                BucketName = locator.BucketName,
                Key = locator.ObjectKey,
            }, cancellationToken).ConfigureAwait(false);
            return new(RecipientPackageDeliveryReadOutcome.Opened,
                new OwnedResponseStream(response.ResponseStream, response));
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return new(string.Equals(exception.ErrorCode, "NoSuchBucket", StringComparison.Ordinal)
                ? RecipientPackageDeliveryReadOutcome.BucketUnavailable
                : RecipientPackageDeliveryReadOutcome.PositivelyAbsent, null);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.Forbidden)
        {
            return new(RecipientPackageDeliveryReadOutcome.Forbidden, null);
        }
        catch (Exception exception) when (exception is AmazonS3Exception or HttpRequestException or IOException or TaskCanceledException)
        {
            return new(RecipientPackageDeliveryReadOutcome.Unavailable, null);
        }
    }

    public void Dispose() => client.Dispose();

    private sealed class OwnedResponseStream(Stream inner, IDisposable response) : Stream
    {
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => inner.Length;
        public override long Position { get => inner.Position; set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => inner.ReadAsync(buffer, cancellationToken);
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing) { if (disposing) { inner.Dispose(); response.Dispose(); } base.Dispose(disposing); }
        public override async ValueTask DisposeAsync() { await inner.DisposeAsync().ConfigureAwait(false); response.Dispose(); GC.SuppressFinalize(this); }
    }
}
