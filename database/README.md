# TripPlatform Database

## Folders
- `database/postgres/` — PostgreSQL DDL scripts
- `database/*.sql` — legacy SQL Server scripts (optional)
- `database/docker-compose.yml` — local Postgres 16

## Local PostgreSQL (Docker)

```powershell
cd database
docker compose up -d
```

Connection string:

```
Host=localhost;Port=5432;Database=tripplatform;Username=trip;Password=trip
```

API `appsettings.Development.json`:

```json
"Database": { "Provider": "PostgreSQL" },
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=tripplatform;Username=trip;Password=trip"
}
```

On first API start, EF Core `EnsureCreated` + `DemoDataSeeder` create tables and Lahore-centric seed data.

## Railway

Production uses Railway Postgres. The web service is configured with:

- `Database__Provider=PostgreSQL`
- `DATABASE_URL` / `ConnectionStrings__DefaultConnection` pointing at the Postgres service
