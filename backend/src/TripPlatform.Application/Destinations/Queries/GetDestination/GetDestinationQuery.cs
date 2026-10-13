using MediatR;
using Microsoft.EntityFrameworkCore;
using TripPlatform.Application.Common.Interfaces;

namespace TripPlatform.Application.Destinations.Queries.GetDestination;

public sealed record GetDestinationQuery(int Id) : IRequest<DestinationProfileDto?>;

public sealed record DestinationAttractionDto(
    string Name,
    string Category,
    int DurationMinutes,
    decimal EntryCostTypical,
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
    IReadOnlyList<string> Activities,
    DestinationInclusionsDto Inclusions,
    IReadOnlyList<string> SampleItinerary);

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
                a.EntryCostTypical,
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
        var sampleItinerary = BuildSampleItinerary(dest.Name, attractions.Select(a => a.Name).Take(3).ToList());

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
            dest.Activities.Where(a => a.IsActive).Select(a => a.Name).ToList(),
            inclusions,
            sampleItinerary);
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

    private static List<string> BuildSampleItinerary(string name, IReadOnlyList<string> attractionNames)
    {
        var day2 = attractionNames.ElementAtOrDefault(0) ?? $"{name} highlights";
        var day3 = attractionNames.ElementAtOrDefault(1) ?? "Local food and markets";
        return
        [
            $"Day 1 — Arrival in {name}, transfer & check-in, evening neighborhood walk",
            $"Day 2 — {day2}, lunch nearby, optional evening activity",
            $"Day 3 — {day3}, free time / shopping",
            $"Day 4 — Flexible day for family pacing or premium experiences",
            $"Final day — Breakfast, last visit, departure transfer"
        ];
    }
}
