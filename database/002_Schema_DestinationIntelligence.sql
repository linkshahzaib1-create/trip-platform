/*
  TripPlatform — core geographic & destination intelligence schema
*/
USE TripPlatform;
GO

IF OBJECT_ID(N'dbo.Countries', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Countries (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        Code NVARCHAR(8) NOT NULL,
        CurrencyCode NVARCHAR(8) NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_Countries_Created DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc DATETIME2 NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Countries_Active DEFAULT (1),
        CONSTRAINT UQ_Countries_Code UNIQUE (Code)
    );
END
GO

IF OBJECT_ID(N'dbo.Regions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Regions (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        Code NVARCHAR(32) NULL,
        CountryId INT NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_Regions_Created DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc DATETIME2 NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Regions_Active DEFAULT (1),
        CONSTRAINT FK_Regions_Countries FOREIGN KEY (CountryId) REFERENCES dbo.Countries(Id)
    );
END
GO

IF OBJECT_ID(N'dbo.Destinations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Destinations (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        Slug NVARCHAR(200) NOT NULL,
        Description NVARCHAR(MAX) NULL,
        DestinationType INT NOT NULL,
        CountryId INT NOT NULL,
        RegionId INT NULL,
        Latitude FLOAT NOT NULL,
        Longitude FLOAT NOT NULL,
        IsInternationalHub BIT NOT NULL CONSTRAINT DF_Dest_Hub DEFAULT (0),
        GlobalPopularityScore INT NOT NULL CONSTRAINT DF_Dest_Global DEFAULT (0),
        RegionalPopularityScore INT NOT NULL CONSTRAINT DF_Dest_Regional DEFAULT (0),
        FamilyFriendlyScore INT NOT NULL CONSTRAINT DF_Dest_Family DEFAULT (0),
        CoupleScore INT NOT NULL CONSTRAINT DF_Dest_Couple DEFAULT (0),
        SoloScore INT NOT NULL CONSTRAINT DF_Dest_Solo DEFAULT (0),
        LuxuryScore INT NOT NULL CONSTRAINT DF_Dest_Luxury DEFAULT (0),
        BudgetScore INT NOT NULL CONSTRAINT DF_Dest_Budget DEFAULT (0),
        RomanceScore INT NOT NULL CONSTRAINT DF_Dest_Romance DEFAULT (0),
        CultureScore INT NOT NULL CONSTRAINT DF_Dest_Culture DEFAULT (0),
        AdventureScore INT NOT NULL CONSTRAINT DF_Dest_Adventure DEFAULT (0),
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_Dest_Created DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc DATETIME2 NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Dest_Active DEFAULT (1),
        CONSTRAINT UQ_Destinations_Slug UNIQUE (Slug),
        CONSTRAINT FK_Destinations_Countries FOREIGN KEY (CountryId) REFERENCES dbo.Countries(Id),
        CONSTRAINT FK_Destinations_Regions FOREIGN KEY (RegionId) REFERENCES dbo.Regions(Id)
    );
END
GO

IF OBJECT_ID(N'dbo.DestinationCategories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DestinationCategories (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(500) NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_DestCat_Created DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc DATETIME2 NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_DestCat_Active DEFAULT (1)
    );
END
GO

IF OBJECT_ID(N'dbo.DestinationCategoryMaps', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DestinationCategoryMaps (
        DestinationId INT NOT NULL,
        CategoryId INT NOT NULL,
        CONSTRAINT PK_DestinationCategoryMaps PRIMARY KEY (DestinationId, CategoryId),
        CONSTRAINT FK_DCM_Dest FOREIGN KEY (DestinationId) REFERENCES dbo.Destinations(Id),
        CONSTRAINT FK_DCM_Cat FOREIGN KEY (CategoryId) REFERENCES dbo.DestinationCategories(Id)
    );
END
GO

IF OBJECT_ID(N'dbo.OriginPopularities', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OriginPopularities (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        OriginDestinationId INT NOT NULL,
        TargetDestinationId INT NOT NULL,
        Score INT NOT NULL,
        Bucket NVARCHAR(64) NOT NULL,
        CONSTRAINT FK_OP_Origin FOREIGN KEY (OriginDestinationId) REFERENCES dbo.Destinations(Id),
        CONSTRAINT FK_OP_Target FOREIGN KEY (TargetDestinationId) REFERENCES dbo.Destinations(Id)
    );
END
GO

IF OBJECT_ID(N'dbo.Attractions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Attractions (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        DestinationId INT NOT NULL,
        ExternalPlaceId NVARCHAR(128) NULL,
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(MAX) NULL,
        Latitude FLOAT NOT NULL,
        Longitude FLOAT NOT NULL,
        Category NVARCHAR(100) NOT NULL,
        AverageVisitDurationMinutes INT NOT NULL,
        EntryCostMin DECIMAL(18,2) NOT NULL,
        EntryCostTypical DECIMAL(18,2) NOT NULL,
        EntryCostMax DECIMAL(18,2) NOT NULL,
        Currency NVARCHAR(8) NOT NULL CONSTRAINT DF_Attr_Cur DEFAULT ('USD'),
        OpeningTime NVARCHAR(16) NULL,
        ClosingTime NVARCHAR(16) NULL,
        PopularityScore INT NOT NULL,
        FamilyScore INT NOT NULL,
        CoupleScore INT NOT NULL,
        SoloScore INT NOT NULL,
        RomanceScore INT NOT NULL,
        CultureScore INT NOT NULL,
        PhotographyScore INT NOT NULL,
        AccessibilityScore INT NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_Attr_Created DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc DATETIME2 NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Attr_Active DEFAULT (1),
        CONSTRAINT FK_Attractions_Dest FOREIGN KEY (DestinationId) REFERENCES dbo.Destinations(Id)
    );
END
GO

IF OBJECT_ID(N'dbo.Activities', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Activities (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        DestinationId INT NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(MAX) NULL,
        Category NVARCHAR(100) NOT NULL,
        DurationMinutes INT NOT NULL,
        CostMin DECIMAL(18,2) NOT NULL,
        CostTypical DECIMAL(18,2) NOT NULL,
        CostMax DECIMAL(18,2) NOT NULL,
        Currency NVARCHAR(8) NOT NULL CONSTRAINT DF_Act_Cur DEFAULT ('USD'),
        MinAge INT NULL,
        MaxAge INT NULL,
        BudgetLevel NVARCHAR(32) NOT NULL,
        FamilyScore INT NOT NULL,
        CoupleScore INT NOT NULL,
        SoloScore INT NOT NULL,
        SeasonHint NVARCHAR(100) NULL,
        AffiliateUrl NVARCHAR(500) NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_Act_Created DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc DATETIME2 NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Act_Active DEFAULT (1),
        CONSTRAINT FK_Activities_Dest FOREIGN KEY (DestinationId) REFERENCES dbo.Destinations(Id)
    );
END
GO
