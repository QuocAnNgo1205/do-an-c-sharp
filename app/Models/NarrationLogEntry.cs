namespace VinhKhanhFoodTour.Models
{
    /// <summary>
    /// Represents a log entry for a triggered narration event.
    /// Used to display narration history in the UI.
    /// </summary>
    public class NarrationLogEntry
    {
        /// <summary>
        /// Name of the POI that triggered the narration.
        /// </summary>
        public string PoiName { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the narration was triggered.
        /// </summary>
        public DateTime TriggeredAt { get; set; }

        /// <summary>
        /// The description text that was narrated.
        /// </summary>
        public string NarrationText { get; set; } = string.Empty;

        /// <summary>
        /// User's location when narration was triggered.
        /// </summary>
        public string UserLocation { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"[{TriggeredAt:HH:mm:ss}] {PoiName} - {NarrationText.Substring(0, Math.Min(40, NarrationText.Length))}...";
        }
    }
}
