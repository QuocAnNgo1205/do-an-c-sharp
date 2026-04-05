using VinhKhanhFoodTour.AdminPortal.Models.Poi;

namespace VinhKhanhFoodTour.AdminPortal.Services.Owner;

public interface IPoiService
{
    /// <summary>
    /// Retrieve the list of POIs owned by the current user
    /// </summary>
    Task<List<PoiDto>> GetOwnerPoisAsync();

    /// <summary>
    /// Create a new POI for the current owner
    /// </summary>
    Task<CreatePoiResponseDto> CreatePoiAsync(CreatePoiDto dto);

    /// <summary>
    /// Create a new POI with image upload in one request
    /// </summary>
    Task<CreatePoiResponseDto> CreatePoiWithImageAsync(CreatePoiDto dto, Stream? imageStream, string? imageName);

    /// <summary>
    /// Upload media (audio/image) for a POI translation
    /// </summary>
    Task<MediaUploadResponseDto> UploadMediaAsync(int poiId, string languageCode, MultipartFormDataContent content);

    /// <summary>
    /// Get listen statistics for owner's POIs
    /// </summary>
    Task<List<PoiListenStatsDto>> GetListenStatsAsync();

    /// <summary>
    /// Get public POI details by ID
    /// </summary>
    Task<PoiDetailDto?> GetPoiByIdAsync(int id);

    /// <summary>
    /// Update an existing POI with optional image upload
    /// </summary>
    Task<bool> UpdatePoiWithImageAsync(int id, CreatePoiDto dto, Stream? imageStream, string? imageName);

    /// <summary>
    /// Delete a POI and its associated data
    /// </summary>
    Task<bool> DeletePoiAsync(int id);

    /// <summary>
    /// Get POI by ID with owner status (not filtered by Approved)
    /// </summary>
    Task<PoiDto?> GetOwnerPoiByIdAsync(int id);
}

public class PoiDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int TriggerRadius { get; set; }
    public int Status { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? ImageUrl { get; set; }
    public List<PoiTranslationDto> Translations { get; set; } = [];
}

public class PoiTranslationDto
{
    public long Id { get; set; }
    public string Language { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? NarrationUrl { get; set; }
}

public class CreatePoiResponseDto
{
    public string Message { get; set; } = string.Empty;
    public int Id { get; set; }
}

public class MediaUploadResponseDto
{
    public string Message { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? AudioFilePath { get; set; }
}

public class PoiListenStatsDto
{
    public int PoiId { get; set; }
    public string PoiName { get; set; } = string.Empty;
    public int ListenCount { get; set; }
}

public class CreatePoiDto
{
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
