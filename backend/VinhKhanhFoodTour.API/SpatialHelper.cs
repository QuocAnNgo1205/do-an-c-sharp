using NetTopologySuite.Geometries;

namespace VinhKhanhFoodTour.API
{
    /// <summary>
    /// Helper class for spatial geometry conversions between DTOs and entities.
    /// </summary>
    public static class SpatialHelper
    {
        /// <summary>
        /// SRID 4326 is the standard for WGS84 (latitude/longitude coordinates).
        /// </summary>
        private const int SRID_WGS84 = 4326;

        private static readonly GeometryFactory GeometryFactory = new GeometryFactory(new PrecisionModel(), SRID_WGS84);

        /// <summary>
        /// Create a Point geometry from latitude and longitude coordinates.
        /// </summary>
        /// <param name="latitude">Latitude in WGS84 (SRID 4326)</param>
        /// <param name="longitude">Longitude in WGS84 (SRID 4326)</param>
        /// <returns>A Point with SRID 4326</returns>
        public static Point CreatePoint(double latitude, double longitude)
        {
            // Note: In geography, the order is typically (longitude, latitude)
            return GeometryFactory.CreatePoint(new Coordinate(longitude, latitude));
        }

        /// <summary>
        /// Extract latitude from a Point geometry.
        /// </summary>
        public static double GetLatitude(Point point)
        {
            return point?.Coordinate?.Y ?? 0.0;
        }

        /// <summary>
        /// Extract longitude from a Point geometry.
        /// </summary>
        public static double GetLongitude(Point point)
        {
            return point?.Coordinate?.X ?? 0.0;
        }
    }
}
