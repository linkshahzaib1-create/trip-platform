# TripPlatform

Destination discovery + personalized trip estimation (not a booking marketplace).

Inspired by Booking.com / Agoda / TripAdvisor / Wanderlog flows for **discovery and planning**, with affiliate handoff later — no payments or inventory locking in v1.

## Folder structure

```
backend/     .NET 10 Clean Architecture + CQRS (MediatR)
frontend/    Angular 22 SPA
database/    SQL Server DDL + seed scripts
```

### Backend layers

- `TripPlatform.Domain` — entities, enums, value objects
- `TripPlatform.Application` — commands/queries, validators, MediatR handlers
- `TripPlatform.Infrastructure` — EF Core (InMemory or SQL Server), seed data
- `TripPlatform.API` — controllers, Swagger, CORS

## Run locally

### API (InMemory database + Lahore demo seed)

```powershell
cd backend
dotnet run --project src/TripPlatform.API --urls http://localhost:5280
```

Swagger: http://localhost:5280/swagger

### Angular UI

```powershell
cd frontend
npm start
```

Open http://localhost:4200

**UI stack:** Tailwind CSS v4 + daisyUI 5 (emerald theme) + `@ng-icons` (Lucide). Fully responsive (`sm` / `md` / `lg` / `xl`). No custom component CSS.

### SQL Server scripts

See [database/README.md](database/README.md). Run scripts `001` → `006` in order, then set:

```json
"Database": { "Provider": "SqlServer" }
```

## Free public hosting (Render)

Cursor does **not** provide public domains or app hosting (only private code backups). This project deploys as **one free service** on [Render](https://render.com): Angular SPA + .NET API in the same container.

1. Push this repo to GitHub.
2. In Render: **New → Blueprint** → select the repo (uses `render.yaml`).
3. Choose the **Free** plan and deploy.

You get a public HTTPS URL like `https://trip-platform.onrender.com`.

Notes:
- Free web services sleep after ~15 minutes idle; first request after sleep can take ~1 minute.
- Data uses in-memory seed (resets when the service restarts).

## Core APIs

- `GET /api/v1/destinations/search?q=Lahore`
- `POST /api/v1/destinations/recommend`
- `POST /api/v1/trips/estimate`
- `GET /api/v1/traveler-categories`
- `GET /api/v1/priorities`

### Example recommend body

```json
{
  "origin": "Lahore",
  "startDate": "2026-12-10",
  "endDate": "2026-12-15",
  "travelers": [{ "age": 29 }, { "age": 27 }],
  "budget": 2000,
  "currency": "USD",
  "travelerCategory": "Dating & Couples",
  "subcategory": "FirstTripTogether",
  "priorities": ["Romance", "Culture", "Comfort"]
}
```

## Notes

- Prices are **ranges** (min/typical/max), not live booking quotes.
- Demo seed includes Lahore → Islamabad, Murree, Hunza, Dubai, Istanbul, Bangkok, Maldives (+ Helsinki→Paris).
- Competitive docs drive phased roadmap: listings, maps, collaboration, AI assistant, monetization in later phases.
