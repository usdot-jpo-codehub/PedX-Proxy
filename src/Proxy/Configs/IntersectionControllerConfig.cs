namespace Proxy.Configs;

// ReSharper disable once ClassNeverInstantiated.Global
public record IntersectionControllerConfig
{
    public string Type { get; init; } = null!;
    public string Address { get; init; } = null!;
    
    public TimeSpan CacheLimit { get; init; } = TimeSpan.FromMinutes(15);
}