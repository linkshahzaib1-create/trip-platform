using Serilog;
using Microsoft.EntityFrameworkCore;
using TripPlatform.Application;
using TripPlatform.Infrastructure;
using TripPlatform.Infrastructure.Persistence;
using TripPlatform.Infrastructure.Persistence.Seed;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    var port = Environment.GetEnvironmentVariable("PORT");
    if (!string.IsNullOrWhiteSpace(port))
    {
        builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
    }

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new() { Title = "Trip Platform API", Version = "v1" });
    });

    var allowedOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
        ?? new[] { "http://localhost:4200", "https://localhost:4200" };

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Frontend", policy =>
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod());
    });

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var provider = app.Configuration.GetValue<string>("Database:Provider") ?? "InMemory";
        if (!provider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
        {
            await db.Database.EnsureCreatedAsync();
            // Ensure inclusions table exists on already-created Postgres databases
            if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
            {
                await db.Database.ExecuteSqlRawAsync("""
                    CREATE TABLE IF NOT EXISTS "DestinationInclusions" (
                        "Id" SERIAL PRIMARY KEY,
                        "DestinationId" INT NOT NULL REFERENCES "Destinations"("Id"),
                        "IsIncluded" BOOLEAN NOT NULL,
                        "PackageTier" VARCHAR(64) NOT NULL,
                        "Title" VARCHAR(300) NOT NULL,
                        "Details" TEXT NULL,
                        "SortOrder" INT NOT NULL DEFAULT 0,
                        "IsCritical" BOOLEAN NOT NULL DEFAULT FALSE,
                        "CreatedAtUtc" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                        "UpdatedAtUtc" TIMESTAMPTZ NULL,
                        "IsActive" BOOLEAN NOT NULL DEFAULT TRUE
                    );
                    """);
                await db.Database.ExecuteSqlRawAsync("""
                    ALTER TABLE "Listings" ADD COLUMN IF NOT EXISTS "ExternalId" VARCHAR(128) NULL;
                    ALTER TABLE "Listings" ADD COLUMN IF NOT EXISTS "Source" VARCHAR(64) NOT NULL DEFAULT 'Seed';
                    ALTER TABLE "Listings" ADD COLUMN IF NOT EXISTS "OpeningHours" VARCHAR(200) NULL;
                    ALTER TABLE "Listings" ADD COLUMN IF NOT EXISTS "FetchedAtUtc" TIMESTAMPTZ NULL;
                    """);
                await db.Database.ExecuteSqlRawAsync("""
                    CREATE TABLE IF NOT EXISTS "FlightQuotes" (
                        "Id" SERIAL PRIMARY KEY,
                        "OriginDestinationId" INT NOT NULL REFERENCES "Destinations"("Id"),
                        "TargetDestinationId" INT NOT NULL REFERENCES "Destinations"("Id"),
                        "Airline" VARCHAR(200) NOT NULL,
                        "CabinClass" VARCHAR(64) NOT NULL DEFAULT 'Economy',
                        "DurationMinutes" INT NOT NULL DEFAULT 0,
                        "PriceMin" NUMERIC(18,2) NOT NULL,
                        "PriceTypical" NUMERIC(18,2) NOT NULL,
                        "PriceMax" NUMERIC(18,2) NOT NULL,
                        "Currency" VARCHAR(8) NOT NULL DEFAULT 'USD',
                        "BaggageNotes" TEXT NULL,
                        "ScheduleNotes" TEXT NULL,
                        "Source" VARCHAR(64) NOT NULL DEFAULT 'RouteEstimate',
                        "FetchedAtUtc" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                        "CreatedAtUtc" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                        "UpdatedAtUtc" TIMESTAMPTZ NULL,
                        "IsActive" BOOLEAN NOT NULL DEFAULT TRUE
                    );
                    CREATE TABLE IF NOT EXISTS "TransportQuotes" (
                        "Id" SERIAL PRIMARY KEY,
                        "DestinationId" INT NOT NULL REFERENCES "Destinations"("Id"),
                        "Mode" VARCHAR(64) NOT NULL,
                        "Title" VARCHAR(300) NOT NULL,
                        "Details" TEXT NULL,
                        "TypicalMinutes" INT NULL,
                        "PriceMin" NUMERIC(18,2) NOT NULL,
                        "PriceTypical" NUMERIC(18,2) NOT NULL,
                        "PriceMax" NUMERIC(18,2) NOT NULL,
                        "Currency" VARCHAR(8) NOT NULL DEFAULT 'USD',
                        "Source" VARCHAR(64) NOT NULL DEFAULT 'CostProfileEstimate',
                        "FetchedAtUtc" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                        "CreatedAtUtc" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                        "UpdatedAtUtc" TIMESTAMPTZ NULL,
                        "IsActive" BOOLEAN NOT NULL DEFAULT TRUE
                    );
                    """);
            }
        }

        var seeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
        await seeder.SeedAsync();
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseSerilogRequestLogging();
    app.UseCors("Frontend");
    app.UseDefaultFiles();
    app.UseStaticFiles();
    app.UseAuthorization();
    app.MapControllers();
    app.MapGet("/health", () => Results.Ok(new
    {
        status = "healthy",
        service = "TripPlatform.API",
        database = app.Configuration.GetValue<string>("Database:Provider") ?? "InMemory"
    }));
    app.MapFallbackToFile("index.html");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
