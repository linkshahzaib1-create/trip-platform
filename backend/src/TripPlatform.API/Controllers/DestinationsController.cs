using MediatR;
using Microsoft.AspNetCore.Mvc;
using TripPlatform.Application.Destinations.Queries.GetDestination;
using TripPlatform.Application.Destinations.Queries.GetDestinationDiscovery;
using TripPlatform.Application.Destinations.Queries.GetTopDestinations;
using TripPlatform.Application.Destinations.Queries.RecommendDestinations;
using TripPlatform.Application.Destinations.Queries.SearchDestinations;

namespace TripPlatform.API.Controllers;

[ApiController]
[Route("api/v1/destinations")]
public sealed class DestinationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DestinationsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("search")]
    public async Task<ActionResult<SearchDestinationsResponse>> Search([FromQuery] string q, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { title = "Query is required", status = 400 });

        var result = await _mediator.Send(new SearchDestinationsQuery(q), cancellationToken);
        return Ok(result);
    }

    /// <summary>Location-based top destinations: near you / outside Pakistan / international.</summary>
    [HttpGet("top")]
    public async Task<ActionResult<TopDestinationsResponse>> Top(
        [FromQuery] string? origin,
        [FromQuery] double? lat,
        [FromQuery] double? lng,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTopDestinationsQuery(origin, lat, lng), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DestinationProfileDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetDestinationQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Real-time discovery payload: hotels, food, attractions, shopping, nightlife,
    /// flights, transport — synced from OpenStreetMap Overpass into the local DB.
    /// </summary>
    [HttpGet("{id:int}/discovery")]
    public async Task<ActionResult<DestinationDiscoveryDto>> Discovery(
        int id,
        [FromQuery] bool refresh = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetDestinationDiscoveryQuery(id, refresh), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("recommend")]
    public async Task<ActionResult<RecommendDestinationsResponse>> Recommend(
        [FromBody] RecommendDestinationsCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}
