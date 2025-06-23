namespace Proxy.Configs;

// ReSharper disable once ClassNeverInstantiated.Global
public class SecurityKeyConfig
{
    public string Owner { get; set; } = null!;
    public string[] Roles { get; set; } = null!;
}