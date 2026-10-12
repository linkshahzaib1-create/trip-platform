using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TripPlatform.Application.Common.Interfaces;
using TripPlatform.Application.Common.Models;
using TripPlatform.Domain.Entities;
using TripPlatform.Domain.Enums;

namespace TripPlatform.Application.Destinations.Queries.RecommendDestinations;

public sealed record TravelerAgeDto(int Age);

public sealed record RecommendDestinationsCommand(
    string Origin,
    DateOnly StartDate,
    DateOnly EndDate,
    IReadOnlyList<TravelerAgeDto> Travelers,
    decimal Budget,
    string Currency,
    string TravelerCategory,
    string? Subcategory,
    IReadOnlyList<string> Priorities) : IRequest<RecommendDestinationsResponse>;

public sealed record RecommendationDto(
    string Destination,
    string Slug,
    int DestinationId,
    CostRangeDto EstimatedCost,
    string TravelTime,
    string TravelModes,
    string BudgetFit,
    string SuitableFor,
    string Bucket,
    IReadOnlyList<string> Reasons,
    int Score);

public sealed record RecommendDestinationsResponse(
    string Origin,
    IReadOnlyList<RecommendationDto> Recommendations);

public sealed class RecommendDestinationsCommandValidator : AbstractValidator<RecommendDestinationsCommand>
{
    public RecommendDestinationsCommandValidator()
    {
        RuleFor(x => x.Origin).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Budget).GreaterThan(0);
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate);
        RuleFor(x => x.Travelers).NotEmpty();
        RuleForEach(x => x.Travelers).ChildRules(t => t.RuleFor(a => a.Age).InclusiveBetween(0, 120));
    }
}

public sealed class RecommendDestinationsCommandHandler
    : IRequestHandler<RecommendDestinationsCommand, RecommendDestinationsResponse>
{
    private readonly IApplicationDbContext _db;

    public RecommendDestinationsCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<RecommendDestinationsResponse> Handle(
        RecommendDestinationsCommand request,
        CancellationToken cancellationToken)
    {
        var origin = await _db.Destinations
            .AsNoTracking()
            .Include(d => d.Country)
            .FirstOrDefaultAsync(d => d.IsActive && d.Name == request.Origin, cancellationToken)
            ?? await _db.Destinations
                .AsNoTracking()
                .Include(d => d.Country)
                .FirstOrDefaultAsync(d => d.IsActive && d.Name.Contains(request.Origin), cancellationToken);

        if (origin is null)
            throw new InvalidOperationException($"Origin '{request.Origin}' was not found.");

        var days = Math.Max(1, request.EndDate.DayNumber - request.StartDate.DayNumber + 1);
        var nights = Math.Max(1, days - 1);
        var adults = request.Travelers.Count(t => t.Age >= 18);
        var children = request.Travelers.Count(t => t.Age < 18);
        var travelerCount = Math.Max(1, request.Travelers.Count);
        var month = request.StartDate.Month;
        var priorities = request.Priorities.Select(p => p.Trim()).Where(p => p.Length > 0).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var category = request.TravelerCategory ?? string.Empty;

        var popularities = await _db.OriginPopularities
            .AsNoTracking()
            .Include(p => p.TargetDestination)!.ThenInclude(d => d!.Country)
            .Include(p => p.TargetDestination)!.ThenInclude(d => d!.CostProfiles)
            .Include(p => p.TargetDestination)!.ThenInclude(d => d!.Seasons)
            .Where(p => p.OriginDestinationId == origin.Id && p.TargetDestination != null && p.TargetDestination.IsActive)
            .ToListAsync(cancellationToken);

        var routes = await _db.Routes
            .AsNoTracking()
            .Include(r => r.Options)
            .Where(r => r.OriginDestinationId == origin.Id && r.IsActive)
            .ToListAsync(cancellationToken);

        var recommendations = new List<RecommendationDto>();

        foreach (var pop in popularities)
        {
            var dest = pop.TargetDestination!;
            var route = routes.FirstOrDefault(r => r.TargetDestinationId == dest.Id);
            var option = SelectRouteOption(route, request.BudgetModeImplied());
            var season = ResolveSeason(dest.Seasons, month);
            var multiplier = season?.PriceMultiplier ?? 1.0m;
            var profile = SelectCostProfile(dest.CostProfiles, season?.Id, request.BudgetModeImplied());

            if (profile is null || option is null)
                continue;

            var transport = new CostRangeDto
            {
                Min = option.CostMin * travelerCount,
                Typical = option.CostTypical * travelerCount,
                Max = option.CostMax * travelerCount,
                Currency = option.Currency
            };

            var stay = ScaleDaily(profile.AccommodationTypical, profile.AccommodationMin, profile.AccommodationMax, nights, multiplier);
            var food = ScaleDaily(profile.FoodTypical, profile.FoodMin, profile.FoodMax, days * travelerCount, multiplier);
            var local = ScaleDaily(profile.LocalTransportTypical, profile.LocalTransportMin, profile.LocalTransportMax, days, multiplier);
            var activities = ScaleDaily(profile.ActivitiesTypical, profile.ActivitiesMin, profile.ActivitiesMax, days * travelerCount, multiplier);
            var transfer = new CostRangeDto
            {
                Min = profile.AirportTransferMin * multiplier,
                Typical = profile.AirportTransferTypical * multiplier,
                Max = profile.AirportTransferMax * multiplier,
                Currency = profile.Currency
            };

            var total = Sum(
                transport,
                stay,
                food,
                local,
                activities,
                transfer);

            var contingency = total.Typical * (profile.ContingencyPercent / 100m);
            total.Min = Math.Round(total.Min * (1 + profile.ContingencyPercent / 200m), 2);
            total.Typical = Math.Round(total.Typical + contingency, 2);
            total.Max = Math.Round(total.Max * (1 + profile.ContingencyPercent / 100m), 2);

            var fit = ComputeBudgetFit(total.Typical, request.Budget);
            var score = ScoreDestination(dest, pop, fit, priorities, category, children > 0, option);
            var reasons = BuildReasons(dest, season, fit, priorities, category, option);

            recommendations.Add(new RecommendationDto(
                dest.Name,
                dest.Slug,
                dest.Id,
                total,
                FormatDuration(option.DurationMinutes),
                string.Join("/", route!.Options.Where(o => o.IsActive).Select(o => o.TravelMode.ToString()).Distinct()),
                fit.ToString(),
                SuitableFor(dest, category),
                pop.Bucket,
                reasons,
                score));
        }

        var ordered = recommendations
            .OrderByDescending(r => r.Score)
            .ThenBy(r => r.EstimatedCost.Typical)
            .Take(12)
            .ToList();

        return new RecommendDestinationsResponse(origin.Name, ordered);
    }

    private static RouteOption? SelectRouteOption(Route? route, string budgetMode)
    {
        if (route?.Options is null || route.Options.Count == 0)
            return null;

        var active = route.Options.Where(o => o.IsActive).ToList();
        if (active.Count == 0)
            return null;

        return budgetMode switch
        {
            "Budget" => active.OrderBy(o => o.CostTypical).First(),
            "Luxury" or "Premium" or "Comfort" => active.OrderBy(o => o.DurationMinutes).First(),
            _ => active.OrderBy(o => o.CostTypical + o.DurationMinutes / 10m).First()
        };
    }

    private static Season? ResolveSeason(IEnumerable<Season> seasons, int month) =>
        seasons.FirstOrDefault(s => s.IsActive && IsMonthInRange(month, s.StartMonth, s.EndMonth));

    private static bool IsMonthInRange(int month, int start, int end)
    {
        if (start <= end)
            return month >= start && month <= end;
        return month >= start || month <= end;
    }

    private static DestinationCostProfile? SelectCostProfile(
        IEnumerable<DestinationCostProfile> profiles,
        int? seasonId,
        string budgetTier)
    {
        var list = profiles.Where(p => p.IsActive).ToList();
        return list.FirstOrDefault(p => p.SeasonId == seasonId && p.BudgetTier.Equals(budgetTier, StringComparison.OrdinalIgnoreCase))
            ?? list.FirstOrDefault(p => p.BudgetTier.Equals(budgetTier, StringComparison.OrdinalIgnoreCase) && p.SeasonId == null)
            ?? list.FirstOrDefault(p => p.BudgetTier.Equals("Balanced", StringComparison.OrdinalIgnoreCase))
            ?? list.FirstOrDefault();
    }

    private static CostRangeDto ScaleDaily(decimal typical, decimal min, decimal max, int units, decimal multiplier) =>
        new()
        {
            Min = Math.Round(min * units * multiplier, 2),
            Typical = Math.Round(typical * units * multiplier, 2),
            Max = Math.Round(max * units * multiplier, 2),
            Currency = "USD"
        };

    private static CostRangeDto Sum(params CostRangeDto[] parts)
    {
        return new CostRangeDto
        {
            Min = parts.Sum(p => p.Min),
            Typical = parts.Sum(p => p.Typical),
            Max = parts.Sum(p => p.Max),
            Currency = parts.FirstOrDefault()?.Currency ?? "USD"
        };
    }

    private static BudgetFit ComputeBudgetFit(decimal typical, decimal budget)
    {
        if (budget <= 0) return BudgetFit.InsufficientData;
        var ratio = typical / budget;
        if (ratio <= 0.75m) return BudgetFit.Excellent;
        if (ratio <= 1.00m) return BudgetFit.Good;
        if (ratio <= 1.25m) return BudgetFit.Partial;
        return BudgetFit.OverBudget;
    }

    private static int ScoreDestination(
        Destination dest,
        OriginPopularity pop,
        BudgetFit fit,
        HashSet<string> priorities,
        string category,
        bool hasChildren,
        RouteOption option)
    {
        var score = pop.Score;
        score += fit switch
        {
            BudgetFit.Excellent => 25,
            BudgetFit.Good => 18,
            BudgetFit.Partial => 5,
            BudgetFit.OverBudget => -20,
            _ => 0
        };

        if (priorities.Contains("Romance")) score += dest.RomanceScore;
        if (priorities.Contains("Culture")) score += dest.CultureScore;
        if (priorities.Contains("Adventure")) score += dest.AdventureScore;
        if (priorities.Contains("Comfort")) score += dest.LuxuryScore / 2;

        if (category.Contains("Couple", StringComparison.OrdinalIgnoreCase) ||
            category.Contains("Dating", StringComparison.OrdinalIgnoreCase) ||
            category.Contains("Married", StringComparison.OrdinalIgnoreCase))
            score += dest.CoupleScore;

        if (category.Contains("Family", StringComparison.OrdinalIgnoreCase) || hasChildren)
            score += dest.FamilyFriendlyScore;

        if (category.Contains("Solo", StringComparison.OrdinalIgnoreCase))
            score += dest.SoloScore;

        // Prefer shorter travel for short trips bias
        score += Math.Max(0, 30 - option.DurationMinutes / 20);
        return score;
    }

    private static List<string> BuildReasons(
        Destination dest,
        Season? season,
        BudgetFit fit,
        HashSet<string> priorities,
        string category,
        RouteOption option)
    {
        var reasons = new List<string>();
        if (fit is BudgetFit.Excellent or BudgetFit.Good)
            reasons.Add($"Fits the selected budget ({fit})");
        else if (fit == BudgetFit.Partial)
            reasons.Add("Slightly above budget — consider Balanced scenario");

        if (category.Contains("Couple", StringComparison.OrdinalIgnoreCase) && dest.CoupleScore >= 70)
            reasons.Add("Strong match for couples");
        if (priorities.Contains("Romance") && dest.RomanceScore >= 70)
            reasons.Add("High romance score");
        if (priorities.Contains("Culture") && dest.CultureScore >= 70)
            reasons.Add("Rich cultural experiences");
        if (season is not null)
            reasons.Add($"{season.Name} conditions for your dates");
        reasons.Add($"Travel time about {FormatDuration(option.DurationMinutes)} via {option.TravelMode}");
        return reasons;
    }

    private static string SuitableFor(Destination dest, string category)
    {
        if (dest.LuxuryScore >= 85) return "Luxury";
        if (dest.BudgetScore >= 80) return "Budget";
        if (dest.RomanceScore >= 80) return "Romance";
        if (dest.CoupleScore >= 80) return "Couple";
        if (dest.FamilyFriendlyScore >= 80) return "Family";
        if (!string.IsNullOrWhiteSpace(category)) return category;
        return "General";
    }

    private static string FormatDuration(int minutes)
    {
        var h = minutes / 60;
        var m = minutes % 60;
        return m == 0 ? $"{h}h" : $"{h}h {m}m";
    }
}

file static class RecommendExtensions
{
    public static string BudgetModeImplied(this RecommendDestinationsCommand request)
    {
        // Map rough budget-per-day to tier for profile selection
        var days = Math.Max(1, request.EndDate.DayNumber - request.StartDate.DayNumber + 1);
        var perDay = request.Budget / days / Math.Max(1, request.Travelers.Count);
        if (perDay < 80) return "Budget";
        if (perDay < 180) return "Balanced";
        if (perDay < 320) return "Comfort";
        return "Luxury";
    }
}
