namespace Proxy.Configs;

public record ProxyConfig
{
    public Dictionary<string, IntersectionConfig> Intersections { get; init; } = null!;
    public SecurityConfig Security { get; init; } = null!;
}
