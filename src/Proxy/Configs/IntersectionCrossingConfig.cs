namespace Proxy.Configs;

// ReSharper disable once ClassNeverInstantiated.Global
public record IntersectionCrossingConfig
{
    public string Description { get; init; } = "";
    public int Phase { get; init; } = default;
}