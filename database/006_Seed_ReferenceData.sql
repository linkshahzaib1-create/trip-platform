/*
  TripPlatform — reference seed (categories, priorities, age tiers).
  Destination demo rows are seeded by the API DemoDataSeeder for InMemory;
  for SQL Server, run this after schema scripts, then load richer data via admin/import or re-run app against SqlServer after adding an EF seed path.
*/
USE TripPlatform;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.TravelerCategories WHERE Code = N'COUPLES')
BEGIN
    INSERT INTO dbo.TravelerCategories (Name, Code, Description)
    VALUES
        (N'Dating & Couples', N'COUPLES', N'Dating and couples trips'),
        (N'Families', N'FAMILIES', NULL),
        (N'Solo Travelers', N'SOLO', NULL),
        (N'Married Couples', N'MARRIED', NULL),
        (N'Friends / Bachelor / Bachelorette', N'FRIENDS', NULL);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Priorities WHERE Code = N'Romance')
BEGIN
    INSERT INTO dbo.Priorities (Name, Code)
    VALUES
        (N'Romance', N'Romance'),
        (N'Culture', N'Culture'),
        (N'Comfort', N'Comfort'),
        (N'Adventure', N'Adventure'),
        (N'Food', N'Food'),
        (N'Nightlife', N'Nightlife'),
        (N'Family', N'Family'),
        (N'Shopping', N'Shopping');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AgeTiers WHERE Name = N'25-34')
BEGIN
    INSERT INTO dbo.AgeTiers (Name, MinAge, MaxAge)
    VALUES
        (N'0-4', 0, 4),
        (N'5-11', 5, 11),
        (N'12-17', 12, 17),
        (N'18-24', 18, 24),
        (N'25-34', 25, 34),
        (N'35-49', 35, 49),
        (N'50+', 50, 120);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Countries WHERE Code = N'PK')
BEGIN
    INSERT INTO dbo.Countries (Name, Code, CurrencyCode) VALUES
        (N'Pakistan', N'PK', N'PKR'),
        (N'United Arab Emirates', N'AE', N'AED'),
        (N'Turkey', N'TR', N'TRY'),
        (N'Thailand', N'TH', N'THB'),
        (N'Maldives', N'MV', N'MVR'),
        (N'France', N'FR', N'EUR'),
        (N'Finland', N'FI', N'EUR');
END
GO

PRINT N'Reference seed complete. Use API DemoDataSeeder or a full destination import for Lahore-centric demo destinations, routes, and cost profiles.';
GO
