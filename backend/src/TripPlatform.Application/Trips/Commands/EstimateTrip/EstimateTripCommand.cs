using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TripPlatform.Application.Common.Interfaces;
using TripPlatform.Application.Common.Models;
using TripPlatform.Domain.Entities;
using TripPlatform.Domain.Enums;

namespace TripPlatform.Application.Trips.Commands.EstimateTrip;

public sealed record EstimateTravelerDto(int Age);

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
    string BudgetMode) : IRequest<EstimateTripResponse>;

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
    IReadOnlyList<string> Reasons);

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

        var season = destination.Seasons.FirstOrDefault(s => s.IsActive && IsMonthInRange(month, s.StartMonth, s.EndMonth));
        var multiplier = season?.PriceMultiplier ?? 1m;
        var profile = destination.CostProfiles
            .Where(p => p.IsActive)
            .FirstOrDefault(p => p.BudgetTier.Equals(budgetMode, StringComparison.OrdinalIgnoreCase))
            ?? destination.CostProfiles.FirstOrDefault(p => p.IsActive);

        if (profile is null)
            throw new InvalidOperationException($"No cost profile for {destination.Name}.");

        var route = await _db.Routes
            .AsNoTracking()
            .Include(r => r.Options)
            .FirstOrDefaultAsync(r => r.OriginDestinationId == origin.Id && r.TargetDestinationId == destination.Id, cancellationToken);

        var option = route?.Options.Where(o => o.IsActive).OrderBy(o => o.CostTypical).FirstOrDefault();

        var lines = new List<EstimateLineDto>();
        if (option is not null)
        {
            lines.Add(new EstimateLineDto(
                "Transport",
                Math.Round(option.CostMin * travelerCount, 2),
                Math.Round(option.CostTypical * travelerCount, 2),
                Math.Round(option.CostMax * travelerCount, 2),
                $"{option.TravelMode} (~{option.DurationMinutes} min)"));
        }

        lines.Add(Line("Accommodation", profile.AccommodationMin, profile.AccommodationTypical, profile.AccommodationMax, nights, multiplier, $"Per night × {nights}"));
        lines.Add(Line("Food", profile.FoodMin, profile.FoodTypical, profile.FoodMax, days * travelerCount, multiplier, "Per person per day"));
        lines.Add(Line("Local transport", profile.LocalTransportMin, profile.LocalTransportTypical, profile.LocalTransportMax, days, multiplier, null));
        lines.Add(Line("Activities", profile.ActivitiesMin, profile.ActivitiesTypical, profile.ActivitiesMax, days * travelerCount, multiplier, null));
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

        var fit = request.Budget is null or <= 0
            ? BudgetFit.InsufficientData
            : total.Typical / request.Budget.Value <= 0.75m ? BudgetFit.Excellent
            : total.Typical / request.Budget.Value <= 1.0m ? BudgetFit.Good
            : total.Typical / request.Budget.Value <= 1.25m ? BudgetFit.Partial
            : BudgetFit.OverBudget;

        var itinerary = BuildItinerary(destination, days, request.StartDate, request.Priorities, request.TravelerCategory);
        var assumptions = new List<string>
        {
            "Estimates are planning ranges, not live booking prices.",
            $"Season applied: {season?.Name ?? "Default"} (×{multiplier:0.00}).",
            $"Cost profile tier: {profile.BudgetTier} (confidence: {profile.Confidence}).",
            "Affiliate booking handoff can be used later for actual reservations."
        };

        var reasons = new List<string>();
        if (request.Priorities.Any(p => p.Equals("Romance", StringComparison.OrdinalIgnoreCase)) && destination.RomanceScore >= 70)
            reasons.Add("Destination scores high for romance");
        if (fit is BudgetFit.Excellent or BudgetFit.Good)
            reasons.Add("Typical estimate fits within budget");
        if (option is not null)
            reasons.Add($"Selected {option.TravelMode} as primary transport");

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
            ScenarioName = "Typical",
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
                Theme = d == 1 ? "Arrival" : d == days ? "Departure" : "Explore",
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
            reasons);
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

    private static List<ItineraryItemDto> BuildItinerary(
        Destination destination,
        int days,
        DateOnly start,
        IReadOnlyList<string> priorities,
        string category)
    {
        var items = new List<ItineraryItemDto>();
        var attractions = destination.Attractions.Where(a => a.IsActive).OrderByDescending(a => a.PopularityScore).ToList();
        var activities = destination.Activities.Where(a => a.IsActive).OrderByDescending(a => a.CoupleScore + a.FamilyScore).ToList();
        var attrIndex = 0;
        var actIndex = 0;

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
            else
            {
                if (attrIndex < attractions.Count)
                {
                    var a = attractions[attrIndex++];
                    items.Add(new ItineraryItemDto(d, a.Name, "Attraction", a.AverageVisitDurationMinutes, a.EntryCostTypical, false));
                }
                if (actIndex < activities.Count && (priorities.Any(p => p.Contains("Romance", StringComparison.OrdinalIgnoreCase)) || category.Contains("Couple", StringComparison.OrdinalIgnoreCase)))
                {
                    var act = activities[actIndex++];
                    items.Add(new ItineraryItemDto(d, act.Name, "Activity", act.DurationMinutes, act.CostTypical, false));
                }
                else if (actIndex < activities.Count)
                {
                    var act = activities[actIndex++];
                    items.Add(new ItineraryItemDto(d, act.Name, "Activity", act.DurationMinutes, act.CostTypical, true));
                }
                items.Add(new ItineraryItemDto(d, "Local lunch", "Food", 60, 20, false));
                items.Add(new ItineraryItemDto(d, "Evening free time / optional shopping", "Leisure", 90, 15, true));
            }
        }

        _ = start; // reserved for date-aware scheduling
        return items;
    }
}
