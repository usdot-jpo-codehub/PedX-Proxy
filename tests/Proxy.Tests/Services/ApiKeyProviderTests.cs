using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Proxy.Configs;
using Proxy.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Proxy.Tests.Services;

[TestClass]
public class ApiKeyProviderTests
{
    private Mock<ILogger<ApiKeyProvider>> _loggerMock;
    private Mock<IOptionsSnapshot<ProxyConfig>> _optionsSnapshotMock;
    private Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private ApiKeyProvider _apiKeyProvider;
    
    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<ApiKeyProvider>>();
        _optionsSnapshotMock = new Mock<IOptionsSnapshot<ProxyConfig>>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        
        _apiKeyProvider = new ApiKeyProvider(
            _loggerMock.Object,
            _optionsSnapshotMock.Object,
            _httpContextAccessorMock.Object);
    }
    
    [TestMethod]
    public async Task ProvideAsync_WithValidKey_ShouldReturnApiKey()
    {
        // Arrange
        var testKey = "test-key";
        var testOwner = "Test Owner";
        var testRoles = new[] { "Admin", "User" };
        
        var securityKeyConfig = new SecurityKeyConfig
        {
            Owner = testOwner,
            Roles = testRoles
        };
        
        var securityConfig = new SecurityConfig
        {
            ApiKeys = new Dictionary<string, SecurityKeyConfig>
            {
                { testKey, securityKeyConfig }
            }
        };
        
        var proxyConfig = new ProxyConfig
        {
            Security = securityConfig
        };
        
        _optionsSnapshotMock.Setup(x => x.Value).Returns(proxyConfig);
        
        // Act
        var result = await _apiKeyProvider.ProvideAsync(testKey);
        
        // Assert
        Assert.IsNotNull(result, "Result should not be null for a valid key");
        Assert.AreEqual(testKey, result.Key, "The Key property should match the input key");
        Assert.AreEqual(testOwner, result.OwnerName, "The OwnerName property should match the configured owner");
        Assert.AreEqual(testRoles.Length, result.Claims.Count, "The Claims collection should contain the correct number of claims");
        
        foreach (var role in testRoles)
        {
            Assert.IsTrue(result.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == role), 
                $"The Claims collection should contain a role claim with value '{role}'");
        }
    }
    
    [TestMethod]
    public async Task ProvideAsync_WithInvalidKey_ShouldReturnNull()
    {
        // Arrange
        var testKey = "invalid-key";
        
        var securityConfig = new SecurityConfig
        {
            ApiKeys = new Dictionary<string, SecurityKeyConfig>()
        };
        
        var proxyConfig = new ProxyConfig
        {
            Security = securityConfig
        };
        
        _optionsSnapshotMock.Setup(x => x.Value).Returns(proxyConfig);
        
        // Setup mock HTTP context
        var mockContext = new Mock<HttpContext>();
        var mockConnection = new Mock<ConnectionInfo>();
        mockConnection.Setup(c => c.RemoteIpAddress).Returns(System.Net.IPAddress.Parse("127.0.0.1"));
        mockContext.Setup(c => c.Connection).Returns(mockConnection.Object);
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(mockContext.Object);
        
        // Act
        var result = await _apiKeyProvider.ProvideAsync(testKey);
        
        // Assert
        Assert.IsNull(result, "Result should be null for an invalid key");
        
        // Verify that the error was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once,
            "An error should be logged for an invalid key");
    }
}
