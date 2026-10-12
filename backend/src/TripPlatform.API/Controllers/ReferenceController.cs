using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TripPlatform.Application.Common.Interfaces;

namespace TripPlatform.API.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class ReferenceController : ControllerBase
{
    private readonly IApplicationDbContext _db;

    public ReferenceController(IApplicationDbContext db) => _db = db;

    [HttpGet("traveler-categories")]
    public async Task<IActionResult> GetTravelerCategories(CancellationToken cancellationToken)
    {
        var data = await _db.TravelerCategories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Code,
                Subcategories = c.Subcategories.Where(s => s.IsActive).Select(s => new { s.Id, s.Name, s.Code })
            })
            .ToListAsync(cancellationToken);
        return Ok(data);
    }

    [HttpGet("priorities")]
    public async Task<IActionResult> GetPriorities(CancellationToken cancellationToken)
    {
        var data = await _db.Priorities.AsNoTracking()
            .Where(p => p.IsActive)
            .Select(p => new { p.Id, p.Name, p.Code })
            .ToListAsync(cancellationToken);
        return Ok(data);
    }

    [HttpGet("currencies")]
    public async Task<IActionResult> GetCurrencies(CancellationToken cancellationToken)
    {
        var data = await _db.CurrencyRates.AsNoTracking()
            .Where(c => c.IsActive)
            .Select(c => new { c.FromCurrency, c.ToCurrency, c.Rate, c.AsOfUtc })
            .ToListAsync(cancellationToken);
        return Ok(data);
    }
}
