using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace TagEkyc.Infrastructure.RawExport;

public enum RecipientPackageReferenceTopology
{
    Disabled,
    PostgresDurable,
    Invalid,
}

public sealed record RecipientPackageReferenceKeyIdentity(string KeyId, int KeyVersion);

public sealed record RecipientPackageReferenceOptions(
    RecipientPackageReferenceTopology Topology,
    string? DatabaseConnectionString,
    RecipientPackageReferenceKeyIdentity? ActiveKey,
    IReadOnlyList<RecipientPackageReferenceKeyIdentity> AcceptedKeys,
    bool IsSyntacticallyValid)
{
    public const string SectionPath = "TagEkyc:RawExport:PackageReference";
    private static readonly Regex KeyIdGrammar = new(
        "^[a-z0-9][a-z0-9._-]{0,63}$", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    public static RecipientPackageReferenceOptions Resolve(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var section = configuration.GetSection(SectionPath);
        var topology = section["Topology"];
        if (topology is null or "" or "Disabled")
        {
            var clean = section.GetChildren().All(child => child.Key == "Topology");
            return new(RecipientPackageReferenceTopology.Disabled, null, null, [], clean);
        }
        if (!string.Equals(topology, "PostgresDurable", StringComparison.Ordinal))
            return Invalid();

        var allowedRoot = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "Topology", "DatabaseConnectionString", "ActiveCursorKeyId", "ActiveCursorKeyVersion", "AcceptedCursorKeys", "CursorKeys" };
        if (section.GetChildren().Any(child => !allowedRoot.Contains(child.Key))) return Invalid();

        var database = section["DatabaseConnectionString"];
        var activeId = section["ActiveCursorKeyId"];
        if (!ValidKeyId(activeId) || !ParseVersion(section["ActiveCursorKeyVersion"], out var activeVersion)
            || string.IsNullOrWhiteSpace(database)) return Invalid();

        var acceptedSection = section.GetSection("AcceptedCursorKeys");
        if (acceptedSection.Value is not null) return Invalid();
        var children = acceptedSection.GetChildren().ToArray();
        if (children.Length is < 1 or > 4) return Invalid();
        var accepted = new List<RecipientPackageReferenceKeyIdentity>(children.Length);
        for (var index = 0; index < children.Length; index++)
        {
            var child = children[index];
            if (!string.Equals(child.Key, index.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)
                || child.GetChildren().Any(value => value.Key is not ("KeyId" or "KeyVersion"))
                || child.GetChildren().Count() != 2)
                return Invalid();
            var id = child["KeyId"];
            if (!ValidKeyId(id) || !ParseVersion(child["KeyVersion"], out var version)) return Invalid();
            accepted.Add(new(id!, version));
        }
        if (accepted.Distinct().Count() != accepted.Count) return Invalid();
        var active = new RecipientPackageReferenceKeyIdentity(activeId!, activeVersion);
        if (accepted.Count(value => value == active) != 1) return Invalid();

        var materialLeaves = new HashSet<(string, int)>();
        foreach (var idNode in section.GetSection("CursorKeys").GetChildren())
        {
            if (!ValidKeyId(idNode.Key) || idNode.Value is not null) return Invalid();
            foreach (var versionNode in idNode.GetChildren())
            {
                if (!ParseVersion(versionNode.Key, out var version) || versionNode.GetChildren().Any()
                    || string.IsNullOrWhiteSpace(versionNode.Value)
                    || !materialLeaves.Add((idNode.Key, version))) return Invalid();
            }
        }
        if (materialLeaves.Count != accepted.Count
            || accepted.Any(value => !materialLeaves.Contains((value.KeyId, value.KeyVersion)))) return Invalid();

        return new(RecipientPackageReferenceTopology.PostgresDurable, database, active, accepted, true);
    }

    internal static bool ValidKeyId(string? value) => value is not null && KeyIdGrammar.IsMatch(value);
    private static bool ParseVersion(string? value, out int version) =>
        int.TryParse(value, System.Globalization.NumberStyles.None,
            System.Globalization.CultureInfo.InvariantCulture, out version) && version > 0;
    private static RecipientPackageReferenceOptions Invalid() =>
        new(RecipientPackageReferenceTopology.Invalid, null, null, [], false);

    public override string ToString() =>
        $"RecipientPackageReferenceOptions {{ Topology = {Topology}, KeyMaterial = [REDACTED] }}";
}
