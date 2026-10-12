using TripPlatform.Domain.Common;
using TripPlatform.Domain.Enums;

namespace TripPlatform.Domain.Entities;

public class Route : AuditableEntity
{
    public int OriginDestinationId { get; set; }
    public Destination? OriginDestination { get; set; }
    public int TargetDestinationId { get; set; }
    public Destination? TargetDestination { get; set; }
    public double DistanceKm { get; set; }
    public ICollection<RouteOption> Options { get; set; } = new List<RouteOption>();
}

public class RouteOption : AuditableEntity
{
    public int RouteId { get; set; }
    public Route? Route { get; set; }
    public TravelMode TravelMode { get; set; }
    public int DurationMinutes { get; set; }
    public decimal CostMin { get; set; }
    public decimal CostTypical { get; set; }
    public decimal CostMax { get; set; }
    public string Currency { get; set; } = "USD";
    public string? SeasonHint { get; set; }
    public string? Notes { get; set; }
    public ConfidenceLevel Confidence { get; set; } = ConfidenceLevel.Medium;
}
