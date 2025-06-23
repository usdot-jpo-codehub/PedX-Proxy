using Microsoft.VisualStudio.TestTools.UnitTesting;
using Proxy.Configs;

namespace Proxy.Tests.Configs;

[TestClass]
public class SecurityKeyConfigTests
{
    [TestMethod]
    public void Owner_Get_ShouldReturnCorrectValue()
    {
        // Arrange
        var expectedOwner = "Test Owner";
        var config = new SecurityKeyConfig
        {
            Owner = expectedOwner
        };

        // Act
        var actualOwner = config.Owner;

        // Assert
        Assert.AreEqual(expectedOwner, actualOwner, "The Owner property getter should return the correct value");
    }

    [TestMethod]
    public void Roles_Get_ShouldReturnCorrectValue()
    {
        // Arrange
        var expectedRoles = new[] { "Admin", "User", "Guest" };
        var config = new SecurityKeyConfig
        {
            Roles = expectedRoles
        };

        // Act
        var actualRoles = config.Roles;

        // Assert
        CollectionAssert.AreEqual(expectedRoles, actualRoles, "The Roles property getter should return the correct value");
    }
}
