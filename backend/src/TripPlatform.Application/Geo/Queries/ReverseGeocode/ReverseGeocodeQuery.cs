using MediatR;
using Microsoft.EntityFrameworkCore;
using TripPlatform.Application.Common.Interfaces;

namespace TripPlatform.Application.Geo.Queries.ReverseGeocode;

public sealed record ReverseGeocodeQuery(double Latitude, double Longitude)
    : IRequest<ReverseGeocodeResponse?>;

public sealed record MatchedDestinationDto(int Id, string Name, string Country, string Slug);

public sealed record ReverseGeocodeResponse(
    string City,
    string? Country,
    string? CountryCode,
    double Latitude,
    double Longitude,
    string DisplayName,
    MatchedDestinationDto? MatchedDestination);

public sealed class ReverseGeocodeQueryHandler : IRequestHandler<ReverseGeocodeQuery, ReverseGeocodeResponse?>
{
    private readonly IGeocodingService _geo;
    private readonly IApplicationDbContext _db;

    public ReverseGeocodeQueryHandler(IGeocodingService geo, IApplicationDbContext db)
    {
        _geo = geo;
        _db = db;
    }

    public async Task<ReverseGeocodeResponse?> Handle(ReverseGeocodeQuery request, CancellationToken cancellationToken)
    {
        var result = await _geo.ReverseGeocodeAsync(request.Latitude, request.Longitude, cancellationToken);
        if (result is null)
            return null;

        // Prefer nearest known destination within ~80km, else name match
        var destinations = await _db.Destinations
            .AsNoTracking()
            .Include(d => d.Country)
            .Where(d => d.IsActive)
            .ToListAsync(cancellationToken);

        MatchedDestinationDto? matched = null;
        var nearest = destinations
            .Select(d => new { Dest = d, Km = HaversineKm(request.Latitude, request.Longitude, d.Latitude, d.Longitude) })
            .OrderBy(x => x.Km)
            .FirstOrDefault();

        if (nearest is not null && nearest.Km <= 80)
        {
            matched = new MatchedDestinationDto(
                nearest.Dest.Id,
                nearest.Dest.Name,
                nearest.Dest.Country?.Name ?? string.Empty,
                nearest.Dest.Slug);
        }
        else
        {
            var byName = destinations.FirstOrDefault(d =>
                d.Name.Equals(result.City, StringComparison.OrdinalIgnoreCase));
            if (byName is not null)
            {
                matched = new MatchedDestinationDto(
                    byName.Id,
                    byName.Name,
                    byName.Country?.Name ?? string.Empty,
                    byName.Slug);
            }
        }

        return new ReverseGeocodeResponse(
            matched?.Name ?? result.City,
            result.Country,
            result.CountryCode,
            result.Latitude,
            result.Longitude,
            result.DisplayName,
            matched);
    }

    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2))
                  * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double DegreesToRadians(double deg) => deg * Math.PI / 180.0;
}
