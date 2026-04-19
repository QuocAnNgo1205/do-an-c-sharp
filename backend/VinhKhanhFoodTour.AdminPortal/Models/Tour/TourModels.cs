namespace VinhKhanhFoodTour.AdminPortal.Models.Tour;

public class TourDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal EstimatedPrice { get; set; }
    public string? ThumbnailUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public int UsageCount { get; set; }
    public List<TourPoiResponseDto> Pois { get; set; } = new();
}

public class TourPoiResponseDto
{
    public int PoiId { get; set; }
    public int OrderIndex { get; set; }
    public string PoiName { get; set; } = string.Empty;
    public string? PoiImageUrl { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

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
    public int PopularityScore => NarrationCount + QrScanCount;
}

public class TourCreateRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal EstimatedPrice { get; set; }
    public string? ThumbnailUrl { get; set; }
    public List<TourPoiItemRequest> Pois { get; set; } = new();
}

public class TourUpdateRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public decimal? EstimatedPrice { get; set; }
    public string? ThumbnailUrl { get; set; }
    public List<TourPoiItemRequest> Pois { get; set; } = new();
}

public class TourPoiItemRequest
{
    public int PoiId { get; set; }
    public int OrderIndex { get; set; }
}

/// <summary>Represents a POI being built into a tour route in the UI.</summary>
public class TourRouteItem
{
    public int PoiId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int OrderIndex { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int PopularityScore { get; set; }
}

/// <summary>Result from the suggest-route API.</summary>
public class SuggestRouteItem
{
    public int PoiId { get; set; }
    public int OrderIndex { get; set; }
    public string PoiName { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int PopularityScore { get; set; }
}

public class SuggestRouteRequest
{
    public List<int> PoiIds { get; set; } = new();
}
