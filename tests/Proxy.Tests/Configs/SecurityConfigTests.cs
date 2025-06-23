using Microsoft.VisualStudio.TestTools.UnitTesting;
using Proxy.Configs;

namespace Proxy.Tests.Configs;

[TestClass]
public class SecurityConfigTests
{
    [TestMethod]
    public void ApiKeys_Get_ShouldReturnCorrectValue()
    {
        // Arrange
        var expectedApiKeys = new Dictionary<string, SecurityKeyConfig>
        {
            {
                "key1", new SecurityKeyConfig
                {
                    Owner = "Owner 1",
                    Roles = new[] { "Admin" }
                }
            },
            {
                "key2", new SecurityKeyConfig
                {
                    Owner = "Owner 2",
                    Roles = new[] { "User", "Guest" }
                }
            }
        };

        var config = new SecurityConfig
        {
            ApiKeys = expectedApiKeys
        };

        // Act
        var actualApiKeys = config.ApiKeys;

        // Assert
        Assert.AreEqual(expectedApiKeys.Count, actualApiKeys.Count, "The ApiKeys property should contain the correct number of entries");
        
        foreach (var key in expectedApiKeys.Keys)
        {
            Assert.IsTrue(actualApiKeys.ContainsKey(key), $"The ApiKeys property should contain the key '{key}'");
            Assert.AreEqual(expectedApiKeys[key].Owner, actualApiKeys[key].Owner, $"The Owner property for key '{key}' should match");
            CollectionAssert.AreEqual(expectedApiKeys[key].Roles, actualApiKeys[key].Roles, $"The Roles property for key '{key}' should match");
        }
    }
}
