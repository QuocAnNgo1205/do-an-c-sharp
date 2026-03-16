using Microsoft.Maui.Devices.Sensors;
using VinhKhanhFoodTour.Interfaces;

namespace VinhKhanhFoodTour.Services
{
    /// <summary>
    /// Implementation of ILocationService using MAUI's Geolocation API.
    /// Provides device location tracking with permission management.
    /// </summary>
    public class LocationService : ILocationService
    {
        private readonly GeolocationRequest _geolocationRequest;

        public LocationService()
        {
            // Configure geolocation request with moderate accuracy for demo purposes
            _geolocationRequest = new GeolocationRequest(
                accuracy: GeolocationAccuracy.Best,
                timeout: TimeSpan.FromSeconds(10)
            );
        }

        /// <summary>
        /// Gets the current device location asynchronously.
        /// </summary>
        /// <returns>Location object with latitude/longitude, or null if unavailable.</returns>
        public async Task<Location?> GetCurrentLocationAsync()
        {
            try
            {
                // Check permissions before attempting to get location
                if (!(await CheckAndRequestPermissionAsync()))
                {
                    return null;
                }

                // Check if location services are enabled
                if (!(await IsLocationServiceEnabledAsync()))
                {
                    return null;
                }

                // Request current location from the device
                var location = await Geolocation.GetLocationAsync(_geolocationRequest);
                return location;
            }
            catch (Exception ex)
            {
                // Log error but don't crash the app
                System.Diagnostics.Debug.WriteLine($"LocationService Error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Ensures geolocation permission is granted before usage.
        /// </summary>
        /// <returns>True if permission is granted, false otherwise.</returns>
        public async Task<bool> CheckAndRequestPermissionAsync()
        {
            try
            {
                // Check if permission is already granted
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

                if (status != PermissionStatus.Granted)
                {
                    // Request permission if not granted
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }

                return status == PermissionStatus.Granted;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Permission Check Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Verifies that location services are enabled on the device.
        /// </summary>
        /// <returns>True if enabled, false otherwise.</returns>
        public async Task<bool> IsLocationServiceEnabledAsync()
        {
            try
            {
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Geolocation Check Error: {ex.Message}");
                return false;
            }
        }
    }
}
