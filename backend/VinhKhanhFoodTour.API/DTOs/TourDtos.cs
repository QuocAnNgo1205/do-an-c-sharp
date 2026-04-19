namespace VinhKhanhFoodTour.DTOs
{
    /// <summary>Request DTO for creating a new Tour.</summary>
    public class TourCreateDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal EstimatedPrice { get; set; }
        public string? ThumbnailUrl { get; set; }
        public List<TourPoiItemDto> Pois { get; set; } = new();
    }

    /// <summary>Request DTO for updating an existing Tour.</summary>
    public class TourUpdateDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public decimal? EstimatedPrice { get; set; }
        public string? ThumbnailUrl { get; set; }
        public List<TourPoiItemDto> Pois { get; set; } = new();
    }

    /// <summary>Represents a single POI entry in the Tour route with its order.</summary>
    public class TourPoiItemDto
    {
        public int PoiId { get; set; }
        public int OrderIndex { get; set; }
    }

    /// <summary>Response DTO returned when listing or fetching a Tour.</summary>
    public class TourResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal EstimatedPrice { get; set; }
        public string? ThumbnailUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        /// <summary>Total number of times this tour has been started by any device.</summary>
        public int UsageCount { get; set; }
        public List<TourPoiResponseDto> Pois { get; set; } = new();
    }

    /// <summary>POI entry in a tour response, including display info.</summary>
    public class TourPoiResponseDto
    {
        public int PoiId { get; set; }
        public int OrderIndex { get; set; }
        public string PoiName { get; set; } = string.Empty;
        public string? PoiImageUrl { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    /// <summary>Lightweight POI summary for the Tour Builder POI pool.</summary>
    public class PoiSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Status { get; set; } = string.Empty;
        public int NarrationCount { get; set; }
        public int QrScanCount { get; set; }
        /// <summary>Popularity score = NarrationCount + QrScanCount.</summary>
        public int PopularityScore => NarrationCount + QrScanCount;
    }

    /// <summary>Request to log a tour usage event from a mobile or web client.</summary>
    public class TourUsageLogRequest
    {
        public string? DeviceId { get; set; }
    }

    /// <summary>
    /// Request to get a suggested route order for a given list of POI IDs.
    /// Returned list is sorted by nearest-neighbour starting from the most popular POI.
    /// </summary>
    public class SuggestRouteRequest
    {
        public List<int> PoiIds { get; set; } = new();
    }

    /// <summary>A single entry in the suggested ordered route.</summary>
    public class SuggestRouteItemDto
    {
        public int PoiId { get; set; }
        public int OrderIndex { get; set; }
        public string PoiName { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int PopularityScore { get; set; }
    }
}
