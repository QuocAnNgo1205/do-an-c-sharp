namespace VinhKhanhFoodTour.Models
{
    /// <summary>
    /// Represents a Point of Interest (POI) for the food tour.
    /// Contains location data and narration information for TTS playback.
    /// </summary>
    public class PoiModel
    {
        /// <summary>
        /// Unique identifier for the POI.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Name of the POI (e.g., "Ben Thanh Market").
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Latitude coordinate of the POI.
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// Longitude coordinate of the POI.
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// Description text to be narrated via TTS when user approaches the POI.
        /// </summary>
        public string DescriptionText { get; set; } = string.Empty;

        /// <summary>
        /// Radius in meters. When user is within this distance, narration triggers.
        /// </summary>
        public double Radius { get; set; }

        public override string ToString()
        {
            return $"{Name} (Lat: {Latitude:F6}, Lon: {Longitude:F6})";
        }
    }
}
