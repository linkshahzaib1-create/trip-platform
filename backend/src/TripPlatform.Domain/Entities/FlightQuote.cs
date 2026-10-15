using TripPlatform.Domain.Common;

namespace TripPlatform.Domain.Entities;

/// <summary>
/// Cached flight fare snapshot (open-data / estimated live quote stored locally).
/// </summary>
public class FlightQuote : AuditableEntity
{
    public int OriginDestinationId { get; set; }
    public Destination? OriginDestination { get; set; }
    public int TargetDestinationId { get; set; }
    public Destination? TargetDestination { get; set; }

    public string Airline { get; set; } = "Multiple carriers";
    public string CabinClass { get; set; } = "Economy";
    public int DurationMinutes { get; set; }
    public decimal PriceMin { get; set; }
    public decimal PriceTypical { get; set; }
    public decimal PriceMax { get; set; }
    public string Currency { get; set; } = "USD";
    public string? BaggageNotes { get; set; }
    public string? ScheduleNotes { get; set; }
    public string Source { get; set; } = "RouteEstimate";
    public DateTime FetchedAtUtc { get; set; } = DateTime.UtcNow;
}
