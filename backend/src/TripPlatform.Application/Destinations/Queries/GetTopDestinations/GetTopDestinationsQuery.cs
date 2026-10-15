using MediatR;
using Microsoft.EntityFrameworkCore;
using TripPlatform.Application.Common.Interfaces;

namespace TripPlatform.Application.Destinations.Queries.GetTopDestinations;

public sealed record GetTopDestinationsQuery(
    string? Origin,
    double? Latitude,
    double? Longitude) : IRequest<TopDestinationsResponse>;

public sealed record TopDestinationCardDto(
    int Id,
    string Name,
    string Country,
    string? Region,
    string Slug,
    string Blurb,
    string Bucket,
    int FamilyScore,
    double? DistanceKm);

public sealed record TopDestinationsResponse(
    string OriginLabel,
    string? OriginCountry,
    IReadOnlyList<TopDestinationCardDto> NearYou,
    IReadOnlyList<TopDestinationCardDto> OutsidePakistan,
    IReadOnlyList<TopDestinationCardDto> International);

public sealed class GetTopDestinationsQueryHandler
    : IRequestHandler<GetTopDestinationsQuery, TopDestinationsResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly IGeocodingService _geo;

    public GetTopDestinationsQueryHandler(IApplicationDbContext db, IGeocodingService geo)
    {
        _db = db;
        _geo = geo;
    }

    public async Task<TopDestinationsResponse> Handle(
        GetTopDestinationsQuery request,
        CancellationToken cancellationToken)
    {
        var all = await _db.Destinations
            .AsNoTracking()
            .Include(d => d.Country)
            .Include(d => d.Region)
            .Where(d => d.IsActive)
            .ToListAsync(cancellationToken);

        Domain.Entities.Destination? origin = null;
        if (!string.IsNullOrWhiteSpace(request.Origin))
        {
            origin = all.FirstOrDefault(d =>
                d.Name.Equals(request.Origin, StringComparison.OrdinalIgnoreCase));
        }

        double originLat;
        double originLng;
        string originLabel;
        string? originCountry;

        if (origin is not null)
        {
            originLat = origin.Latitude;
            originLng = origin.Longitude;
            originLabel = origin.Name;
            originCountry = origin.Country?.Name;
        }
        else if (request.Latitude is not null && request.Longitude is not null)
        {
            originLat = request.Latitude.Value;
            originLng = request.Longitude.Value;
            var geo = await _geo.ReverseGeocodeAsync(originLat, originLng, cancellationToken);
            originLabel = geo?.City ?? "Your location";
            originCountry = geo?.Country;
            origin = all
                .Select(d => new { Dest = d, Km = HaversineKm(originLat, originLng, d.Latitude, d.Longitude) })
                .OrderBy(x => x.Km)
                .Select(x => x.Dest)
                .FirstOrDefault();
            if (origin is not null && HaversineKm(originLat, originLng, origin.Latitude, origin.Longitude) <= 80)
            {
                originLabel = origin.Name;
                originCountry = origin.Country?.Name;
                originLat = origin.Latitude;
                originLng = origin.Longitude;
            }
        }
        else
        {
            origin = all.FirstOrDefault(d => d.Name == "Lahore") ?? all.First();
            originLat = origin.Latitude;
            originLng = origin.Longitude;
            originLabel = origin.Name;
            originCountry = origin.Country?.Name;
        }

        var originCountryCode = origin?.Country?.Code ?? "PK";
        var isPakistanOrigin = originCountryCode.Equals("PK", StringComparison.OrdinalIgnoreCase)
            || string.Equals(originCountry, "Pakistan", StringComparison.OrdinalIgnoreCase);

        var cards = all
            .Where(d => origin is null || d.Id != origin.Id)
            .Select(d =>
            {
                var km = HaversineKm(originLat, originLng, d.Latitude, d.Longitude);
                var isPk = d.Country?.Code == "PK";
                var bucket = km <= 250 ? "NearYou"
                    : isPk ? "Domestic"
                    : "International";
                return new
                {
                    Dest = d,
                    Km = km,
                    Bucket = bucket,
                    Card = new TopDestinationCardDto(
                        d.Id,
                        d.Name,
                        d.Country?.Name ?? string.Empty,
                        d.Region?.Name,
                        d.Slug,
                        BlurbFor(d),
                        bucket,
                        d.FamilyFriendlyScore,
                        Math.Round(km, 0))
                };
            })
            .ToList();

        var nearYou = cards
            .Where(c => isPakistanOrigin
                ? c.Dest.Country?.Code == "PK" && c.Km <= 900
                : c.Km <= 600)
            .OrderBy(c => c.Km)
            .ThenByDescending(c => c.Dest.RegionalPopularityScore)
            .Take(8)
            .Select(c => c.Card)
            .ToList();

        var outsidePakistan = cards
            .Where(c => c.Dest.Country?.Code != "PK")
            .OrderByDescending(c => c.Dest.GlobalPopularityScore)
            .ThenBy(c => c.Km)
            .Take(8)
            .Select(c => c.Card with { Bucket = "OutsidePakistan" })
            .ToList();

        var international = cards
            .Where(c => c.Dest.IsInternationalHub || c.Dest.Country?.Code != "PK")
            .OrderByDescending(c => c.Dest.GlobalPopularityScore)
            .Take(8)
            .Select(c => c.Card with { Bucket = "International" })
            .ToList();

        // If near-you empty (e.g. Helsinki with few neighbors), fall back to domestic hubs
        if (nearYou.Count == 0)
        {
            nearYou = cards
                .OrderBy(c => c.Km)
                .Take(6)
                .Select(c => c.Card)
                .ToList();
        }

        return new TopDestinationsResponse(originLabel, originCountry, nearYou, outsidePakistan, international);
    }

    private static string BlurbFor(Domain.Entities.Destination d) => d.Name switch
    {
        "Hunza" => "Valleys, forts & mountain roads",
        "Murree" => "Family weekends & cool weather",
        "Islamabad" => "Easy pacing, parks & culture",
        "Lahore" => "Food streets, forts & culture",
        "Dubai" => "City lights, malls & desert",
        "Istanbul" => "History, food & Bosphorus",
        "Bangkok" => "Temples, street food & markets",
        "Paris" => "Romance, museums & cafés",
        "Maldives" => "Overwater calm & honeymoon vibe",
        "Helsinki" => "Nordic design & Baltic shores",
        _ => d.Description ?? $"{d.Name} travel experiences"
    };

    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;
        var dLat = (lat2 - lat1) * Math.PI / 180.0;
        var dLon = (lon2 - lon1) * Math.PI / 180.0;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0)
                  * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
}
