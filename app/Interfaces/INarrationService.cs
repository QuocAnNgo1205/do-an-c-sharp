using VinhKhanhFoodTour.Models;

namespace VinhKhanhFoodTour.Interfaces
{
    /// <summary>
    /// Interface for narration and geofence management service.
    /// Handles distance calculations, cooldown checks, and TTS playback.
    /// </summary>
    public interface INarrationService
    {
        /// <summary>
        /// Calculates the distance in meters between a user location and a POI using the Haversine formula.
        /// </summary>
        /// <param name="userLatitude">User's current latitude.</param>
        /// <param name="userLongitude">User's current longitude.</param>
        /// <param name="poiLatitude">POI's latitude.</param>
        /// <param name="poiLongitude">POI's longitude.</param>
        /// <returns>Distance in meters.</returns>
        double CalculateDistance(double userLatitude, double userLongitude, double poiLatitude, double poiLongitude);

        /// <summary>
        /// Checks if a POI should trigger narration based on:
        /// 1. User being within the geofence radius
        /// 2. Cooldown period not being active
        /// </summary>
        /// <param name="poi">The POI to check.</param>
        /// <param name="userLatitude">User's current latitude.</param>
        /// <param name="userLongitude">User's current longitude.</param>
        /// <returns>True if narration should be triggered, false otherwise.</returns>
        bool ShouldTriggerNarration(PoiModel poi, double userLatitude, double userLongitude);

        /// <summary>
        /// Plays the narration text using Text-to-Speech.
        /// </summary>
        /// <param name="text">The text to speak.</param>
        /// <param name="cancellationToken">CancellationToken to stop playback.</param>
        Task PlayNarrationAsync(string text, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resets the cooldown for a specific POI (used for testing purposes).
        /// </summary>
        /// <param name="poiId">The ID of the POI.</param>
        void ResetPoiCooldown(int poiId);

        /// <summary>
        /// Gets the last triggered time for a POI, or null if never triggered.
        /// </summary>
        /// <param name="poiId">The ID of the POI.</param>
        /// <returns>DateTime of last trigger, or null if never triggered.</returns>
        DateTime? GetLastTriggeredTime(int poiId);
    }
}
