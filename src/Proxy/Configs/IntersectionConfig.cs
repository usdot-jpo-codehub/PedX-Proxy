namespace Proxy.Configs;

// ReSharper disable once ClassNeverInstantiated.Global
public record IntersectionConfig
{
    public string Description { get; init; } = "";
    public IntersectionControllerConfig Controller { get; init; } = null!;
    public Dictionary<string, IntersectionCrossingConfig> Crossings { get; init; } = null!;
}
