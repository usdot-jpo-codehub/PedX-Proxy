namespace Proxy.Configs;

// ReSharper disable once ClassNeverInstantiated.Global
public class SecurityConfig
{
    public Dictionary<string, SecurityKeyConfig> ApiKeys { get; set; } = null!;
}