using MediatR;
using Microsoft.AspNetCore.Mvc;
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

    [HttpPost("recommend")]
    public async Task<ActionResult<RecommendDestinationsResponse>> Recommend(
        [FromBody] RecommendDestinationsCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}
