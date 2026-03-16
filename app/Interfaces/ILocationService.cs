using Microsoft.Maui.Devices.Sensors;

namespace VinhKhanhFoodTour.Interfaces
{
    /// <summary>
    /// Interface for location tracking service.
    /// Abstracts the geolocation functionality for the application.
    /// </summary>
    public interface ILocationService
    {
        /// <summary>
        /// Gets the current location of the device asynchronously.
        /// </summary>
        /// <returns>A Location object containing latitude and longitude, or null if unavailable.</returns>
        Task<Location?> GetCurrentLocationAsync();

        /// <summary>
        /// Checks if geolocation permission is granted.
        /// </summary>
        /// <returns>True if permission is granted, false otherwise.</returns>
        Task<bool> CheckAndRequestPermissionAsync();

        /// <summary>
        /// Checks if location services are enabled on the device.
        /// </summary>
        /// <returns>True if location services are enabled, false otherwise.</returns>
        Task<bool> IsLocationServiceEnabledAsync();
    }
}
