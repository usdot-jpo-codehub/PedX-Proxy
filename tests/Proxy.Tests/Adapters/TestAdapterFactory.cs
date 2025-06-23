using Microsoft.Extensions.Options;
using Moq;
using Proxy.Adapters;
using Proxy.Configs;
using Proxy.Services;
using System;
using System.Threading.Tasks;

namespace Proxy.Tests.Adapters
{
    /// <summary>
    /// Test version of AdapterFactory that bypasses the real adapter creation
    /// and returns a mock adapter instead
    /// </summary>
    public class TestAdapterFactory : AdapterFactory
    {
        private readonly IAdapter _mockAdapter;

        public TestAdapterFactory(
            IServiceProvider serviceProvider, 
            IOptionsMonitor<ProxyConfig> proxyConfigMonitor,
            IAdapter mockAdapter) 
            : base(serviceProvider, proxyConfigMonitor)
        {
            _mockAdapter = mockAdapter;
        }

        // Override the adapter creation method to return our mock adapter
        // This bypasses the real MaxTimeAdapter creation that makes HTTP requests
        public override Task<IAdapter> GetAdapterAsync(IntersectionConfig intersectionConfig)
        {
            // First check if it's in the cache (parent implementation)
            if (TryGetAdapterFromCache(intersectionConfig.Controller.Address, out var cachedAdapter))
            {
                return Task.FromResult(cachedAdapter);
            }
            
            // Instead of creating a real adapter, use our mock
            // Add it to the cache with the address as key
            SetAdapterInCache(intersectionConfig.Controller.Address, _mockAdapter, intersectionConfig.Controller.CacheLimit);
            
            // Call InitializeAsync and return
            return _mockAdapter.InitializeAsync();
        }

        // Helper methods for testing - expose protected functionality
        public bool TryGetAdapterFromCache(string key, out IAdapter adapter)
        {
            if (TryGetFromCache(key, out IAdapter? cachedAdapter) && cachedAdapter != null)
            {
                adapter = cachedAdapter;
                return true;
            }
            
            adapter = null!;
            return false;
        }
        
        public void SetAdapterInCache(string key, IAdapter adapter, TimeSpan? expiration = null)
        {
            SetInCache(key, adapter, expiration);
        }
    }
}
