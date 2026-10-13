using TripPlatform.Domain.Common;
using TripPlatform.Domain.Enums;

namespace TripPlatform.Domain.Entities;

public class Country : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? CurrencyCode { get; set; }

    public ICollection<Region> Regions { get; set; } = new List<Region>();
    public ICollection<Destination> Destinations { get; set; } = new List<Destination>();
}

public class Region : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public int CountryId { get; set; }
    public Country? Country { get; set; }

    public ICollection<Destination> Destinations { get; set; } = new List<Destination>();
}

public class Destination : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DestinationType DestinationType { get; set; } = DestinationType.City;
    public int CountryId { get; set; }
    public Country? Country { get; set; }
    public int? RegionId { get; set; }
    public Region? Region { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsInternationalHub { get; set; }

    public int GlobalPopularityScore { get; set; }
    public int RegionalPopularityScore { get; set; }
    public int FamilyFriendlyScore { get; set; }
    public int CoupleScore { get; set; }
    public int SoloScore { get; set; }
    public int LuxuryScore { get; set; }
    public int BudgetScore { get; set; }
    public int RomanceScore { get; set; }
    public int CultureScore { get; set; }
    public int AdventureScore { get; set; }

    public ICollection<DestinationCategoryMap> CategoryMaps { get; set; } = new List<DestinationCategoryMap>();
    public ICollection<Attraction> Attractions { get; set; } = new List<Attraction>();
    public ICollection<Activity> Activities { get; set; } = new List<Activity>();
    public ICollection<DestinationCostProfile> CostProfiles { get; set; } = new List<DestinationCostProfile>();
    public ICollection<Season> Seasons { get; set; } = new List<Season>();
    public ICollection<OriginPopularity> OriginPopularities { get; set; } = new List<OriginPopularity>();
    public ICollection<Listing> Listings { get; set; } = new List<Listing>();
    public ICollection<DestinationInclusion> Inclusions { get; set; } = new List<DestinationInclusion>();
}

public class DestinationCategory : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<DestinationCategoryMap> DestinationMaps { get; set; } = new List<DestinationCategoryMap>();
}

public class DestinationCategoryMap
{
    public int DestinationId { get; set; }
    public Destination? Destination { get; set; }
    public int CategoryId { get; set; }
    public DestinationCategory? Category { get; set; }
}

public class OriginPopularity
{
    public int Id { get; set; }
    public int OriginDestinationId { get; set; }
    public Destination? OriginDestination { get; set; }
    public int TargetDestinationId { get; set; }
    public Destination? TargetDestination { get; set; }
    public int Score { get; set; }
    public string Bucket { get; set; } = "Nearby"; // Nearby | Domestic | InternationalBudget | InternationalMid | InternationalPremium
}
