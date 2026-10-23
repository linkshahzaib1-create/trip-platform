using TripPlatform.Domain.Common;

namespace TripPlatform.Domain.Entities;

/// <summary>
/// Unified listing for hotels, restaurants, and attractions (competitive OTA-style catalog).
/// Affiliate deep links support Booking.com / Agoda / GetYourGuide handoff without in-app booking.
/// </summary>
public class Listing : AuditableEntity
{
    public int DestinationId { get; set; }
    public Destination? Destination { get; set; }
    public string ListingType { get; set; } = "Hotel"; // Hotel | Apartment | Home | Restaurant | Attraction | Experience | Shopping | Nightlife
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Neighborhood { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public decimal? PriceMin { get; set; }
    public decimal? PriceTypical { get; set; }
    public decimal? PriceMax { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal? Rating { get; set; }
    public int ReviewCount { get; set; }
    public int? StarRating { get; set; }
    public string? AmenitiesCsv { get; set; }
    public string? Cuisine { get; set; }
    public bool FamilyFriendly { get; set; }
    public bool ChildFriendly { get; set; }
    public string? AffiliateUrl { get; set; }
    public string? AffiliatePartner { get; set; }
    public string? ImageUrl { get; set; }
    public int PopularityScore { get; set; }

    /// <summary>OpenStreetMap node/way id or other external key.</summary>
    public string? ExternalId { get; set; }

    /// <summary>OpenStreetMap | Seed | Manual</summary>
    public string Source { get; set; } = "Seed";

    public string? OpeningHours { get; set; }
    public DateTime? FetchedAtUtc { get; set; }
}
