using TripPlatform.Domain.Common;
using TripPlatform.Domain.Enums;

namespace TripPlatform.Domain.Entities;

public class TripPlan : AuditableEntity
{
    public string? UserId { get; set; }
    public int OriginDestinationId { get; set; }
    public Destination? OriginDestination { get; set; }
    public int? SelectedDestinationId { get; set; }
    public Destination? SelectedDestination { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int Nights { get; set; }
    public int Days { get; set; }
    public decimal? BudgetTotal { get; set; }
    public string Currency { get; set; } = "USD";
    public BudgetMode BudgetMode { get; set; } = BudgetMode.Balanced;
    public string? TravelerCategoryCode { get; set; }
    public string? TravelerSubcategoryCode { get; set; }
    public string PrioritiesCsv { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft";

    public ICollection<TripTraveler> Travelers { get; set; } = new List<TripTraveler>();
    public ICollection<TripDay> DaysPlan { get; set; } = new List<TripDay>();
    public ICollection<TripEstimate> Estimates { get; set; } = new List<TripEstimate>();
}

public class TripTraveler : BaseEntity
{
    public int TripPlanId { get; set; }
    public TripPlan? TripPlan { get; set; }
    public int Age { get; set; }
    public string Role { get; set; } = "Adult"; // Adult | Child
}

public class TripDay : BaseEntity
{
    public int TripPlanId { get; set; }
    public TripPlan? TripPlan { get; set; }
    public int DayNumber { get; set; }
    public DateOnly Date { get; set; }
    public string? Theme { get; set; }
    public ICollection<ItineraryItem> Items { get; set; } = new List<ItineraryItem>();
}

public class ItineraryItem : BaseEntity
{
    public int TripDayId { get; set; }
    public TripDay? TripDay { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "Activity";
    public int Sequence { get; set; }
    public int DurationMinutes { get; set; }
    public decimal EstimatedCostTypical { get; set; }
    public string Currency { get; set; } = "USD";
    public bool IsOptional { get; set; }
    public string? PriorityTags { get; set; }
    public string? Notes { get; set; }
}

public class TripEstimate : AuditableEntity
{
    public int TripPlanId { get; set; }
    public TripPlan? TripPlan { get; set; }
    public string ScenarioName { get; set; } = "Typical";
    public decimal TotalMin { get; set; }
    public decimal TotalTypical { get; set; }
    public decimal TotalMax { get; set; }
    public string Currency { get; set; } = "USD";
    public BudgetFit BudgetFit { get; set; }
    public string? ExplanationJson { get; set; }
    public string? SnapshotJson { get; set; }
    public ICollection<EstimateLine> Lines { get; set; } = new List<EstimateLine>();
}

public class EstimateLine : BaseEntity
{
    public int TripEstimateId { get; set; }
    public TripEstimate? TripEstimate { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal Min { get; set; }
    public decimal Typical { get; set; }
    public decimal Max { get; set; }
    public string Currency { get; set; } = "USD";
    public string? Notes { get; set; }
}
