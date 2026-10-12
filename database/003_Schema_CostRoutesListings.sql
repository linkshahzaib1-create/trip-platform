/*
  TripPlatform — cost profiles, seasons, routes, listings
*/
USE TripPlatform;
GO

IF OBJECT_ID(N'dbo.Seasons', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Seasons (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        DestinationId INT NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        Kind INT NOT NULL,
        StartMonth INT NOT NULL,
        StartDay INT NOT NULL,
        EndMonth INT NOT NULL,
        EndDay INT NOT NULL,
        PriceMultiplier DECIMAL(18,2) NOT NULL,
        Notes NVARCHAR(500) NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_Seasons_Created DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc DATETIME2 NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Seasons_Active DEFAULT (1),
        CONSTRAINT FK_Seasons_Dest FOREIGN KEY (DestinationId) REFERENCES dbo.Destinations(Id)
    );
END
GO

IF OBJECT_ID(N'dbo.DestinationCostProfiles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DestinationCostProfiles (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        DestinationId INT NOT NULL,
        SeasonId INT NULL,
        BudgetTier NVARCHAR(32) NOT NULL,
        Currency NVARCHAR(8) NOT NULL,
        AccommodationMin DECIMAL(18,2) NOT NULL,
        AccommodationTypical DECIMAL(18,2) NOT NULL,
        AccommodationMax DECIMAL(18,2) NOT NULL,
        FoodMin DECIMAL(18,2) NOT NULL,
        FoodTypical DECIMAL(18,2) NOT NULL,
        FoodMax DECIMAL(18,2) NOT NULL,
        LocalTransportMin DECIMAL(18,2) NOT NULL,
        LocalTransportTypical DECIMAL(18,2) NOT NULL,
        LocalTransportMax DECIMAL(18,2) NOT NULL,
        ActivitiesMin DECIMAL(18,2) NOT NULL,
        ActivitiesTypical DECIMAL(18,2) NOT NULL,
        ActivitiesMax DECIMAL(18,2) NOT NULL,
        EntertainmentMin DECIMAL(18,2) NOT NULL,
        EntertainmentTypical DECIMAL(18,2) NOT NULL,
        EntertainmentMax DECIMAL(18,2) NOT NULL,
        ShoppingMin DECIMAL(18,2) NOT NULL,
        ShoppingTypical DECIMAL(18,2) NOT NULL,
        ShoppingMax DECIMAL(18,2) NOT NULL,
        AirportTransferMin DECIMAL(18,2) NOT NULL,
        AirportTransferTypical DECIMAL(18,2) NOT NULL,
        AirportTransferMax DECIMAL(18,2) NOT NULL,
        ContingencyPercent DECIMAL(18,2) NOT NULL,
        Source NVARCHAR(100) NOT NULL,
        SourceId NVARCHAR(100) NULL,
        RetrievedAtUtc DATETIME2 NOT NULL,
        ValidFromUtc DATETIME2 NOT NULL,
        ValidToUtc DATETIME2 NULL,
        Confidence INT NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_DCP_Created DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc DATETIME2 NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_DCP_Active DEFAULT (1),
        CONSTRAINT FK_DCP_Dest FOREIGN KEY (DestinationId) REFERENCES dbo.Destinations(Id),
        CONSTRAINT FK_DCP_Season FOREIGN KEY (SeasonId) REFERENCES dbo.Seasons(Id)
    );
END
GO

IF OBJECT_ID(N'dbo.Routes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Routes (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        OriginDestinationId INT NOT NULL,
        TargetDestinationId INT NOT NULL,
        DistanceKm FLOAT NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_Routes_Created DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc DATETIME2 NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Routes_Active DEFAULT (1),
        CONSTRAINT FK_Routes_Origin FOREIGN KEY (OriginDestinationId) REFERENCES dbo.Destinations(Id),
        CONSTRAINT FK_Routes_Target FOREIGN KEY (TargetDestinationId) REFERENCES dbo.Destinations(Id)
    );
END
GO

IF OBJECT_ID(N'dbo.RouteOptions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RouteOptions (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        RouteId INT NOT NULL,
        TravelMode INT NOT NULL,
        DurationMinutes INT NOT NULL,
        CostMin DECIMAL(18,2) NOT NULL,
        CostTypical DECIMAL(18,2) NOT NULL,
        CostMax DECIMAL(18,2) NOT NULL,
        Currency NVARCHAR(8) NOT NULL,
        SeasonHint NVARCHAR(100) NULL,
        Notes NVARCHAR(500) NULL,
        Confidence INT NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_RO_Created DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc DATETIME2 NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_RO_Active DEFAULT (1),
        CONSTRAINT FK_RouteOptions_Routes FOREIGN KEY (RouteId) REFERENCES dbo.Routes(Id)
    );
END
GO

IF OBJECT_ID(N'dbo.Listings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Listings (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        DestinationId INT NOT NULL,
        ListingType NVARCHAR(32) NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(MAX) NULL,
        Neighborhood NVARCHAR(200) NULL,
        Latitude FLOAT NOT NULL,
        Longitude FLOAT NOT NULL,
        PriceMin DECIMAL(18,2) NULL,
        PriceTypical DECIMAL(18,2) NULL,
        PriceMax DECIMAL(18,2) NULL,
        Currency NVARCHAR(8) NOT NULL,
        Rating DECIMAL(18,2) NULL,
        ReviewCount INT NOT NULL,
        StarRating INT NULL,
        AmenitiesCsv NVARCHAR(500) NULL,
        Cuisine NVARCHAR(100) NULL,
        FamilyFriendly BIT NOT NULL CONSTRAINT DF_List_Family DEFAULT (0),
        ChildFriendly BIT NOT NULL CONSTRAINT DF_List_Child DEFAULT (0),
        AffiliateUrl NVARCHAR(500) NULL,
        AffiliatePartner NVARCHAR(100) NULL,
        ImageUrl NVARCHAR(500) NULL,
        PopularityScore INT NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_List_Created DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc DATETIME2 NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_List_Active DEFAULT (1),
        CONSTRAINT FK_Listings_Dest FOREIGN KEY (DestinationId) REFERENCES dbo.Destinations(Id)
    );
END
GO
