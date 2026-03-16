using VinhKhanhFoodTour.Interfaces;
using VinhKhanhFoodTour.Models;

namespace VinhKhanhFoodTour.Services
{
    /// <summary>
    /// Implementation of INarrationService.
    /// Handles geofence detection, cooldown management, and TTS narration playback.
    /// </summary>
    public class NarrationService : INarrationService
    {
        /// <summary>
        /// Cooldown duration in minutes. Prevents repeated narration of the same POI.
        /// </summary>
        private const int COOLDOWN_DURATION_MINUTES = 5;

        /// <summary>
        /// Tracks the last triggered time for each POI (by ID).
        /// Prevents audio spam by enforcing cooldown period.
        /// </summary>
        private Dictionary<int, DateTime> _poiLastTriggeredTimes = new();

        /// <summary>
        /// Calculates the distance between two geographic coordinates using the Haversine formula.
        /// 
        /// The Haversine formula calculates the great-circle distance between two points
        /// on a sphere given their latitudes and longitudes.
        /// 
        /// Formula:
        /// a = sin²(Δφ/2) + cos φ1 ⋅ cos φ2 ⋅ sin²(Δλ/2)
        /// c = 2 ⋅ atan2( √a, √(1−a) )
        /// d = R ⋅ c
        /// 
        /// where:
        /// - φ is latitude, λ is longitude, R is earth's radius (≈6371 km)
        /// - Δφ is the difference in latitude
        /// - Δλ is the difference in longitude
        /// </summary>
        /// <param name="userLatitude">User's current latitude in degrees.</param>
        /// <param name="userLongitude">User's current longitude in degrees.</param>
        /// <param name="poiLatitude">POI's latitude in degrees.</param>
        /// <param name="poiLongitude">POI's longitude in degrees.</param>
        /// <returns>Distance in meters.</returns>
        public double CalculateDistance(double userLatitude, double userLongitude, double poiLatitude, double poiLongitude)
        {
            const double EARTH_RADIUS_KM = 6371.0; // Earth's radius in kilometers

            // Convert latitude and longitude from degrees to radians
            double lat1Rad = DegreesToRadians(userLatitude);
            double lon1Rad = DegreesToRadians(userLongitude);
            double lat2Rad = DegreesToRadians(poiLatitude);
            double lon2Rad = DegreesToRadians(poiLongitude);

            // Calculate differences in latitude and longitude
            double dLat = lat2Rad - lat1Rad;
            double dLon = lon2Rad - lon1Rad;

            // Haversine formula implementation
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            // Calculate distance in kilometers, then convert to meters
            double distanceKm = EARTH_RADIUS_KM * c;
            double distanceMeters = distanceKm * 1000;

            return distanceMeters;
        }

        /// <summary>
        /// Checks if a POI should trigger narration.
        /// 
        /// Conditions for triggering:
        /// 1. User must be within the POI's radius (geofence check)
        /// 2. POI must not be in cooldown period (last narration was more than 5 minutes ago)
        /// </summary>
        /// <param name="poi">The POI to evaluate.</param>
        /// <param name="userLatitude">User's current latitude.</param>
        /// <param name="userLongitude">User's current longitude.</param>
        /// <returns>True if narration should trigger, false otherwise.</returns>
        public bool ShouldTriggerNarration(PoiModel poi, double userLatitude, double userLongitude)
        {
            // Calculate distance between user and POI
            double distance = CalculateDistance(userLatitude, userLongitude, poi.Latitude, poi.Longitude);

            // Check if user is within geofence radius
            if (distance > poi.Radius)
            {
                return false; // User is outside the POI's activation radius
            }

            // Check cooldown period
            // If POI was triggered before, check if cooldown has passed
            if (_poiLastTriggeredTimes.TryGetValue(poi.Id, out var lastTriggerTime))
            {
                TimeSpan timeSinceLastTrigger = DateTime.UtcNow - lastTriggerTime;

                // If cooldown period hasn't elapsed, skip this narration
                if (timeSinceLastTrigger.TotalMinutes < COOLDOWN_DURATION_MINUTES)
                {
                    return false; // POI is in cooldown, prevent audio spam
                }
            }

            // All conditions met: within radius and not in cooldown
            return true;
        }

        /// <summary>
        /// Plays narration text using the MAUI TextToSpeech API.
        /// </summary>
        /// <param name="text">The text to narrate.</param>
        /// <param name="cancellationToken">Token to cancel TTS playback.</param>
        public async Task PlayNarrationAsync(string text, CancellationToken cancellationToken = default)
        {
            try
            {
                // Configure TTS settings for narration
                var settings = new SpeechOptions();

                // Use TextToSpeech API to speak the text
                await TextToSpeech.SpeakAsync(text, cancelToken: cancellationToken);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TTS Error: {ex.Message}");
                // Continue app execution even if TTS fails
            }
        }

        /// <summary>
        /// Marks a POI as triggered at the current time, starting its cooldown period.
        /// Called after narration is played to enforce cooldown.
        /// </summary>
        /// <param name="poiId">The ID of the POI that was triggered.</param>
        public void MarkPoiAsTriggered(int poiId)
        {
            _poiLastTriggeredTimes[poiId] = DateTime.UtcNow;
        }

        /// <summary>
        /// Resets the cooldown for a specific POI (useful for testing).
        /// </summary>
        /// <param name="poiId">The ID of the POI.</param>
        public void ResetPoiCooldown(int poiId)
        {
            _poiLastTriggeredTimes.Remove(poiId);
        }

        /// <summary>
        /// Gets the last triggered date/time for a POI.
        /// </summary>
        /// <param name="poiId">The ID of the POI.</param>
        /// <returns>DateTime of last trigger, or null if never triggered.</returns>
        public DateTime? GetLastTriggeredTime(int poiId)
        {
            return _poiLastTriggeredTimes.TryGetValue(poiId, out var time) ? time : null;
        }

        /// <summary>
        /// Converts degrees to radians for trigonometric calculations.
        /// </summary>
        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }
    }
}
