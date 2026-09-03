using System.Net;
using System.Security.Cryptography;
using Amazon.S3;
using Amazon.S3.Model;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed class S3CompatibleRecipientPackageProvider :
    IRecipientPackageObjectWriter,
    IRecipientPackageObjectReader,
    IRecipientPackageObjectLifecycle,
    IRecipientPackagePostureProbe,
    IDisposable
{
    private const string BindingMetadata = "tagekyc-c2-binding-sha256";
    private const string EnvelopeMetadata = "tagekyc-c2-envelope-sha256";
    private readonly RecipientPackageOptions options;
    private readonly RecipientPackageObjectClientFactory factory;
    private readonly IAmazonS3 writer;
    private readonly IAmazonS3 reconciler;
    private readonly IAmazonS3 lifecycle;
    private readonly IAmazonS3 posture;

    internal S3CompatibleRecipientPackageProvider(
        RecipientPackageOptions options,
        RecipientPackageObjectClientFactory factory)
    {
        this.options = options;
        this.factory = factory;
        writer = factory.CreateWriter();
        reconciler = factory.CreateReconciler();
        lifecycle = factory.CreateLifecycle();
        posture = factory.CreatePostureProbe();
    }

    public async Task<RecipientPackagePutResult> PutIfAbsentAsync(
        RecipientPackageLocator locator,
        RecipientPackageEncryptedSpool spool,
        CancellationToken cancellationToken)
    {
        RequireLocator(locator);
        await using var source = spool.OpenRead();
        var request = new PutObjectRequest
        {
            BucketName = locator.BucketName,
            Key = locator.ObjectKey,
            InputStream = source,
            AutoResetStreamPosition = false,
            AutoCloseStream = false,
            IfNoneMatch = "*",
        };
        request.Headers.ContentLength = spool.Length;
        request.Metadata[BindingMetadata] = Convert.ToHexString(locator.ObjectBindingDigest).ToLowerInvariant();
        try
        {
            var response = await writer.PutObjectAsync(request, cancellationToken).ConfigureAwait(false);
            return new(RecipientPackagePutOutcome.Created, (int)response.HttpStatusCode,
                response.ETag, spool.Length);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.PreconditionFailed)
        {
            return new(RecipientPackagePutOutcome.ConditionalConflict, (int)exception.StatusCode, null, null);
        }
        catch (AmazonS3Exception exception) when ((int)exception.StatusCode >= 500)
        {
            return new(RecipientPackagePutOutcome.OutcomeUnknown, (int)exception.StatusCode, null, null);
        }
        catch (Exception exception) when (exception is AmazonS3Exception or HttpRequestException or IOException or TaskCanceledException)
        {
            return new(RecipientPackagePutOutcome.OutcomeUnknown, null, null, null);
        }
    }

    public async Task<RecipientPackageInspection> InspectAsync(
        RecipientPackageLocator locator,
        CancellationToken cancellationToken) =>
        await InspectCoreAsync(reconciler, locator, cancellationToken).ConfigureAwait(false);

    async Task<RecipientPackageInspection> IRecipientPackageObjectLifecycle.InspectAsync(
        RecipientPackageLocator locator,
        CancellationToken cancellationToken) =>
        await InspectCoreAsync(lifecycle, locator, cancellationToken).ConfigureAwait(false);

    private async Task<RecipientPackageInspection> InspectCoreAsync(
        IAmazonS3 client,
        RecipientPackageLocator locator,
        CancellationToken cancellationToken)
    {
        RequireLocator(locator);
        try
        {
            using var response = await client.GetObjectAsync(new GetObjectRequest
            {
                BucketName = locator.BucketName,
                Key = locator.ObjectKey,
            }, cancellationToken).ConfigureAwait(false);
            if (response.ContentLength is < 1 or > RecipientPackageOptions.MaximumEncryptedPackageLength)
                return new(RecipientPackageInspectionOutcome.OutcomeUnknown, response.ContentLength, null, null, response.ETag);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            byte[]? envelope = null;
            var buffer = new byte[64 * 1024];
            try
            {
                using var prefix = new MemoryStream();
                long total = 0;
                while (true)
                {
                    var read = await response.ResponseStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                    if (read == 0) break;
                    total += read;
                    if (total > RecipientPackageOptions.MaximumEncryptedPackageLength)
                        return new(RecipientPackageInspectionOutcome.OutcomeUnknown, total, null, null, response.ETag);
                    hash.AppendData(buffer, 0, read);
                    if (prefix.Length < RecipientPackageOptions.MaximumHeaderLength + 64)
                    {
                        var take = Math.Min(read, checked((int)(RecipientPackageOptions.MaximumHeaderLength + 64 - prefix.Length)));
                        prefix.Write(buffer, 0, take);
                    }
                }
                prefix.Position = 0;
                if (!RecipientPackageCodec.TryReadEnvelopeDigest(prefix, out envelope))
                    return new(RecipientPackageInspectionOutcome.OutcomeUnknown, total, null, null, response.ETag);
                return new(RecipientPackageInspectionOutcome.Present, total, hash.GetHashAndReset(), envelope, response.ETag);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(buffer);
            }
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return new(RecipientPackageInspectionOutcome.PositivelyAbsent, null, null, null, null);
        }
        catch (AmazonS3Exception exception) when ((int)exception.StatusCode >= 500)
        {
            return new(RecipientPackageInspectionOutcome.Unavailable, null, null, null, null);
        }
        catch (Exception exception) when (exception is AmazonS3Exception or HttpRequestException or IOException or TaskCanceledException)
        {
            return new(RecipientPackageInspectionOutcome.OutcomeUnknown, null, null, null, null);
        }
    }

    public async Task<Stream> OpenReadAsync(RecipientPackageLocator locator, CancellationToken cancellationToken)
        => await OpenReadCoreAsync(reconciler, locator, cancellationToken).ConfigureAwait(false);

    async Task<Stream> IRecipientPackageObjectLifecycle.OpenReadAsync(
        RecipientPackageLocator locator,
        CancellationToken cancellationToken) =>
        await OpenReadCoreAsync(lifecycle, locator, cancellationToken).ConfigureAwait(false);

    private async Task<Stream> OpenReadCoreAsync(
        IAmazonS3 client,
        RecipientPackageLocator locator,
        CancellationToken cancellationToken)
    {
        RequireLocator(locator);
        var response = await client.GetObjectAsync(new GetObjectRequest
        {
            BucketName = locator.BucketName,
            Key = locator.ObjectKey,
        }, cancellationToken).ConfigureAwait(false);
        if (response.ContentLength is < 1 or > RecipientPackageOptions.MaximumEncryptedPackageLength)
        {
            response.Dispose();
            throw new IOException("RAW_EXPORT_RECIPIENT_PACKAGE_OBJECT_LENGTH_INVALID");
        }
        return new BoundedProviderReadStream(
            response.ResponseStream, response, response.ContentLength,
            RecipientPackageOptions.MaximumEncryptedPackageLength);
    }

    public async Task<RecipientPackageDeleteOutcome> DeleteAsync(
        RecipientPackageLocator locator,
        CancellationToken cancellationToken)
    {
        RequireLocator(locator);
        try
        {
            await lifecycle.DeleteObjectAsync(new DeleteObjectRequest
            {
                BucketName = locator.BucketName,
                Key = locator.ObjectKey,
            }, cancellationToken).ConfigureAwait(false);
            return RecipientPackageDeleteOutcome.DeletedAcknowledged;
        }
        catch (AmazonS3Exception exception) when ((int)exception.StatusCode >= 500)
        {
            return RecipientPackageDeleteOutcome.Unavailable;
        }
        catch (Exception exception) when (exception is AmazonS3Exception or HttpRequestException or IOException or TaskCanceledException)
        {
            return RecipientPackageDeleteOutcome.OutcomeUnknown;
        }
    }

    public async Task<RecipientPackageBucketPosture> InspectAsync(CancellationToken cancellationToken)
    {
        var provider = Provider;
        try
        {
            var versioning = await posture.GetBucketVersioningAsync(new GetBucketVersioningRequest
            {
                BucketName = provider.BucketName,
            }, cancellationToken).ConfigureAwait(false);
            var versioningAbsent = string.Equals(
                versioning.VersioningConfig?.Status?.Value,
                VersionStatus.Off.Value,
                StringComparison.Ordinal);
            var lockAbsent = false;
            try
            {
                await posture.GetObjectLockConfigurationAsync(new GetObjectLockConfigurationRequest
                {
                    BucketName = provider.BucketName,
                }, cancellationToken).ConfigureAwait(false);
            }
            catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound
                || exception.ErrorCode is "ObjectLockConfigurationNotFoundError" or "NoSuchObjectLockConfiguration")
            {
                lockAbsent = true;
            }
            var lifecycleAbsent = false;
            try
            {
                var lifecycleResponse = await posture.GetLifecycleConfigurationAsync(new GetLifecycleConfigurationRequest
                {
                    BucketName = provider.BucketName,
                }, cancellationToken).ConfigureAwait(false);
                lifecycleAbsent = lifecycleResponse.Configuration?.Rules is null or { Count: 0 };
            }
            catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
            {
                lifecycleAbsent = true;
            }
            var publicAbsent = false;
            try
            {
                await posture.GetBucketPolicyAsync(new GetBucketPolicyRequest { BucketName = provider.BucketName }, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
            {
                publicAbsent = true;
            }
            return new(true, lockAbsent, versioningAbsent, lifecycleAbsent, publicAbsent);
        }
        catch (Exception exception) when (exception is AmazonS3Exception or HttpRequestException or IOException or TaskCanceledException)
        {
            return new(false, false, false, false, false);
        }
    }

    private RecipientPackageProviderConfiguration Provider => options.Provider
        ?? throw new InvalidOperationException("PROD_RAW_EXPORT_RECIPIENT_PACKAGE_CONFIG_INVALID");

    private void RequireLocator(RecipientPackageLocator locator)
    {
        var provider = Provider;
        if (!string.Equals(locator.ProviderKind, RecipientPackageProviderConfiguration.ProviderKind, StringComparison.Ordinal)
            || !string.Equals(locator.ProviderConfigurationId, provider.ProviderConfigurationId, StringComparison.Ordinal)
            || !string.Equals(locator.BucketName, provider.BucketName, StringComparison.Ordinal)
            || !locator.ObjectKey.StartsWith("raw-export/c2-package/v1/", StringComparison.Ordinal)
            || !CryptographicOperations.FixedTimeEquals(locator.ProviderEndpointFingerprint,
                RecipientPackageCodec.ProviderEndpointFingerprint(provider)))
            throw new InvalidOperationException("RAW_EXPORT_RECIPIENT_PACKAGE_LOCATOR_MISMATCH");
    }

    public void Dispose() => factory.Dispose();
}
