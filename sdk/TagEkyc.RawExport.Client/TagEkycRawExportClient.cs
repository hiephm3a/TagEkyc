namespace TagEkyc.RawExport.Client;

using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class TagEkycRawExportClient
{
    private static readonly string[] RequiredClasses = ["ChipDg2Portrait", "LiveSelfieImage"];
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    private readonly HttpClient _httpClient;
    private readonly TagEkycRawExportClientOptions _options;
    private readonly ITagEkycRecipientPrivateKeySource _privateKeys;

    public TagEkycRawExportClient(
        HttpClient httpClient,
        TagEkycRawExportClientOptions options,
        ITagEkycRecipientPrivateKeySource privateKeys)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = Validate(options);
        _privateKeys = privateKeys ?? throw new ArgumentNullException(nameof(privateKeys));
    }

    public async Task<TagEkycRawBiometricMaterialLease> AcquireAsync(
        TagEkycRawExportAcquisitionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.VerificationSessionId == Guid.Empty || request.OperationId == Guid.Empty)
            throw Invalid("VerificationSessionId and OperationId are required.");

        var operation = request.OperationId.ToString("N");
        var authorization = await SendJsonAsync<AuthorizationDecision>(
            HttpMethod.Post,
            "/api/ekyc/raw-export/authorizations",
            new AuthorizationRequest(request.VerificationSessionId, _options.PolicyId,
                _options.PolicyVersion, RequiredClasses),
            $"raw:{operation}:authorize",
            cancellationToken).ConfigureAwait(false);
        if (!string.Equals(authorization.Outcome, "Authorized", StringComparison.Ordinal)
            || authorization.PermitId is not Guid permitId || permitId == Guid.Empty
            || !authorization.AuthorizedRawClasses.Order(StringComparer.Ordinal)
                .SequenceEqual(RequiredClasses.Order(StringComparer.Ordinal), StringComparer.Ordinal))
            throw new TagEkycRawExportClientException(
                authorization.PrimaryCause ?? "RAW_EXPORT_NOT_AUTHORIZED",
                "TagEkyc did not authorize the required raw biometric classes.");

        var binding = await SendJsonAsync<JobBinding>(
            HttpMethod.Post,
            "/api/ekyc/raw-export/jobs",
            new JobRequest(permitId),
            $"raw:{operation}:job",
            cancellationToken).ConfigureAwait(false);
        if (binding.JobId == Guid.Empty) throw Invalid("TagEkyc returned an invalid raw-export job.");

        var job = await WaitForPackageAsync(binding.JobId, request.VerificationSessionId, cancellationToken)
            .ConfigureAwait(false);
        var packageId = job.PackageId!.Value;
        var delivery = await SendJsonAsync<Delivery>(
            HttpMethod.Post,
            $"/api/ekyc/raw-export/packages/{packageId:D}/deliveries",
            body: null,
            $"raw:{operation}:delivery",
            cancellationToken).ConfigureAwait(false);
        if (delivery.PackageId != packageId)
            throw Invalid("TagEkyc returned a delivery for a different package.");
        var package = await DownloadAsync(delivery, cancellationToken).ConfigureAwait(false);
        try
        {
            using var privateKey = await _privateKeys.AcquireAsync(
                _options.RecipientKeyId, _options.RecipientKeyVersion, cancellationToken).ConfigureAwait(false);
            return TagEkycRecipientPackageDecoder.Decode(
                package,
                privateKey.Key.Span,
                packageId,
                binding.JobId,
                request.VerificationSessionId,
                _options.RecipientClientApplicationId,
                _options.RecipientKeyId,
                _options.RecipientKeyVersion);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(package);
        }
    }

    private async Task<JobStatus> WaitForPackageAsync(
        Guid jobId,
        Guid expectedSessionId,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < _options.MaximumPollAttempts; attempt++)
        {
            var job = await SendJsonAsync<JobStatus>(HttpMethod.Get,
                $"/api/ekyc/raw-export/jobs/{jobId:D}", null, null, cancellationToken).ConfigureAwait(false);
            if (job.JobId != jobId || job.VerificationSessionId != expectedSessionId
                || job.RecipientClientApplicationId != _options.RecipientClientApplicationId)
                throw Invalid("TagEkyc returned a raw-export job for a different identity.");
            if (!job.RawClasses.Order(StringComparer.Ordinal)
                .SequenceEqual(RequiredClasses.Order(StringComparer.Ordinal), StringComparer.Ordinal))
                throw Invalid("TagEkyc raw-export job did not contain the required raw classes.");
            if (job.PackageId is Guid packageId && packageId != Guid.Empty
                && string.Equals(job.PackageState, "Finalized", StringComparison.Ordinal))
                return job;
            if (job.State is "TerminalFailed" or "Cancelled" or "Expired" or "ReconciliationExpired")
                throw new TagEkycRawExportClientException(
                    "RAW_EXPORT_JOB_TERMINAL",
                    $"TagEkyc raw-export job ended in state {job.State}.");
            if (attempt + 1 < _options.MaximumPollAttempts)
                await Task.Delay(_options.PollInterval, cancellationToken).ConfigureAwait(false);
        }
        throw new TagEkycRawExportClientException(
            "RAW_EXPORT_JOB_TIMEOUT",
            "TagEkyc raw-export package was not finalized within the configured polling window.");
    }

    private async Task<byte[]> DownloadAsync(Delivery delivery, CancellationToken cancellationToken)
    {
        if (delivery.DeliveryId == Guid.Empty || delivery.PackageId == Guid.Empty
            || delivery.EncryptedPackageLength is < 1 or > int.MaxValue
            || delivery.EncryptedPackageLength > _options.MaximumPackageBytes)
            throw Invalid("TagEkyc returned invalid delivery metadata.");

        using var request = CreateRequest(HttpMethod.Get,
            $"/api/ekyc/raw-export/deliveries/{delivery.DeliveryId:D}/content", null, null);
        using var response = await _httpClient.SendAsync(
            request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        if (response.Content.Headers.ContentLength != delivery.EncryptedPackageLength)
            throw Invalid("TagEkyc package length does not match delivery metadata.");

        var bytes = new byte[checked((int)delivery.EncryptedPackageLength)];
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await stream.ReadExactlyAsync(bytes, cancellationToken).ConfigureAwait(false);
            if (await stream.ReadAsync(new byte[1], cancellationToken).ConfigureAwait(false) != 0)
                throw Invalid("TagEkyc package exceeded its declared length.");
            byte[] expected;
            try { expected = DecodeBase64UrlSha256(delivery.PackageCiphertextDigest); }
            catch (FormatException exception) { throw Invalid("TagEkyc returned an invalid package digest.", exception); }
            var observed = SHA256.HashData(bytes);
            try
            {
                if (expected.Length != observed.Length
                    || !CryptographicOperations.FixedTimeEquals(expected, observed))
                    throw Invalid("TagEkyc package digest did not match delivery metadata.");
            }
            finally
            {
                CryptographicOperations.ZeroMemory(expected);
                CryptographicOperations.ZeroMemory(observed);
            }
            return bytes;
        }
        catch
        {
            CryptographicOperations.ZeroMemory(bytes);
            throw;
        }
    }

    private async Task<T> SendJsonAsync<T>(
        HttpMethod method,
        string path,
        object? body,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        using var request = CreateRequest(method, path, body, idempotencyKey);
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        try
        {
            return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken).ConfigureAwait(false)
                ?? throw Invalid("TagEkyc returned an empty response.");
        }
        catch (JsonException exception) { throw Invalid("TagEkyc returned malformed JSON.", exception); }
    }

    private HttpRequestMessage CreateRequest(
        HttpMethod method,
        string path,
        object? body,
        string? idempotencyKey)
    {
        var request = new HttpRequestMessage(method, new Uri(_options.BaseAddress, path));
        request.Headers.TryAddWithoutValidation(_options.ApiKeyHeaderName, _options.ApiKey);
        if (idempotencyKey is not null) request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
        if (body is not null) request.Content = JsonContent.Create(body, options: JsonOptions);
        return request;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;
        string? code = null;
        string? message = null;
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorEnvelope>(JsonOptions, cancellationToken)
                .ConfigureAwait(false);
            code = error?.Error.Code;
            message = error?.Error.Message;
        }
        catch (JsonException) { }
        throw new TagEkycRawExportClientException(
            code ?? "RAW_EXPORT_HTTP_FAILURE",
            message ?? $"TagEkyc returned HTTP {(int)response.StatusCode}.",
            response.StatusCode);
    }

    private static TagEkycRawExportClientOptions Validate(TagEkycRawExportClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!options.BaseAddress.IsAbsoluteUri || options.BaseAddress.Scheme != Uri.UriSchemeHttps
            || string.IsNullOrWhiteSpace(options.ApiKeyHeaderName) || string.IsNullOrWhiteSpace(options.ApiKey)
            || options.PolicyId == Guid.Empty || options.PolicyVersion < 1
            || options.RecipientClientApplicationId == Guid.Empty
            || string.IsNullOrWhiteSpace(options.RecipientKeyId) || options.RecipientKeyVersion < 1
            || options.PollInterval < TimeSpan.Zero || options.MaximumPollAttempts < 1
            || options.MaximumPackageBytes < 1)
            throw new ArgumentException("TagEkyc raw-export client options are invalid.", nameof(options));
        return options;
    }

    private static TagEkycRawExportClientException Invalid(string message, Exception? inner = null) =>
        new("RAW_EXPORT_RESPONSE_INVALID", message, innerException: inner);

    private static byte[] DecodeBase64UrlSha256(string value)
    {
        if (value.Length != 43 || value.IndexOf('=') >= 0)
            throw new FormatException("The SHA-256 digest is not canonical base64url.");
        var bytes = Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/') + "=");
        if (bytes.Length != 32) throw new FormatException("The SHA-256 digest has an invalid length.");
        return bytes;
    }

    private sealed record AuthorizationRequest(Guid VerificationSessionId, Guid PolicyId, int PolicyVersion, IReadOnlyList<string> RequestedRawClasses);
    private sealed record AuthorizationDecision(Guid DecisionId, string Outcome, string? PrimaryCause, Guid? PermitId, DateTimeOffset? PermitExpiresAtUtc, IReadOnlyList<string> AuthorizedRawClasses);
    private sealed record JobRequest(Guid PermitId);
    private sealed record JobBinding(Guid JobId, string BindStatus);
    private sealed record JobStatus(Guid JobId, Guid VerificationSessionId, Guid RecipientClientApplicationId, string State, long Revision, DateTimeOffset JobExpiresAtUtc, IReadOnlyList<string> RawClasses, Guid? PackageId, string? PackageState, DateTimeOffset? PackageFinalizedAtUtc);
    private sealed record Delivery(Guid DeliveryId, Guid PackageId, string State, DateTimeOffset AuthorizedAtUtc, DateTimeOffset AuthorizationExpiresAtUtc, int StreamAttemptCount, DateTimeOffset? ServerStreamCompletedAtUtc, long EncryptedPackageLength, string PackageCiphertextDigest, string? DeliveryReceiptDigest);
    private sealed record ErrorEnvelope(ErrorBody Error);
    private sealed record ErrorBody(string Code, string Message, string CorrelationId);
}
