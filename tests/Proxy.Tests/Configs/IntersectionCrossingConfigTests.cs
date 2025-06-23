using Microsoft.VisualStudio.TestTools.UnitTesting;
using Proxy.Configs;

namespace Proxy.Tests.Configs;

[TestClass]
public class IntersectionCrossingConfigTests
{
    [TestMethod]
    public void Description_Get_ShouldReturnCorrectValue()
    {
        // Arrange
        var expectedDescription = "Test Description";
        var config = new IntersectionCrossingConfig
        {
            Description = expectedDescription
        };

        // Act
        var actualDescription = config.Description;

        // Assert
        Assert.AreEqual(expectedDescription, actualDescription, "The Description property getter should return the correct value");
    }

    [TestMethod]
    public void Description_Get_ShouldReturnEmptyStringByDefault()
    {
        // Arrange
        var config = new IntersectionCrossingConfig();

        // Act
        var description = config.Description;

        // Assert
        Assert.AreEqual(string.Empty, description, "The Description property should default to an empty string");
    }
}
