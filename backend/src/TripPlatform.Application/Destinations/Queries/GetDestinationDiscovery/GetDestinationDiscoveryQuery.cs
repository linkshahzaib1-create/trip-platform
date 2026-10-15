using MediatR;
using Microsoft.EntityFrameworkCore;
using TripPlatform.Application.Common.Interfaces;

namespace TripPlatform.Application.Destinations.Queries.GetDestinationDiscovery;

public sealed record GetDestinationDiscoveryQuery(int DestinationId, bool Refresh = false)
    : IRequest<DestinationDiscoveryDto?>;

public sealed record PlaceCardDto(
    int Id,
    string ListingType,
    string Name,
    string? Description,
    string? Neighborhood,
    decimal? PriceMin,
    decimal? PriceTypical,
    decimal? PriceMax,
    string Currency,
    decimal? Rating,
    int? StarRating,
    string? Amenities,
    string? Cuisine,
    string? OpeningHours,
    string? ImageUrl,
    string Source,
    DateTime? FetchedAtUtc);

public sealed record FlightCardDto(
    int Id,
    string From,
    string To,
    string Airline,
    string CabinClass,
    int DurationMinutes,
    decimal PriceMin,
    decimal PriceTypical,
    decimal PriceMax,
    string Currency,
    string? BaggageNotes,
    string? ScheduleNotes,
    DateTime FetchedAtUtc);

public sealed record TransportCardDto(
    int Id,
    string Mode,
    string Title,
    string? Details,
    int? TypicalMinutes,
    decimal PriceMin,
    decimal PriceTypical,
    decimal PriceMax,
    string Currency,
    DateTime FetchedAtUtc);

public sealed record ActivityCostDto(
    int Id,
    string Name,
    string Category,
    int DurationMinutes,
    decimal CostMin,
    decimal CostTypical,
    decimal CostMax,
    string Currency,
    string BudgetLevel,
    string? Description,
    int FamilyScore,
    int CoupleScore);

public sealed record DestinationDiscoveryDto(
    int DestinationId,
    string DestinationName,
    IReadOnlyList<PlaceCardDto> Hotels,
    IReadOnlyList<PlaceCardDto> Restaurants,
    IReadOnlyList<PlaceCardDto> Attractions,
    IReadOnlyList<PlaceCardDto> Shopping,
    IReadOnlyList<PlaceCardDto> Nightlife,
    IReadOnlyList<FlightCardDto> Flights,
    IReadOnlyList<TransportCardDto> Transport,
    IReadOnlyList<ActivityCostDto> Activities,
    DateTime? LastSyncedUtc);

public sealed class GetDestinationDiscoveryQueryHandler
    : IRequestHandler<GetDestinationDiscoveryQuery, DestinationDiscoveryDto?>
{
    private readonly IApplicationDbContext _db;
    private readonly IPlaceSyncService _sync;

    public GetDestinationDiscoveryQueryHandler(IApplicationDbContext db, IPlaceSyncService sync)
    {
        _db = db;
        _sync = sync;
    }

    public async Task<DestinationDiscoveryDto?> Handle(
        GetDestinationDiscoveryQuery request,
        CancellationToken cancellationToken)
    {
        var dest = await _db.Destinations
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == request.DestinationId && d.IsActive, cancellationToken);
        if (dest is null)
            return null;

        if (request.Refresh)
            await _sync.SyncDestinationPlacesAsync(dest.Id, cancellationToken);
        else
            await _sync.EnsureFreshAsync(dest.Id, TimeSpan.FromHours(24), cancellationToken);

        var listings = await _db.Listings
            .AsNoTracking()
            .Where(l => l.DestinationId == dest.Id && l.IsActive)
            .OrderByDescending(l => l.PopularityScore)
            .ThenBy(l => l.Name)
            .ToListAsync(cancellationToken);

        PlaceCardDto MapPlace(Domain.Entities.Listing l) => new(
            l.Id,
            l.ListingType,
            l.Name,
            l.Description,
            l.Neighborhood,
            l.PriceMin,
            l.PriceTypical,
            l.PriceMax,
            l.Currency,
            l.Rating,
            l.StarRating,
            l.AmenitiesCsv,
            l.Cuisine,
            l.OpeningHours,
            l.ImageUrl,
            l.Source,
            l.FetchedAtUtc);

        var hotels = listings.Where(l => l.ListingType == "Hotel").Take(12).Select(MapPlace).ToList();
        var restaurants = listings.Where(l => l.ListingType == "Restaurant").Take(12).Select(MapPlace).ToList();
        var attractions = listings.Where(l => l.ListingType == "Attraction").Take(12).Select(MapPlace).ToList();
        var shopping = listings.Where(l => l.ListingType == "Shopping").Take(12).Select(MapPlace).ToList();
        var nightlife = listings.Where(l => l.ListingType == "Nightlife").Take(12).Select(MapPlace).ToList();

        var flights = await _db.FlightQuotes
            .AsNoTracking()
            .Include(f => f.OriginDestination)
            .Include(f => f.TargetDestination)
            .Where(f => f.OriginDestinationId == dest.Id || f.TargetDestinationId == dest.Id)
            .OrderBy(f => f.PriceTypical)
            .Take(10)
            .Select(f => new FlightCardDto(
                f.Id,
                f.OriginDestination!.Name,
                f.TargetDestination!.Name,
                f.Airline,
                f.CabinClass,
                f.DurationMinutes,
                f.PriceMin,
                f.PriceTypical,
                f.PriceMax,
                f.Currency,
                f.BaggageNotes,
                f.ScheduleNotes,
                f.FetchedAtUtc))
            .ToListAsync(cancellationToken);

        var transport = await _db.TransportQuotes
            .AsNoTracking()
            .Where(t => t.DestinationId == dest.Id && t.IsActive)
            .OrderBy(t => t.Mode)
            .Select(t => new TransportCardDto(
                t.Id,
                t.Mode,
                t.Title,
                t.Details,
                t.TypicalMinutes,
                t.PriceMin,
                t.PriceTypical,
                t.PriceMax,
                t.Currency,
                t.FetchedAtUtc))
            .ToListAsync(cancellationToken);

        var activities = await _db.Activities
            .AsNoTracking()
            .Where(a => a.DestinationId == dest.Id && a.IsActive)
            .OrderByDescending(a => a.FamilyScore + a.CoupleScore)
            .Select(a => new ActivityCostDto(
                a.Id,
                a.Name,
                a.Category,
                a.DurationMinutes,
                a.CostMin,
                a.CostTypical,
                a.CostMax,
                a.Currency,
                a.BudgetLevel,
                a.Description,
                a.FamilyScore,
                a.CoupleScore))
            .ToListAsync(cancellationToken);

        var lastSync = listings
            .Where(l => l.FetchedAtUtc != null)
            .Select(l => l.FetchedAtUtc)
            .DefaultIfEmpty()
            .Max();

        return new DestinationDiscoveryDto(
            dest.Id,
            dest.Name,
            hotels,
            restaurants,
            attractions,
            shopping,
            nightlife,
            flights,
            transport,
            activities,
            lastSync);
    }
}
