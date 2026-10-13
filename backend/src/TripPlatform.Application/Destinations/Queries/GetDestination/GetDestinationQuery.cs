using MediatR;
using Microsoft.EntityFrameworkCore;
using TripPlatform.Application.Common.Interfaces;

namespace TripPlatform.Application.Destinations.Queries.GetDestination;

public sealed record GetDestinationQuery(int Id) : IRequest<DestinationProfileDto?>;

public sealed record DestinationAttractionDto(
    string Name,
    string Category,
    int DurationMinutes,
    decimal EntryCostTypical,
    int FamilyScore,
    int CoupleScore);

public sealed record DestinationProfileDto(
    int Id,
    string Name,
    string Slug,
    string Country,
    string? Region,
    string? Description,
    int FamilyFriendlyScore,
    int CoupleScore,
    int RomanceScore,
    int CultureScore,
    int AdventureScore,
    IReadOnlyList<string> Highlights,
    IReadOnlyList<DestinationAttractionDto> Attractions,
    IReadOnlyList<string> Activities);

public sealed class GetDestinationQueryHandler : IRequestHandler<GetDestinationQuery, DestinationProfileDto?>
{
    private readonly IApplicationDbContext _db;

    public GetDestinationQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<DestinationProfileDto?> Handle(GetDestinationQuery request, CancellationToken cancellationToken)
    {
        var dest = await _db.Destinations
            .AsNoTracking()
            .Include(d => d.Country)
            .Include(d => d.Region)
            .Include(d => d.Attractions)
            .Include(d => d.Activities)
            .FirstOrDefaultAsync(d => d.Id == request.Id && d.IsActive, cancellationToken);

        if (dest is null)
            return null;

        var attractions = dest.Attractions
            .Where(a => a.IsActive)
            .OrderByDescending(a => a.PopularityScore)
            .Select(a => new DestinationAttractionDto(
                a.Name,
                a.Category,
                a.AverageVisitDurationMinutes,
                a.EntryCostTypical,
                a.FamilyScore,
                a.CoupleScore))
            .ToList();

        var highlights = attractions.Take(5).Select(a => a.Name).ToList();
        if (highlights.Count == 0)
        {
            highlights =
            [
                $"{dest.Name} sightseeing",
                "Local food experiences",
                "Cultural landmarks",
                "Comfortable stays"
            ];
        }

        return new DestinationProfileDto(
            dest.Id,
            dest.Name,
            dest.Slug,
            dest.Country?.Name ?? string.Empty,
            dest.Region?.Name,
            dest.Description,
            dest.FamilyFriendlyScore,
            dest.CoupleScore,
            dest.RomanceScore,
            dest.CultureScore,
            dest.AdventureScore,
            highlights,
            attractions,
            dest.Activities.Where(a => a.IsActive).Select(a => a.Name).ToList());
    }
}
