/*
  TripPlatform — PostgreSQL schema
  Run against a PostgreSQL 15+ database.
*/
CREATE TABLE IF NOT EXISTS "Countries" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(200) NOT NULL,
    "Code" VARCHAR(8) NOT NULL UNIQUE,
    "CurrencyCode" VARCHAR(8) NULL,
    "CreatedAtUtc" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAtUtc" TIMESTAMPTZ NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS "Regions" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(200) NOT NULL,
    "Code" VARCHAR(32) NULL,
    "CountryId" INT NOT NULL REFERENCES "Countries"("Id"),
    "CreatedAtUtc" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAtUtc" TIMESTAMPTZ NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS "Destinations" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(200) NOT NULL,
    "Slug" VARCHAR(200) NOT NULL UNIQUE,
    "Description" TEXT NULL,
    "DestinationType" INT NOT NULL,
    "CountryId" INT NOT NULL REFERENCES "Countries"("Id"),
    "RegionId" INT NULL REFERENCES "Regions"("Id"),
    "Latitude" DOUBLE PRECISION NOT NULL,
    "Longitude" DOUBLE PRECISION NOT NULL,
    "IsInternationalHub" BOOLEAN NOT NULL DEFAULT FALSE,
    "GlobalPopularityScore" INT NOT NULL DEFAULT 0,
    "RegionalPopularityScore" INT NOT NULL DEFAULT 0,
    "FamilyFriendlyScore" INT NOT NULL DEFAULT 0,
    "CoupleScore" INT NOT NULL DEFAULT 0,
    "SoloScore" INT NOT NULL DEFAULT 0,
    "LuxuryScore" INT NOT NULL DEFAULT 0,
    "BudgetScore" INT NOT NULL DEFAULT 0,
    "RomanceScore" INT NOT NULL DEFAULT 0,
    "CultureScore" INT NOT NULL DEFAULT 0,
    "AdventureScore" INT NOT NULL DEFAULT 0,
    "CreatedAtUtc" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAtUtc" TIMESTAMPTZ NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE INDEX IF NOT EXISTS "IX_Destinations_Name" ON "Destinations"("Name");

CREATE TABLE IF NOT EXISTS "DestinationCategories" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(100) NOT NULL,
    "Description" VARCHAR(500) NULL,
    "CreatedAtUtc" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAtUtc" TIMESTAMPTZ NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS "DestinationCategoryMaps" (
    "DestinationId" INT NOT NULL REFERENCES "Destinations"("Id"),
    "CategoryId" INT NOT NULL REFERENCES "DestinationCategories"("Id"),
    PRIMARY KEY ("DestinationId", "CategoryId")
);

CREATE TABLE IF NOT EXISTS "OriginPopularities" (
    "Id" SERIAL PRIMARY KEY,
    "OriginDestinationId" INT NOT NULL REFERENCES "Destinations"("Id"),
    "TargetDestinationId" INT NOT NULL REFERENCES "Destinations"("Id"),
    "Score" INT NOT NULL,
    "Bucket" VARCHAR(64) NOT NULL
);

CREATE TABLE IF NOT EXISTS "Attractions" (
    "Id" SERIAL PRIMARY KEY,
    "DestinationId" INT NOT NULL REFERENCES "Destinations"("Id"),
    "ExternalPlaceId" VARCHAR(128) NULL,
    "Name" VARCHAR(200) NOT NULL,
    "Description" TEXT NULL,
    "Latitude" DOUBLE PRECISION NOT NULL DEFAULT 0,
    "Longitude" DOUBLE PRECISION NOT NULL DEFAULT 0,
    "Category" VARCHAR(100) NOT NULL,
    "AverageVisitDurationMinutes" INT NOT NULL DEFAULT 90,
    "EntryCostMin" NUMERIC(18,2) NOT NULL DEFAULT 0,
    "EntryCostTypical" NUMERIC(18,2) NOT NULL DEFAULT 0,
    "EntryCostMax" NUMERIC(18,2) NOT NULL DEFAULT 0,
    "Currency" VARCHAR(8) NOT NULL DEFAULT 'USD',
    "OpeningTime" VARCHAR(16) NULL,
    "ClosingTime" VARCHAR(16) NULL,
    "PopularityScore" INT NOT NULL DEFAULT 0,
    "FamilyScore" INT NOT NULL DEFAULT 0,
    "CoupleScore" INT NOT NULL DEFAULT 0,
    "SoloScore" INT NOT NULL DEFAULT 0,
    "RomanceScore" INT NOT NULL DEFAULT 0,
    "CultureScore" INT NOT NULL DEFAULT 0,
    "PhotographyScore" INT NOT NULL DEFAULT 0,
    "AccessibilityScore" INT NOT NULL DEFAULT 3,
    "CreatedAtUtc" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAtUtc" TIMESTAMPTZ NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS "Activities" (
    "Id" SERIAL PRIMARY KEY,
    "DestinationId" INT NOT NULL REFERENCES "Destinations"("Id"),
    "Name" VARCHAR(200) NOT NULL,
    "Description" TEXT NULL,
    "Category" VARCHAR(100) NOT NULL,
    "DurationMinutes" INT NOT NULL DEFAULT 180,
    "CostMin" NUMERIC(18,2) NOT NULL DEFAULT 0,
    "CostTypical" NUMERIC(18,2) NOT NULL DEFAULT 0,
    "CostMax" NUMERIC(18,2) NOT NULL DEFAULT 0,
    "Currency" VARCHAR(8) NOT NULL DEFAULT 'USD',
    "MinAge" INT NULL,
    "MaxAge" INT NULL,
    "BudgetLevel" VARCHAR(32) NOT NULL DEFAULT 'Balanced',
    "FamilyScore" INT NOT NULL DEFAULT 0,
    "CoupleScore" INT NOT NULL DEFAULT 0,
    "SoloScore" INT NOT NULL DEFAULT 0,
    "SeasonHint" VARCHAR(100) NULL,
    "AffiliateUrl" VARCHAR(500) NULL,
    "CreatedAtUtc" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAtUtc" TIMESTAMPTZ NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE
);

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

CREATE INDEX IF NOT EXISTS "IX_DestinationInclusions_DestinationId"
    ON "DestinationInclusions"("DestinationId");

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

-- Seed is performed by the API DemoDataSeeder on first boot.
