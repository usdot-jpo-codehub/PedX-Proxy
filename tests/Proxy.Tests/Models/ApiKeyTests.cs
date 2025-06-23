using Microsoft.VisualStudio.TestTools.UnitTesting;
using Proxy.Models;
using System.Security.Claims;

namespace Proxy.Tests.Models;

[TestClass]
public class ApiKeyTests
{
    [TestMethod]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange
        var expectedKey = "api-key-123";
        var expectedOwner = "Test Owner";
        var expectedClaims = new List<Claim> { new Claim("role", "admin") };

        // Act
        var apiKey = new ApiKey(expectedKey, expectedOwner, expectedClaims);

        // Assert
        Assert.AreEqual(expectedKey, apiKey.Key, "The Key property should be initialized correctly");
        Assert.AreEqual(expectedOwner, apiKey.OwnerName, "The OwnerName property should be initialized correctly");
        Assert.AreEqual(expectedClaims.Count, apiKey.Claims.Count, "The Claims collection should have the correct count");
        Assert.AreEqual(expectedClaims[0].Type, apiKey.Claims.First().Type, "The Claim type should match");
        Assert.AreEqual(expectedClaims[0].Value, apiKey.Claims.First().Value, "The Claim value should match");
    }

    [TestMethod]
    public void Constructor_WithNullClaims_ShouldInitializeEmptyClaimsCollection()
    {
        // Arrange
        var expectedKey = "api-key-123";
        var expectedOwner = "Test Owner";

        // Act
        var apiKey = new ApiKey(expectedKey, expectedOwner, null);

        // Assert
        Assert.AreEqual(expectedKey, apiKey.Key, "The Key property should be initialized correctly");
        Assert.AreEqual(expectedOwner, apiKey.OwnerName, "The OwnerName property should be initialized correctly");
        Assert.IsNotNull(apiKey.Claims, "The Claims collection should not be null");
        Assert.AreEqual(0, apiKey.Claims.Count, "The Claims collection should be empty");
    }
}
