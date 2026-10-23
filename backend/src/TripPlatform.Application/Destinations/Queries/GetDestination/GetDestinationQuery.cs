using MediatR;
using Microsoft.EntityFrameworkCore;
using TripPlatform.Application.Common;
using TripPlatform.Application.Common.Interfaces;

namespace TripPlatform.Application.Destinations.Queries.GetDestination;

public sealed record GetDestinationQuery(int Id) : IRequest<DestinationProfileDto?>;

public sealed record DestinationAttractionDto(
    string Name,
    string Category,
    int DurationMinutes,
    decimal EntryCostMin,
    decimal EntryCostTypical,
    decimal EntryCostMax,
    string Currency,
    string? OpeningHours,
    int FamilyScore,
    int CoupleScore);

public sealed record DestinationActivityDto(
    int Id,
    string Name,
    string Category,
    int DurationMinutes,
    decimal CostMin,
    decimal CostTypical,
    decimal CostMax,
    string Currency,
    string BudgetLevel,
    string? Description,
    int FamilyScore,
    int CoupleScore);

public sealed record InclusionItemDto(
    string Title,
    string? Details,
    bool IsCritical,
    int SortOrder);

public sealed record InclusionGroupDto(
    string PackageTier,
    string PackageLabel,
    IReadOnlyList<InclusionItemDto> Items);

public sealed record DestinationInclusionsDto(
    IReadOnlyList<InclusionGroupDto> Included,
    IReadOnlyList<InclusionGroupDto> NotIncluded);

public sealed record DestinationProfileDto(
    int Id,
    string Name,
    string Slug,
    string Country,
    string? Region,
    string? Description,
    int FamilyFriendlyScore,
    int CoupleScore,
    int RomanceScore,
    int CultureScore,
    int AdventureScore,
    IReadOnlyList<string> Highlights,
    IReadOnlyList<DestinationAttractionDto> Attractions,
    IReadOnlyList<DestinationActivityDto> Activities,
    DestinationInclusionsDto Inclusions,
    IReadOnlyList<string> SampleItinerary,
    IReadOnlyList<string> Photos,
    string? HeroImageUrl);

public sealed class GetDestinationQueryHandler : IRequestHandler<GetDestinationQuery, DestinationProfileDto?>
{
    private readonly IApplicationDbContext _db;

    private static readonly Dictionary<string, string> TierLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Standard"] = "STANDARD PACKAGE",
        ["Guided"] = "GUIDED",
        ["PremiumGuided"] = "PREMIUM GUIDED",
        ["Family"] = "FAMILY",
        ["AllVersions"] = "ALL VERSIONS",
        ["SelfGuided"] = "SELF-GUIDED"
    };

    public GetDestinationQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<DestinationProfileDto?> Handle(GetDestinationQuery request, CancellationToken cancellationToken)
    {
        var dest = await _db.Destinations
            .AsNoTracking()
            .Include(d => d.Country)
            .Include(d => d.Region)
            .Include(d => d.Attractions)
            .Include(d => d.Activities)
            .Include(d => d.Inclusions)
            .FirstOrDefaultAsync(d => d.Id == request.Id && d.IsActive, cancellationToken);

        if (dest is null)
            return null;

        var attractions = dest.Attractions
            .Where(a => a.IsActive)
            .OrderByDescending(a => a.PopularityScore)
            .Select(a => new DestinationAttractionDto(
                a.Name,
                a.Category,
                a.AverageVisitDurationMinutes,
                a.EntryCostMin,
                a.EntryCostTypical,
                a.EntryCostMax,
                a.Currency,
                a.OpeningTime is null && a.ClosingTime is null
                    ? null
                    : $"{a.OpeningTime ?? "09:00"}–{a.ClosingTime ?? "18:00"}",
                a.FamilyScore,
                a.CoupleScore))
            .ToList();

        var activities = dest.Activities
            .Where(a => a.IsActive)
            .OrderByDescending(a => a.FamilyScore + a.CoupleScore)
            .Select(a => new DestinationActivityDto(
                a.Id,
                a.Name,
                a.Category,
                a.DurationMinutes,
                a.CostMin,
                a.CostTypical,
                a.CostMax,
                a.Currency,
                a.BudgetLevel,
                a.Description,
                a.FamilyScore,
                a.CoupleScore))
            .ToList();

        var highlights = attractions.Take(5).Select(a => a.Name).ToList();
        if (highlights.Count == 0)
        {
            highlights =
            [
                $"{dest.Name} sightseeing",
                "Local food experiences",
                "Cultural landmarks",
                "Comfortable stays"
            ];
        }

        var inclusions = BuildInclusions(dest.Inclusions.Where(i => i.IsActive).ToList());
        var sampleItinerary = BuildSampleItinerary(dest.Name, attractions.Select(a => a.Name).Take(3).ToList(), daysHint: 14);
        var photos = DestinationMediaCatalog.GetPhotos(dest.Slug);

        return new DestinationProfileDto(
            dest.Id,
            dest.Name,
            dest.Slug,
            dest.Country?.Name ?? string.Empty,
            dest.Region?.Name,
            dest.Description,
            dest.FamilyFriendlyScore,
            dest.CoupleScore,
            dest.RomanceScore,
            dest.CultureScore,
            dest.AdventureScore,
            highlights,
            attractions,
            activities,
            inclusions,
            sampleItinerary,
            photos,
            photos.FirstOrDefault());
    }

    private static DestinationInclusionsDto BuildInclusions(IReadOnlyList<Domain.Entities.DestinationInclusion> rows)
    {
        InclusionGroupDto[] Group(bool included) =>
            rows.Where(r => r.IsIncluded == included)
                .GroupBy(r => r.PackageTier)
                .OrderBy(g => TierSort(g.Key))
                .Select(g => new InclusionGroupDto(
                    g.Key,
                    TierLabels.GetValueOrDefault(g.Key, g.Key.ToUpperInvariant()),
                    g.OrderBy(x => x.SortOrder)
                        .Select(x => new InclusionItemDto(x.Title, x.Details, x.IsCritical, x.SortOrder))
                        .ToList()))
                .ToArray();

        return new DestinationInclusionsDto(Group(true), Group(false));
    }

    private static int TierSort(string tier) => tier.ToLowerInvariant() switch
    {
        "standard" or "selfguided" => 1,
        "guided" => 2,
        "premiumguided" => 3,
        "family" => 4,
        "allversions" => 5,
        _ => 9
    };

    private static List<string> BuildSampleItinerary(string name, IReadOnlyList<string> attractionNames, int daysHint)
    {
        var day2 = attractionNames.ElementAtOrDefault(0) ?? $"{name} highlights";
        var day3 = attractionNames.ElementAtOrDefault(1) ?? "Local food and markets";
        var list = new List<string>
        {
            $"Day 1 — Arrival in {name}, transfer & check-in, evening neighborhood walk",
            $"Day 2 — {day2}, lunch nearby, optional evening activity",
            $"Day 3 — {day3}, free time / shopping",
            $"Day 4 — Flexible day for family pacing or premium experiences",
            $"Final day — Breakfast, last visit, departure transfer"
        };

        if (daysHint >= 10)
        {
            list.Insert(4, $"Days 5–7 — Core {name} cultural circuit (heritage sites, food walks)");
            list.Insert(5, $"Days 8–11 — Extended side trips / craft markets / nearby heritage day trips");
            list.Insert(6, $"Days 12–13 — Shopping & farewell experiences in {name}");
        }

        return list;
    }
}
