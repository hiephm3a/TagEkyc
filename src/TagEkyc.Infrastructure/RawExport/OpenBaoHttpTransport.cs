using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using TagEkyc.Infrastructure.Secrets;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed class OpenBaoHttpTransport : IDisposable
{
    private readonly HttpClient client;
    private readonly OpenBaoConnectionOptions options;

    internal OpenBaoHttpTransport(OpenBaoConnectionOptions options)
    {
        this.options = options;
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.None,
            ConnectTimeout = options.RequestTimeout,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        };
        if (!string.IsNullOrWhiteSpace(options.CaCertificatePath))
        {
            var root = X509Certificate2.CreateFromPem(File.ReadAllText(options.CaCertificatePath));
            handler.SslOptions.RemoteCertificateValidationCallback = (_, certificate, chain, errors) =>
            {
                if (certificate is null
                    || (errors & System.Net.Security.SslPolicyErrors.RemoteCertificateNameMismatch) != 0
                    || (errors & System.Net.Security.SslPolicyErrors.RemoteCertificateNotAvailable) != 0)
                    return false;
                using var custom = new X509Chain();
                custom.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                custom.ChainPolicy.CustomTrustStore.Add(root);
                custom.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                return custom.Build(new X509Certificate2(certificate));
            };
        }
        client = new HttpClient(handler) { BaseAddress = options.Address, Timeout = options.RequestTimeout };
    }

    internal async Task<JsonDocument> PostAsync(string path, object payload, string? token, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(payload),
        };
        request.Headers.Accept.ParseAdd("application/json");
        if (!string.IsNullOrWhiteSpace(options.Namespace))
            request.Headers.TryAddWithoutValidation("X-Vault-Namespace", options.Namespace);
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.TryAddWithoutValidation("X-Vault-Token", token);
        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (HttpRequestException exception)
        {
            throw new OpenBaoTransportException(0, true, exception);
        }
        using (response)
        {
        if (!response.IsSuccessStatusCode)
            throw new OpenBaoTransportException((int)response.StatusCode, response.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }

    internal async Task<JsonDocument> GetAsync(string path, string token, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Accept.ParseAdd("application/json");
        if (!string.IsNullOrWhiteSpace(options.Namespace))
            request.Headers.TryAddWithoutValidation("X-Vault-Namespace", options.Namespace);
        request.Headers.TryAddWithoutValidation("X-Vault-Token", token);
        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (HttpRequestException exception)
        {
            throw new OpenBaoTransportException(0, true, exception);
        }
        using (response)
        {
        if (!response.IsSuccessStatusCode)
            throw new OpenBaoTransportException((int)response.StatusCode,
                response.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests
                || (int)response.StatusCode >= 500);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }

    public void Dispose() => client.Dispose();
}

internal sealed class OpenBaoTransportException(int statusCode, bool transient, Exception? innerException = null)
    : Exception("OPENBAO_TRANSPORT_FAILURE", innerException)
{
    internal int StatusCode { get; } = statusCode;
    internal bool IsTransient { get; } = transient;
}

internal sealed class OpenBaoTokenSession(OpenBaoHttpTransport transport, OpenBaoConnectionOptions options)
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private string? token;
    private DateTimeOffset usableUntilUtc;

    internal async Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        if (token is not null && usableUntilUtc > DateTimeOffset.UtcNow.AddSeconds(15))
            return token;
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (token is not null && usableUntilUtc > DateTimeOffset.UtcNow.AddSeconds(15))
                return token;
            var roleId = SecretRefResolver.Resolve(options.RoleIdSecretRef).Value;
            var secretId = SecretRefResolver.Resolve(options.SecretIdSecretRef).Value;
            using var document = await transport.PostAsync("/v1/auth/approle/login", new { role_id = roleId, secret_id = secretId }, null, cancellationToken).ConfigureAwait(false);
            var auth = document.RootElement.GetProperty("auth");
            var next = auth.GetProperty("client_token").GetString();
            var lease = auth.TryGetProperty("lease_duration", out var duration) ? duration.GetInt32() : 60;
            if (string.IsNullOrWhiteSpace(next) || lease < 1)
                throw new InvalidOperationException("OPENBAO_APPROLE_RESPONSE_INVALID");
            token = next;
            usableUntilUtc = DateTimeOffset.UtcNow.AddSeconds(Math.Max(1, lease - Math.Min(30, lease / 2)));
            return token;
        }
        finally
        {
            gate.Release();
        }
    }

    internal void Invalidate() { token = null; usableUntilUtc = default; }
}
