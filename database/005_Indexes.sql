/*
  TripPlatform — useful indexes for discovery and estimation
*/
USE TripPlatform;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Destinations_Name' AND object_id = OBJECT_ID(N'dbo.Destinations'))
    CREATE INDEX IX_Destinations_Name ON dbo.Destinations(Name);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OriginPopularities_Origin' AND object_id = OBJECT_ID(N'dbo.OriginPopularities'))
    CREATE INDEX IX_OriginPopularities_Origin ON dbo.OriginPopularities(OriginDestinationId, Score DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Routes_OriginTarget' AND object_id = OBJECT_ID(N'dbo.Routes'))
    CREATE INDEX IX_Routes_OriginTarget ON dbo.Routes(OriginDestinationId, TargetDestinationId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CostProfiles_DestTier' AND object_id = OBJECT_ID(N'dbo.DestinationCostProfiles'))
    CREATE INDEX IX_CostProfiles_DestTier ON dbo.DestinationCostProfiles(DestinationId, BudgetTier);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Listings_DestType' AND object_id = OBJECT_ID(N'dbo.Listings'))
    CREATE INDEX IX_Listings_DestType ON dbo.Listings(DestinationId, ListingType);
GO
