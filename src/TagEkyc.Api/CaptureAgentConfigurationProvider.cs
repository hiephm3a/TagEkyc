using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using TagEkyc.Application;
using TagEkyc.Application.Ports;
using TagEkyc.Contracts.RawExport;

namespace TagEkyc.Api;

internal sealed class CaptureAgentConfigurationProvider(IConfiguration configuration)
    : ICaptureAgentConfigurationProvider
{
    public Task<CaptureAgentConfigurationProjection?> GetSelfAsync(
        AuthenticatedClientContext caller,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var producerId = configuration[$"TagEkyc:RawExport:CaptureAgentPrincipals:{caller.PrincipalId:N}:ProducerId"];
        return producerId is null
            ? Task.FromResult<CaptureAgentConfigurationProjection?>(null)
            : GetCurrentAsync(caller, producerId, cancellationToken);
    }

    public Task<CaptureAgentConfigurationProjection?> GetCurrentAsync(
        AuthenticatedClientContext caller,
        string producerId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(producerId) || caller.AllowedCaptureAgentIds?.Contains(producerId) != true)
            return Task.FromResult<CaptureAgentConfigurationProjection?>(null);

        var agentId = producerId;
        var section = configuration.GetSection($"TagEkyc:RawExport:CaptureAgents:{agentId}");
        if (!TryLong(section, "ConfigurationRevision", out var revision) || revision <= 0 ||
            !TryInstant(section, "EffectiveAtUtc", out var effective) ||
            !TryInstant(section, "ExpiresAtUtc", out var expires) || expires <= effective ||
            !bool.TryParse(section["RawExportEnabled"], out var enabled) ||
            !TryLong(section, "PlaintextBudgetSeconds", out var budget) || budget <= 0 ||
            !int.TryParse(section["RawExportSourceClaimSafetyMarginMilliseconds"], NumberStyles.None,
                CultureInfo.InvariantCulture, out var margin) || margin <= 0)
            return Task.FromResult<CaptureAgentConfigurationProjection?>(null);

        var document = new CaptureAgentRawExportConfiguration(revision, effective, expires, enabled, budget, margin);
        var canonical = string.Join('|', revision, effective.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            expires.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture), enabled, budget, margin, agentId);
        var etag = $"\"{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant()}\"";
        return Task.FromResult<CaptureAgentConfigurationProjection?>(new(document, etag));
    }

    private static bool TryLong(IConfiguration section, string key, out long value) =>
        long.TryParse(section[key], NumberStyles.None, CultureInfo.InvariantCulture, out value);

    private static bool TryInstant(IConfiguration section, string key, out DateTimeOffset value) =>
        DateTimeOffset.TryParseExact(section[key], "O", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out value) && value.Offset == TimeSpan.Zero;
}
