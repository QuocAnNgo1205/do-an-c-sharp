namespace VinhKhanhFoodTour.DTOs
{
    /// <summary>
    /// Request DTO for creating a new POI.
    /// Clients still send Latitude and Longitude; we convert to Point internally.
    /// </summary>
    public class CreatePoiRequest
    {
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Latitude in WGS84 (SRID 4326)
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// Longitude in WGS84 (SRID 4326)
        /// </summary>
        public double Longitude { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
