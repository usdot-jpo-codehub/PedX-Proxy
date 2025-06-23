using System.Security.Claims;
using AspNetCore.Authentication.ApiKey;
using Microsoft.Extensions.Options;
using Proxy.Configs;
using Proxy.Models;

namespace Proxy.Services;

// ReSharper disable once ClassNeverInstantiated.Global
public class ApiKeyProvider(
    ILogger<ApiKeyProvider> logger,
    IOptionsSnapshot<ProxyConfig> proxyConfigSnapshot,
    IHttpContextAccessor httpContextAccessor) : IApiKeyProvider
{
    private readonly ILogger<ApiKeyProvider> _logger = logger;

    public Task<IApiKey?> ProvideAsync(string key)
    {
        if (proxyConfigSnapshot.Value.Security.ApiKeys.TryGetValue(key, out var securityKeyConfig))
        {
            var roleClaims = securityKeyConfig.Roles
                .Select(role => new Claim(ClaimTypes.Role, role))
                .ToList();

            return Task.FromResult<IApiKey?>(new ApiKey(key, securityKeyConfig.Owner, roleClaims));
        }

        var remoteIpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
        _logger.LogError("Invalid API key '{Key}' received from address '{Address}'", key, remoteIpAddress);
        
        return Task.FromResult<IApiKey?>(null);
    }
}