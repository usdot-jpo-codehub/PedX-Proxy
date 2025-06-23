using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Proxy.Adapters;
using Proxy.Configs;
using Proxy.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Proxy.Tests.Adapters
{
    [TestClass]
    public class AdapterFactoryConfigChangeTests
    {
        private Mock<IServiceProvider> _serviceProviderMock;
        private Mock<IOptionsMonitor<ProxyConfig>> _optionsMonitorMock;
        private AdapterFactory _adapterFactory;
        private ProxyConfig _currentConfig;

        [TestInitialize]
        public void Setup()
        {
            // Setup service provider mock that can handle ActivatorUtilities
            _serviceProviderMock = new Mock<IServiceProvider>();
            
            // Make the service provider also implement IServiceProviderIsService
            _serviceProviderMock
                .As<IServiceProviderIsService>()
                .Setup(x => x.IsService(It.IsAny<Type>()))
                .Returns(false);  // Default to false for IsService
                
            // Setup mocks for MaxTimeAdapter dependencies
            var loggerMock = new Mock<ILogger<MaxTimeAdapter>>();
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            
            // Configure the HttpClientFactory mock to return a real HttpClient when CreateClient is called
            httpClientFactoryMock
                .Setup(factory => factory.CreateClient(It.IsAny<string>()))
                .Returns(new HttpClient());
                
            // Setup the service provider to return the mocked dependencies
            _serviceProviderMock
                .Setup(sp => sp.GetService(typeof(ILogger<MaxTimeAdapter>)))
                .Returns(loggerMock.Object);
                
            _serviceProviderMock
                .Setup(sp => sp.GetService(typeof(IHttpClientFactory)))
                .Returns(httpClientFactoryMock.Object);
                
            _optionsMonitorMock = new Mock<IOptionsMonitor<ProxyConfig>>();
            
            // Initial config
            _currentConfig = new ProxyConfig
            {
                Intersections = new Dictionary<string, IntersectionConfig>
                {
                    ["test-intersection"] = new IntersectionConfig
                    {
                        Description = "Test Intersection",
                        Controller = new IntersectionControllerConfig
                        {
                            Type = "MaxTime",
                            Address = "localhost"
                        },
                        Crossings = new Dictionary<string, IntersectionCrossingConfig>()
                    }
                }
            };
            
            // Setup CurrentValue to return our config
            _optionsMonitorMock.Setup(m => m.CurrentValue).Returns(() => _currentConfig);
            _optionsMonitorMock.Setup(m => m.Get(It.IsAny<string>())).Returns(_currentConfig);
            
            // Create a custom IDisposable implementation that we can use to detect registration
            var mockDisposable = new Mock<IDisposable>();
            
            // We'll use this setup to work around the extension method issue
            // Instead of mocking OnChange directly, we create a custom IDisposable
            // that we can verify was requested
            _optionsMonitorMock
                .Setup(x => x.OnChange(It.IsAny<Action<ProxyConfig, string>>()))
                .Returns(mockDisposable.Object);
            
            _adapterFactory = new AdapterFactory(_serviceProviderMock.Object, _optionsMonitorMock.Object);
        }

        [TestMethod]
        public void Constructor_ShouldRegisterForConfigChanges()
        {
            // Assert
            // We can't directly test the registration anymore, 
            // but we can ensure that OnChange was set up to return a disposable
            _optionsMonitorMock.Verify(x => x.OnChange(It.IsAny<Action<ProxyConfig, string>>()), Times.Once);
        }

        [TestMethod]
        public async Task OnConfigChange_ShouldClearAdapterCache()
        {
            // Arrange
            var mockAdapter = new Mock<IAdapter>();
            mockAdapter.Setup(a => a.InitializeAsync()).ReturnsAsync(mockAdapter.Object);

            // Create a test-specific adapter factory that uses our mock adapter
            var testAdapterFactory = new TestAdapterFactory(
                _serviceProviderMock.Object, 
                _optionsMonitorMock.Object,
                mockAdapter.Object);
            
            // Get an adapter to populate the cache
            var adapter = await testAdapterFactory.GetAdapterAsync("test-intersection");
            
            // Verify the adapter was initialized
            mockAdapter.Verify(a => a.InitializeAsync(), Times.Once);
            
            // Act - Simulate config change
            var newConfig = new ProxyConfig
            {
                Intersections = new Dictionary<string, IntersectionConfig>
                {
                    ["test-intersection"] = new IntersectionConfig
                    {
                        Description = "Updated Intersection",
                        Controller = new IntersectionControllerConfig
                        {
                            Type = "MaxTime",
                            Address = "localhost:8080" // Changed address
                        },
                        Crossings = new Dictionary<string, IntersectionCrossingConfig>()
                    }
                }
            };
            
            // Update the current value that will be returned
            _currentConfig = newConfig;
            
            // Trigger the OnChange callback directly to simulate a config change
            var onChangeCallbacks = _optionsMonitorMock.Invocations
                .Where(i => i.Method.Name == "OnChange")
                .Select(i => i.Arguments[0])
                .OfType<Action<ProxyConfig, string>>()
                .ToList();
                
            // Execute the OnChange callback
            if (onChangeCallbacks.Any())
            {
                onChangeCallbacks.First().Invoke(_currentConfig, null);
            }

            // Act - Get the adapter again with the new config
            await testAdapterFactory.GetAdapterAsync("test-intersection");

            // Assert - Should have initialized the adapter twice
            mockAdapter.Verify(a => a.InitializeAsync(), Times.Exactly(2), 
                "The adapter should be initialized twice - once before and once after config change");
        }
    }
}
