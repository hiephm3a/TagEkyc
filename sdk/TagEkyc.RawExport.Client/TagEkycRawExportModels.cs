namespace TagEkyc.RawExport.Client;

using System.Net;
using System.Security.Cryptography;

public sealed record TagEkycRawExportClientOptions(
    Uri BaseAddress,
    string ApiKeyHeaderName,
    string ApiKey,
    Guid PolicyId,
    int PolicyVersion,
    Guid RecipientClientApplicationId,
    string RecipientKeyId,
    int RecipientKeyVersion,
    TimeSpan PollInterval,
    int MaximumPollAttempts,
    int MaximumPackageBytes = 34 * 1024 * 1024);

public sealed record TagEkycRawExportAcquisitionRequest(
    Guid VerificationSessionId,
    Guid OperationId);

public interface ITagEkycRecipientPrivateKeySource
{
    ValueTask<TagEkycRecipientPrivateKeyLease> AcquireAsync(
        string keyId,
        int keyVersion,
        CancellationToken cancellationToken = default);
}

public sealed class TagEkycRecipientPrivateKeyLease : IDisposable
{
    private byte[]? _key;

    public TagEkycRecipientPrivateKeyLease(byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (key.Length == 0) throw new ArgumentException("Private key is required.", nameof(key));
        _key = key;
    }

    public ReadOnlyMemory<byte> Key => _key ?? throw new ObjectDisposedException(nameof(TagEkycRecipientPrivateKeyLease));

    public void Dispose()
    {
        var key = Interlocked.Exchange(ref _key, null);
        if (key is not null) CryptographicOperations.ZeroMemory(key);
    }
}

public sealed class TagEkycRawBiometricMaterialLease : IDisposable
{
    private byte[]? _chipDg2Portrait;
    private byte[]? _liveSelfieImage;

    internal TagEkycRawBiometricMaterialLease(
        Guid verificationSessionId,
        Guid jobId,
        Guid packageId,
        byte[] chipDg2Portrait,
        byte[] liveSelfieImage)
    {
        VerificationSessionId = verificationSessionId;
        JobId = jobId;
        PackageId = packageId;
        _chipDg2Portrait = chipDg2Portrait;
        _liveSelfieImage = liveSelfieImage;
    }

    public Guid VerificationSessionId { get; }
    public Guid JobId { get; }
    public Guid PackageId { get; }
    public ReadOnlyMemory<byte> ChipDg2Portrait =>
        _chipDg2Portrait ?? throw new ObjectDisposedException(nameof(TagEkycRawBiometricMaterialLease));
    public ReadOnlyMemory<byte> LiveSelfieImage =>
        _liveSelfieImage ?? throw new ObjectDisposedException(nameof(TagEkycRawBiometricMaterialLease));

    public void Dispose()
    {
        var portrait = Interlocked.Exchange(ref _chipDg2Portrait, null);
        var selfie = Interlocked.Exchange(ref _liveSelfieImage, null);
        if (portrait is not null) CryptographicOperations.ZeroMemory(portrait);
        if (selfie is not null) CryptographicOperations.ZeroMemory(selfie);
    }
}

public sealed class TagEkycRawExportClientException : Exception
{
    public TagEkycRawExportClientException(
        string code,
        string message,
        HttpStatusCode? statusCode = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
        StatusCode = statusCode;
    }

    public string Code { get; }
    public HttpStatusCode? StatusCode { get; }
}
