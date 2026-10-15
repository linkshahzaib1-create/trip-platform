using TripPlatform.Domain.Common;

namespace TripPlatform.Domain.Entities;

/// <summary>
/// Cached local transport quote (taxi, metro, car rental) for a destination.
/// </summary>
public class TransportQuote : AuditableEntity
{
    public int DestinationId { get; set; }
    public Destination? Destination { get; set; }

    /// <summary>Taxi | PublicTransit | CarRental | Transfer</summary>
    public string Mode { get; set; } = "Taxi";
    public string Title { get; set; } = string.Empty;
    public string? Details { get; set; }
    public int? TypicalMinutes { get; set; }
    public decimal PriceMin { get; set; }
    public decimal PriceTypical { get; set; }
    public decimal PriceMax { get; set; }
    public string Currency { get; set; } = "USD";
    public string Source { get; set; } = "CostProfileEstimate";
    public DateTime FetchedAtUtc { get; set; } = DateTime.UtcNow;
}
