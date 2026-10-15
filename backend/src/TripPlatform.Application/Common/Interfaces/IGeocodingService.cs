namespace TripPlatform.Application.Common.Interfaces;

public sealed record GeoCityResult(
    string City,
    string? Country,
    string? CountryCode,
    double Latitude,
    double Longitude,
    string DisplayName);

public interface IGeocodingService
{
    Task<GeoCityResult?> ReverseGeocodeAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
    Task<GeoCityResult?> ForwardGeocodeAsync(string placeName, CancellationToken cancellationToken = default);
}
