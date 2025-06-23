using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Proxy.Adapters;
using Proxy.Configs;
using Proxy.Services;
using System.Net;
using System.Net.Http;
using System.Text;

namespace Proxy.Tests.Services;

[TestClass]
public class AdapterFactoryTests
{
    private Mock<IServiceProvider> _serviceProviderMock = null!;
    private Mock<IOptionsMonitor<ProxyConfig>> _proxyConfigMonitorMock = null!;
    private Mock<IAdapter> _adapterMock = null!;
    private ProxyConfig _proxyConfig = null!;

    [TestInitialize]
    public void Setup()
    {
        // Setup mocks
        _serviceProviderMock = new Mock<IServiceProvider>();
        _proxyConfigMonitorMock = new Mock<IOptionsMonitor<ProxyConfig>>();
        _adapterMock = new Mock<IAdapter>();

        // Setup adapter mock
        _adapterMock
            .Setup(a => a.InitializeAsync())
            .ReturnsAsync(_adapterMock.Object);

        // Setup test proxy configuration
        _proxyConfig = new ProxyConfig
        {
            Intersections = new Dictionary<string, IntersectionConfig>
            {
                {
                    "intersection1", new IntersectionConfig
                    {
                        Description = "Test Intersection 1",
                        Controller = new IntersectionControllerConfig
                        {
                            Type = "MaxTime",
                            Address = "controller1.example.com",
                            CacheLimit = TimeSpan.FromMinutes(10)
                        },
                        Crossings = new Dictionary<string, IntersectionCrossingConfig>
                        {
                            {
                                "crossing1", new IntersectionCrossingConfig
                                {
                                    Description = "Test Crossing 1",
                                    Phase = 2
                                }
                            }
                        }
                    }
                },
                {
                    "intersection2", new IntersectionConfig
                    {
                        Description = "Test Intersection 2",
                        Controller = new IntersectionControllerConfig
                        {
                            Type = "MaxTime",
                            Address = "controller2.example.com",
                            CacheLimit = TimeSpan.FromMinutes(10)
                        },
                        Crossings = new Dictionary<string, IntersectionCrossingConfig>
                        {
                            {
                                "crossing2", new IntersectionCrossingConfig
                                {
                                    Description = "Test Crossing 2",
                                    Phase = 4
                                }
                            }
                        }
                    }
                }
            }
        };

        // Setup proxy config monitor mock
        _proxyConfigMonitorMock
            .Setup(m => m.CurrentValue)
            .Returns(_proxyConfig);
            
        // Setup service provider to return adapter when requested
        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(MaxTimeAdapter)))
            .Returns(_adapterMock.Object);
    }

    [TestMethod]
    public void GetIntersections_ShouldReturnAllIntersections()
    {
        // Arrange
        var adapterFactory = new AdapterFactory(_serviceProviderMock.Object, _proxyConfigMonitorMock.Object);

        // Act
        var intersections = adapterFactory.GetIntersections().ToList();

        // Assert
        Assert.AreEqual(2, intersections.Count);
        Assert.AreEqual("intersection1", intersections[0].Id);
        Assert.AreEqual("Test Intersection 1", intersections[0].Description);
        Assert.AreEqual("intersection2", intersections[1].Id);
        Assert.AreEqual("Test Intersection 2", intersections[1].Description);
    }

    [TestMethod]
    public void GetIntersection_WithValidId_ShouldReturnIntersection()
    {
        // Arrange
        var adapterFactory = new AdapterFactory(_serviceProviderMock.Object, _proxyConfigMonitorMock.Object);

        // Act
        var intersection = adapterFactory.GetIntersection("intersection1");

        // Assert
        Assert.IsNotNull(intersection);
        Assert.AreEqual("intersection1", intersection.Id);
        Assert.AreEqual("Test Intersection 1", intersection.Description);
    }

    [TestMethod]
    [ExpectedException(typeof(KeyNotFoundException))]
    public void GetIntersection_WithInvalidId_ShouldThrowException()
    {
        // Arrange
        var adapterFactory = new AdapterFactory(_serviceProviderMock.Object, _proxyConfigMonitorMock.Object);

        // Act & Assert - This should throw an exception
        adapterFactory.GetIntersection("invalid-intersection-id");
    }

    [TestMethod]
    public async Task GetAdapterAsync_WithValidIntersectionId_ShouldReturnAdapter()
    {
        // Arrange
        var adapterFactory = new AdapterFactory(_serviceProviderMock.Object, _proxyConfigMonitorMock.Object);

        // Create mocks for MaxTimeAdapter dependencies
        var loggerMock = new Mock<ILogger<MaxTimeAdapter>>();
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        
        // Create a mock HttpMessageHandler to handle HTTP requests
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(
                    "[{\"Name\":\"activePedestrianDetectorPlan\",\"Data\":{\"0\":1}}," +
                    "{\"Name\":\"auxPedDetectorCallPhase-1\",\"Data\":{\"0\":2,\"1\":4}}," +
                    "{\"Name\":\"auxPedDetectorButtonPushTime-1\",\"Data\":{\"0\":35,\"1\":35}}," +
                    "{\"Name\":\"cabinetIOModuleType\",\"Data\":{\"0\":2}}," +
                    "{\"Name\":\"cabinetInputPointControlType-1\",\"Data\":{\"0\":6,\"1\":6}}," +
                    "{\"Name\":\"cabinetInputPointControlIndex-1\",\"Data\":{\"0\":1,\"1\":2}}]",
                    Encoding.UTF8,
                    "application/json"
                )
            });

        // Create an HttpClient with the mocked handler
        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        httpClient.BaseAddress = new Uri($"http://{_proxyConfig.Intersections["intersection1"].Controller.Address}/maxtime/api/");
        
        // Setup HttpClientFactory to return our mocked client
        httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Setup service provider to return necessary dependencies
        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(ILogger<MaxTimeAdapter>)))
            .Returns(loggerMock.Object);
            
        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IHttpClientFactory)))
            .Returns(httpClientFactoryMock.Object);

        // Get the intersection config
        var intersectionConfig = _proxyConfig.Intersections["intersection1"];
        
        // Setup service provider to return a real MaxTimeAdapter with our mocked dependencies
        var adapterType = typeof(MaxTimeAdapter);
        _serviceProviderMock
            .Setup(sp => sp.GetService(adapterType))
            .Returns(() => new MaxTimeAdapter(loggerMock.Object, httpClientFactoryMock.Object, intersectionConfig));

        // Act
        var adapter = await adapterFactory.GetAdapterAsync("intersection1");

        // Assert
        Assert.IsNotNull(adapter);
        Assert.IsInstanceOfType(adapter, typeof(MaxTimeAdapter));
    }

    [TestMethod]
    [ExpectedException(typeof(KeyNotFoundException))]
    public async Task GetAdapterAsync_WithInvalidIntersectionId_ShouldThrowException()
    {
        // Arrange
        var adapterFactory = new AdapterFactory(_serviceProviderMock.Object, _proxyConfigMonitorMock.Object);

        // Act & Assert - This should throw an exception
        await adapterFactory.GetAdapterAsync("invalid-intersection-id");
    }

    [TestMethod]
    public async Task GetAdapterAsync_CachesAdapters()
    {
        // Arrange
        // Create a mock HttpMessageHandler to control HTTP responses
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(
                    "[{\"Name\":\"activePedestrianDetectorPlan\",\"Data\":{\"0\":1}}," +
                    "{\"Name\":\"auxPedDetectorCallPhase-1\",\"Data\":{\"0\":2,\"1\":4}}," +
                    "{\"Name\":\"auxPedDetectorButtonPushTime-1\",\"Data\":{\"0\":35,\"1\":35}}," +
                    "{\"Name\":\"cabinetIOModuleType\",\"Data\":{\"0\":2}}," +
                    "{\"Name\":\"cabinetInputPointControlType-1\",\"Data\":{\"0\":6,\"1\":6}}," +
                    "{\"Name\":\"cabinetInputPointControlIndex-1\",\"Data\":{\"0\":1,\"1\":2}}]",
                    Encoding.UTF8,
                    "application/json"
                )
            });

        // Create a real HttpClient with the mocked handler
        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        
        // Create and setup HttpClientFactory mock
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Create a logger mock
        var loggerMock = new Mock<ILogger<MaxTimeAdapter>>();

        // Setup service provider to return our mocked dependencies
        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(ILogger<MaxTimeAdapter>)))
            .Returns(loggerMock.Object);

        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IHttpClientFactory)))
            .Returns(httpClientFactoryMock.Object);

        // Configure proxy config monitor to return our test config
        _proxyConfigMonitorMock
            .Setup(m => m.CurrentValue)
            .Returns(_proxyConfig);

        // Create the adapter factory with our mocked dependencies
        var adapterFactory = new AdapterFactory(_serviceProviderMock.Object, _proxyConfigMonitorMock.Object);
        
        // Act - Call GetAdapterAsync twice with the same intersection ID
        var adapter1 = await adapterFactory.GetAdapterAsync("intersection1");
        var adapter2 = await adapterFactory.GetAdapterAsync("intersection1");

        // Assert - Both calls should return the same adapter instance
        Assert.IsNotNull(adapter1);
        Assert.IsNotNull(adapter2);
        Assert.AreSame(adapter1, adapter2);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task GetAdapterAsync_WithInvalidAdapterType_ShouldThrowException()
    {
        // Arrange
        var adapterFactory = new AdapterFactory(_serviceProviderMock.Object, _proxyConfigMonitorMock.Object);
        
        // Create a modified intersection config with invalid adapter type
        var invalidIntersectionConfig = new IntersectionConfig
        {
            Description = "Invalid Adapter Test",
            Controller = new IntersectionControllerConfig
            {
                Type = "NonExistentAdapter", // This adapter type doesn't exist
                Address = "invalid-adapter.example.com",
                CacheLimit = TimeSpan.FromMinutes(10)
            }
        };

        // Act & Assert - This should throw ArgumentException
        await adapterFactory.GetAdapterAsync(invalidIntersectionConfig);
    }
}
