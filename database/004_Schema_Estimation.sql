/*
  TripPlatform — traveler reference data + trip estimation tables
*/
USE TripPlatform;
GO

IF OBJECT_ID(N'dbo.TravelerCategories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TravelerCategories (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        Code NVARCHAR(50) NOT NULL,
        Description NVARCHAR(500) NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_TC_Created DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc DATETIME2 NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_TC_Active DEFAULT (1),
        CONSTRAINT UQ_TravelerCategories_Code UNIQUE (Code)
    );
END
GO

IF OBJECT_ID(N'dbo.TravelerSubcategories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TravelerSubcategories (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CategoryId INT NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        Code NVARCHAR(50) NOT NULL,
        DefaultRecommendationBehavior NVARCHAR(500) NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_TSC_Created DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc DATETIME2 NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_TSC_Active DEFAULT (1),
        CONSTRAINT FK_TSC_TC FOREIGN KEY (CategoryId) REFERENCES dbo.TravelerCategories(Id)
    );
END
GO

IF OBJECT_ID(N'dbo.AgeTiers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgeTiers (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(50) NOT NULL,
        MinAge INT NOT NULL,
        MaxAge INT NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_AT_Created DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc DATETIME2 NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_AT_Active DEFAULT (1)
    );
END
GO

IF OBJECT_ID(N'dbo.Priorities', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Priorities (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        Code NVARCHAR(50) NOT NULL,
        Description NVARCHAR(500) NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_Pr_Created DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc DATETIME2 NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Pr_Active DEFAULT (1)
    );
END
GO

IF OBJECT_ID(N'dbo.CurrencyRates', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CurrencyRates (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        FromCurrency NVARCHAR(8) NOT NULL,
        ToCurrency NVARCHAR(8) NOT NULL,
        Rate DECIMAL(18,6) NOT NULL,
        AsOfUtc DATETIME2 NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_CR_Created DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc DATETIME2 NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_CR_Active DEFAULT (1)
    );
END
GO

IF OBJECT_ID(N'dbo.TripPlans', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TripPlans (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserId NVARCHAR(128) NULL,
        OriginDestinationId INT NOT NULL,
        SelectedDestinationId INT NULL,
        StartDate DATE NOT NULL,
        EndDate DATE NOT NULL,
        Nights INT NOT NULL,
        Days INT NOT NULL,
        BudgetTotal DECIMAL(18,2) NULL,
        Currency NVARCHAR(8) NOT NULL,
        BudgetMode INT NOT NULL,
        TravelerCategoryCode NVARCHAR(50) NULL,
        TravelerSubcategoryCode NVARCHAR(50) NULL,
        PrioritiesCsv NVARCHAR(500) NOT NULL,
        Status NVARCHAR(50) NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_TP_Created DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc DATETIME2 NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_TP_Active DEFAULT (1),
        CONSTRAINT FK_TP_Origin FOREIGN KEY (OriginDestinationId) REFERENCES dbo.Destinations(Id),
        CONSTRAINT FK_TP_Selected FOREIGN KEY (SelectedDestinationId) REFERENCES dbo.Destinations(Id)
    );
END
GO

IF OBJECT_ID(N'dbo.TripTravelers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TripTravelers (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        TripPlanId INT NOT NULL,
        Age INT NOT NULL,
        Role NVARCHAR(32) NOT NULL,
        CONSTRAINT FK_TT_TP FOREIGN KEY (TripPlanId) REFERENCES dbo.TripPlans(Id)
    );
END
GO

IF OBJECT_ID(N'dbo.TripDays', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TripDays (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        TripPlanId INT NOT NULL,
        DayNumber INT NOT NULL,
        Date DATE NOT NULL,
        Theme NVARCHAR(100) NULL,
        CONSTRAINT FK_TD_TP FOREIGN KEY (TripPlanId) REFERENCES dbo.TripPlans(Id)
    );
END
GO

IF OBJECT_ID(N'dbo.ItineraryItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ItineraryItems (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        TripDayId INT NOT NULL,
        Title NVARCHAR(200) NOT NULL,
        Category NVARCHAR(50) NOT NULL,
        Sequence INT NOT NULL,
        DurationMinutes INT NOT NULL,
        EstimatedCostTypical DECIMAL(18,2) NOT NULL,
        Currency NVARCHAR(8) NOT NULL,
        IsOptional BIT NOT NULL,
        PriorityTags NVARCHAR(200) NULL,
        Notes NVARCHAR(500) NULL,
        CONSTRAINT FK_II_TD FOREIGN KEY (TripDayId) REFERENCES dbo.TripDays(Id)
    );
END
GO

IF OBJECT_ID(N'dbo.TripEstimates', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TripEstimates (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        TripPlanId INT NOT NULL,
        ScenarioName NVARCHAR(50) NOT NULL,
        TotalMin DECIMAL(18,2) NOT NULL,
        TotalTypical DECIMAL(18,2) NOT NULL,
        TotalMax DECIMAL(18,2) NOT NULL,
        Currency NVARCHAR(8) NOT NULL,
        BudgetFit INT NOT NULL,
        ExplanationJson NVARCHAR(MAX) NULL,
        SnapshotJson NVARCHAR(MAX) NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_TE_Created DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc DATETIME2 NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_TE_Active DEFAULT (1),
        CONSTRAINT FK_TE_TP FOREIGN KEY (TripPlanId) REFERENCES dbo.TripPlans(Id)
    );
END
GO

IF OBJECT_ID(N'dbo.EstimateLines', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EstimateLines (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        TripEstimateId INT NOT NULL,
        Category NVARCHAR(100) NOT NULL,
        Min DECIMAL(18,2) NOT NULL,
        Typical DECIMAL(18,2) NOT NULL,
        Max DECIMAL(18,2) NOT NULL,
        Currency NVARCHAR(8) NOT NULL,
        Notes NVARCHAR(500) NULL,
        CONSTRAINT FK_EL_TE FOREIGN KEY (TripEstimateId) REFERENCES dbo.TripEstimates(Id)
    );
END
GO
