namespace TripPlatform.Application.Common.Interfaces;

public interface IPlaceSyncService
{
    /// <summary>
    /// Refresh hotels, restaurants, attractions, shopping, nightlife from open data into local DB.
    /// Returns number of listings upserted.
    /// </summary>
    Task<int> SyncDestinationPlacesAsync(int destinationId, CancellationToken cancellationToken = default);

    Task EnsureFreshAsync(int destinationId, TimeSpan maxAge, CancellationToken cancellationToken = default);
}