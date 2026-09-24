using System.Globalization;
using Microsoft.Extensions.Configuration;
using TagEkyc.Contracts.RawExport;

namespace TagEkyc.Infrastructure.RawExport;

public sealed record RawExportAssemblyOptions(
    RawExportAssemblyTopology Topology,
    int? PollIntervalMilliseconds = null)
{
    public const string SectionName = "TagEkyc:RawExport:Assembly";
    public const string ConfigInvalid = "PROD_RAW_EXPORT_ASSEMBLY_CONFIG_INVALID";
    public const string RoleTopologyInvalid = "PROD_RAW_EXPORT_ASSEMBLY_ROLE_TOPOLOGY_INVALID";
    public const string AuthenticatorUnavailable = "PROD_RAW_EXPORT_ASSEMBLY_AUTHENTICATOR_UNAVAILABLE";
    public const string C2Unavailable = "PROD_RAW_EXPORT_ASSEMBLY_C2_UNAVAILABLE";
    public const string WorkSourceUnavailable = "PROD_RAW_EXPORT_ASSEMBLY_WORK_SOURCE_UNAVAILABLE";

    public static RawExportAssemblyOptions Resolve(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var value = configuration[$"{SectionName}:Topology"];
        var topology = value switch
        {
            null or "" or "Disabled" => RawExportAssemblyTopology.Disabled,
            "FixtureProof" => RawExportAssemblyTopology.FixtureProof,
            "DurableWorker" => RawExportAssemblyTopology.DurableWorker,
            _ => RawExportAssemblyTopology.Invalid,
        };
        var intervalText = configuration[$"{SectionName}:PollIntervalMilliseconds"];
        int? interval = int.TryParse(intervalText, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
        return new(topology, interval);
    }
}
