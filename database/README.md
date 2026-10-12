# TripPlatform Database Scripts

Run in order against SQL Server (SSMS or `sqlcmd`):

1. `001_CreateDatabase.sql`
2. `002_Schema_DestinationIntelligence.sql`
3. `003_Schema_CostRoutesListings.sql`
4. `004_Schema_Estimation.sql`
5. `005_Indexes.sql`
6. `006_Seed_ReferenceData.sql`

Local development currently uses **EF Core InMemory** (`Database:Provider = InMemory` in API `appsettings.json`). Switch to SQL Server with:

```json
"Database": { "Provider": "SqlServer" },
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=TripPlatform;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

Rich Lahore-centric demo destinations/routes/cost profiles are loaded by `DemoDataSeeder` when the API starts on InMemory.
