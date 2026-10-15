using MediatR;
using Microsoft.AspNetCore.Mvc;
using TripPlatform.Application.Geo.Queries.ReverseGeocode;

namespace TripPlatform.API.Controllers;

[ApiController]
[Route("api/v1/geo")]
public sealed class GeoController : ControllerBase
{
    private readonly IMediator _mediator;

    public GeoController(IMediator mediator) => _mediator = mediator;

    /// <summary>Reverse-geocode lat/lng to a city (OpenStreetMap Nominatim) and match a known destination when possible.</summary>
    [HttpGet("reverse")]
    public async Task<ActionResult<ReverseGeocodeResponse>> Reverse(
        [FromQuery] double lat,
        [FromQuery] double lng,
        CancellationToken cancellationToken)
    {
        if (lat is < -90 or > 90 || lng is < -180 or > 180)
            return BadRequest(new { title = "Invalid coordinates", status = 400 });

        var result = await _mediator.Send(new ReverseGeocodeQuery(lat, lng), cancellationToken);
        return result is null
            ? NotFound(new { title = "Could not resolve location", status = 404 })
            : Ok(result);
    }
}
