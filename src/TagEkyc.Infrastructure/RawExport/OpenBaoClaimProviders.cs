using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TagEkyc.Contracts.RawExport;
using TagEkyc.Infrastructure.ProtectedValues;

namespace TagEkyc.Infrastructure.RawExport;

internal sealed record OpenBaoHmacKeyBinding(
    string KeyId,
    int KeyVersion,
    string TransitKeyName,
    DateTimeOffset NotBeforeUtc,
    DateTimeOffset NotAfterUtc);

internal sealed record OpenBaoHmacProviderOptions(
    OpenBaoConnectionOptions Connection,
    string TransitMount,
    IReadOnlyDictionary<(string KeyId, int KeyVersion), OpenBaoHmacKeyBinding> Keys);

internal sealed record OpenBaoClaimProviderOptionsSet(
    OpenBaoHmacProviderOptions? ContentCommitment,
    OpenBaoHmacProviderOptions? SubjectRefToken,
    CommitmentKeySelector? ActiveContentSelector,
    SubjectTokenKeySelector? ActiveSubjectSelector,
    string? ErrorCode)
{
    internal const string SectionName = "TagEkyc:RawExport:ClaimProviders";
    internal const string ConfigurationInvalid = "PROD_RAW_EXPORT_CLAIM_PROVIDER_CONFIGURATION_INVALID";
    internal const string SeparationInvalid = "PROD_RAW_EXPORT_CLAIM_PROVIDER_SEPARATION_INVALID";

    internal bool IsValid => ErrorCode is null
                             && ContentCommitment is not null
                             && SubjectRefToken is not null
                             && ActiveContentSelector is not null
                             && ActiveSubjectSelector is not null;

    internal static OpenBaoClaimProviderOptionsSet Resolve(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        try
        {
            var root = configuration.GetSection(SectionName);
            var content = ResolveProvider(root.GetSection("ContentCommitment"));
            var subject = ResolveProvider(root.GetSection("SubjectRefToken"));
            var broker = configuration.GetSection(RawIngressBrokerOptions.SectionName);
            var activeContent = new CommitmentKeySelector(
                Required(broker, "CommitmentSelectorId"),
                PositiveInt(broker, "CommitmentSelectorVersion"));
            var activeSubject = new SubjectTokenKeySelector(
                Required(broker, "SubjectTokenSelectorId"),
                PositiveInt(broker, "SubjectTokenSelectorVersion"));
            var contentKeys = content.Keys.Values.Select(value => value.TransitKeyName)
                .ToHashSet(StringComparer.Ordinal);
            if (contentKeys.Overlaps(subject.Keys.Values.Select(value => value.TransitKeyName))
                || SameCredential(content.Connection, subject.Connection))
                return new(null, null, null, null, SeparationInvalid);
            var kekRole = configuration[$"{OpenBaoKekOptions.SectionName}:RoleIdSecretRef"]?.Trim();
            var kekSecret = configuration[$"{OpenBaoKekOptions.SectionName}:SecretIdSecretRef"]?.Trim();
            if ((!string.IsNullOrEmpty(kekRole)
                 && (string.Equals(kekRole, content.Connection.RoleIdSecretRef, StringComparison.Ordinal)
                     || string.Equals(kekRole, subject.Connection.RoleIdSecretRef, StringComparison.Ordinal)))
                || (!string.IsNullOrEmpty(kekSecret)
                    && (string.Equals(kekSecret, content.Connection.SecretIdSecretRef, StringComparison.Ordinal)
                        || string.Equals(kekSecret, subject.Connection.SecretIdSecretRef, StringComparison.Ordinal))))
                return new(null, null, null, null, SeparationInvalid);
            return new(content, subject, activeContent, activeSubject, null);
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException or OverflowException)
        {
            return new(null, null, null, null, ConfigurationInvalid);
        }
    }

    private static string Required(IConfiguration section, string key)
    {
        var value = section[key]?.Trim();
        return string.IsNullOrWhiteSpace(value) ? throw new FormatException(key) : value;
    }

    private static int PositiveInt(IConfiguration section, string key) =>
        int.TryParse(Required(section, key), NumberStyles.None,
            CultureInfo.InvariantCulture, out var value) && value >= 1
            ? value
            : throw new FormatException(key);

    private static OpenBaoHmacProviderOptions ResolveProvider(IConfigurationSection section)
    {
        var addressText = Required(section, "Address");
        if (!Uri.TryCreate(addressText, UriKind.Absolute, out var address)
            || address.Scheme != Uri.UriSchemeHttps)
            throw new FormatException("Address");
        if (!int.TryParse(section["RequestTimeoutSeconds"], NumberStyles.None,
                CultureInfo.InvariantCulture, out var timeoutSeconds))
            timeoutSeconds = 15;
        if (timeoutSeconds is < 1 or > 60)
            throw new FormatException("RequestTimeoutSeconds");
        var connection = new OpenBaoConnectionOptions(
            address,
            string.IsNullOrWhiteSpace(section["Namespace"]) ? null : section["Namespace"]!.Trim(),
            Required(section, "RoleIdSecretRef"),
            Required(section, "SecretIdSecretRef"),
            string.IsNullOrWhiteSpace(section["CaCertificatePath"])
                ? null
                : section["CaCertificatePath"]!.Trim(),
            TimeSpan.FromSeconds(timeoutSeconds));
        var transitMount = Required(section, "TransitMount").Trim('/');
        if (string.IsNullOrWhiteSpace(transitMount))
            throw new FormatException("TransitMount");
        var keys = new Dictionary<(string, int), OpenBaoHmacKeyBinding>();
        foreach (var child in section.GetSection("Keys").GetChildren())
        {
            var keyId = Required(child, "KeyId");
            _ = new CommitmentKeySelector(keyId, 1);
            if (!int.TryParse(child["KeyVersion"], NumberStyles.None,
                    CultureInfo.InvariantCulture, out var keyVersion) || keyVersion < 1)
                throw new FormatException("KeyVersion");
            var transitKeyName = Required(child, "TransitKeyName");
            if (transitKeyName.Any(character => character is not
                    (>= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '_')))
                throw new FormatException("TransitKeyName");
            if (!DateTimeOffset.TryParse(Required(child, "NotBeforeUtc"), CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var notBefore)
                || !DateTimeOffset.TryParse(Required(child, "NotAfterUtc"), CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var notAfter)
                || notBefore >= notAfter)
                throw new FormatException("ValidityWindow");
            var binding = new OpenBaoHmacKeyBinding(
                keyId, keyVersion, transitKeyName, notBefore, notAfter);
            if (!keys.TryAdd((keyId, keyVersion), binding))
                throw new FormatException("DuplicateSelector");
        }
        if (keys.Count == 0)
            throw new FormatException("Keys");
        return new(connection, transitMount, keys);
    }

    private static bool SameCredential(OpenBaoConnectionOptions left, OpenBaoConnectionOptions right) =>
        string.Equals(left.RoleIdSecretRef, right.RoleIdSecretRef, StringComparison.Ordinal)
        || string.Equals(left.SecretIdSecretRef, right.SecretIdSecretRef, StringComparison.Ordinal);
}

internal abstract class OpenBaoHmacSelectorCatalog(
    OpenBaoHmacProviderOptions? options,
    TimeProvider timeProvider)
{
    internal IReadOnlyCollection<OpenBaoHmacKeyBinding> All =>
        options?.Keys.Values.ToArray() ?? [];

    internal OpenBaoHmacKeyBinding? Find(string keyId, int keyVersion)
    {
        if (options is null || !options.Keys.TryGetValue((keyId, keyVersion), out var binding))
            return null;
        var now = timeProvider.GetUtcNow();
        return now >= binding.NotBeforeUtc && now < binding.NotAfterUtc ? binding : null;
    }
}

internal sealed class OpenBaoContentCommitmentCatalog(
    OpenBaoClaimProviderOptionsSet options,
    TimeProvider timeProvider)
    : OpenBaoHmacSelectorCatalog(options.ContentCommitment, timeProvider);

internal sealed class OpenBaoSubjectRefTokenCatalog(
    OpenBaoClaimProviderOptionsSet options,
    TimeProvider timeProvider)
    : OpenBaoHmacSelectorCatalog(options.SubjectRefToken, timeProvider);

internal sealed class OpenBaoTransitHmacClient : IDisposable
{
    private static readonly byte[] SeparationProbePayload =
        "tagekyc-raw-export-claim-provider-separation-v1"u8.ToArray();
    private readonly OpenBaoHmacProviderOptions options;
    private readonly OpenBaoHttpTransport transport;
    private readonly OpenBaoTokenSession session;

    internal OpenBaoTransitHmacClient(OpenBaoHmacProviderOptions options)
    {
        this.options = options;
        transport = new OpenBaoHttpTransport(options.Connection);
        session = new OpenBaoTokenSession(transport, options.Connection);
    }

    internal async Task<byte[]> ComputeAsync(
        OpenBaoHmacKeyBinding binding,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken)
    {
        using var response = await PostAsync(
            $"/v1/{options.TransitMount}/hmac/{Uri.EscapeDataString(binding.TransitKeyName)}/sha2-256",
            new
            {
                input = Convert.ToBase64String(payload.Span),
                key_version = binding.KeyVersion,
            }, cancellationToken).ConfigureAwait(false);
        string? encoded;
        try
        {
            encoded = response.RootElement.GetProperty("data").GetProperty("hmac").GetString();
        }
        catch (Exception exception) when (exception is InvalidOperationException or KeyNotFoundException)
        {
            throw new JsonException("OPENBAO_HMAC_RESPONSE_SHAPE_INVALID", exception);
        }
        return ParseHmac(encoded, binding.KeyVersion);
    }

    internal static byte[] ParseHmac(string? encoded, int keyVersion)
    {
        var prefix = $"vault:v{keyVersion}:";
        if (encoded is null || !encoded.StartsWith(prefix, StringComparison.Ordinal)
            || encoded.Length <= prefix.Length)
            throw new JsonException("OPENBAO_HMAC_RESPONSE_SHAPE_INVALID");
        byte[] result;
        try
        {
            result = Convert.FromBase64String(encoded[prefix.Length..]);
        }
        catch (FormatException exception)
        {
            throw new JsonException("OPENBAO_HMAC_RESPONSE_SHAPE_INVALID", exception);
        }
        if (result.Length != ContentCommitmentResult.MacLength)
        {
            CryptographicOperations.ZeroMemory(result);
            throw new JsonException("OPENBAO_HMAC_RESPONSE_SHAPE_INVALID");
        }
        return result;
    }

    internal async Task ValidateAsync(
        OpenBaoHmacKeyBinding binding,
        CancellationToken cancellationToken)
    {
        using var response = await GetAsync(
            $"/v1/{options.TransitMount}/keys/{Uri.EscapeDataString(binding.TransitKeyName)}",
            cancellationToken).ConfigureAwait(false);
        ValidateMetadata(response.RootElement.GetProperty("data"), binding);
    }

    internal async Task<bool> CanComputeAsync(
        OpenBaoHmacKeyBinding binding,
        CancellationToken cancellationToken)
    {
        byte[]? result = null;
        try
        {
            result = await ComputeAsync(binding, SeparationProbePayload, cancellationToken)
                .ConfigureAwait(false);
            return true;
        }
        catch (OpenBaoTransportException exception) when (exception.StatusCode == 403)
        {
            return false;
        }
        finally
        {
            if (result is not null)
                CryptographicOperations.ZeroMemory(result);
        }
    }

    internal static void ValidateMetadata(JsonElement data, OpenBaoHmacKeyBinding binding)
    {
        static InvalidOperationException Invalid(string reason) =>
            new($"OPENBAO_HMAC_KEY_REFERENCE_INVALID:{reason}");
        if (!string.Equals(data.GetProperty("name").GetString(), binding.TransitKeyName,
                StringComparison.Ordinal))
            throw Invalid("NAME");
        if (!string.Equals(data.GetProperty("type").GetString(), "hmac", StringComparison.Ordinal))
            throw Invalid("TYPE");
        if (!data.TryGetProperty("derived", out var derived) || derived.GetBoolean())
            throw Invalid("DERIVED");
        if (!data.TryGetProperty("exportable", out var exportable) || exportable.GetBoolean())
            throw Invalid("EXPORTABLE");
        if (!data.TryGetProperty("allow_plaintext_backup", out var backup) || backup.GetBoolean())
            throw Invalid("PLAINTEXT_BACKUP");
        if (!data.TryGetProperty("latest_version", out var latest)
            || !data.TryGetProperty("min_available_version", out var minimumAvailable)
            || binding.KeyVersion > latest.GetInt32()
            || (minimumAvailable.GetInt32() > 0
                && binding.KeyVersion < minimumAvailable.GetInt32()))
            throw Invalid("VERSION");
        if (data.TryGetProperty("min_encryption_version", out var minimum)
            && minimum.GetInt32() > binding.KeyVersion)
            throw Invalid("MIN_VERSION");
    }

    private async Task<JsonDocument> PostAsync(
        string path,
        object payload,
        CancellationToken cancellationToken)
    {
        var token = await session.GetTokenAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await transport.PostAsync(path, payload, token, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OpenBaoTransportException exception) when (exception.StatusCode is 401 or 403)
        {
            session.Invalidate();
            token = await session.GetTokenAsync(cancellationToken).ConfigureAwait(false);
            return await transport.PostAsync(path, payload, token, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task<JsonDocument> GetAsync(string path, CancellationToken cancellationToken)
    {
        var token = await session.GetTokenAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await transport.GetAsync(path, token, cancellationToken).ConfigureAwait(false);
        }
        catch (OpenBaoTransportException exception) when (exception.StatusCode is 401 or 403)
        {
            session.Invalidate();
            token = await session.GetTokenAsync(cancellationToken).ConfigureAwait(false);
            return await transport.GetAsync(path, token, cancellationToken).ConfigureAwait(false);
        }
    }

    public void Dispose() => transport.Dispose();
}

internal sealed class OpenBaoContentCommitmentService : IContentCommitmentService, IDisposable
{
    private readonly OpenBaoContentCommitmentCatalog catalog;
    private readonly Lazy<OpenBaoTransitHmacClient>? client;

    public OpenBaoContentCommitmentService(
        OpenBaoClaimProviderOptionsSet options,
        OpenBaoContentCommitmentCatalog catalog)
    {
        this.catalog = catalog;
        if (options.ContentCommitment is not null)
            client = new Lazy<OpenBaoTransitHmacClient>(
                () => new OpenBaoTransitHmacClient(options.ContentCommitment),
                LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public async ValueTask<ContentCommitmentResult> ComputeAsync(
        CommitmentKeySelector selector,
        ReadOnlyMemory<byte> lpPayload,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selector);
        cancellationToken.ThrowIfCancellationRequested();
        var binding = catalog.Find(selector.KeyId, selector.KeyVersion);
        if (binding is null || client is null)
            return ContentCommitmentResult.Failed(ContentCommitmentFailure.ProviderFailure);
        try
        {
            var result = await client.Value.ComputeAsync(binding, lpPayload, cancellationToken)
                .ConfigureAwait(false);
            return ContentCommitmentResult.Success(result);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return ContentCommitmentResult.Failed(ContentCommitmentFailure.ProviderFailure);
        }
    }

    internal async Task ValidateAsync(
        OpenBaoHmacKeyBinding binding,
        CancellationToken cancellationToken)
    {
        if (client is null)
            throw new InvalidOperationException("OPENBAO_CONTENT_COMMITMENT_NOT_CONFIGURED");
        await client.Value.ValidateAsync(binding, cancellationToken).ConfigureAwait(false);
    }

    internal Task<bool> CanComputeAsync(
        OpenBaoHmacKeyBinding binding,
        CancellationToken cancellationToken) =>
        client is null
            ? throw new InvalidOperationException("OPENBAO_CONTENT_COMMITMENT_NOT_CONFIGURED")
            : client.Value.CanComputeAsync(binding, cancellationToken);

    public void Dispose()
    {
        if (client?.IsValueCreated == true)
            client.Value.Dispose();
    }
}

internal sealed class OpenBaoSubjectRefTokenService : ISubjectRefTokenService, IDisposable
{
    private readonly OpenBaoSubjectRefTokenCatalog catalog;
    private readonly Lazy<OpenBaoTransitHmacClient>? client;

    public OpenBaoSubjectRefTokenService(
        OpenBaoClaimProviderOptionsSet options,
        OpenBaoSubjectRefTokenCatalog catalog)
    {
        this.catalog = catalog;
        if (options.SubjectRefToken is not null)
            client = new Lazy<OpenBaoTransitHmacClient>(
                () => new OpenBaoTransitHmacClient(options.SubjectRefToken),
                LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public async ValueTask<SubjectRefTokenResult> ComputeAsync(
        SubjectTokenKeySelector selector,
        ReadOnlyMemory<byte> lpPayload,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selector);
        cancellationToken.ThrowIfCancellationRequested();
        var binding = catalog.Find(selector.KeyId, selector.KeyVersion);
        if (binding is null || client is null)
            return SubjectRefTokenResult.Failed(SubjectRefTokenFailure.ProviderFailure);
        try
        {
            var result = await client.Value.ComputeAsync(binding, lpPayload, cancellationToken)
                .ConfigureAwait(false);
            return SubjectRefTokenResult.Success(result);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return SubjectRefTokenResult.Failed(SubjectRefTokenFailure.ProviderFailure);
        }
    }

    internal async Task ValidateAsync(
        OpenBaoHmacKeyBinding binding,
        CancellationToken cancellationToken)
    {
        if (client is null)
            throw new InvalidOperationException("OPENBAO_SUBJECT_TOKEN_NOT_CONFIGURED");
        await client.Value.ValidateAsync(binding, cancellationToken).ConfigureAwait(false);
    }

    internal Task<bool> CanComputeAsync(
        OpenBaoHmacKeyBinding binding,
        CancellationToken cancellationToken) =>
        client is null
            ? throw new InvalidOperationException("OPENBAO_SUBJECT_TOKEN_NOT_CONFIGURED")
            : client.Value.CanComputeAsync(binding, cancellationToken);

    public void Dispose()
    {
        if (client?.IsValueCreated == true)
            client.Value.Dispose();
    }
}

public sealed class RawExportClaimProviderReadinessException(
    string code,
    Exception? innerException = null) : Exception(code, innerException)
{
    public string Code { get; } = code;
}

public sealed class RawExportClaimProviderReadinessValidator
{
    public const string ConfigurationInvalid =
        OpenBaoClaimProviderOptionsSet.ConfigurationInvalid;
    public const string SeparationInvalid =
        OpenBaoClaimProviderOptionsSet.SeparationInvalid;
    public const string ProviderInvalid = "PROD_RAW_EXPORT_CLAIM_PROVIDER_INVALID";

    private readonly OpenBaoClaimProviderOptionsSet options;
    private readonly OpenBaoContentCommitmentCatalog contentCatalog;
    private readonly OpenBaoSubjectRefTokenCatalog subjectCatalog;
    private readonly OpenBaoContentCommitmentService content;
    private readonly OpenBaoSubjectRefTokenService subject;

    internal RawExportClaimProviderReadinessValidator(
        OpenBaoClaimProviderOptionsSet options,
        OpenBaoContentCommitmentCatalog contentCatalog,
        OpenBaoSubjectRefTokenCatalog subjectCatalog,
        OpenBaoContentCommitmentService content,
        OpenBaoSubjectRefTokenService subject)
    {
        this.options = options;
        this.contentCatalog = contentCatalog;
        this.subjectCatalog = subjectCatalog;
        this.content = content;
        this.subject = subject;
    }

    public async Task ValidateAsync(CancellationToken cancellationToken)
    {
        if (!options.IsValid)
            throw new RawExportClaimProviderReadinessException(
                options.ErrorCode ?? ConfigurationInvalid);
        var activeContent = contentCatalog.Find(
                options.ActiveContentSelector!.KeyId,
                options.ActiveContentSelector.KeyVersion);
        var activeSubject = subjectCatalog.Find(
                options.ActiveSubjectSelector!.KeyId,
                options.ActiveSubjectSelector.KeyVersion);
        if (activeContent is null || activeSubject is null)
            throw new RawExportClaimProviderReadinessException(ConfigurationInvalid);
        try
        {
            foreach (var binding in contentCatalog.All)
                await content.ValidateAsync(binding, cancellationToken).ConfigureAwait(false);
            foreach (var binding in subjectCatalog.All)
                await subject.ValidateAsync(binding, cancellationToken).ConfigureAwait(false);
            var contentCanUseSubject = await content.CanComputeAsync(
                activeSubject, cancellationToken).ConfigureAwait(false);
            var subjectCanUseContent = await subject.CanComputeAsync(
                activeContent, cancellationToken).ConfigureAwait(false);
            RequirePolicyIsolation(contentCanUseSubject, subjectCanUseContent);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (RawExportClaimProviderReadinessException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new RawExportClaimProviderReadinessException(
                ProviderInvalid,
                exception);
        }
    }

    internal static void RequirePolicyIsolation(
        bool contentCredentialCanUseSubjectKey,
        bool subjectCredentialCanUseContentKey)
    {
        if (contentCredentialCanUseSubjectKey || subjectCredentialCanUseContentKey)
            throw new RawExportClaimProviderReadinessException(SeparationInvalid);
    }
}

public static class OpenBaoClaimProviderServiceCollectionExtensions
{
    public static IServiceCollection AddTagEkycProductionRawExportClaimProviders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        if (services.Any(descriptor =>
                descriptor.ImplementationType == typeof(FixtureContentCommitmentCatalog)
                || descriptor.ImplementationType == typeof(InProcessContentCommitmentService)))
            throw new InvalidOperationException(
                RawExportClaimProviderProductionGuard.FixtureContentCommitment);
        if (services.Any(descriptor =>
                descriptor.ServiceType == typeof(FixtureSubjectTokenCatalog)))
            throw new InvalidOperationException(
                RawExportClaimProviderProductionGuard.FixtureSubjectRefToken);
        RequireCompatibleRegistration<IContentCommitmentService,
            OpenBaoContentCommitmentService>(
            services,
            RawExportClaimProviderProductionGuard.ProvidersMissing);
        RequireCompatibleRegistration<ISubjectRefTokenService,
            OpenBaoSubjectRefTokenService>(
            services,
            RawExportClaimProviderProductionGuard.ProvidersMissing);
        services.TryAddSingleton(_ => OpenBaoClaimProviderOptionsSet.Resolve(configuration));
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton(sp => new OpenBaoContentCommitmentCatalog(
            sp.GetRequiredService<OpenBaoClaimProviderOptionsSet>(),
            sp.GetRequiredService<TimeProvider>()));
        services.TryAddSingleton(sp => new OpenBaoSubjectRefTokenCatalog(
            sp.GetRequiredService<OpenBaoClaimProviderOptionsSet>(),
            sp.GetRequiredService<TimeProvider>()));
        services.TryAddSingleton<IContentCommitmentService,
            OpenBaoContentCommitmentService>();
        services.TryAddSingleton<ISubjectRefTokenService,
            OpenBaoSubjectRefTokenService>();
        services.TryAddSingleton(sp => new RawExportClaimProviderReadinessValidator(
            sp.GetRequiredService<OpenBaoClaimProviderOptionsSet>(),
            sp.GetRequiredService<OpenBaoContentCommitmentCatalog>(),
            sp.GetRequiredService<OpenBaoSubjectRefTokenCatalog>(),
            (OpenBaoContentCommitmentService)sp.GetRequiredService<IContentCommitmentService>(),
            (OpenBaoSubjectRefTokenService)sp.GetRequiredService<ISubjectRefTokenService>()));
        return services;
    }

    private static void RequireCompatibleRegistration<TService, TProduction>(
        IServiceCollection services,
        string incompatibleRegistrationCode)
        where TProduction : class, TService
    {
        var registrations = services
            .Where(descriptor => descriptor.ServiceType == typeof(TService))
            .ToArray();
        if (registrations.Length == 0
            || (registrations.Length == 1
                && registrations[0].Lifetime == ServiceLifetime.Singleton
                && registrations[0].ImplementationType == typeof(TProduction)))
            return;
        throw new InvalidOperationException(incompatibleRegistrationCode);
    }
}
