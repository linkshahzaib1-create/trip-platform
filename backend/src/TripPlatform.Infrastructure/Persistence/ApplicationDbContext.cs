using Microsoft.EntityFrameworkCore;
using TripPlatform.Application.Common.Interfaces;
using TripPlatform.Domain.Entities;

namespace TripPlatform.Infrastructure.Persistence;

public sealed class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Region> Regions => Set<Region>();
    public DbSet<Destination> Destinations => Set<Destination>();
    public DbSet<DestinationCategory> DestinationCategories => Set<DestinationCategory>();
    public DbSet<DestinationCategoryMap> DestinationCategoryMaps => Set<DestinationCategoryMap>();
    public DbSet<OriginPopularity> OriginPopularities => Set<OriginPopularity>();
    public DbSet<Attraction> Attractions => Set<Attraction>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<Season> Seasons => Set<Season>();
    public DbSet<DestinationCostProfile> DestinationCostProfiles => Set<DestinationCostProfile>();
    public DbSet<Route> Routes => Set<Route>();
    public DbSet<RouteOption> RouteOptions => Set<RouteOption>();
    public DbSet<TravelerCategory> TravelerCategories => Set<TravelerCategory>();
    public DbSet<TravelerSubcategory> TravelerSubcategories => Set<TravelerSubcategory>();
    public DbSet<AgeTier> AgeTiers => Set<AgeTier>();
    public DbSet<Priority> Priorities => Set<Priority>();
    public DbSet<CurrencyRate> CurrencyRates => Set<CurrencyRate>();
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<DestinationInclusion> DestinationInclusions => Set<DestinationInclusion>();
    public DbSet<TripPlan> TripPlans => Set<TripPlan>();
    public DbSet<TripTraveler> TripTravelers => Set<TripTraveler>();
    public DbSet<TripDay> TripDays => Set<TripDay>();
    public DbSet<ItineraryItem> ItineraryItems => Set<ItineraryItem>();
    public DbSet<TripEstimate> TripEstimates => Set<TripEstimate>();
    public DbSet<EstimateLine> EstimateLines => Set<EstimateLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DestinationCategoryMap>()
            .HasKey(x => new { x.DestinationId, x.CategoryId });

        modelBuilder.Entity<Destination>()
            .HasIndex(x => x.Slug)
            .IsUnique();

        modelBuilder.Entity<Country>()
            .HasIndex(x => x.Code)
            .IsUnique();

        modelBuilder.Entity<Route>()
            .HasOne(r => r.OriginDestination)
            .WithMany()
            .HasForeignKey(r => r.OriginDestinationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Route>()
            .HasOne(r => r.TargetDestination)
            .WithMany()
            .HasForeignKey(r => r.TargetDestinationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OriginPopularity>()
            .HasOne(o => o.OriginDestination)
            .WithMany()
            .HasForeignKey(o => o.OriginDestinationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OriginPopularity>()
            .HasOne(o => o.TargetDestination)
            .WithMany(d => d.OriginPopularities)
            .HasForeignKey(o => o.TargetDestinationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DestinationInclusion>()
            .HasOne(i => i.Destination)
            .WithMany(d => d.Inclusions)
            .HasForeignKey(i => i.DestinationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DestinationInclusion>()
            .Property(i => i.PackageTier)
            .HasMaxLength(64);

        modelBuilder.Entity<DestinationInclusion>()
            .Property(i => i.Title)
            .HasMaxLength(300);

        foreach (var property in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetPrecision(18);
            property.SetScale(2);
        }

        base.OnModelCreating(modelBuilder);
    }
}
