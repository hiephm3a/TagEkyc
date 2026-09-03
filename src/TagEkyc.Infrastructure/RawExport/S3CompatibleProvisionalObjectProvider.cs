using System.Buffers.Binary;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace TagEkyc.Infrastructure.RawExport;

internal abstract class S3CompatibleProvisionalObjectProviderBase : IDisposable
{
    protected const string BindingMetadataKey = "tagekyc-binding-sha256";
    protected const string PutOperationMetadataKey = "tagekyc-put-operation-sha256";
    protected readonly ProvisionalObjectCustodyOptions Options;
    protected readonly IAmazonS3 Client;
    private readonly bool ownsClient;
    internal int MaximumErrorRetry { get; }

    protected S3CompatibleProvisionalObjectProviderBase(ProvisionalObjectCustodyOptions options)
        : this(options, CreateClient(options), true)
    {
    }

    protected S3CompatibleProvisionalObjectProviderBase(
        ProvisionalObjectCustodyOptions options,
        IAmazonS3 client,
        bool ownsClient)
    {
        Options = options.IsSyntacticallyValid && options.Topology == ProvisionalObjectTopology.S3CompatibleDurable
            ? options
            : throw new ArgumentException("PROD_RAW_EXPORT_OBJECT_TOPOLOGY_INVALID", nameof(options));
        Client = client;
        this.ownsClient = ownsClient;
        MaximumErrorRetry = client.Config.MaxErrorRetry;
    }

    private static AmazonS3Client CreateClient(ProvisionalObjectCustodyOptions options)
    {
        if (!options.IsSyntacticallyValid || options.Topology != ProvisionalObjectTopology.S3CompatibleDurable)
            throw new ArgumentException("PROD_RAW_EXPORT_OBJECT_TOPOLOGY_INVALID", nameof(options));
        var config = new AmazonS3Config
        {
            ServiceURL = options.ServiceUrl!.AbsoluteUri.TrimEnd('/'),
            ForcePathStyle = true,
            MaxErrorRetry = 0,
            Timeout = options.OperationTimeout,
            AuthenticationRegion = "us-east-1",
        };
        return new AmazonS3Client(
            new BasicAWSCredentials(options.AccessKeyId!, options.SecretAccessKey!), config);
    }

    public void Dispose()
    {
        if (ownsClient)
            Client.Dispose();
    }

    protected static byte[] DecodeMetadata(string? value)
    {
        if (value is null || value.Length != 64) return [];
        try { return Convert.FromHexString(value); }
        catch (FormatException) { return []; }
    }

    protected static string? ReadMetadata(MetadataCollection metadata, string key) =>
        metadata[key] ?? metadata[$"x-amz-meta-{key}"];
}

internal sealed class S3CompatibleProvisionalObjectWriter
    : S3CompatibleProvisionalObjectProviderBase, IProvisionalObjectWriter
{
    internal S3CompatibleProvisionalObjectWriter(ProvisionalObjectCustodyOptions options)
        : base(options)
    {
    }

    internal S3CompatibleProvisionalObjectWriter(ProvisionalObjectCustodyOptions options, IAmazonS3 client)
        : base(options, client, false)
    {
    }

    public async Task<ConditionalPutResult> PutIfAbsentAsync(
        ExactWriteRequest request,
        Stream ciphertext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);
        if (request.CiphertextLength is < 1 or > ProvisionalObjectCustodyOptions.FixedMaximumSinglePartCiphertextBytes)
            throw new ArgumentOutOfRangeException(nameof(request), "RAW_EXPORT_PROVISIONAL_OBJECT_SIZE_LIMIT_EXCEEDED");

        using var hashing = new HashingNonSeekableReadStream(ciphertext);
        var put = new PutObjectRequest
        {
            BucketName = Options.BucketName,
            Key = request.Locator.ObjectKey,
            InputStream = hashing,
            AutoResetStreamPosition = false,
            AutoCloseStream = false,
            IfNoneMatch = "*",
        };
        put.Headers.ContentLength = request.CiphertextLength;
        put.Metadata[BindingMetadataKey] = Convert.ToHexString(request.Locator.ObjectBindingDigest).ToLowerInvariant();
        put.Metadata[PutOperationMetadataKey] = Convert.ToHexString(ProvisionalObjectDigests.PutOperation(request.PutOperationId)).ToLowerInvariant();

        try
        {
            var response = await Client.PutObjectAsync(put, cancellationToken).ConfigureAwait(false);
            var extra = await hashing.ReadOneMoreAsync(cancellationToken).ConfigureAwait(false);
            if (hashing.BytesRead != request.CiphertextLength || extra)
                return new(ConditionalPutOutcome.OutcomeUnknown, (int)response.HttpStatusCode,
                    hashing.BytesRead, null, null);
            var digest = hashing.FinalizeDigest();
            return new(ConditionalPutOutcome.Created, (int)response.HttpStatusCode,
                hashing.BytesRead, digest, ProviderReceipt(request, (int)response.HttpStatusCode, digest));
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.PreconditionFailed)
        {
            return new(ConditionalPutOutcome.ConditionalConflict, (int)exception.StatusCode,
                hashing.BytesRead, null, ProviderReceipt(request, (int)exception.StatusCode, null));
        }
        catch (Exception exception) when (exception is AmazonS3Exception or HttpRequestException or IOException or TaskCanceledException)
        {
            return new(ConditionalPutOutcome.OutcomeUnknown, null, hashing.BytesRead, null, null);
        }
    }

    private static byte[] ProviderReceipt(ExactWriteRequest request, int status, byte[]? digest)
    {
        var binding = Convert.ToHexString(request.Locator.ObjectBindingDigest).ToLowerInvariant();
        return status == 200 && digest is not null
            ? HashCanonical(
                "tip-88c1-object-put-created-evidence-v1",
                binding,
                request.PutOperationId.ToString("N"),
                "Created",
                "200",
                request.CiphertextLength.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Convert.ToHexString(digest).ToLowerInvariant())
            : HashCanonical(
                "tip-88c1-object-put-conflict-evidence-v1",
                binding,
                request.PutOperationId.ToString("N"),
                "ConditionalConflictObserved",
                status.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    private static byte[] HashCanonical(params string[] fields)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> length = stackalloc byte[4];
        foreach (var field in fields)
        {
            var bytes = Encoding.UTF8.GetBytes(field.Normalize(NormalizationForm.FormC));
            BinaryPrimitives.WriteInt32BigEndian(length, bytes.Length);
            hash.AppendData(length);
            hash.AppendData(bytes);
        }
        return hash.GetHashAndReset();
    }
}

internal sealed class S3CompatibleProvisionalObjectReconciler(ProvisionalObjectCustodyOptions options)
    : S3CompatibleProvisionalObjectProviderBase(options), IProvisionalObjectReconciler
{
    public async Task<ExactObjectInspection> InspectExactAsync(ExactObjectLocator locator, CancellationToken cancellationToken)
    {
        try
        {
            var response = await Client.GetObjectMetadataAsync(new GetObjectMetadataRequest
            {
                BucketName = Options.BucketName,
                Key = locator.ObjectKey,
            }, cancellationToken).ConfigureAwait(false);
            return new(ExactObjectInspectionOutcome.Present, response.ContentLength,
                DecodeMetadata(ReadMetadata(response.Metadata, BindingMetadataKey)),
                DecodeMetadata(ReadMetadata(response.Metadata, PutOperationMetadataKey)),
                (int)response.HttpStatusCode);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return new(ExactObjectInspectionOutcome.PositivelyAbsent, null, null, null, 404);
        }
        catch (AmazonS3Exception exception) when ((int)exception.StatusCode >= 500)
        {
            return new(ExactObjectInspectionOutcome.ProviderUnavailable, null, null, null, (int)exception.StatusCode);
        }
        catch (Exception exception) when (exception is AmazonS3Exception or HttpRequestException or IOException or TaskCanceledException)
        {
            return new(ExactObjectInspectionOutcome.Indeterminate, null, null, null, null);
        }
    }

    public async Task<ExactObjectRead> OpenExactReadAsync(ExactObjectLocator locator, CancellationToken cancellationToken)
    {
        try
        {
            var response = await Client.GetObjectAsync(new GetObjectRequest
            {
                BucketName = Options.BucketName,
                Key = locator.ObjectKey,
            }, cancellationToken).ConfigureAwait(false);
            if (response.ContentLength is < 1 or > ProvisionalObjectCustodyOptions.FixedMaximumSinglePartCiphertextBytes)
            {
                response.Dispose();
                throw new ProvisionalObjectReadException(ExactObjectReadFailure.Indeterminate);
            }
            return new(
                new BoundedProviderReadStream(
                    response.ResponseStream,
                    response,
                    response.ContentLength,
                    ProvisionalObjectCustodyOptions.FixedMaximumSinglePartCiphertextBytes),
                response.ContentLength);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            throw new ProvisionalObjectReadException(ExactObjectReadFailure.PositivelyAbsent, exception);
        }
        catch (AmazonS3Exception exception) when ((int)exception.StatusCode >= 500)
        {
            throw new ProvisionalObjectReadException(ExactObjectReadFailure.ProviderUnavailable, exception);
        }
        catch (ProvisionalObjectReadException)
        {
            throw;
        }
        catch (Exception exception) when (exception is AmazonS3Exception or HttpRequestException or IOException or TaskCanceledException)
        {
            throw new ProvisionalObjectReadException(ExactObjectReadFailure.Indeterminate, exception);
        }
    }
}

internal enum ExactObjectReadFailure { PositivelyAbsent, ProviderUnavailable, Indeterminate }

internal sealed class ProvisionalObjectReadException : IOException
{
    internal ProvisionalObjectReadException(ExactObjectReadFailure failure, Exception? innerException = null)
        : base($"RAW_EXPORT_PROVISIONAL_OBJECT_READ_{failure.ToString().ToUpperInvariant()}", innerException) =>
        Failure = failure;

    internal ExactObjectReadFailure Failure { get; }
}

internal sealed class BoundedProviderReadStream(
    Stream inner,
    IDisposable owner,
    long declaredLength,
    long maximumLength) : Stream
{
    private long bytesRead;
    public override bool CanRead => inner.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => declaredLength;
    public override long Position { get => bytesRead; set => throw new NotSupportedException(); }
    public override void Flush() { }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var read = inner.Read(buffer, offset, count);
        Account(read);
        return read;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var read = await inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        Account(read);
        return read;
    }

    private void Account(int read)
    {
        bytesRead += read;
        if (bytesRead > declaredLength || bytesRead > maximumLength)
            throw new ProvisionalObjectReadException(ExactObjectReadFailure.Indeterminate);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            inner.Dispose();
            owner.Dispose();
        }
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        await inner.DisposeAsync().ConfigureAwait(false);
        owner.Dispose();
        GC.SuppressFinalize(this);
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}

internal sealed class S3CompatibleProvisionalObjectLifecycle(ProvisionalObjectCustodyOptions options)
    : S3CompatibleProvisionalObjectProviderBase(options), IProvisionalObjectLifecycle
{
    public async Task<ExactDeleteResult> DeleteExactAsync(ExactObjectLocator locator, CancellationToken cancellationToken)
    {
        try
        {
            var response = await Client.DeleteObjectAsync(new DeleteObjectRequest
            {
                BucketName = Options.BucketName,
                Key = locator.ObjectKey,
            }, cancellationToken).ConfigureAwait(false);
            return new(ExactDeleteOutcome.DeletedAcknowledged, (int)response.HttpStatusCode);
        }
        catch (AmazonS3Exception exception) when ((int)exception.StatusCode >= 500)
        {
            return new(ExactDeleteOutcome.ProviderUnavailable, (int)exception.StatusCode);
        }
        catch (Exception exception) when (exception is AmazonS3Exception or HttpRequestException or IOException or TaskCanceledException)
        {
            return new(ExactDeleteOutcome.OutcomeUnknown, null);
        }
    }
}

internal sealed class S3CompatibleProvisionalObjectPostureProbe
    : S3CompatibleProvisionalObjectProviderBase, IProvisionalObjectPostureProbe
{
    public S3CompatibleProvisionalObjectPostureProbe(ProvisionalObjectCustodyOptions options)
        : base(options)
    {
    }

    internal S3CompatibleProvisionalObjectPostureProbe(
        ProvisionalObjectCustodyOptions options,
        IAmazonS3 client)
        : base(options, client, false)
    {
    }

    public async Task<ObjectBucketPosture> InspectBucketPostureAsync(CancellationToken cancellationToken)
    {
        var versioningNeverEnabled = false;
        try
        {
            var versioning = await Client.GetBucketVersioningAsync(new GetBucketVersioningRequest
                { BucketName = Options.BucketName }, cancellationToken).ConfigureAwait(false);
            var versioningStatus = versioning.VersioningConfig?.Status?.Value;
            versioningNeverEnabled = string.Equals(
                versioningStatus, VersionStatus.Off.Value, StringComparison.Ordinal);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return new(false, false, false, false, false);
        }
        catch (Exception exception) when (exception is AmazonS3Exception
            or HttpRequestException or IOException or TaskCanceledException)
        {
            // Versioning is the first exact bucket-scoped probe. Failure here
            // cannot distinguish an absent bucket from inaccessible posture.
            return new(false, false, false, false, false);
        }

        var lockAbsent = false;
        try
        {
            _ = await Client.GetObjectLockConfigurationAsync(
                new GetObjectLockConfigurationRequest { BucketName = Options.BucketName }, cancellationToken).ConfigureAwait(false);
            // A successful null/unknown response is not typed absence.
        }
        catch (AmazonS3Exception exception) when (IsExactMissingObjectLock(exception))
        {
            lockAbsent = true;
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return new(false, false, false, false, false);
        }
        catch (Exception exception) when (exception is AmazonS3Exception
            or HttpRequestException or IOException or TaskCanceledException)
        {
            lockAbsent = false;
        }

        var lifecycleAbsent = false;
        try
        {
            var lifecycle = await Client.GetLifecycleConfigurationAsync(
                new GetLifecycleConfigurationRequest { BucketName = Options.BucketName }, cancellationToken).ConfigureAwait(false);
            lifecycleAbsent = lifecycle.Configuration?.Rules is { Count: 0 };
        }
        catch (AmazonS3Exception exception) when (IsExactMissingLifecycle(exception))
        {
            lifecycleAbsent = true;
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return new(false, false, false, false, false);
        }
        catch (Exception exception) when (exception is AmazonS3Exception
            or HttpRequestException or IOException or TaskCanceledException)
        {
            lifecycleAbsent = false;
        }

        var aclPrivate = false;
        try
        {
            var acl = await Client.GetACLAsync(
                new GetACLRequest { BucketName = Options.BucketName }, cancellationToken).ConfigureAwait(false);
            aclPrivate = acl.AccessControlList?.Grants is { } grants
                && grants.All(grant => grant.Grantee is not null && grant.Grantee.URI is null);
        }
        catch (Exception exception) when (exception is AmazonS3Exception
            or HttpRequestException or IOException or TaskCanceledException)
        {
            aclPrivate = false;
        }

        var policyAbsent = false;
        try
        {
            var policy = await Client.GetBucketPolicyAsync(
                new GetBucketPolicyRequest { BucketName = Options.BucketName }, cancellationToken).ConfigureAwait(false);
            policyAbsent = IsDefinitelyNonPublicPolicy(policy.Policy);
        }
        catch (AmazonS3Exception exception) when (IsExactMissingPolicy(exception))
        {
            policyAbsent = true;
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return new(false, false, false, false, false);
        }
        catch (Exception exception) when (exception is AmazonS3Exception
            or HttpRequestException or IOException or TaskCanceledException)
        {
            policyAbsent = false;
        }

        return new(true, versioningNeverEnabled, lockAbsent, lifecycleAbsent,
            aclPrivate && policyAbsent);
    }

    private static bool IsDefinitelyNonPublicPolicy(string? policy)
    {
        if (string.IsNullOrWhiteSpace(policy)) return false;
        try
        {
            using var document = JsonDocument.Parse(policy);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("Version", out var version)
                || version.ValueKind != JsonValueKind.String
                || !document.RootElement.TryGetProperty("Statement", out var statements)
                || statements.ValueKind != JsonValueKind.Array
                || statements.GetArrayLength() == 0)
                return false;
            foreach (var statement in statements.EnumerateArray())
            {
                if (statement.ValueKind != JsonValueKind.Object
                    || !statement.TryGetProperty("Effect", out var effect)
                    || !string.Equals(effect.GetString(), "Deny", StringComparison.Ordinal)
                    || !statement.TryGetProperty("Principal", out _)
                    || !statement.TryGetProperty("Action", out _)
                    || !statement.TryGetProperty("Resource", out _))
                    return false;
            }
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool IsExactMissingObjectLock(AmazonS3Exception exception) =>
        exception.StatusCode == HttpStatusCode.NotFound
        && string.Equals(exception.ErrorCode, "ObjectLockConfigurationNotFoundError", StringComparison.Ordinal);

    private static bool IsExactMissingLifecycle(AmazonS3Exception exception) =>
        exception.StatusCode == HttpStatusCode.NotFound
        && string.Equals(exception.ErrorCode, "NoSuchLifecycleConfiguration", StringComparison.Ordinal);

    private static bool IsExactMissingPolicy(AmazonS3Exception exception) =>
        exception.StatusCode == HttpStatusCode.NotFound
        && string.Equals(exception.ErrorCode, "NoSuchBucketPolicy", StringComparison.Ordinal);
}

internal sealed class HashingNonSeekableReadStream(Stream inner) : Stream
{
    private readonly IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    private bool finalized;
    internal long BytesRead { get; private set; }
    public override bool CanRead => inner.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override void Flush() { }
    public override int Read(byte[] buffer, int offset, int count)
    {
        var read = inner.Read(buffer, offset, count);
        if (read > 0) { hash.AppendData(buffer, offset, read); BytesRead += read; }
        return read;
    }
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var read = await inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        if (read > 0) { hash.AppendData(buffer.Span[..read]); BytesRead += read; }
        return read;
    }
    internal async Task<bool> ReadOneMoreAsync(CancellationToken cancellationToken)
    {
        var one = new byte[1];
        return await ReadAsync(one, cancellationToken).ConfigureAwait(false) != 0;
    }
    internal byte[] FinalizeDigest()
    {
        if (finalized) throw new InvalidOperationException("Digest already finalized.");
        finalized = true;
        return hash.GetHashAndReset();
    }
    protected override void Dispose(bool disposing) { if (disposing) hash.Dispose(); base.Dispose(disposing); }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
