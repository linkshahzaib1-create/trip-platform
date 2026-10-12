# ---- Frontend (Angular) ----
FROM node:22-alpine AS frontend
WORKDIR /frontend
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

# ---- Backend (.NET) ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend
WORKDIR /src
COPY backend/ ./
RUN dotnet restore TripPlatform.slnx \
 && dotnet publish src/TripPlatform.API/TripPlatform.API.csproj -c Release -o /app/publish /p:UseAppHost=false

# Copy Angular build into API wwwroot (same-origin SPA + API)
COPY --from=frontend /frontend/dist/trip-platform/browser /app/publish/wwwroot

# ---- Runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=backend /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
ENV PORT=10000
EXPOSE 10000
ENTRYPOINT ["dotnet", "TripPlatform.API.dll"]
