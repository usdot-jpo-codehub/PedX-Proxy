using AspNetCore.Authentication.ApiKey;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proxy.Services;

namespace Proxy.Endpoints;

[ApiController]
[Route("/intersections")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[Authorize(AuthenticationSchemes = ApiKeyDefaults.AuthenticationScheme, Roles = "reader")]
public class IntersectionsEndpoint(ILogger<IntersectionsEndpoint> logger, IAdapterFactory adapterFactory)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult GetIntersections()
    {
        logger.LogInformation("Getting all intersections for user '{User}'", HttpContext.User.Identity?.Name);

        var intersections = adapterFactory.GetIntersections();

        return Ok(intersections);
    }

    [HttpGet("{intersectionId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult GetIntersection(string intersectionId)
    {
        logger.LogInformation("Getting intersection '{IntersectionId}' for user '{User}'",
            intersectionId, HttpContext.User.Identity?.Name);

        try
        {
            var intersection = adapterFactory.GetIntersection(intersectionId);

            return Ok(intersection);
        }
        catch (KeyNotFoundException e)
        {
            logger.LogError(e, "Intersection '{IntersectionId}' not found for user '{User}'",
                intersectionId, HttpContext.User.Identity?.Name);
            
            return NotFound(e.Message);
        }
    }

    [HttpGet("{intersectionId}/crossings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetCrossings(string intersectionId)
    {
        logger.LogInformation("Getting all crossing states at intersection '{IntersectionId}' for user '{User}'",
            intersectionId, HttpContext.User.Identity?.Name);

        try
        {
            var adapter = await adapterFactory.GetAdapterAsync(intersectionId);
            var crossingStates = await adapter.GetCrossingStatesAsync();

            return Ok(crossingStates);
        }
        catch (KeyNotFoundException e)
        {
            logger.LogError(e, "Intersection '{IntersectionId}' not found for user '{User}'",
                intersectionId, HttpContext.User.Identity?.Name);
            
            return NotFound(e.Message);
        }
    }

    [HttpGet("{intersectionId}/crossings/{crossingIds}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetCrossing(string intersectionId, string crossingIds)
    {
        // Split crossing IDs from comma-separated string
        var crossingIdsArray = crossingIds.Split(',');

        logger.LogInformation(
            "Getting crossing states '{CrossingIds}' at intersection '{IntersectionId}' for user '{User}'",
            crossingIds, intersectionId, HttpContext.User.Identity?.Name);

        try
        {
            var adapter = await adapterFactory.GetAdapterAsync(intersectionId);
            var crossingStates = await adapter.GetCrossingStatesAsync(crossingIdsArray);

            return Ok(crossingStates);
        }
        catch (KeyNotFoundException e)
        {
            logger.LogError(e, "Crossing(s) '{CrossingIds}' at intersection '{IntersectionId}' not found for user '{User}'",
                crossingIds, intersectionId, HttpContext.User.Identity?.Name);
            
            return NotFound(e.Message);
        }
    }

    [Authorize(Roles = "caller")]
    [HttpPost("{intersectionId}/crossings/{crossingIds}/call")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> CallCrossings(string intersectionId, string crossingIds, bool extended = false)
    {
        // Split crossing IDs from comma-separated string
        var crossingIdsArray = crossingIds.Split(',');

        logger.LogInformation(
            "Calling for {CallType} crossings '{CrossingIds}' at intersection '{IntersectionId}' for user '{User}'",
            extended ? "extended" : "standard", crossingIds, intersectionId, HttpContext.User.Identity?.Name);

        try
        {
            var adapter = await adapterFactory.GetAdapterAsync(intersectionId);
            var crossingStates = await adapter.CallCrossingsAsync(crossingIdsArray, extended);

            return Ok(crossingStates);
        }
        catch (KeyNotFoundException e)
        {
            logger.LogError(e,
                "Crossing(s) '{CrossingIds}' at intersection '{IntersectionId}' not found for user '{User}'",
                crossingIds, intersectionId, HttpContext.User.Identity?.Name);

            return NotFound(e.Message);
        }
        catch (ApplicationException e)
        {
            logger.LogError(e,
                "Crossing(s) '{CrossingIds}' at intersection '{IntersectionId}' did not update correctly for user '{User}'",
                crossingIds, intersectionId, HttpContext.User.Identity?.Name);

            return BadRequest(e.Message);
        }
    }
}