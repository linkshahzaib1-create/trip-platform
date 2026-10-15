using Microsoft.EntityFrameworkCore;
using TripPlatform.Domain.Entities;

namespace TripPlatform.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Country> Countries { get; }
    DbSet<Region> Regions { get; }
    DbSet<Destination> Destinations { get; }
    DbSet<DestinationCategory> DestinationCategories { get; }
    DbSet<DestinationCategoryMap> DestinationCategoryMaps { get; }
    DbSet<OriginPopularity> OriginPopularities { get; }
    DbSet<Attraction> Attractions { get; }
    DbSet<Activity> Activities { get; }
    DbSet<Season> Seasons { get; }
    DbSet<DestinationCostProfile> DestinationCostProfiles { get; }
    DbSet<Route> Routes { get; }
    DbSet<RouteOption> RouteOptions { get; }
    DbSet<TravelerCategory> TravelerCategories { get; }
    DbSet<TravelerSubcategory> TravelerSubcategories { get; }
    DbSet<AgeTier> AgeTiers { get; }
    DbSet<Priority> Priorities { get; }
    DbSet<CurrencyRate> CurrencyRates { get; }
    DbSet<Listing> Listings { get; }
    DbSet<FlightQuote> FlightQuotes { get; }
    DbSet<TransportQuote> TransportQuotes { get; }
    DbSet<DestinationInclusion> DestinationInclusions { get; }
    DbSet<TripPlan> TripPlans { get; }
    DbSet<TripTraveler> TripTravelers { get; }
    DbSet<TripDay> TripDays { get; }
    DbSet<ItineraryItem> ItineraryItems { get; }
    DbSet<TripEstimate> TripEstimates { get; }
    DbSet<EstimateLine> EstimateLines { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
