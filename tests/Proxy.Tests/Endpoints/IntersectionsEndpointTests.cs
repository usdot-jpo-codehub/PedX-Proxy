using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Proxy.Adapters;
using Proxy.Endpoints;
using Proxy.Models;
using Proxy.Services;
using System.Security.Claims;
using System.Security.Principal;

namespace Proxy.Tests.Endpoints;

[TestClass]
public class IntersectionsEndpointTests
{
    private Mock<ILogger<IntersectionsEndpoint>> _loggerMock;
    private Mock<IAdapterFactory> _adapterFactoryMock;
    private Mock<HttpContext> _httpContextMock;
    private Mock<ClaimsPrincipal> _userMock;
    private Mock<IIdentity> _identityMock;
    private IntersectionsEndpoint _endpoint;

    [TestInitialize]
    public void Setup()
    {
        // Setup mocks
        _loggerMock = new Mock<ILogger<IntersectionsEndpoint>>();
        _adapterFactoryMock = new Mock<IAdapterFactory>();
        _httpContextMock = new Mock<HttpContext>();
        _userMock = new Mock<ClaimsPrincipal>();
        _identityMock = new Mock<IIdentity>();

        // Setup identity mock
        _identityMock.Setup(i => i.Name).Returns("test-user");
        _identityMock.Setup(i => i.IsAuthenticated).Returns(true);

        // Setup user mock
        _userMock.Setup(u => u.Identity).Returns(_identityMock.Object);

        // Setup HttpContext mock
        _httpContextMock.Setup(c => c.User).Returns(_userMock.Object);

        // Create the endpoint
        _endpoint = new IntersectionsEndpoint(_loggerMock.Object, _adapterFactoryMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = _httpContextMock.Object
            }
        };

        // Setup test intersections
        var testIntersections = new List<Intersection>
        {
            new() { Id = "intersection1", Description = "Test Intersection 1" },
            new() { Id = "intersection2", Description = "Test Intersection 2" }
        };

        // Setup adapter factory mock
        _adapterFactoryMock
            .Setup(factory => factory.GetIntersections())
            .Returns(testIntersections);

        _adapterFactoryMock
            .Setup(factory => factory.GetIntersection("intersection1"))
            .Returns(testIntersections[0]);

        _adapterFactoryMock
            .Setup(factory => factory.GetIntersection("non-existent-intersection"))
            .Throws(new KeyNotFoundException("Intersection 'non-existent-intersection' not found."));
    }

    [TestMethod]
    public void GetIntersections_ShouldReturnAllIntersections()
    {
        // Act
        var result = _endpoint.GetIntersections();

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));

        var okResult = (OkObjectResult)result;
        Assert.AreEqual(StatusCodes.Status200OK, okResult.StatusCode);

        var intersections = (IEnumerable<Intersection>)okResult.Value;
        Assert.AreEqual(2, intersections.Count());
        Assert.AreEqual("intersection1", intersections.ElementAt(0).Id);
        Assert.AreEqual("Test Intersection 1", intersections.ElementAt(0).Description);
        Assert.AreEqual("intersection2", intersections.ElementAt(1).Id);
        Assert.AreEqual("Test Intersection 2", intersections.ElementAt(1).Description);

        // Verify that the logger was called
        _loggerMock.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Getting all intersections for user 'test-user'")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [TestMethod]
    public void GetIntersection_WithValidId_ShouldReturnIntersection()
    {
        // Act
        var result = _endpoint.GetIntersection("intersection1");

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));

        var okResult = (OkObjectResult)result;
        Assert.AreEqual(StatusCodes.Status200OK, okResult.StatusCode);

        var intersection = (Intersection)okResult.Value;
        Assert.AreEqual("intersection1", intersection.Id);
        Assert.AreEqual("Test Intersection 1", intersection.Description);

        // Verify that the logger was called
        _loggerMock.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Getting intersection 'intersection1' for user 'test-user'")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [TestMethod]
    public void GetIntersection_WithInvalidId_ShouldReturnNotFound()
    {
        // Act
        var result = _endpoint.GetIntersection("non-existent-intersection");

        // Assert
        Assert.IsInstanceOfType(result, typeof(NotFoundObjectResult));

        var notFoundResult = (NotFoundObjectResult)result;
        Assert.AreEqual(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        Assert.AreEqual("Intersection 'non-existent-intersection' not found.", notFoundResult.Value);

        // Verify that the logger was called for the error
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task GetCrossings_ShouldReturnAllCrossingsForIntersection()
    {
        // Arrange
        var testCrossings = new[]
        {
            new Crossing
            {
                Id = "crossing1",
                Description = "Test Crossing 1",
                Phase = 2,
                Signal = Crossing.SignalState.Stop,
                Calls = Crossing.CallState.None
            },
            new Crossing
            {
                Id = "crossing2",
                Description = "Test Crossing 2",
                Phase = 4,
                Signal = Crossing.SignalState.Walk,
                Calls = Crossing.CallState.Standard
            }
        };

        var adapterMock = new Mock<IAdapter>();
        adapterMock.Setup(a => a.GetCrossingStatesAsync(null)).ReturnsAsync(testCrossings);

        _adapterFactoryMock
            .Setup(factory => factory.GetAdapterAsync("intersection1"))
            .ReturnsAsync(adapterMock.Object);

        // Act
        var result = await _endpoint.GetCrossings("intersection1");

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));

        var okResult = (OkObjectResult)result;
        Assert.AreEqual(StatusCodes.Status200OK, okResult.StatusCode);

        var crossings = (Crossing[])okResult.Value;
        Assert.AreEqual(2, crossings.Length);
        Assert.AreEqual("crossing1", crossings[0].Id);
        Assert.AreEqual("crossing2", crossings[1].Id);
        Assert.AreEqual(Crossing.SignalState.Stop, crossings[0].Signal);
        Assert.AreEqual(Crossing.SignalState.Walk, crossings[1].Signal);
    }

    [TestMethod]
    public async Task GetCrossings_WithInvalidIntersectionId_ShouldReturnNotFound()
    {
        // Arrange
        _adapterFactoryMock
            .Setup(factory => factory.GetAdapterAsync("non-existent-intersection"))
            .ThrowsAsync(new KeyNotFoundException("Intersection 'non-existent-intersection' not found."));

        // Act
        var result = await _endpoint.GetCrossings("non-existent-intersection");

        // Assert
        Assert.IsInstanceOfType(result, typeof(NotFoundObjectResult));

        var notFoundResult = (NotFoundObjectResult)result;
        Assert.AreEqual(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        Assert.AreEqual("Intersection 'non-existent-intersection' not found.", notFoundResult.Value);
    }

    [TestMethod]
    public async Task GetCrossing_ShouldReturnSpecificCrossings()
    {
        // Arrange
        var testCrossings = new[]
        {
            new Crossing
            {
                Id = "crossing1",
                Description = "Test Crossing 1",
                Phase = 2,
                Signal = Crossing.SignalState.Stop,
                Calls = Crossing.CallState.None
            }
        };

        var adapterMock = new Mock<IAdapter>();
        adapterMock.Setup(a => a.GetCrossingStatesAsync(new[] { "crossing1" })).ReturnsAsync(testCrossings);

        _adapterFactoryMock
            .Setup(factory => factory.GetAdapterAsync("intersection1"))
            .ReturnsAsync(adapterMock.Object);

        // Act
        var result = await _endpoint.GetCrossing("intersection1", "crossing1");

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));

        var okResult = (OkObjectResult)result;
        Assert.AreEqual(StatusCodes.Status200OK, okResult.StatusCode);

        var crossings = (Crossing[])okResult.Value;
        Assert.AreEqual(1, crossings.Length);
        Assert.AreEqual("crossing1", crossings[0].Id);
        Assert.AreEqual(Crossing.SignalState.Stop, crossings[0].Signal);
    }

    [TestMethod]
    public async Task GetCrossing_WithMultipleCrossingIds_ShouldReturnAllSpecifiedCrossings()
    {
        // Arrange
        var testCrossings = new[]
        {
            new Crossing
            {
                Id = "crossing1",
                Description = "Test Crossing 1",
                Phase = 2,
                Signal = Crossing.SignalState.Stop,
                Calls = Crossing.CallState.None
            },
            new Crossing
            {
                Id = "crossing3",
                Description = "Test Crossing 3",
                Phase = 6,
                Signal = Crossing.SignalState.Clear,
                Calls = Crossing.CallState.Extended
            }
        };

        var adapterMock = new Mock<IAdapter>();
        adapterMock.Setup(a => a.GetCrossingStatesAsync(new[] { "crossing1", "crossing3" })).ReturnsAsync(testCrossings);

        _adapterFactoryMock
            .Setup(factory => factory.GetAdapterAsync("intersection1"))
            .ReturnsAsync(adapterMock.Object);

        // Act
        var result = await _endpoint.GetCrossing("intersection1", "crossing1,crossing3");

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));

        var okResult = (OkObjectResult)result;
        Assert.AreEqual(StatusCodes.Status200OK, okResult.StatusCode);

        var crossings = (Crossing[])okResult.Value;
        Assert.AreEqual(2, crossings.Length);
        Assert.AreEqual("crossing1", crossings[0].Id);
        Assert.AreEqual("crossing3", crossings[1].Id);
    }

    [TestMethod]
    public async Task GetCrossing_WithInvalidCrossingId_ShouldReturnNotFound()
    {
        // Arrange
        var adapterMock = new Mock<IAdapter>();
        adapterMock.Setup(a => a.GetCrossingStatesAsync(new[] { "invalid-crossing" }))
            .ThrowsAsync(new KeyNotFoundException("Crossing 'invalid-crossing' not found at intersection."));

        _adapterFactoryMock
            .Setup(factory => factory.GetAdapterAsync("intersection1"))
            .ReturnsAsync(adapterMock.Object);

        // Act
        var result = await _endpoint.GetCrossing("intersection1", "invalid-crossing");

        // Assert
        Assert.IsInstanceOfType(result, typeof(NotFoundObjectResult));

        var notFoundResult = (NotFoundObjectResult)result;
        Assert.AreEqual(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        Assert.AreEqual("Crossing 'invalid-crossing' not found at intersection.", notFoundResult.Value);
    }

    [TestMethod]
    public async Task CallCrossings_ShouldCallCrossingsAndReturnUpdatedStates()
    {
        // Arrange
        var testCrossings = new[]
        {
            new Crossing
            {
                Id = "crossing1",
                Description = "Test Crossing 1",
                Phase = 2,
                Signal = Crossing.SignalState.Stop,
                Calls = Crossing.CallState.Standard
            }
        };

        var adapterMock = new Mock<IAdapter>();
        adapterMock.Setup(a => a.CallCrossingsAsync(new[] { "crossing1" }, false)).ReturnsAsync(testCrossings);

        _adapterFactoryMock
            .Setup(factory => factory.GetAdapterAsync("intersection1"))
            .ReturnsAsync(adapterMock.Object);

        // Act
        var result = await _endpoint.CallCrossings("intersection1", "crossing1");

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));

        var okResult = (OkObjectResult)result;
        Assert.AreEqual(StatusCodes.Status200OK, okResult.StatusCode);

        var crossings = (Crossing[])okResult.Value;
        Assert.AreEqual(1, crossings.Length);
        Assert.AreEqual("crossing1", crossings[0].Id);
        Assert.AreEqual(Crossing.CallState.Standard, crossings[0].Calls);
    }

    [TestMethod]
    public async Task CallCrossings_WithExtendedOption_ShouldCallWithExtendedOption()
    {
        // Arrange
        var testCrossings = new[]
        {
            new Crossing
            {
                Id = "crossing1",
                Description = "Test Crossing 1",
                Phase = 2,
                Signal = Crossing.SignalState.Stop,
                Calls = Crossing.CallState.Extended
            }
        };

        var adapterMock = new Mock<IAdapter>();
        adapterMock.Setup(a => a.CallCrossingsAsync(new[] { "crossing1" }, true)).ReturnsAsync(testCrossings);

        _adapterFactoryMock
            .Setup(factory => factory.GetAdapterAsync("intersection1"))
            .ReturnsAsync(adapterMock.Object);

        // Act
        var result = await _endpoint.CallCrossings("intersection1", "crossing1", true);

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));

        var okResult = (OkObjectResult)result;
        Assert.AreEqual(StatusCodes.Status200OK, okResult.StatusCode);

        var crossings = (Crossing[])okResult.Value;
        Assert.AreEqual(1, crossings.Length);
        Assert.AreEqual("crossing1", crossings[0].Id);
        Assert.AreEqual(Crossing.CallState.Extended, crossings[0].Calls);
    }

    [TestMethod]
    public async Task CallCrossings_WithMultipleCrossingIds_ShouldCallAllSpecifiedCrossings()
    {
        // Arrange
        var testCrossings = new[]
        {
            new Crossing
            {
                Id = "crossing1",
                Description = "Test Crossing 1",
                Phase = 2,
                Signal = Crossing.SignalState.Stop,
                Calls = Crossing.CallState.Standard
            },
            new Crossing
            {
                Id = "crossing3",
                Description = "Test Crossing 3",
                Phase = 6,
                Signal = Crossing.SignalState.Stop,
                Calls = Crossing.CallState.Standard
            }
        };

        var adapterMock = new Mock<IAdapter>();
        adapterMock.Setup(a => a.CallCrossingsAsync(new[] { "crossing1", "crossing3" }, false)).ReturnsAsync(testCrossings);

        _adapterFactoryMock
            .Setup(factory => factory.GetAdapterAsync("intersection1"))
            .ReturnsAsync(adapterMock.Object);

        // Act
        var result = await _endpoint.CallCrossings("intersection1", "crossing1,crossing3");

        // Assert
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));

        var okResult = (OkObjectResult)result;
        Assert.AreEqual(StatusCodes.Status200OK, okResult.StatusCode);

        var crossings = (Crossing[])okResult.Value;
        Assert.AreEqual(2, crossings.Length);
        Assert.AreEqual("crossing1", crossings[0].Id);
        Assert.AreEqual("crossing3", crossings[1].Id);
    }

    [TestMethod]
    public async Task CallCrossings_WithInvalidIntersectionId_ShouldReturnNotFound()
    {
        // Arrange
        _adapterFactoryMock
            .Setup(factory => factory.GetAdapterAsync("non-existent-intersection"))
            .ThrowsAsync(new KeyNotFoundException("Intersection 'non-existent-intersection' not found."));

        // Act
        var result = await _endpoint.CallCrossings("non-existent-intersection", "crossing1");

        // Assert
        Assert.IsInstanceOfType(result, typeof(NotFoundObjectResult));

        var notFoundResult = (NotFoundObjectResult)result;
        Assert.AreEqual(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        Assert.AreEqual("Intersection 'non-existent-intersection' not found.", notFoundResult.Value);
    }

    [TestMethod]
    public async Task CallCrossings_WithInvalidCrossingId_ShouldReturnNotFound()
    {
        // Arrange
        var adapterMock = new Mock<IAdapter>();
        adapterMock.Setup(a => a.CallCrossingsAsync(new[] { "invalid-crossing" }, false))
            .ThrowsAsync(new KeyNotFoundException("Crossing 'invalid-crossing' not found at intersection."));

        _adapterFactoryMock
            .Setup(factory => factory.GetAdapterAsync("intersection1"))
            .ReturnsAsync(adapterMock.Object);

        // Act
        var result = await _endpoint.CallCrossings("intersection1", "invalid-crossing");

        // Assert
        Assert.IsInstanceOfType(result, typeof(NotFoundObjectResult));

        var notFoundResult = (NotFoundObjectResult)result;
        Assert.AreEqual(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        Assert.AreEqual("Crossing 'invalid-crossing' not found at intersection.", notFoundResult.Value);
    }

    [TestMethod]
    public async Task CallCrossings_WithFailedCall_ShouldReturnBadRequest()
    {
        // Arrange
        var adapterMock = new Mock<IAdapter>();
        adapterMock.Setup(a => a.CallCrossingsAsync(new[] { "crossing1" }, false))
            .ThrowsAsync(new ApplicationException("Crossings states did not update correctly at controller."));

        _adapterFactoryMock
            .Setup(factory => factory.GetAdapterAsync("intersection1"))
            .ReturnsAsync(adapterMock.Object);

        // Act
        var result = await _endpoint.CallCrossings("intersection1", "crossing1");

        // Assert
        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));

        var badRequestResult = (BadRequestObjectResult)result;
        Assert.AreEqual(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
        Assert.AreEqual("Crossings states did not update correctly at controller.", badRequestResult.Value);
    }
}
