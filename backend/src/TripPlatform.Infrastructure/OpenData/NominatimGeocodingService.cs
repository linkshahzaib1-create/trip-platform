using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using TripPlatform.Application.Common.Interfaces;

namespace TripPlatform.Infrastructure.OpenData;

public sealed class NominatimGeocodingService : IGeocodingService
{
    private readonly HttpClient _http;
    private readonly ILogger<NominatimGeocodingService> _logger;

    public NominatimGeocodingService(HttpClient http, ILogger<NominatimGeocodingService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<GeoCityResult?> ReverseGeocodeAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url =
                $"reverse?lat={latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&lon={longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&format=json&addressdetails=1";
            var data = await _http.GetFromJsonAsync<NominatimResult>(url, cancellationToken);
            return Map(data);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Nominatim reverse geocode failed for {Lat},{Lng}", latitude, longitude);
            return null;
        }
    }

    public async Task<GeoCityResult?> ForwardGeocodeAsync(
        string placeName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(placeName))
            return null;

        try
        {
            var url = $"search?q={Uri.EscapeDataString(placeName)}&format=json&addressdetails=1&limit=1";
            var list = await _http.GetFromJsonAsync<List<NominatimResult>>(url, cancellationToken);
            return Map(list?.FirstOrDefault());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Nominatim forward geocode failed for {Place}", placeName);
            return null;
        }
    }

    private static GeoCityResult? Map(NominatimResult? data)
    {
        if (data is null)
            return null;

        var addr = data.Address;
        var city = addr?.City
            ?? addr?.Town
            ?? addr?.Village
            ?? addr?.Municipality
            ?? addr?.County
            ?? addr?.State
            ?? data.Name
            ?? "Unknown";

        if (!double.TryParse(data.Lat, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var lat))
            lat = 0;
        if (!double.TryParse(data.Lon, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var lng))
            lng = 0;

        return new GeoCityResult(
            city,
            addr?.Country,
            addr?.CountryCode?.ToUpperInvariant(),
            lat,
            lng,
            data.DisplayName ?? city);
    }

    private sealed class NominatimResult
    {
        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("lat")]
        public string? Lat { get; set; }

        [JsonPropertyName("lon")]
        public string? Lon { get; set; }

        [JsonPropertyName("address")]
        public NominatimAddress? Address { get; set; }
    }

    private sealed class NominatimAddress
    {
        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("town")]
        public string? Town { get; set; }

        [JsonPropertyName("village")]
        public string? Village { get; set; }

        [JsonPropertyName("municipality")]
        public string? Municipality { get; set; }

        [JsonPropertyName("county")]
        public string? County { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("country_code")]
        public string? CountryCode { get; set; }
    }
}
