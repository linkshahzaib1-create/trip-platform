using TripPlatform.Domain.Common;
using TripPlatform.Domain.Enums;

namespace TripPlatform.Domain.Entities;

public class Attraction : AuditableEntity
{
    public int DestinationId { get; set; }
    public Destination? Destination { get; set; }
    public string? ExternalPlaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Category { get; set; } = "Sightseeing";
    public int AverageVisitDurationMinutes { get; set; } = 90;
    public decimal EntryCostMin { get; set; }
    public decimal EntryCostTypical { get; set; }
    public decimal EntryCostMax { get; set; }
    public string Currency { get; set; } = "USD";
    public string? OpeningTime { get; set; }
    public string? ClosingTime { get; set; }
    public int PopularityScore { get; set; }
    public int FamilyScore { get; set; }
    public int CoupleScore { get; set; }
    public int SoloScore { get; set; }
    public int RomanceScore { get; set; }
    public int CultureScore { get; set; }
    public int PhotographyScore { get; set; }
    public int AccessibilityScore { get; set; } = 3;
}

public class Activity : AuditableEntity
{
    public int DestinationId { get; set; }
    public Destination? Destination { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = "Tour";
    public int DurationMinutes { get; set; } = 180;
    public decimal CostMin { get; set; }
    public decimal CostTypical { get; set; }
    public decimal CostMax { get; set; }
    public string Currency { get; set; } = "USD";
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public string BudgetLevel { get; set; } = "Balanced";
    public int FamilyScore { get; set; }
    public int CoupleScore { get; set; }
    public int SoloScore { get; set; }
    public string? SeasonHint { get; set; }
    public string? AffiliateUrl { get; set; }
}
