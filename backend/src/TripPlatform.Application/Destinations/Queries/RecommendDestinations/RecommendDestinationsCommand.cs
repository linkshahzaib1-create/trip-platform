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
    int Adults,
    IReadOnlyList<int>? ChildAges,
    IReadOnlyList<TravelerAgeDto>? Travelers,
    decimal Budget,
    string Currency,
    string TravelerCategory,
    string? Subcategory,
    IReadOnlyList<string> Priorities) : IRequest<RecommendDestinationsResponse>;

public sealed record RecommendationDto(
    string Destination,
    string Slug,
    int DestinationId,
    string Country,
    string Description,
    CostRangeDto EstimatedCost,
    string TravelTime,
    string TravelModes,
    string BudgetFit,
    string SuitableFor,
    string Bucket,
    bool FamilyFriendly,
    int FamilyScore,
    IReadOnlyList<string> Highlights,
    IReadOnlyList<string> Reasons,
    int Score);

public sealed record RecommendDestinationsResponse(
    string Origin,
    int Adults,
    int Children,
    IReadOnlyList<int> ChildAges,
    IReadOnlyList<RecommendationDto> Recommendations);

public sealed class RecommendDestinationsCommandValidator : AbstractValidator<RecommendDestinationsCommand>
{
    public RecommendDestinationsCommandValidator()
    {
        RuleFor(x => x.Origin).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Budget).GreaterThan(0);
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate);
        RuleFor(x => x.Adults).GreaterThanOrEqualTo(1).When(x => x.Travelers is null || x.Travelers.Count == 0);
        RuleForEach(x => x.ChildAges).InclusiveBetween(0, 17);
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
        var childAges = request.ChildAges?.Where(a => a >= 0 && a < 18).ToList() ?? new List<int>();
        var travelers = BuildTravelers(request.Adults, childAges, request.Travelers);

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
        var adults = travelers.Count(t => t.Age >= 18);
        var children = travelers.Count(t => t.Age < 18);
        var travelerCount = Math.Max(1, travelers.Count);
        var month = request.StartDate.Month;
        var priorities = request.Priorities.Select(p => p.Trim()).Where(p => p.Length > 0).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var category = string.IsNullOrWhiteSpace(request.TravelerCategory)
            ? (children > 0 ? "Families" : "Dating & Couples")
            : request.TravelerCategory;
        var hasYoungKids = childAges.Any(a => a <= 11);
        var hasTeens = childAges.Any(a => a is >= 12 and <= 17);

        var popularities = await _db.OriginPopularities
            .AsNoTracking()
            .Include(p => p.TargetDestination)!.ThenInclude(d => d!.Country)
            .Include(p => p.TargetDestination)!.ThenInclude(d => d!.CostProfiles)
            .Include(p => p.TargetDestination)!.ThenInclude(d => d!.Seasons)
            .Include(p => p.TargetDestination)!.ThenInclude(d => d!.Attractions)
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

            // With children, prefer family-friendly destinations and de-prioritize ultra-luxury adult-only spots
            if (children > 0 && dest.FamilyFriendlyScore < 55 && dest.LuxuryScore >= 90 && dest.RomanceScore >= 95)
                continue;

            var route = routes.FirstOrDefault(r => r.TargetDestinationId == dest.Id);
            var option = SelectRouteOption(route, BudgetModeImplied(request, travelers.Count));
            var season = ResolveSeason(dest.Seasons, month);
            var multiplier = season?.PriceMultiplier ?? 1.0m;
            var profile = SelectCostProfile(dest.CostProfiles, season?.Id, BudgetModeImplied(request, travelers.Count));

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

            var total = Sum(transport, stay, food, local, activities, transfer);
            var contingency = total.Typical * (profile.ContingencyPercent / 100m);
            total.Min = Math.Round(total.Min * (1 + profile.ContingencyPercent / 200m), 2);
            total.Typical = Math.Round(total.Typical + contingency, 2);
            total.Max = Math.Round(total.Max * (1 + profile.ContingencyPercent / 100m), 2);

            var fit = ComputeBudgetFit(total.Typical, request.Budget);
            var score = ScoreDestination(dest, pop, fit, priorities, category, children > 0, hasYoungKids, hasTeens, option);
            var reasons = BuildReasons(dest, season, fit, priorities, category, option, children, hasYoungKids);
            var highlights = dest.Attractions
                .Where(a => a.IsActive)
                .OrderByDescending(a => children > 0 ? a.FamilyScore : a.PopularityScore)
                .Take(4)
                .Select(a => a.Name)
                .ToList();

            recommendations.Add(new RecommendationDto(
                dest.Name,
                dest.Slug,
                dest.Id,
                dest.Country?.Name ?? string.Empty,
                dest.Description ?? $"{dest.Name} travel destination",
                total,
                FormatDuration(option.DurationMinutes),
                string.Join("/", route!.Options.Where(o => o.IsActive).Select(o => o.TravelMode.ToString()).Distinct()),
                fit.ToString(),
                SuitableFor(dest, category, children > 0),
                pop.Bucket,
                dest.FamilyFriendlyScore >= 70,
                dest.FamilyFriendlyScore,
                highlights,
                reasons,
                score));
        }

        var ordered = recommendations
            .OrderByDescending(r => r.Score)
            .ThenBy(r => r.EstimatedCost.Typical)
            .Take(12)
            .ToList();

        return new RecommendDestinationsResponse(origin.Name, adults, children, childAges, ordered);
    }

    private static List<TravelerAgeDto> BuildTravelers(int adults, IReadOnlyList<int> childAges, IReadOnlyList<TravelerAgeDto>? travelers)
    {
        if (travelers is { Count: > 0 })
            return travelers.ToList();

        var list = new List<TravelerAgeDto>();
        for (var i = 0; i < Math.Max(1, adults); i++)
            list.Add(new TravelerAgeDto(30));
        list.AddRange(childAges.Select(a => new TravelerAgeDto(a)));
        return list;
    }

    private static string BudgetModeImplied(RecommendDestinationsCommand request, int travelerCount)
    {
        var days = Math.Max(1, request.EndDate.DayNumber - request.StartDate.DayNumber + 1);
        var perDay = request.Budget / days / Math.Max(1, travelerCount);
        if (perDay < 80) return "Budget";
        if (perDay < 180) return "Balanced";
        if (perDay < 320) return "Comfort";
        return "Luxury";
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

    private static bool IsMonthInRange(int month, int start, int end) =>
        start <= end ? month >= start && month <= end : month >= start || month <= end;

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

    private static CostRangeDto Sum(params CostRangeDto[] parts) =>
        new()
        {
            Min = parts.Sum(p => p.Min),
            Typical = parts.Sum(p => p.Typical),
            Max = parts.Sum(p => p.Max),
            Currency = parts.FirstOrDefault()?.Currency ?? "USD"
        };

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
        bool hasYoungKids,
        bool hasTeens,
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
        if (priorities.Contains("Family") || hasChildren) score += dest.FamilyFriendlyScore;

        if (hasChildren)
        {
            score += dest.FamilyFriendlyScore;
            if (hasYoungKids) score += dest.FamilyFriendlyScore / 2;
            if (hasTeens) score += dest.AdventureScore / 3;
            if (pop.Bucket is "Nearby" or "Domestic") score += 15;
        }
        else if (category.Contains("Couple", StringComparison.OrdinalIgnoreCase) ||
                 category.Contains("Dating", StringComparison.OrdinalIgnoreCase) ||
                 category.Contains("Married", StringComparison.OrdinalIgnoreCase))
        {
            score += dest.CoupleScore;
        }

        if (category.Contains("Solo", StringComparison.OrdinalIgnoreCase))
            score += dest.SoloScore;

        score += Math.Max(0, 30 - option.DurationMinutes / 20);
        return score;
    }

    private static List<string> BuildReasons(
        Destination dest,
        Season? season,
        BudgetFit fit,
        HashSet<string> priorities,
        string category,
        RouteOption option,
        int children,
        bool hasYoungKids)
    {
        var reasons = new List<string>();
        if (fit is BudgetFit.Excellent or BudgetFit.Good)
            reasons.Add($"Fits the selected budget ({fit})");
        else if (fit == BudgetFit.Partial)
            reasons.Add("Slightly above budget — consider a shorter stay or Balanced scenario");

        if (children > 0)
        {
            reasons.Add(hasYoungKids
                ? "Strong family-friendly options for younger children"
                : "Good mix of activities for families with teens");
            if (dest.FamilyFriendlyScore >= 80)
                reasons.Add("High family suitability score");
        }
        else if (category.Contains("Couple", StringComparison.OrdinalIgnoreCase) && dest.CoupleScore >= 70)
        {
            reasons.Add("Strong match for couples");
        }

        if (priorities.Contains("Romance") && dest.RomanceScore >= 70)
            reasons.Add("High romance score");
        if (priorities.Contains("Culture") && dest.CultureScore >= 70)
            reasons.Add("Rich cultural experiences");
        if (season is not null)
            reasons.Add($"{season.Name} conditions for your dates");
        reasons.Add($"Travel time about {FormatDuration(option.DurationMinutes)} via {option.TravelMode}");
        return reasons;
    }

    private static string SuitableFor(Destination dest, string category, bool hasChildren)
    {
        if (hasChildren && dest.FamilyFriendlyScore >= 70) return "Family";
        if (dest.LuxuryScore >= 85) return "Luxury";
        if (dest.BudgetScore >= 80) return "Budget";
        if (dest.RomanceScore >= 80) return "Romance";
        if (dest.CoupleScore >= 80) return "Couple";
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
