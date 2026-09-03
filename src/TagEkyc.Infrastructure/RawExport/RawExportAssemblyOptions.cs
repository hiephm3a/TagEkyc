using Microsoft.Extensions.Configuration;
using TagEkyc.Contracts.RawExport;

namespace TagEkyc.Infrastructure.RawExport;

public sealed record RawExportAssemblyOptions(RawExportAssemblyTopology Topology)
{
    public const string SectionName = "TagEkyc:RawExport:Assembly";
    public const string ConfigInvalid = "PROD_RAW_EXPORT_ASSEMBLY_CONFIG_INVALID";
    public const string RoleTopologyInvalid = "PROD_RAW_EXPORT_ASSEMBLY_ROLE_TOPOLOGY_INVALID";
    public const string AuthenticatorUnavailable = "PROD_RAW_EXPORT_ASSEMBLY_AUTHENTICATOR_UNAVAILABLE";
    public const string C2Unavailable = "PROD_RAW_EXPORT_ASSEMBLY_C2_UNAVAILABLE";

    public static RawExportAssemblyOptions Resolve(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var value = configuration[$"{SectionName}:Topology"];
        return new(value switch
        {
            null or "" or "Disabled" => RawExportAssemblyTopology.Disabled,
            "FixtureProof" => RawExportAssemblyTopology.FixtureProof,
            _ => RawExportAssemblyTopology.Invalid,
        });
    }
}
