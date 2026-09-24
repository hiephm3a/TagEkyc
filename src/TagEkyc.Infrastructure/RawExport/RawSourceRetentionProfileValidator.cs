using System.Globalization;
using Microsoft.Extensions.Configuration;
using TagEkyc.Application.CaptureRuntime;

namespace TagEkyc.Infrastructure.RawExport;

// A closed, server-authored snapshot. Invalid input never becomes a default profile.
public sealed class RawSourceRetentionProfileValidator : IRawSourceRetentionProfileProvider
{
    private static readonly string[] Members =
    [
        "ClientApplicationId", "PolicyId", "PolicyVersion", "RawClasses",
        "ControllerIdentity", "StableDataScopeId", "RetentionPolicyId",
        "RetentionPolicyVersion", "RetentionClass", "RevocationPolicyId",
        "PurgePolicyId", "LegalHoldPolicyId", "MaximumRetentionSeconds"
    ];

    private readonly Dictionary<Guid, RawSourceRetentionProfile> profiles = [];
    public bool IsReady { get; }
    public IReadOnlySet<Guid> ClientApplicationIds => profiles.Keys.ToHashSet();

    public RawSourceRetentionProfileValidator(IConfiguration configuration)
    {
        var section = configuration.GetSection("RawSourceRetentionProfiles");
        if (section.Value is not null || section.GetChildren().Any(c => c.Key != "Entries")) return;
        var entries = section.GetSection("Entries");
        if (entries.Value is not null) return;
        var rows = entries.GetChildren().ToArray();
        for (var i = 0; i < rows.Length; i++)
        {
            var row = rows[i];
            if (row.Key != i.ToString(CultureInfo.InvariantCulture) || row.Value is not null) return;
            var fields = row.GetChildren().ToArray();
            if (fields.Length != Members.Length || fields.Any(f => !Members.Contains(f.Key, StringComparer.Ordinal))) return;
            if (fields.Any(f => f.Key != "RawClasses" && (f.Value is null || f.GetChildren().Any()))) return;
            var classes = row.GetSection("RawClasses");
            var classRows = classes.GetChildren().ToArray();
            if (classes.Value is not null || classRows.Length != 2 ||
                classRows[0].Key != "0" || classRows[1].Key != "1" ||
                classRows.Any(c => c.Value is null || c.GetChildren().Any())) return;
            if (!Guid.TryParse(row["ClientApplicationId"], out var client) ||
                !Guid.TryParse(row["PolicyId"], out var policy) ||
                !TryInteger(row["PolicyVersion"], out var version) ||
                !TryInteger(row["RetentionPolicyVersion"], out var retentionVersion) ||
                !TryInteger(row["MaximumRetentionSeconds"], out var seconds)) return;
            var profile = new RawSourceRetentionProfile(client, policy, version,
                classRows.Select(c => c.Value!).ToArray(), row["ControllerIdentity"]!,
                row["StableDataScopeId"]!, row["RetentionPolicyId"]!, retentionVersion,
                row["RetentionClass"]!, row["RevocationPolicyId"]!, row["PurgePolicyId"]!,
                row["LegalHoldPolicyId"]!, seconds);
            if (!profile.IsValid() || !profiles.TryAdd(client, profile)) return;
        }
        // Missing/empty entries authorize no Client, but are legal in Prepared mode.
        IsReady = true;
    }

    public RawSourceRetentionProfile? Find(Guid clientApplicationId) =>
        IsReady && profiles.TryGetValue(clientApplicationId, out var value) ? value.Copy() : null;

    private static bool TryInteger(string? value, out int result) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out result);
}
