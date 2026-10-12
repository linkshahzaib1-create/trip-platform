using MediatR;
using Microsoft.AspNetCore.Mvc;
using TripPlatform.Application.Trips.Commands.EstimateTrip;

namespace TripPlatform.API.Controllers;

[ApiController]
[Route("api/v1/trips")]
public sealed class TripsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TripsController(IMediator mediator) => _mediator = mediator;

    [HttpPost("estimate")]
    public async Task<ActionResult<EstimateTripResponse>> Estimate(
        [FromBody] EstimateTripCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}
