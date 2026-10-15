using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TripPlatform.Application.Common.Interfaces;
using TripPlatform.Infrastructure.OpenData;
using TripPlatform.Infrastructure.Persistence;
using TripPlatform.Infrastructure.Persistence.Seed;

namespace TripPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration.GetValue<string>("Database:Provider") ?? "InMemory";
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["DATABASE_URL"];

        // Railway often injects postgres:// URLs — normalize for Npgsql
        if (!string.IsNullOrWhiteSpace(connectionString) &&
            (connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
             connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)))
        {
            connectionString = ConvertPostgresUri(connectionString);
        }

        if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) ||
            provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));
        }
        else if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
        }
        else
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(configuration.GetValue<string>("Database:InMemoryName") ?? "TripPlatform"));
        }

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<DemoDataSeeder>();

        services.AddHttpClient<IGeocodingService, NominatimGeocodingService>(client =>
        {
            client.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("TripPlatform/1.0 (travel-cost-estimation; contact@tripplatform.local)");
            client.Timeout = TimeSpan.FromSeconds(20);
        });

        services.AddHttpClient<IPlaceSyncService, OverpassPlaceSyncService>(client =>
        {
            client.BaseAddress = new Uri("https://overpass-api.de/api/interpreter");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("TripPlatform/1.0 (travel-cost-estimation)");
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        return services;
    }

    private static string ConvertPostgresUri(string uri)
    {
        var databaseUri = new Uri(uri);
        var userInfo = databaseUri.UserInfo.Split(':', 2);
        var user = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
        var db = databaseUri.AbsolutePath.Trim('/');
        return $"Host={databaseUri.Host};Port={(databaseUri.IsDefaultPort ? 5432 : databaseUri.Port)};Database={db};Username={user};Password={password};SSL Mode=Require;Trust Server Certificate=true";
    }
}
