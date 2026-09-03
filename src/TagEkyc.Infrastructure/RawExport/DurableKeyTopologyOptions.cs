using Microsoft.Extensions.Configuration;

namespace TagEkyc.Infrastructure.RawExport;

public enum DurableKeyTopology
{
    Invalid,
    ProcessLocalFixture,
    DurableKey,
}

public sealed record DurableKeyTopologyOptions(DurableKeyTopology Topology)
{
    public const string ConfigurationPath = "TagEkyc:RawExport:AttemptKey:Topology";

    public static DurableKeyTopologyOptions Resolve(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var value = configuration[ConfigurationPath]?.Trim();
        return new(value switch
        {
            "ProcessLocalFixture" => DurableKeyTopology.ProcessLocalFixture,
            "DurableKey" => DurableKeyTopology.DurableKey,
            _ => DurableKeyTopology.Invalid,
        });
    }
}
