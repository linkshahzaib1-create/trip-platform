using TripPlatform.Domain.Common;
using TripPlatform.Domain.Enums;

namespace TripPlatform.Domain.Entities;

public class Season : AuditableEntity
{
    public int DestinationId { get; set; }
    public Destination? Destination { get; set; }
    public string Name { get; set; } = string.Empty;
    public SeasonKind Kind { get; set; }
    public int StartMonth { get; set; }
    public int StartDay { get; set; } = 1;
    public int EndMonth { get; set; }
    public int EndDay { get; set; } = 28;
    public decimal PriceMultiplier { get; set; } = 1.0m;
    public string? Notes { get; set; }
}

public class DestinationCostProfile : AuditableEntity
{
    public int DestinationId { get; set; }
    public Destination? Destination { get; set; }
    public int? SeasonId { get; set; }
    public Season? Season { get; set; }
    public string BudgetTier { get; set; } = "Balanced"; // Budget | Balanced | Comfort | Luxury
    public string Currency { get; set; } = "USD";

    public decimal AccommodationMin { get; set; }
    public decimal AccommodationTypical { get; set; }
    public decimal AccommodationMax { get; set; }

    public decimal FoodMin { get; set; }
    public decimal FoodTypical { get; set; }
    public decimal FoodMax { get; set; }

    public decimal LocalTransportMin { get; set; }
    public decimal LocalTransportTypical { get; set; }
    public decimal LocalTransportMax { get; set; }

    public decimal ActivitiesMin { get; set; }
    public decimal ActivitiesTypical { get; set; }
    public decimal ActivitiesMax { get; set; }

    public decimal EntertainmentMin { get; set; }
    public decimal EntertainmentTypical { get; set; }
    public decimal EntertainmentMax { get; set; }

    public decimal ShoppingMin { get; set; }
    public decimal ShoppingTypical { get; set; }
    public decimal ShoppingMax { get; set; }

    public decimal AirportTransferMin { get; set; }
    public decimal AirportTransferTypical { get; set; }
    public decimal AirportTransferMax { get; set; }

    public decimal ContingencyPercent { get; set; } = 10m;

    public string Source { get; set; } = "Seed";
    public string? SourceId { get; set; }
    public DateTime RetrievedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ValidFromUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ValidToUtc { get; set; }
    public ConfidenceLevel Confidence { get; set; } = ConfidenceLevel.Medium;
}
