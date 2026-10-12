using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TripPlatform.Application.Common.Interfaces;

namespace TripPlatform.Application.Destinations.Queries.SearchDestinations;

public sealed record DestinationMatchDto(int Id, string Name, string Country, string Type, string Slug);

public sealed record SearchDestinationsQuery(string Query) : IRequest<SearchDestinationsResponse>;

public sealed record SearchDestinationsResponse(string Query, IReadOnlyList<DestinationMatchDto> Matches);

public sealed class SearchDestinationsQueryValidator : FluentValidation.AbstractValidator<SearchDestinationsQuery>
{
    public SearchDestinationsQueryValidator()
    {
        RuleFor(x => x.Query).NotEmpty().MaximumLength(100);
    }
}

public sealed class SearchDestinationsQueryHandler : IRequestHandler<SearchDestinationsQuery, SearchDestinationsResponse>
{
    private readonly IApplicationDbContext _db;

    public SearchDestinationsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<SearchDestinationsResponse> Handle(SearchDestinationsQuery request, CancellationToken cancellationToken)
    {
        var q = request.Query.Trim();
        var matches = await _db.Destinations
            .AsNoTracking()
            .Include(d => d.Country)
            .Where(d => d.IsActive && (d.Name.Contains(q) || d.Slug.Contains(q.ToLower())))
            .OrderByDescending(d => d.GlobalPopularityScore)
            .Take(10)
            .Select(d => new DestinationMatchDto(
                d.Id,
                d.Name,
                d.Country != null ? d.Country.Name : string.Empty,
                d.DestinationType.ToString(),
                d.Slug))
            .ToListAsync(cancellationToken);

        return new SearchDestinationsResponse(q, matches);
    }
}
