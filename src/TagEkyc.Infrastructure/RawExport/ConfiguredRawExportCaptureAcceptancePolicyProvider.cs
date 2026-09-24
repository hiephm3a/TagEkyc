using Microsoft.Extensions.Configuration;
using TagEkyc.Application.RawExport;

namespace TagEkyc.Infrastructure.RawExport;

public sealed class ConfiguredRawExportCaptureAcceptancePolicyProvider
    : IRawExportCaptureAcceptancePolicyProvider
{
    private const string EntriesPath = "TagEkyc:RawExport:CaptureAcceptancePolicies:Entries";
    private readonly IReadOnlyDictionary<Guid, RawExportCaptureAcceptancePolicy> policies;

    public ConfiguredRawExportCaptureAcceptancePolicyProvider(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var parsed = new Dictionary<Guid, RawExportCaptureAcceptancePolicy>();
        foreach (var child in configuration.GetSection(EntriesPath).GetChildren())
        {
            var keys = child.GetChildren().Select(value => value.Key).Order(StringComparer.Ordinal).ToArray();
            if (!keys.SequenceEqual(new[]
                { "AcceptancePolicyId", "AcceptancePolicyVersion", "ClientApplicationId" }, StringComparer.Ordinal)
                || !Guid.TryParseExact(child["ClientApplicationId"], "D", out var clientId)
                || clientId == Guid.Empty
                || !int.TryParse(child["AcceptancePolicyVersion"],
                    System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture, out var version)
                || version < 1
                || !ValidPolicyId(child["AcceptancePolicyId"])
                || !parsed.TryAdd(clientId, new(clientId, child["AcceptancePolicyId"]!, version)))
                throw new InvalidOperationException("RAW_EXPORT_CAPTURE_ACCEPTANCE_POLICY_CONFIGURATION_INVALID");
        }
        policies = parsed;
    }

    public RawExportCaptureAcceptancePolicy? Find(Guid clientApplicationId) =>
        clientApplicationId != Guid.Empty && policies.TryGetValue(clientApplicationId, out var policy)
            ? policy
            : null;

    public IReadOnlySet<Guid> ClientApplicationIds => policies.Keys.ToHashSet();

    private static bool ValidPolicyId(string? value) =>
        value is { Length: >= 1 and <= 128 }
        && value.All(character => character is >= 'A' and <= 'Z'
            or >= 'a' and <= 'z' or >= '0' and <= '9' or '.' or '_' or '-');
}
