using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TripPlatform.Application.Common;
using TripPlatform.Application.Common.Interfaces;
using TripPlatform.Application.Common.Models;
using TripPlatform.Domain.Entities;
using TripPlatform.Domain.Enums;

namespace TripPlatform.Application.Trips.Commands.EstimateTrip;

public sealed record EstimateTravelerDto(int Age);

/// <summary>
/// Trip cost estimate. Optional planning fields are additive — existing clients keep working.
/// PlanningMode: AiBudget | Custom (null = classic auto itinerary).
/// </summary>
public sealed record EstimateTripCommand(
    string Origin,
    string Destination,
    DateOnly StartDate,
    DateOnly EndDate,
    IReadOnlyList<EstimateTravelerDto> Travelers,
    decimal? Budget,
    string Currency,
    string TravelerCategory,
    string? Subcategory,
    IReadOnlyList<string> Priorities,
    string BudgetMode,
    string? PlanningMode = null,
    int? SelectedFlightQuoteId = null,
    int? SelectedListingId = null,
    string? StayType = null,
    IReadOnlyList<int>? SelectedActivityIds = null) : IRequest<EstimateTripResponse>;

public sealed record EstimateLineDto(string Category, decimal Min, decimal Typical, decimal Max, string? Notes);

public sealed record ItineraryItemDto(int DayNumber, string Title, string Category, int DurationMinutes, decimal EstimatedCost, bool IsOptional);

public sealed record EstimateTripResponse(
    int TripPlanId,
    string Origin,
    string Destination,
    int Days,
    int Nights,
    CostRangeDto Total,
    string BudgetFit,
    IReadOnlyList<EstimateLineDto> Breakdown,
    IReadOnlyList<ItineraryItemDto> Itinerary,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> Reasons,
    string? PlanningMode = null);

public sealed class EstimateTripCommandValidator : AbstractValidator<EstimateTripCommand>
{
    public EstimateTripCommandValidator()
    {
        RuleFor(x => x.Origin).NotEmpty();
        RuleFor(x => x.Destination).NotEmpty();
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate);
        RuleFor(x => x.Travelers).NotEmpty();
    }
}

public sealed class EstimateTripCommandHandler : IRequestHandler<EstimateTripCommand, EstimateTripResponse>
{
    private readonly IApplicationDbContext _db;

    public EstimateTripCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<EstimateTripResponse> Handle(EstimateTripCommand request, CancellationToken cancellationToken)
    {
        var origin = await FindDestinationAsync(request.Origin, cancellationToken)
            ?? throw new InvalidOperationException($"Origin '{request.Origin}' not found.");
        var destination = await FindDestinationAsync(request.Destination, cancellationToken)
            ?? throw new InvalidOperationException($"Destination '{request.Destination}' not found.");

        var days = Math.Max(1, request.EndDate.DayNumber - request.StartDate.DayNumber + 1);
        var nights = Math.Max(1, days - 1);
        var travelerCount = Math.Max(1, request.Travelers.Count);
        var month = request.StartDate.Month;
        var budgetMode = string.IsNullOrWhiteSpace(request.BudgetMode) ? "Balanced" : request.BudgetMode;
        var planningMode = string.IsNullOrWhiteSpace(request.PlanningMode) ? "Classic" : request.PlanningMode;

        var season = destination.Seasons.FirstOrDefault(s => s.IsActive && IsMonthInRange(month, s.StartMonth, s.EndMonth));
        var multiplier = season?.PriceMultiplier ?? 1m;
        var profile = destination.CostProfiles
            .Where(p => p.IsActive)
            .FirstOrDefault(p => p.BudgetTier.Equals(budgetMode, StringComparison.OrdinalIgnoreCase))
            ?? destination.CostProfiles.FirstOrDefault(p => p.IsActive);

        if (profile is null)
            throw new InvalidOperationException($"No cost profile for {destination.Name}.");

        // Stay tier adjuster for Apartment/Home vs Hotel
        var stayFactor = (request.StayType ?? "Hotel").ToLowerInvariant() switch
        {
            "apartment" => 0.85m,
            "home" => 0.75m,
            _ => 1.0m
        };

        var lines = new List<EstimateLineDto>();

        // Flight / transport — prefer selected flight quote when provided
        if (request.SelectedFlightQuoteId is int fqId)
        {
            var fq = await _db.FlightQuotes.AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == fqId && f.IsActive, cancellationToken);
            if (fq is not null)
            {
                lines.Add(new EstimateLineDto(
                    "Transport",
                    Math.Round(fq.PriceMin * travelerCount, 2),
                    Math.Round(fq.PriceTypical * travelerCount, 2),
                    Math.Round(fq.PriceMax * travelerCount, 2),
                    $"{fq.Airline} · {fq.CabinClass} · {fq.ScheduleNotes}"));
            }
        }

        if (lines.All(l => l.Category != "Transport"))
        {
            var route = await _db.Routes
                .AsNoTracking()
                .Include(r => r.Options)
                .FirstOrDefaultAsync(r => r.OriginDestinationId == origin.Id && r.TargetDestinationId == destination.Id, cancellationToken);

            var option = route?.Options.Where(o => o.IsActive).OrderBy(o => o.CostTypical).FirstOrDefault();
            if (option is not null)
            {
                lines.Add(new EstimateLineDto(
                    "Transport",
                    Math.Round(option.CostMin * travelerCount, 2),
                    Math.Round(option.CostTypical * travelerCount, 2),
                    Math.Round(option.CostMax * travelerCount, 2),
                    $"{option.TravelMode} (~{option.DurationMinutes} min)"));
            }
        }

        // Accommodation — prefer selected listing when provided
        if (request.SelectedListingId is int listingId)
        {
            var listing = await _db.Listings.AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == listingId && l.IsActive, cancellationToken);
            if (listing?.PriceTypical is not null)
            {
                var min = (listing.PriceMin ?? listing.PriceTypical.Value) * nights;
                var typ = listing.PriceTypical.Value * nights;
                var max = (listing.PriceMax ?? listing.PriceTypical.Value) * nights;
                lines.Add(new EstimateLineDto(
                    "Accommodation",
                    Math.Round(min * multiplier, 2),
                    Math.Round(typ * multiplier, 2),
                    Math.Round(max * multiplier, 2),
                    $"{listing.ListingType}: {listing.Name} × {nights} nights"));
            }
        }

        if (lines.All(l => l.Category != "Accommodation"))
        {
            lines.Add(Line(
                "Accommodation",
                profile.AccommodationMin * stayFactor,
                profile.AccommodationTypical * stayFactor,
                profile.AccommodationMax * stayFactor,
                nights,
                multiplier,
                $"{request.StayType ?? "Hotel"} · per night × {nights}"));
        }

        lines.Add(Line("Food", profile.FoodMin, profile.FoodTypical, profile.FoodMax, days * travelerCount, multiplier, "Per person per day"));
        lines.Add(Line("Local transport", profile.LocalTransportMin, profile.LocalTransportTypical, profile.LocalTransportMax, days, multiplier, null));

        var selectedActivities = await ResolveSelectedActivitiesAsync(destination, request.SelectedActivityIds, cancellationToken);
        if (selectedActivities.Count > 0)
        {
            var actMin = selectedActivities.Sum(a => a.CostMin);
            var actTyp = selectedActivities.Sum(a => a.CostTypical);
            var actMax = selectedActivities.Sum(a => a.CostMax);
            lines.Add(new EstimateLineDto(
                "Activities",
                Math.Round(actMin * multiplier, 2),
                Math.Round(actTyp * multiplier, 2),
                Math.Round(actMax * multiplier, 2),
                $"{selectedActivities.Count} selected experience(s)"));
        }
        else
        {
            lines.Add(Line("Activities", profile.ActivitiesMin, profile.ActivitiesTypical, profile.ActivitiesMax, days * travelerCount, multiplier, null));
        }

        lines.Add(Line("Transfers", profile.AirportTransferMin, profile.AirportTransferTypical, profile.AirportTransferMax, 1, multiplier, "Airport/station transfers"));

        var subMin = lines.Sum(l => l.Min);
        var subTypical = lines.Sum(l => l.Typical);
        var subMax = lines.Sum(l => l.Max);
        var contingencyTypical = Math.Round(subTypical * profile.ContingencyPercent / 100m, 2);
        lines.Add(new EstimateLineDto("Contingency", Math.Round(subMin * profile.ContingencyPercent / 200m, 2), contingencyTypical, Math.Round(subMax * profile.ContingencyPercent / 100m, 2), $"{profile.ContingencyPercent}% reserve"));

        var total = new CostRangeDto
        {
            Min = lines.Sum(l => l.Min),
            Typical = lines.Sum(l => l.Typical),
            Max = lines.Sum(l => l.Max),
            Currency = request.Currency
        };

        // AiBudget: shrink optional activity spend conceptually by noting packing
        var itinerary = BuildItinerary(
            destination,
            days,
            request.StartDate,
            request.Priorities,
            request.TravelerCategory,
            request.Subcategory,
            planningMode,
            request.Budget,
            total.Typical,
            selectedActivities);

        if (planningMode.Equals("AiBudget", StringComparison.OrdinalIgnoreCase) &&
            request.Budget is > 0 &&
            total.Typical > request.Budget.Value)
        {
            // Soft-pack: reduce activities line toward budget by marking overage in reasons; scale activity typical
            var over = total.Typical - request.Budget.Value;
            var actLine = lines.FirstOrDefault(l => l.Category == "Activities");
            if (actLine is not null)
            {
                var idx = lines.IndexOf(actLine);
                var newTyp = Math.Max(0, actLine.Typical - over);
                var newMax = Math.Max(newTyp, actLine.Max - over * 0.5m);
                lines[idx] = actLine with
                {
                    Typical = Math.Round(newTyp, 2),
                    Max = Math.Round(newMax, 2),
                    Notes = (actLine.Notes ?? "") + " · smart-budget trimmed"
                };
                total.Typical = lines.Sum(l => l.Typical);
                total.Max = lines.Sum(l => l.Max);
                total.Min = lines.Sum(l => l.Min);
            }
        }

        var fit = request.Budget is null or <= 0
            ? BudgetFit.InsufficientData
            : total.Typical / request.Budget.Value <= 0.75m ? BudgetFit.Excellent
            : total.Typical / request.Budget.Value <= 1.0m ? BudgetFit.Good
            : total.Typical / request.Budget.Value <= 1.25m ? BudgetFit.Partial
            : BudgetFit.OverBudget;

        var assumptions = new List<string>
        {
            "Estimates are planning ranges, not live booking prices — use Check live price links for current fares.",
            $"Season applied: {season?.Name ?? "Default"} (×{multiplier:0.00}).",
            $"Cost profile tier: {profile.BudgetTier} (confidence: {profile.Confidence}).",
            $"Planning mode: {planningMode}.",
            $"Stay type: {request.StayType ?? "Hotel"}."
        };

        var reasons = new List<string>();
        if (request.Priorities.Any(p => p.Equals("Romance", StringComparison.OrdinalIgnoreCase)) && destination.RomanceScore >= 70)
            reasons.Add("Destination scores high for romance");
        if (fit is BudgetFit.Excellent or BudgetFit.Good)
            reasons.Add("Typical estimate fits within budget");
        if (planningMode.Equals("AiBudget", StringComparison.OrdinalIgnoreCase))
            reasons.Add("Smart budget planner prioritized experiences within your total");
        if (planningMode.Equals("Custom", StringComparison.OrdinalIgnoreCase) && selectedActivities.Count > 0)
            reasons.Add($"Custom mode: {selectedActivities.Count} activities you selected");
        if (!string.IsNullOrWhiteSpace(request.Subcategory))
            reasons.Add($"Tailored for subcategory: {request.Subcategory}");

        var trip = new TripPlan
        {
            OriginDestinationId = origin.Id,
            SelectedDestinationId = destination.Id,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Days = days,
            Nights = nights,
            BudgetTotal = request.Budget,
            Currency = request.Currency,
            BudgetMode = Enum.TryParse<BudgetMode>(budgetMode, true, out var bm) ? bm : BudgetMode.Balanced,
            TravelerCategoryCode = request.TravelerCategory,
            TravelerSubcategoryCode = request.Subcategory,
            PrioritiesCsv = string.Join(',', request.Priorities),
            Status = "Estimated",
            Travelers = request.Travelers.Select(t => new TripTraveler
            {
                Age = t.Age,
                Role = t.Age >= 18 ? "Adult" : "Child"
            }).ToList()
        };

        var estimate = new TripEstimate
        {
            ScenarioName = planningMode,
            TotalMin = total.Min,
            TotalTypical = total.Typical,
            TotalMax = total.Max,
            Currency = request.Currency,
            BudgetFit = fit,
            ExplanationJson = System.Text.Json.JsonSerializer.Serialize(reasons),
            Lines = lines.Select(l => new EstimateLine
            {
                Category = l.Category,
                Min = l.Min,
                Typical = l.Typical,
                Max = l.Max,
                Currency = request.Currency,
                Notes = l.Notes
            }).ToList()
        };
        trip.Estimates.Add(estimate);

        for (var d = 1; d <= days; d++)
        {
            var dayItems = itinerary.Where(i => i.DayNumber == d).ToList();
            var day = new TripDay
            {
                DayNumber = d,
                Date = request.StartDate.AddDays(d - 1),
                Theme = ResolveDayTheme(d, days),
                Items = dayItems.Select((item, idx) => new ItineraryItem
                {
                    Title = item.Title,
                    Category = item.Category,
                    Sequence = idx + 1,
                    DurationMinutes = item.DurationMinutes,
                    EstimatedCostTypical = item.EstimatedCost,
                    Currency = request.Currency,
                    IsOptional = item.IsOptional
                }).ToList()
            };
            trip.DaysPlan.Add(day);
        }

        _db.TripPlans.Add(trip);
        await _db.SaveChangesAsync(cancellationToken);

        return new EstimateTripResponse(
            trip.Id,
            origin.Name,
            destination.Name,
            days,
            nights,
            total,
            fit.ToString(),
            lines,
            itinerary,
            assumptions,
            reasons,
            planningMode);
    }

    private async Task<List<Activity>> ResolveSelectedActivitiesAsync(
        Destination destination,
        IReadOnlyList<int>? ids,
        CancellationToken ct)
    {
        if (ids is null || ids.Count == 0)
            return new List<Activity>();

        var fromDest = destination.Activities.Where(a => a.IsActive && ids.Contains(a.Id)).ToList();
        if (fromDest.Count == ids.Count)
            return fromDest;

        return await _db.Activities
            .AsNoTracking()
            .Where(a => a.IsActive && ids.Contains(a.Id))
            .ToListAsync(ct);
    }

    private async Task<Destination?> FindDestinationAsync(string name, CancellationToken ct) =>
        await _db.Destinations
            .Include(d => d.CostProfiles)
            .Include(d => d.Seasons)
            .Include(d => d.Attractions)
            .Include(d => d.Activities)
            .FirstOrDefaultAsync(d => d.IsActive && (d.Name == name || d.Slug == name.ToLowerInvariant()), ct)
        ?? await _db.Destinations
            .Include(d => d.CostProfiles)
            .Include(d => d.Seasons)
            .Include(d => d.Attractions)
            .Include(d => d.Activities)
            .FirstOrDefaultAsync(d => d.IsActive && d.Name.Contains(name), ct);

    private static EstimateLineDto Line(string category, decimal min, decimal typical, decimal max, int units, decimal multiplier, string? notes) =>
        new(category,
            Math.Round(min * units * multiplier, 2),
            Math.Round(typical * units * multiplier, 2),
            Math.Round(max * units * multiplier, 2),
            notes);

    private static bool IsMonthInRange(int month, int start, int end) =>
        start <= end ? month >= start && month <= end : month >= start || month <= end;

    private static string ResolveDayTheme(int d, int days)
    {
        if (d == 1) return "Arrival";
        if (d == days) return "Departure";
        if (days >= 10 && d <= Math.Max(5, days / 2)) return "Core exploration";
        if (days >= 10) return "Extended / side trips";
        return "Explore";
    }

    private static List<ItineraryItemDto> BuildItinerary(
        Destination destination,
        int days,
        DateOnly start,
        IReadOnlyList<string> priorities,
        string category,
        string? subcategory,
        string planningMode,
        decimal? budget,
        decimal currentTypical,
        IReadOnlyList<Activity> selectedActivities)
    {
        var items = new List<ItineraryItemDto>();
        var attractions = destination.Attractions.Where(a => a.IsActive).OrderByDescending(a => a.PopularityScore).ToList();
        var activities = selectedActivities.Count > 0
            ? selectedActivities.ToList()
            : destination.Activities.Where(a => a.IsActive).OrderByDescending(a => a.CoupleScore + a.FamilyScore).ToList();

        // AiBudget: prefer cheaper activities when over budget
        if (planningMode.Equals("AiBudget", StringComparison.OrdinalIgnoreCase) &&
            budget is > 0 &&
            currentTypical > budget.Value)
        {
            activities = activities.OrderBy(a => a.CostTypical).ToList();
        }

        var attrIndex = 0;
        var actIndex = 0;
        var midPoint = Math.Max(5, days / 2);
        var isFamily = category.Contains("Famil", StringComparison.OrdinalIgnoreCase)
            || string.Equals(subcategory, "YoungKids", StringComparison.OrdinalIgnoreCase)
            || string.Equals(subcategory, "YOUNG_KIDS", StringComparison.OrdinalIgnoreCase);
        var isCouple = category.Contains("Couple", StringComparison.OrdinalIgnoreCase)
            || category.Contains("Dating", StringComparison.OrdinalIgnoreCase)
            || category.Contains("Married", StringComparison.OrdinalIgnoreCase);

        for (var d = 1; d <= days; d++)
        {
            if (d == 1)
            {
                items.Add(new ItineraryItemDto(d, "Arrival & transfer to accommodation", "Transfer", 90, 25, false));
                items.Add(new ItineraryItemDto(d, "Check-in and neighborhood walk", "Leisure", 120, 0, false));
                items.Add(new ItineraryItemDto(d, "Welcome dinner", "Food", 90, 30, false));
            }
            else if (d == days)
            {
                items.Add(new ItineraryItemDto(d, "Breakfast and final stroll", "Food", 60, 15, false));
                if (attrIndex < attractions.Count)
                {
                    var a = attractions[attrIndex++];
                    items.Add(new ItineraryItemDto(d, a.Name, "Attraction", a.AverageVisitDurationMinutes, a.EntryCostTypical, true));
                }
                items.Add(new ItineraryItemDto(d, "Transfer to airport/station", "Transfer", 90, 25, false));
            }
            else if (days >= 10 && d > midPoint)
            {
                // Multi-week / extended pacing — heritage side-trip style blocks
                var sideThemes = new[]
                {
                    $"Day trip: heritage forts & crafts near {destination.Name}",
                    $"Cultural market & traditional workshops",
                    $"Scenic outlook / nature excursion from {destination.Name}",
                    $"Local museum & craft shopping (Khan Market–style browse)",
                    $"Relaxed café morning + evening cultural show"
                };
                var theme = sideThemes[(d - midPoint - 1) % sideThemes.Length];
                items.Add(new ItineraryItemDto(d, theme, "SideTrip", 240, 35, false));
                if (actIndex < activities.Count)
                {
                    var act = activities[actIndex++];
                    var optional = planningMode.Equals("AiBudget", StringComparison.OrdinalIgnoreCase);
                    items.Add(new ItineraryItemDto(d, act.Name, "Activity", act.DurationMinutes, act.CostTypical, optional));
                }
                items.Add(new ItineraryItemDto(d, "Regional lunch", "Food", 60, 18, false));
            }
            else
            {
                if (attrIndex < attractions.Count)
                {
                    var a = attractions[attrIndex++];
                    items.Add(new ItineraryItemDto(d, a.Name, "Attraction", a.AverageVisitDurationMinutes, a.EntryCostTypical, false));
                }
                else if (days >= 7)
                {
                    items.Add(new ItineraryItemDto(d, $"{destination.Name} heritage walking circuit", "Culture", 150, 12, false));
                }

                if (actIndex < activities.Count)
                {
                    var act = activities[actIndex++];
                    var forceInclude = selectedActivities.Count > 0
                        || priorities.Any(p => p.Contains("Romance", StringComparison.OrdinalIgnoreCase))
                        || isCouple
                        || isFamily;
                    items.Add(new ItineraryItemDto(d, act.Name, "Activity", act.DurationMinutes, act.CostTypical, !forceInclude));
                }

                items.Add(new ItineraryItemDto(d, isFamily ? "Family-friendly lunch" : "Local lunch", "Food", 60, 20, false));
                items.Add(new ItineraryItemDto(d, "Evening free time / optional shopping", "Leisure", 90, 15, true));
            }
        }

        _ = start;
        _ = DestinationMediaCatalog.HeroFor(destination.Slug); // keep catalog referenced / warm
        return items;
    }
}
