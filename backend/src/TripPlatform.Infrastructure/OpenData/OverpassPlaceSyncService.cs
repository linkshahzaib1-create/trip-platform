using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TripPlatform.Application.Common.Interfaces;
using TripPlatform.Domain.Entities;
using TripPlatform.Infrastructure.Persistence;

namespace TripPlatform.Infrastructure.OpenData;

/// <summary>
/// Fetches hotels, restaurants, attractions, shops, and nightlife from OpenStreetMap Overpass
/// and caches them in the local Listings table with estimated prices.
/// </summary>
public sealed class OverpassPlaceSyncService : IPlaceSyncService
{
    private readonly HttpClient _http;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<OverpassPlaceSyncService> _logger;

    public OverpassPlaceSyncService(
        HttpClient http,
        ApplicationDbContext db,
        ILogger<OverpassPlaceSyncService> logger)
    {
        _http = http;
        _db = db;
        _logger = logger;
    }

    public async Task EnsureFreshAsync(int destinationId, TimeSpan maxAge, CancellationToken cancellationToken = default)
    {
        var latest = await _db.Listings
            .AsNoTracking()
            .Where(l => l.DestinationId == destinationId && l.Source == "OpenStreetMap" && l.FetchedAtUtc != null)
            .OrderByDescending(l => l.FetchedAtUtc)
            .Select(l => l.FetchedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (latest is null || DateTime.UtcNow - latest.Value > maxAge)
            await SyncDestinationPlacesAsync(destinationId, cancellationToken);
    }

    public async Task<int> SyncDestinationPlacesAsync(int destinationId, CancellationToken cancellationToken = default)
    {
        var dest = await _db.Destinations
            .Include(d => d.CostProfiles)
            .FirstOrDefaultAsync(d => d.Id == destinationId && d.IsActive, cancellationToken);

        if (dest is null)
            return 0;

        var profile = dest.CostProfiles
            .OrderBy(p => p.BudgetTier == "Balanced" ? 0 : 1)
            .FirstOrDefault();

        var radius = dest.Name is "Maldives" or "Hunza" or "Murree" ? 12000 : 8000;
        var query = BuildOverpassQuery(dest.Latitude, dest.Longitude, radius);

        OverpassResponse? response = null;
        try
        {
            // Overpass expects data=<query>
            using var form = new StringContent("data=" + Uri.EscapeDataString(query), Encoding.UTF8, "application/x-www-form-urlencoded");
            using var httpResponse = await _http.PostAsync(string.Empty, form, cancellationToken);
            if (httpResponse.IsSuccessStatusCode)
            {
                await using var stream = await httpResponse.Content.ReadAsStreamAsync(cancellationToken);
                response = await JsonSerializer.DeserializeAsync<OverpassResponse>(stream, cancellationToken: cancellationToken);
            }
            else
            {
                _logger.LogWarning("Overpass returned {Status} for {Dest}", httpResponse.StatusCode, dest.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Overpass sync failed for {Dest} — keeping local seed data", dest.Name);
        }

        var upserted = 0;
        var now = DateTime.UtcNow;

        if (response?.Elements is { Count: > 0 })
        {
            foreach (var el in response.Elements)
            {
                if (el.Tags is null || string.IsNullOrWhiteSpace(el.Tags.Name))
                    continue;

                var listingType = MapListingType(el.Tags);
                if (listingType is null)
                    continue;

                var lat = el.Lat ?? el.Center?.Lat ?? dest.Latitude;
                var lng = el.Lon ?? el.Center?.Lon ?? dest.Longitude;
                var externalId = $"osm:{el.Type}:{el.Id}";

                var existing = await _db.Listings
                    .FirstOrDefaultAsync(l => l.ExternalId == externalId, cancellationToken);

                var (min, typical, max) = EstimatePrice(listingType, profile, el.Tags);
                var amenities = BuildAmenities(el.Tags);

                if (existing is null)
                {
                    _db.Listings.Add(new Listing
                    {
                        DestinationId = dest.Id,
                        ListingType = listingType,
                        Name = el.Tags.Name.Trim(),
                        Description = el.Tags.Description ?? el.Tags.Cuisine,
                        Neighborhood = el.Tags.Suburb ?? el.Tags.Neighbourhood,
                        Latitude = lat,
                        Longitude = lng,
                        PriceMin = min,
                        PriceTypical = typical,
                        PriceMax = max,
                        Currency = "USD",
                        Rating = EstimateRating(el.Tags),
                        ReviewCount = 0,
                        StarRating = listingType == "Hotel" ? EstimateStars(el.Tags) : null,
                        AmenitiesCsv = amenities,
                        Cuisine = el.Tags.Cuisine,
                        FamilyFriendly = listingType is "Hotel" or "Attraction" or "Restaurant",
                        ChildFriendly = listingType is "Attraction" or "Restaurant",
                        ImageUrl = null,
                        PopularityScore = PopularityFromTags(el.Tags),
                        ExternalId = externalId,
                        Source = "OpenStreetMap",
                        OpeningHours = el.Tags.OpeningHours,
                        FetchedAtUtc = now,
                        IsActive = true
                    });
                    upserted++;
                }
                else
                {
                    existing.Name = el.Tags.Name.Trim();
                    existing.ListingType = listingType;
                    existing.Latitude = lat;
                    existing.Longitude = lng;
                    existing.PriceMin = min;
                    existing.PriceTypical = typical;
                    existing.PriceMax = max;
                    existing.AmenitiesCsv = amenities;
                    existing.Cuisine = el.Tags.Cuisine;
                    existing.OpeningHours = el.Tags.OpeningHours;
                    existing.FetchedAtUtc = now;
                    existing.Source = "OpenStreetMap";
                    existing.UpdatedAtUtc = now;
                    upserted++;
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        await EnsureTransportQuotesAsync(dest, profile, cancellationToken);
        await EnsureFlightQuotesForOriginRoutesAsync(dest.Id, cancellationToken);

        return upserted;
    }

    private async Task EnsureTransportQuotesAsync(
        Destination dest,
        DestinationCostProfile? profile,
        CancellationToken cancellationToken)
    {
        var existing = await _db.TransportQuotes.AnyAsync(t => t.DestinationId == dest.Id, cancellationToken);
        if (existing)
        {
            // Refresh prices lightly
            var quotes = await _db.TransportQuotes.Where(t => t.DestinationId == dest.Id).ToListAsync(cancellationToken);
            foreach (var q in quotes)
            {
                q.FetchedAtUtc = DateTime.UtcNow;
                q.UpdatedAtUtc = DateTime.UtcNow;
            }
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        var local = profile?.LocalTransportTypical ?? 8m;
        _db.TransportQuotes.AddRange(
            new TransportQuote
            {
                DestinationId = dest.Id,
                Mode = "Taxi",
                Title = "City taxi / ride-hail (short trip)",
                Details = "Typical short city ride; airport runs cost more.",
                TypicalMinutes = 25,
                PriceMin = Math.Round(local * 0.6m, 2),
                PriceTypical = local,
                PriceMax = Math.Round(local * 2.2m, 2),
                Source = "CostProfileEstimate",
                FetchedAtUtc = DateTime.UtcNow
            },
            new TransportQuote
            {
                DestinationId = dest.Id,
                Mode = "PublicTransit",
                Title = "Metro / bus day pass",
                Details = "Public transit where available.",
                TypicalMinutes = 40,
                PriceMin = Math.Round(local * 0.15m, 2),
                PriceTypical = Math.Round(local * 0.35m, 2),
                PriceMax = Math.Round(local * 0.7m, 2),
                Source = "CostProfileEstimate",
                FetchedAtUtc = DateTime.UtcNow
            },
            new TransportQuote
            {
                DestinationId = dest.Id,
                Mode = "CarRental",
                Title = "Car rental (per day)",
                Details = "Economy car estimate excluding fuel.",
                TypicalMinutes = null,
                PriceMin = 25,
                PriceTypical = 45,
                PriceMax = 85,
                Source = "CostProfileEstimate",
                FetchedAtUtc = DateTime.UtcNow
            },
            new TransportQuote
            {
                DestinationId = dest.Id,
                Mode = "Transfer",
                Title = "Airport / station transfer",
                Details = "One-way private transfer estimate.",
                TypicalMinutes = 45,
                PriceMin = profile?.AirportTransferMin ?? 15,
                PriceTypical = profile?.AirportTransferTypical ?? 30,
                PriceMax = profile?.AirportTransferMax ?? 60,
                Source = "CostProfileEstimate",
                FetchedAtUtc = DateTime.UtcNow
            });

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureFlightQuotesForOriginRoutesAsync(int destinationId, CancellationToken cancellationToken)
    {
        var routes = await _db.Routes
            .Include(r => r.Options)
            .Where(r => r.OriginDestinationId == destinationId || r.TargetDestinationId == destinationId)
            .ToListAsync(cancellationToken);

        foreach (var route in routes)
        {
            var flight = route.Options.FirstOrDefault(o => o.TravelMode == Domain.Enums.TravelMode.Flight)
                ?? route.Options.OrderBy(o => o.CostTypical).FirstOrDefault();
            if (flight is null)
                continue;

            var exists = await _db.FlightQuotes.AnyAsync(
                f => f.OriginDestinationId == route.OriginDestinationId
                     && f.TargetDestinationId == route.TargetDestinationId,
                cancellationToken);
            if (exists)
                continue;

            _db.FlightQuotes.Add(new FlightQuote
            {
                OriginDestinationId = route.OriginDestinationId,
                TargetDestinationId = route.TargetDestinationId,
                Airline = "Multiple carriers",
                CabinClass = "Economy",
                DurationMinutes = flight.DurationMinutes,
                PriceMin = flight.CostMin,
                PriceTypical = flight.CostTypical,
                PriceMax = flight.CostMax,
                BaggageNotes = "Cabin bag usually included; checked baggage often extra ($20–$60).",
                ScheduleNotes = $"{flight.DurationMinutes / 60}h {flight.DurationMinutes % 60}m typical duration",
                Source = "RouteEstimate",
                FetchedAtUtc = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static string BuildOverpassQuery(double lat, double lng, int radiusMeters)
    {
        var latS = lat.ToString(CultureInfo.InvariantCulture);
        var lngS = lng.ToString(CultureInfo.InvariantCulture);
        return $"""
            [out:json][timeout:45];
            (
              node["tourism"="hotel"](around:{radiusMeters},{latS},{lngS});
              node["tourism"="guest_house"](around:{radiusMeters},{latS},{lngS});
              node["amenity"="restaurant"](around:{radiusMeters},{latS},{lngS});
              node["amenity"="cafe"](around:{radiusMeters},{latS},{lngS});
              node["amenity"="fast_food"](around:{radiusMeters},{latS},{lngS});
              node["tourism"="attraction"](around:{radiusMeters},{latS},{lngS});
              node["tourism"="museum"](around:{radiusMeters},{latS},{lngS});
              node["shop"="mall"](around:{radiusMeters},{latS},{lngS});
              node["shop"="souvenir"](around:{radiusMeters},{latS},{lngS});
              node["shop"="clothes"](around:{radiusMeters},{latS},{lngS});
              node["amenity"="nightclub"](around:{radiusMeters},{latS},{lngS});
              node["amenity"="bar"](around:{radiusMeters},{latS},{lngS});
              node["leisure"="park"](around:{radiusMeters},{latS},{lngS});
            );
            out center 40;
            """;
    }

    private static string? MapListingType(OsmTags tags)
    {
        if (tags.Tourism is "hotel" or "guest_house" or "hostel")
            return "Hotel";
        if (tags.Amenity is "restaurant" or "cafe" or "fast_food" || !string.IsNullOrWhiteSpace(tags.Cuisine))
            return "Restaurant";
        if (tags.Tourism is "attraction" or "museum" || tags.Leisure == "park")
            return "Attraction";
        if (tags.Shop is not null)
            return "Shopping";
        if (tags.Amenity is "nightclub" or "bar")
            return "Nightlife";
        return null;
    }

    private static (decimal Min, decimal Typical, decimal Max) EstimatePrice(
        string listingType,
        DestinationCostProfile? profile,
        OsmTags tags)
    {
        return listingType switch
        {
            "Hotel" => (
                profile?.AccommodationMin ?? 40,
                profile?.AccommodationTypical ?? 75,
                profile?.AccommodationMax ?? 140),
            "Restaurant" => (
                Math.Round((profile?.FoodMin ?? 8) * 0.8m, 2),
                profile?.FoodTypical ?? 15,
                Math.Round((profile?.FoodMax ?? 30) * 1.2m, 2)),
            "Attraction" => (
                profile?.ActivitiesMin ?? 0,
                profile?.ActivitiesTypical ?? 12,
                profile?.ActivitiesMax ?? 40),
            "Shopping" => (5, 25, 120),
            "Nightlife" => (10, 30, 80),
            _ => (0, 10, 30)
        };
    }

    private static string? BuildAmenities(OsmTags tags)
    {
        var parts = new List<string>();
        if (tags.Wifi == "yes" || tags.InternetAccess == "wlan") parts.Add("Wi‑Fi");
        if (tags.Wheelchair == "yes") parts.Add("Wheelchair access");
        if (tags.OutdoorSeating == "yes") parts.Add("Outdoor seating");
        if (!string.IsNullOrWhiteSpace(tags.Cuisine)) parts.Add(tags.Cuisine);
        return parts.Count == 0 ? null : string.Join(", ", parts);
    }

    private static decimal? EstimateRating(OsmTags tags) =>
        tags.Stars is not null && decimal.TryParse(tags.Stars, out var s) ? Math.Min(5, s) : 4.2m;

    private static int? EstimateStars(OsmTags tags) =>
        tags.Stars is not null && int.TryParse(tags.Stars.Split('.')[0], out var s) ? Math.Clamp(s, 1, 5) : 3;

    private static int PopularityFromTags(OsmTags tags) =>
        tags.Tourism is "attraction" or "museum" ? 80 : 65;

    private sealed class OverpassResponse
    {
        [JsonPropertyName("elements")]
        public List<OverpassElement>? Elements { get; set; }
    }

    private sealed class OverpassElement
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "node";

        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("lat")]
        public double? Lat { get; set; }

        [JsonPropertyName("lon")]
        public double? Lon { get; set; }

        [JsonPropertyName("center")]
        public OverpassCenter? Center { get; set; }

        [JsonPropertyName("tags")]
        public OsmTags? Tags { get; set; }
    }

    private sealed class OverpassCenter
    {
        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        [JsonPropertyName("lon")]
        public double Lon { get; set; }
    }

    private sealed class OsmTags
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("tourism")]
        public string? Tourism { get; set; }

        [JsonPropertyName("amenity")]
        public string? Amenity { get; set; }

        [JsonPropertyName("shop")]
        public string? Shop { get; set; }

        [JsonPropertyName("leisure")]
        public string? Leisure { get; set; }

        [JsonPropertyName("cuisine")]
        public string? Cuisine { get; set; }

        [JsonPropertyName("opening_hours")]
        public string? OpeningHours { get; set; }

        [JsonPropertyName("stars")]
        public string? Stars { get; set; }

        [JsonPropertyName("internet_access")]
        public string? InternetAccess { get; set; }

        [JsonPropertyName("wifi")]
        public string? Wifi { get; set; }

        [JsonPropertyName("wheelchair")]
        public string? Wheelchair { get; set; }

        [JsonPropertyName("outdoor_seating")]
        public string? OutdoorSeating { get; set; }

        [JsonPropertyName("suburb")]
        public string? Suburb { get; set; }

        [JsonPropertyName("neighbourhood")]
        public string? Neighbourhood { get; set; }
    }
}
