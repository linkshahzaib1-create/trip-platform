using Microsoft.EntityFrameworkCore;
using TripPlatform.Domain.Entities;
using TripPlatform.Domain.Enums;
using TripPlatform.Infrastructure.Persistence;

namespace TripPlatform.Infrastructure.Persistence.Seed;

public sealed class DemoDataSeeder
{
    private readonly ApplicationDbContext _db;

    public DemoDataSeeder(ApplicationDbContext db) => _db = db;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (_db.Destinations.Any())
        {
            await SeedInclusionsIfMissingAsync(cancellationToken);
            await SeedTransportAndFlightQuotesIfMissingAsync(cancellationToken);
            return;
        }

        var pakistan = new Country { Name = "Pakistan", Code = "PK", CurrencyCode = "PKR" };
        var uae = new Country { Name = "United Arab Emirates", Code = "AE", CurrencyCode = "AED" };
        var turkey = new Country { Name = "Turkey", Code = "TR", CurrencyCode = "TRY" };
        var thailand = new Country { Name = "Thailand", Code = "TH", CurrencyCode = "THB" };
        var maldives = new Country { Name = "Maldives", Code = "MV", CurrencyCode = "MVR" };
        var france = new Country { Name = "France", Code = "FR", CurrencyCode = "EUR" };
        var finland = new Country { Name = "Finland", Code = "FI", CurrencyCode = "EUR" };

        _db.Countries.AddRange(pakistan, uae, turkey, thailand, maldives, france, finland);
        await _db.SaveChangesAsync(cancellationToken);

        var punjab = new Region { Name = "Punjab", CountryId = pakistan.Id };
        var gb = new Region { Name = "Gilgit-Baltistan", CountryId = pakistan.Id };
        var ict = new Region { Name = "Islamabad Capital Territory", CountryId = pakistan.Id };
        _db.Regions.AddRange(punjab, gb, ict);
        await _db.SaveChangesAsync(cancellationToken);

        var categories = new[]
        {
            "Beach", "Mountain", "City", "Historical", "Cultural", "Adventure",
            "Religious", "Wildlife", "Luxury", "Shopping", "Nightlife", "Family", "Romantic"
        }.Select(n => new DestinationCategory { Name = n }).ToList();
        _db.DestinationCategories.AddRange(categories);
        await _db.SaveChangesAsync(cancellationToken);

        Destination City(string name, string slug, Country c, Region? r, double lat, double lng,
            int global, int regional, int family, int couple, int solo, int luxury, int budget, int romance, int culture, int adventure,
            bool hub = false) => new()
        {
            Name = name,
            Slug = slug,
            CountryId = c.Id,
            RegionId = r?.Id,
            Latitude = lat,
            Longitude = lng,
            DestinationType = DestinationType.City,
            GlobalPopularityScore = global,
            RegionalPopularityScore = regional,
            FamilyFriendlyScore = family,
            CoupleScore = couple,
            SoloScore = solo,
            LuxuryScore = luxury,
            BudgetScore = budget,
            RomanceScore = romance,
            CultureScore = culture,
            AdventureScore = adventure,
            IsInternationalHub = hub,
            Description = $"{name} travel destination"
        };

        var lahore = City("Lahore", "lahore", pakistan, punjab, 31.5204, 74.3587, 72, 95, 85, 80, 75, 60, 85, 70, 90, 40);
        var islamabad = City("Islamabad", "islamabad", pakistan, ict, 33.6844, 73.0479, 65, 90, 88, 75, 70, 55, 80, 65, 70, 50);
        var murree = City("Murree", "murree", pakistan, punjab, 33.9070, 73.3943, 55, 88, 90, 85, 60, 45, 75, 80, 50, 70);
        var hunza = City("Hunza", "hunza", pakistan, gb, 36.3167, 74.6500, 70, 92, 75, 80, 85, 50, 70, 75, 80, 95);
        var dubai = City("Dubai", "dubai", uae, null, 25.2048, 55.2708, 95, 92, 93, 90, 80, 95, 65, 85, 70, 60, hub: true);
        var istanbul = City("Istanbul", "istanbul", turkey, null, 41.0082, 28.9784, 94, 85, 80, 92, 88, 85, 70, 90, 95, 55, hub: true);
        var bangkok = City("Bangkok", "bangkok", thailand, null, 13.7563, 100.5018, 93, 80, 75, 78, 90, 80, 75, 70, 85, 65, hub: true);
        var male = City("Maldives", "maldives", maldives, null, 3.2028, 73.2207, 90, 70, 70, 98, 60, 98, 40, 99, 40, 70);
        var paris = City("Paris", "paris", france, null, 48.8566, 2.3522, 98, 75, 70, 95, 85, 95, 55, 97, 98, 40, hub: true);
        var helsinki = City("Helsinki", "helsinki", finland, null, 60.1699, 24.9384, 70, 60, 75, 70, 80, 70, 65, 60, 75, 50, hub: true);

        _db.Destinations.AddRange(lahore, islamabad, murree, hunza, dubai, istanbul, bangkok, male, paris, helsinki);
        await _db.SaveChangesAsync(cancellationToken);

        void MapCat(Destination d, params string[] names)
        {
            foreach (var n in names)
            {
                var cat = categories.First(c => c.Name == n);
                _db.DestinationCategoryMaps.Add(new DestinationCategoryMap { DestinationId = d.Id, CategoryId = cat.Id });
            }
        }

        MapCat(lahore, "City", "Historical", "Cultural", "Family");
        MapCat(islamabad, "City", "Family");
        MapCat(murree, "Mountain", "Family", "Romantic");
        MapCat(hunza, "Mountain", "Adventure", "Cultural");
        MapCat(dubai, "City", "Luxury", "Shopping", "Family");
        MapCat(istanbul, "City", "Cultural", "Historical", "Romantic");
        MapCat(bangkok, "City", "Cultural", "Nightlife", "Shopping");
        MapCat(male, "Beach", "Luxury", "Romantic");
        MapCat(paris, "City", "Cultural", "Romantic", "Luxury");
        MapCat(helsinki, "City", "Cultural");

        AddAttraction(lahore, "Badshahi Mosque", "Culture", 31.5880, 74.3100, 120, 0, 0, 2, 95, 4, 2, 4, 2, 5, 5);
        AddAttraction(lahore, "Lahore Fort", "History", 31.5883, 74.3150, 150, 2, 4, 6, 90, 4, 3, 4, 2, 5, 5);
        AddAttraction(lahore, "Shalimar Gardens", "Culture", 31.5889, 74.3810, 90, 1, 2, 3, 80, 4, 4, 3, 4, 5, 5);
        AddAttraction(hunza, "Attabad Lake", "Nature", 36.3500, 74.8667, 180, 0, 5, 10, 92, 4, 5, 5, 5, 3, 5);
        AddAttraction(hunza, "Baltit Fort", "History", 36.3250, 74.6500, 90, 2, 4, 6, 85, 3, 4, 4, 3, 5, 5);
        AddAttraction(dubai, "Burj Khalifa", "Landmark", 25.1972, 55.2744, 120, 30, 45, 60, 98, 5, 4, 4, 4, 4, 5);
        AddAttraction(istanbul, "Hagia Sophia", "Culture", 41.0086, 28.9802, 90, 20, 25, 35, 97, 4, 5, 4, 4, 5, 5);
        AddAttraction(male, "House Reef Snorkel Spot", "Nature", 3.2028, 73.2207, 120, 30, 50, 80, 90, 3, 5, 4, 5, 2, 5);
        AddAttraction(paris, "Eiffel Tower", "Landmark", 48.8584, 2.2945, 120, 20, 30, 45, 99, 4, 5, 5, 5, 5, 5);

        AddActivity(lahore, "Lahore Food Tour", "Food", 180, 15, 25, 40, 3, 5, 4);
        AddActivity(lahore, "Walled City Heritage Walk", "Culture", 150, 8, 15, 25, 4, 4, 5);
        AddActivity(islamabad, "Margalla Hills Trail Walk", "Adventure", 180, 0, 10, 25, 5, 4, 5);
        AddActivity(murree, "Mall Road Family Stroll", "Family", 120, 5, 12, 25, 5, 4, 3);
        AddActivity(hunza, "Hunza Jeep Tour", "Adventure", 300, 40, 70, 110, 4, 4, 5);
        AddActivity(dubai, "Dubai Desert Safari", "Adventure", 360, 40, 70, 120, 5, 5, 4);
        AddActivity(dubai, "Dubai Marina Evening Cruise", "Cruise", 90, 25, 45, 80, 4, 5, 4);
        AddActivity(istanbul, "Bosphorus Cruise", "Cruise", 120, 20, 35, 60, 4, 5, 4);
        AddActivity(bangkok, "Chao Phraya Dinner Cruise", "Cruise", 120, 25, 45, 75, 3, 5, 4);
        AddActivity(bangkok, "Street Food Night Market Tour", "Food", 180, 15, 28, 45, 4, 4, 5);
        AddActivity(male, "Maldives Snorkeling", "Water", 180, 30, 60, 120, 3, 5, 4);
        AddActivity(paris, "Seine River Cruise", "Cruise", 90, 25, 40, 70, 4, 5, 4);
        AddActivity(paris, "Louvre Timed-Entry Visit", "Culture", 180, 20, 35, 55, 3, 5, 4);
        AddActivity(helsinki, "Suomenlinna Ferry Day Trip", "Culture", 240, 15, 30, 50, 4, 4, 5);

        Season Peak(Destination d, string name, int sm, int em, decimal m) => new()
        {
            DestinationId = d.Id, Name = name, Kind = SeasonKind.Peak, StartMonth = sm, EndMonth = em, PriceMultiplier = m
        };
        Season Low(Destination d, string name, int sm, int em, decimal m) => new()
        {
            DestinationId = d.Id, Name = name, Kind = SeasonKind.Low, StartMonth = sm, EndMonth = em, PriceMultiplier = m
        };
        Season Shoulder(Destination d, string name, int sm, int em, decimal m) => new()
        {
            DestinationId = d.Id, Name = name, Kind = SeasonKind.Shoulder, StartMonth = sm, EndMonth = em, PriceMultiplier = m
        };

        var seasons = new List<Season>
        {
            Peak(dubai, "Peak Winter", 11, 3, 1.25m),
            Low(dubai, "Summer Low", 6, 9, 0.85m),
            Peak(istanbul, "Spring/Fall Peak", 4, 6, 1.15m),
            Peak(male, "Dry Peak", 12, 4, 1.30m),
            Peak(paris, "Summer Peak", 6, 8, 1.35m),
            Peak(paris, "Christmas/NY", 12, 1, 1.40m),
            Peak(murree, "Summer Peak", 5, 8, 1.20m),
            Shoulder(hunza, "Shoulder", 4, 6, 1.05m),
            Peak(hunza, "Summer Peak", 7, 9, 1.20m),
            Low(bangkok, "Rainy Low", 6, 10, 0.90m),
            Peak(bangkok, "Cool Peak", 11, 2, 1.15m)
        };
        _db.Seasons.AddRange(seasons);
        await _db.SaveChangesAsync(cancellationToken);

        void Profile(Destination d, string tier, decimal aMin, decimal aTyp, decimal aMax,
            decimal fMin, decimal fTyp, decimal fMax,
            decimal tMin, decimal tTyp, decimal tMax,
            decimal actMin, decimal actTyp, decimal actMax,
            decimal trMin, decimal trTyp, decimal trMax, int? seasonId = null)
        {
            _db.DestinationCostProfiles.Add(new DestinationCostProfile
            {
                DestinationId = d.Id,
                SeasonId = seasonId,
                BudgetTier = tier,
                Currency = "USD",
                AccommodationMin = aMin, AccommodationTypical = aTyp, AccommodationMax = aMax,
                FoodMin = fMin, FoodTypical = fTyp, FoodMax = fMax,
                LocalTransportMin = tMin, LocalTransportTypical = tTyp, LocalTransportMax = tMax,
                ActivitiesMin = actMin, ActivitiesTypical = actTyp, ActivitiesMax = actMax,
                EntertainmentMin = actMin / 2, EntertainmentTypical = actTyp / 2, EntertainmentMax = actMax / 2,
                ShoppingMin = 5, ShoppingTypical = 20, ShoppingMax = 80,
                AirportTransferMin = trMin, AirportTransferTypical = trTyp, AirportTransferMax = trMax,
                ContingencyPercent = 10,
                Source = "Seed",
                Confidence = ConfidenceLevel.Medium,
                ValidFromUtc = DateTime.UtcNow.AddMonths(-1),
                ValidToUtc = DateTime.UtcNow.AddMonths(6)
            });
        }

        Profile(islamabad, "Budget", 20, 30, 45, 8, 12, 20, 5, 10, 15, 5, 12, 25, 10, 15, 25);
        Profile(islamabad, "Balanced", 35, 50, 70, 12, 20, 30, 8, 15, 25, 10, 20, 40, 15, 20, 35);
        Profile(murree, "Budget", 25, 35, 50, 10, 15, 22, 5, 10, 18, 5, 15, 30, 0, 0, 0);
        Profile(murree, "Balanced", 40, 55, 80, 15, 22, 35, 8, 14, 22, 10, 25, 45, 0, 0, 0);
        Profile(hunza, "Budget", 20, 35, 55, 10, 15, 25, 8, 15, 25, 10, 25, 50, 20, 40, 80);
        Profile(hunza, "Balanced", 40, 60, 90, 15, 25, 40, 12, 20, 35, 20, 40, 70, 30, 50, 100);
        Profile(dubai, "Budget", 70, 95, 120, 25, 35, 50, 10, 18, 30, 20, 40, 60, 15, 25, 40);
        Profile(dubai, "Balanced", 100, 140, 180, 35, 50, 70, 15, 25, 40, 30, 55, 90, 20, 35, 60);
        Profile(dubai, "Comfort", 150, 200, 280, 50, 70, 100, 25, 40, 60, 50, 80, 130, 30, 50, 80);
        Profile(dubai, "Luxury", 250, 400, 700, 80, 120, 200, 40, 70, 120, 80, 150, 250, 50, 100, 200);
        Profile(istanbul, "Budget", 40, 60, 85, 15, 25, 40, 8, 15, 25, 15, 30, 50, 15, 25, 40);
        Profile(istanbul, "Balanced", 70, 100, 140, 25, 40, 60, 12, 22, 35, 25, 45, 80, 20, 35, 55);
        Profile(bangkok, "Budget", 25, 40, 60, 10, 18, 30, 5, 10, 20, 15, 30, 55, 10, 18, 30);
        Profile(bangkok, "Balanced", 50, 75, 110, 18, 30, 50, 8, 15, 28, 25, 45, 80, 15, 25, 40);
        Profile(male, "Budget", 100, 160, 220, 30, 45, 70, 10, 20, 35, 30, 60, 100, 50, 120, 250);
        Profile(male, "Balanced", 180, 250, 350, 40, 60, 90, 15, 30, 50, 50, 90, 150, 80, 180, 300);
        Profile(male, "Luxury", 350, 550, 900, 70, 110, 180, 30, 50, 80, 80, 150, 250, 150, 280, 450);
        Profile(paris, "Balanced", 120, 180, 260, 35, 55, 90, 15, 25, 40, 30, 55, 100, 40, 60, 90);
        Profile(lahore, "Budget", 20, 30, 40, 10, 15, 20, 5, 10, 15, 5, 12, 20, 10, 15, 25);
        Profile(lahore, "Balanced", 35, 50, 75, 15, 25, 40, 8, 15, 25, 10, 20, 40, 12, 20, 35);

        await _db.SaveChangesAsync(cancellationToken);

        void Pop(Destination origin, Destination target, int score, string bucket) =>
            _db.OriginPopularities.Add(new OriginPopularity
            {
                OriginDestinationId = origin.Id,
                TargetDestinationId = target.Id,
                Score = score,
                Bucket = bucket
            });

        Pop(lahore, islamabad, 90, "Nearby");
        Pop(lahore, murree, 92, "Nearby");
        Pop(lahore, hunza, 88, "Domestic");
        Pop(lahore, dubai, 94, "InternationalMid");
        Pop(lahore, istanbul, 86, "InternationalMid");
        Pop(lahore, bangkok, 80, "InternationalBudget");
        Pop(lahore, male, 78, "InternationalPremium");
        Pop(helsinki, paris, 95, "InternationalMid");

        void AddRoute(Destination o, Destination t, double km, params (TravelMode mode, int mins, decimal min, decimal typ, decimal max)[] options)
        {
            var route = new Route
            {
                OriginDestinationId = o.Id,
                TargetDestinationId = t.Id,
                DistanceKm = km,
                Options = options.Select(x => new RouteOption
                {
                    TravelMode = x.mode,
                    DurationMinutes = x.mins,
                    CostMin = x.min,
                    CostTypical = x.typ,
                    CostMax = x.max,
                    Currency = "USD",
                    Confidence = ConfidenceLevel.Medium
                }).ToList()
            };
            _db.Routes.Add(route);
        }

        AddRoute(lahore, islamabad, 380,
            (TravelMode.Flight, 80, 80, 110, 150),
            (TravelMode.Bus, 330, 15, 22, 30),
            (TravelMode.PrivateCar, 270, 50, 75, 100));
        AddRoute(lahore, murree, 370, (TravelMode.PrivateCar, 300, 40, 60, 90), (TravelMode.Bus, 360, 12, 20, 28));
        AddRoute(lahore, hunza, 620, (TravelMode.Flight, 90, 90, 140, 200), (TravelMode.PrivateCar, 840, 80, 120, 180));
        AddRoute(lahore, dubai, 1950, (TravelMode.Flight, 210, 180, 280, 420));
        AddRoute(lahore, istanbul, 4200, (TravelMode.Flight, 390, 280, 380, 550));
        AddRoute(lahore, bangkok, 3800, (TravelMode.Flight, 330, 220, 320, 480));
        AddRoute(lahore, male, 3200, (TravelMode.Flight, 360, 350, 480, 700));
        AddRoute(helsinki, paris, 1900, (TravelMode.Flight, 195, 90, 160, 280));

        var couples = new TravelerCategory { Name = "Dating & Couples", Code = "COUPLES", Description = "Dating and couples trips" };
        var families = new TravelerCategory { Name = "Families", Code = "FAMILIES" };
        var solo = new TravelerCategory { Name = "Solo Travelers", Code = "SOLO" };
        var married = new TravelerCategory { Name = "Married Couples", Code = "MARRIED" };
        var friends = new TravelerCategory { Name = "Friends / Bachelor / Bachelorette", Code = "FRIENDS" };
        _db.TravelerCategories.AddRange(couples, families, solo, married, friends);
        await _db.SaveChangesAsync(cancellationToken);

        _db.TravelerSubcategories.AddRange(
            new TravelerSubcategory { CategoryId = couples.Id, Name = "First Trip Together", Code = "FIRST_TRIP", DefaultRecommendationBehavior = "Boutique stays, cafes, scenic/photo locations" },
            new TravelerSubcategory { CategoryId = couples.Id, Name = "Boyfriend & Girlfriend", Code = "BF_GF" },
            new TravelerSubcategory { CategoryId = couples.Id, Name = "Proposal / Engagement Prep", Code = "PROPOSAL" },
            new TravelerSubcategory { CategoryId = married.Id, Name = "Honeymooners", Code = "HONEYMOON" },
            new TravelerSubcategory { CategoryId = married.Id, Name = "Parents Only Escape", Code = "PARENTS_ONLY" },
            new TravelerSubcategory { CategoryId = families.Id, Name = "Young Kids", Code = "YOUNG_KIDS" },
            new TravelerSubcategory { CategoryId = families.Id, Name = "Teens & Tweens", Code = "TEENS" },
            new TravelerSubcategory { CategoryId = solo.Id, Name = "Budget Backpacker", Code = "BACKPACKER" },
            new TravelerSubcategory { CategoryId = friends.Id, Name = "Bachelor/Groom", Code = "BACHELOR" });

        _db.Priorities.AddRange(
            new Priority { Name = "Romance", Code = "Romance" },
            new Priority { Name = "Culture", Code = "Culture" },
            new Priority { Name = "Comfort", Code = "Comfort" },
            new Priority { Name = "Adventure", Code = "Adventure" },
            new Priority { Name = "Food", Code = "Food" },
            new Priority { Name = "Nightlife", Code = "Nightlife" },
            new Priority { Name = "Family", Code = "Family" },
            new Priority { Name = "Shopping", Code = "Shopping" });

        _db.AgeTiers.AddRange(
            new AgeTier { Name = "0-4", MinAge = 0, MaxAge = 4 },
            new AgeTier { Name = "5-11", MinAge = 5, MaxAge = 11 },
            new AgeTier { Name = "12-17", MinAge = 12, MaxAge = 17 },
            new AgeTier { Name = "18-24", MinAge = 18, MaxAge = 24 },
            new AgeTier { Name = "25-34", MinAge = 25, MaxAge = 34 },
            new AgeTier { Name = "35-49", MinAge = 35, MaxAge = 49 },
            new AgeTier { Name = "50+", MinAge = 50, MaxAge = 120 });

        _db.CurrencyRates.Add(new CurrencyRate { FromCurrency = "USD", ToCurrency = "PKR", Rate = 278m, AsOfUtc = DateTime.UtcNow });

        _db.Listings.AddRange(
            new Listing
            {
                DestinationId = dubai.Id,
                ListingType = "Hotel",
                Name = "Marina View Hotel",
                PriceMin = 90, PriceTypical = 130, PriceMax = 190,
                Rating = 4.4m, ReviewCount = 2200, StarRating = 4,
                FamilyFriendly = true,
                AffiliatePartner = "Booking.com",
                AffiliateUrl = "https://www.booking.com/",
                ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/0/09/Dubai_Marina_Skyline.jpg/640px-Dubai_Marina_Skyline.jpg",
                Source = "Seed",
                PopularityScore = 88
            },
            new Listing
            {
                DestinationId = dubai.Id,
                ListingType = "Apartment",
                Name = "Downtown Serviced Apartment",
                PriceMin = 70, PriceTypical = 110, PriceMax = 160,
                Rating = 4.3m, ReviewCount = 640, StarRating = 4,
                FamilyFriendly = true,
                AmenitiesCsv = "Kitchen, Wi‑Fi, Washer",
                AffiliatePartner = "Booking.com",
                AffiliateUrl = "https://www.booking.com/",
                ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/c/c7/Burj_Khalifa.jpg/640px-Burj_Khalifa.jpg",
                Source = "Seed",
                PopularityScore = 82
            },
            new Listing
            {
                DestinationId = lahore.Id,
                ListingType = "Restaurant",
                Name = "Food Street Legacy Spot",
                Cuisine = "Pakistani",
                PriceMin = 8, PriceTypical = 15, PriceMax = 30,
                Rating = 4.6m, ReviewCount = 900,
                ChildFriendly = true,
                Source = "Seed",
                PopularityScore = 91
            },
            new Listing
            {
                DestinationId = murree.Id,
                ListingType = "Hotel",
                Name = "Pine Hill Family Resort",
                PriceMin = 35, PriceTypical = 55, PriceMax = 85,
                Rating = 4.2m, StarRating = 3,
                FamilyFriendly = true,
                AffiliatePartner = "Agoda",
                AffiliateUrl = "https://www.agoda.com/",
                Source = "Seed",
                PopularityScore = 84
            },
            new Listing
            {
                DestinationId = paris.Id,
                ListingType = "Home",
                Name = "Marais Charming Home Stay",
                PriceMin = 95, PriceTypical = 140, PriceMax = 210,
                Rating = 4.7m, ReviewCount = 310,
                FamilyFriendly = true,
                AmenitiesCsv = "Full kitchen, Self check-in",
                AffiliateUrl = "https://www.booking.com/",
                AffiliatePartner = "Booking.com",
                ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/a/a8/Tour_Eiffel_Wikimedia_Commons.jpg/640px-Tour_Eiffel_Wikimedia_Commons.jpg",
                Source = "Seed",
                PopularityScore = 86
            });

        await SeedInclusionsForDestinationsAsync(
            new[] { lahore, islamabad, murree, hunza, dubai, istanbul, bangkok, male, paris, helsinki },
            cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        await SeedTransportAndFlightQuotesIfMissingAsync(cancellationToken);
    }

    private async Task SeedInclusionsIfMissingAsync(CancellationToken cancellationToken)
    {
        if (_db.DestinationInclusions.Any())
            return;

        var destinations = _db.Destinations.Where(d => d.IsActive).ToList();
        await SeedInclusionsForDestinationsAsync(destinations, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedTransportAndFlightQuotesIfMissingAsync(CancellationToken cancellationToken)
    {
        if (!_db.TransportQuotes.Any())
        {
            foreach (var dest in _db.Destinations.Where(d => d.IsActive).ToList())
            {
                var profile = _db.DestinationCostProfiles
                    .Where(p => p.DestinationId == dest.Id)
                    .OrderBy(p => p.BudgetTier == "Balanced" ? 0 : 1)
                    .FirstOrDefault();
                var local = profile?.LocalTransportTypical ?? 8m;
                _db.TransportQuotes.AddRange(
                    new TransportQuote
                    {
                        DestinationId = dest.Id,
                        Mode = "Taxi",
                        Title = "City taxi / ride-hail (short trip)",
                        Details = "Typical short city ride; airport runs cost more.",
                        TypicalMinutes = 25,
                        PriceMin = Math.Round(local * 0.6m, 2),
                        PriceTypical = local,
                        PriceMax = Math.Round(local * 2.2m, 2),
                        Source = "Seed",
                        FetchedAtUtc = DateTime.UtcNow
                    },
                    new TransportQuote
                    {
                        DestinationId = dest.Id,
                        Mode = "PublicTransit",
                        Title = "Metro / bus day pass",
                        Details = "Public transit where available.",
                        TypicalMinutes = 40,
                        PriceMin = Math.Round(local * 0.15m, 2),
                        PriceTypical = Math.Round(local * 0.35m, 2),
                        PriceMax = Math.Round(local * 0.7m, 2),
                        Source = "Seed",
                        FetchedAtUtc = DateTime.UtcNow
                    },
                    new TransportQuote
                    {
                        DestinationId = dest.Id,
                        Mode = "CarRental",
                        Title = "Car rental (per day)",
                        Details = "Economy car estimate excluding fuel.",
                        PriceMin = 25,
                        PriceTypical = 45,
                        PriceMax = 85,
                        Source = "Seed",
                        FetchedAtUtc = DateTime.UtcNow
                    },
                    new TransportQuote
                    {
                        DestinationId = dest.Id,
                        Mode = "Transfer",
                        Title = "Airport / station transfer",
                        Details = "One-way private transfer estimate.",
                        TypicalMinutes = 45,
                        PriceMin = profile?.AirportTransferMin ?? 15,
                        PriceTypical = profile?.AirportTransferTypical ?? 30,
                        PriceMax = profile?.AirportTransferMax ?? 60,
                        Source = "Seed",
                        FetchedAtUtc = DateTime.UtcNow
                    });
            }
        }

        if (!_db.FlightQuotes.Any())
        {
            var routes = _db.Routes.Include(r => r.Options).Where(r => r.IsActive).ToList();
            foreach (var route in routes)
            {
                var flight = route.Options.FirstOrDefault(o => o.TravelMode == TravelMode.Flight)
                    ?? route.Options.OrderBy(o => o.CostTypical).FirstOrDefault();
                if (flight is null)
                    continue;

                _db.FlightQuotes.Add(new FlightQuote
                {
                    OriginDestinationId = route.OriginDestinationId,
                    TargetDestinationId = route.TargetDestinationId,
                    Airline = "Multiple carriers",
                    CabinClass = "Economy",
                    DurationMinutes = flight.DurationMinutes,
                    PriceMin = flight.CostMin,
                    PriceTypical = flight.CostTypical,
                    PriceMax = flight.CostMax,
                    BaggageNotes = "Cabin bag usually included; checked baggage often extra ($20–$60).",
                    ScheduleNotes = $"{flight.DurationMinutes / 60}h {flight.DurationMinutes % 60}m typical duration",
                    Source = "Seed",
                    FetchedAtUtc = DateTime.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedInclusionsForDestinationsAsync(
        IEnumerable<Destination> destinations,
        CancellationToken cancellationToken)
    {
        foreach (var d in destinations)
        {
            var nightsHint = d.Name is "Maldives" or "Paris" or "Dubai" ? "5–7 nights" : "3–5 nights";
            AddInc(d, true, "Standard", 1, "Accommodation", $"{nightsHint} in selected hotels / guesthouses based on trip category.");
            AddInc(d, true, "Standard", 2, "Daily breakfast", "Breakfast included at the accommodation.");
            AddInc(d, true, "Standard", 3, "Arrival transfer", $"Airport / station transfer into {d.Name} (must confirm arrival details in advance).");
            AddInc(d, true, "Standard", 4, "Departure transfer", "Transfer on the official trip end date to airport / station.");
            AddInc(d, true, "Standard", 5, "Local orientation", "Welcome briefing with destination tips and suggested pacing.");
            AddInc(d, true, "Standard", 6, "Day-by-day trip plan", "Personalized estimated itinerary with attractions and meal windows.");
            AddInc(d, true, "Standard", 7, "Luggage support guidance", "Packing guidance and luggage transfer options where available (max ~20 kg / person).");

            AddInc(d, true, "Guided", 1, "Accommodation", "7 nights in selected properties.");
            AddInc(d, true, "Guided", 2, "Breakfast", "Daily breakfast.");
            AddInc(d, true, "Guided", 3, "Airport transfers", $"Arrival and departure transfers for {d.Name}.");
            AddInc(d, true, "Guided", 4, "Local guide (shared)", "Guided sightseeing on selected days.");
            AddInc(d, true, "Guided", 5, "Orientation walking tour", "City introduction walk on day 1.");
            AddInc(d, true, "Guided", 6, "Luggage transfer option", "One suitcase per person (max 20 kg) between stays when multi-stop.");

            AddInc(d, true, "PremiumGuided", 1, "Premium accommodation", "Higher-category hotels / resorts.");
            AddInc(d, true, "PremiumGuided", 2, "Breakfast & selected dinners", "Daily breakfast plus 2–3 curated dinners.");
            AddInc(d, true, "PremiumGuided", 3, "Private transfers", "Private arrival / departure transfers.");
            AddInc(d, true, "PremiumGuided", 4, "Private guide options", "Private or small-group guiding on key days.");
            AddInc(d, true, "PremiumGuided", 5, "Priority experiences", "Skip-the-line / timed-entry guidance for major attractions.");

            AddInc(d, false, "Standard", 1, "Meals other than breakfast", "Lunch, dinner, snacks and drinks are usually at your own expense.");
            AddInc(d, false, "Standard", 2, "Private guide & support vehicle", "Available as an upgrade.");
            AddInc(d, false, "Standard", 3, "Transfers on non-official end dates", "Extra transfer fees may apply.");
            AddInc(d, false, "Standard", 4, "Extra luggage", "Additional bags may incur a fee.");

            AddInc(d, false, "Guided", 1, "Meals other than breakfast", null);
            AddInc(d, false, "Guided", 2, "Personal expenses & shopping", null);
            AddInc(d, false, "Guided", 3, "Optional activities", "Theme parks, premium shows, spa, etc.");

            AddInc(d, false, "PremiumGuided", 1, "Meals other than listed inclusions", null);
            AddInc(d, false, "PremiumGuided", 2, "Gratuities", "Tips for guides and drivers.");
            AddInc(d, false, "PremiumGuided", 3, "Optional premium experiences", null);

            AddInc(d, false, "AllVersions", 1, "Tourist / city tax", "Approx. $1–$5 per person per night where applicable.", critical: false);
            AddInc(d, false, "AllVersions", 2, "Local transit tickets & ferries", "Metro, buses, short ferries, and attraction tickets unless listed.", critical: false);
            AddInc(d, false, "AllVersions", 3, "Travel insurance", "Strongly recommended for all travelers.", critical: true);
            AddInc(d, false, "AllVersions", 4, "Trip protection / cancellation cover", "Optional protection for delays and cancellations.", critical: true);
            if (d.FamilyFriendlyScore >= 70)
            {
                AddInc(d, true, "Family", 1, "Family-friendly hotel guidance", "Properties with family rooms / connecting options preferred.");
                AddInc(d, true, "Family", 2, "Kid-friendly activity suggestions", "Parks, gentle sightseeing, and shorter daily pacing.");
                AddInc(d, false, "Family", 1, "Childcare / babysitting", "Not included unless arranged separately.");
            }
        }

        await Task.CompletedTask;
    }

    private void AddInc(Destination d, bool included, string tier, int order, string title, string? details, bool critical = false)
    {
        _db.DestinationInclusions.Add(new DestinationInclusion
        {
            DestinationId = d.Id,
            IsIncluded = included,
            PackageTier = tier,
            SortOrder = order,
            Title = title,
            Details = details,
            IsCritical = critical
        });
    }

    private void AddAttraction(Destination d, string name, string category, double lat, double lng, int duration,
        decimal min, decimal typ, decimal max, int popularity, int family, int couple, int solo, int romance, int culture, int photo)
    {
        _db.Attractions.Add(new Attraction
        {
            DestinationId = d.Id,
            Name = name,
            Category = category,
            Latitude = lat,
            Longitude = lng,
            AverageVisitDurationMinutes = duration,
            EntryCostMin = min,
            EntryCostTypical = typ,
            EntryCostMax = max,
            PopularityScore = popularity,
            FamilyScore = family,
            CoupleScore = couple,
            SoloScore = solo,
            RomanceScore = romance,
            CultureScore = culture,
            PhotographyScore = photo
        });
    }

    private void AddActivity(Destination d, string name, string category, int duration, decimal min, decimal typ, decimal max, int family, int couple, int solo)
    {
        _db.Activities.Add(new Activity
        {
            DestinationId = d.Id,
            Name = name,
            Category = category,
            DurationMinutes = duration,
            CostMin = min,
            CostTypical = typ,
            CostMax = max,
            FamilyScore = family,
            CoupleScore = couple,
            SoloScore = solo,
            AffiliateUrl = "https://www.getyourguide.com/"
        });
    }
}
