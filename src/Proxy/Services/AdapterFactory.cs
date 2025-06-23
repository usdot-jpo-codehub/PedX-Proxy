using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Proxy.Adapters;
using Proxy.Configs;
using Proxy.Models;

namespace Proxy.Services;

public class AdapterFactory : IAdapterFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionsMonitor<ProxyConfig> _proxyConfigMonitor;
    private IMemoryCache _adapterCache;

    public AdapterFactory(IServiceProvider serviceProvider, IOptionsMonitor<ProxyConfig> proxyConfigMonitor)
    {
        _serviceProvider = serviceProvider;
        _proxyConfigMonitor = proxyConfigMonitor;
        
        // Initialize adapter cache
        _adapterCache = new MemoryCache(new MemoryCacheOptions());

        // Add a change listener to reset the adapter cache on config change
        _proxyConfigMonitor.OnChange(_ =>
        {
            var adapterCache = _adapterCache;
            _adapterCache = new MemoryCache(new MemoryCacheOptions());
            adapterCache.Dispose();
        });
    }
    
    public Task<IAdapter> GetAdapterAsync(string intersectionId)
    {
        // Get current proxy config
        var proxyConfig = _proxyConfigMonitor.CurrentValue;
        
        // Check if intersection config exists in proxy config
        if (!proxyConfig.Intersections.TryGetValue(intersectionId, out var intersectionConfig))
            throw new KeyNotFoundException($"Intersection '{intersectionId}' not found.");

        // Call GetAdapterAsync with intersection config
        return GetAdapterAsync(intersectionConfig);
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public virtual Task<IAdapter> GetAdapterAsync(IntersectionConfig intersectionConfig)
    {
        // Return existing adapter if found in cache
        if (TryGetFromCache(intersectionConfig.Controller.Address, out IAdapter? existingAdapter) && existingAdapter != null)
            return Task.FromResult(existingAdapter);
        
        // Create new adapter if not found in cache
        var adapterType = Type.GetType($"Proxy.Adapters.{intersectionConfig.Controller.Type}Adapter");

        if (adapterType == null)
            throw new ArgumentException($"Adapter '{intersectionConfig.Controller.Type}' not found");

        var newAdapter = (IAdapter)ActivatorUtilities.CreateInstance(_serviceProvider, adapterType, intersectionConfig);

        // Add adapter to cache
        SetInCache(intersectionConfig.Controller.Address, newAdapter, intersectionConfig.Controller.CacheLimit);
        
        // Call InitializeAsync and return the task
        return newAdapter.InitializeAsync();
    }
    
    // Protected helper methods to make testing easier
    protected bool TryGetFromCache(string key, out IAdapter? adapter)
    {
        return _adapterCache.TryGetValue(key, out adapter);
    }
    
    protected void SetInCache(string key, IAdapter adapter, TimeSpan? expiration = null)
    {
        _adapterCache.Set(key, adapter, expiration ?? TimeSpan.FromHours(1));
    }
    
    public IEnumerable<Intersection> GetIntersections()
    {
        return _proxyConfigMonitor.CurrentValue.Intersections
            .Select(intersection => new Intersection
            {
                Id = intersection.Key,
                Description = intersection.Value.Description
            });
    }
    
    public Intersection GetIntersection(string intersectionId)
    {
        if (!_proxyConfigMonitor.CurrentValue.Intersections.TryGetValue(intersectionId, out var intersectionConfig))
            throw new KeyNotFoundException($"Intersection '{intersectionId}' not found.");

        return new Intersection
        {
            Id = intersectionId,
            Description = intersectionConfig.Description
        };
    }
}