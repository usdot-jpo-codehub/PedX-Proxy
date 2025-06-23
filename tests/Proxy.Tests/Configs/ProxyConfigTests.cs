using Microsoft.VisualStudio.TestTools.UnitTesting;
using Proxy.Configs;

namespace Proxy.Tests.Configs;

[TestClass]
public class ProxyConfigTests
{
    [TestMethod]
    public void Security_Get_ShouldReturnCorrectValue()
    {
        // Arrange
        var expectedSecurity = new SecurityConfig
        {
            ApiKeys = new Dictionary<string, SecurityKeyConfig>
            {
                {
                    "testKey", new SecurityKeyConfig
                    {
                        Owner = "Test Owner",
                        Roles = new[] { "Admin", "User" }
                    }
                }
            }
        };

        var config = new ProxyConfig
        {
            Security = expectedSecurity
        };

        // Act
        var actualSecurity = config.Security;

        // Assert
        Assert.IsNotNull(actualSecurity, "The Security property should not be null");
        Assert.AreEqual(expectedSecurity.ApiKeys.Count, actualSecurity.ApiKeys.Count, "The ApiKeys count should match");
        
        var expectedKey = expectedSecurity.ApiKeys.First();
        var actualKey = actualSecurity.ApiKeys.First();
        
        Assert.AreEqual(expectedKey.Key, actualKey.Key, "The API key should match");
        Assert.AreEqual(expectedKey.Value.Owner, actualKey.Value.Owner, "The Owner property should match");
        CollectionAssert.AreEqual(expectedKey.Value.Roles, actualKey.Value.Roles, "The Roles property should match");
    }
}
