using VinhKhanhFoodTour.AdminPortal.Services.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using VinhKhanhFoodTour.AdminPortal.Models.Poi;
using System.Globalization;

namespace VinhKhanhFoodTour.AdminPortal.Services.Owner;

public class PoiService : IPoiService
{
    private readonly ApiClient _apiClient;
    private readonly ILogger<PoiService> _logger;

    public PoiService(ApiClient apiClient, ILogger<PoiService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<List<PoiDto>> GetOwnerPoisAsync()
    {
        try
        {
            var result = await _apiClient.GetAsync<List<PoiDto>>("api/v1/Poi/owner/my-pois");
            return result ?? new List<PoiDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching owner POIs: {ex.Message}");
            throw;
        }
    }

    public async Task<CreatePoiResponseDto> CreatePoiAsync(CreatePoiDto dto)
    {
        try
        {
            // 🔴 MỚI: Gửi dữ liệu dưới dạng JSON (Admin portal dùng JSON cho bước 1)
            // Gửi tới base endpoint api/v1/Poi (mặc định [FromBody] CreatePoiRequest)
            var result = await _apiClient.PostAsync<CreatePoiResponseDto>("api/v1/Poi", dto);
            return result ?? new CreatePoiResponseDto { Message = "POI created successfully" };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating POI: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Phương thức mới: Tạo POI với image trong một lần gọi
    /// </summary>
    public async Task<CreatePoiResponseDto> CreatePoiWithImageAsync(CreatePoiDto dto, Stream? imageStream, string? imageName)
    {
        try
        {
            // 🟢 FIX: Use correct field names (matching CreatePoiFormRequest) and handle null values
            var content = new MultipartFormDataContent();
            
            // Add form fields with matching property names from backend (case-sensitive)
            // 🌐 FIX: Dùng CultureInfo.InvariantCulture để đảm bảo số thực dùng dấu chấm (.), không dùng dấu phẩy (,)
            content.Add(new StringContent(dto.Name ?? string.Empty), "name");
            content.Add(new StringContent(dto.Latitude.ToString(CultureInfo.InvariantCulture)), "latitude");
            content.Add(new StringContent(dto.Longitude.ToString(CultureInfo.InvariantCulture)), "longitude");
            content.Add(new StringContent(dto.Title ?? string.Empty), "title");
            content.Add(new StringContent(dto.Description ?? string.Empty), "description");

            // Add image file if provided
            if (imageStream != null && !string.IsNullOrEmpty(imageName))
            {
                _logger.LogInformation($"[CreatePoiWithImageAsync] Adding image: {imageName} ({imageStream.Length} bytes)");
                content.Add(new StreamContent(imageStream), "imageFile", imageName);
            }
            else
            {
                _logger.LogWarning("[CreatePoiWithImageAsync] No image stream provided");
            }

            _logger.LogInformation($"[CreatePoiWithImageAsync] Posting to api/v1/Poi/owner/create with multipart/form-data");
            var result = await _apiClient.PostFormAsync<CreatePoiResponseDto>("api/v1/Poi/owner/create", content);
            return result ?? new CreatePoiResponseDto { Message = "POI created successfully" };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating POI with image: {ex.Message}");
            throw;
        }
    }

    public async Task<MediaUploadResponseDto> UploadMediaAsync(int poiId, string languageCode, MultipartFormDataContent content)
    {
        try
        {
            var result = await _apiClient.PostFormAsync<MediaUploadResponseDto>(
                $"api/v1/Poi/owner/{poiId}/translations/{languageCode}/media", content);
            return result ?? new MediaUploadResponseDto { Message = "Media uploaded successfully" };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error uploading media for POI {poiId}: {ex.Message}");
            throw;
        }
    }

    public async Task<List<PoiListenStatsDto>> GetListenStatsAsync(string mode = "listen")
    {
        try
        {
            var result = await _apiClient.GetAsync<List<PoiListenStatsDto>>($"api/v1/Sync/owner/stats/listens?mode={mode}");
            return result ?? new List<PoiListenStatsDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching listen stats: {ex.Message}");
            throw;
        }
    }

    public async Task<List<ListenTrendDto>> GetListenTrendAsync(string type, string mode = "listen")
    {
        try
        {
            var result = await _apiClient.GetAsync<List<ListenTrendDto>>($"api/v1/Sync/owner/stats/listens/trend?type={type}&mode={mode}");
            return result ?? new List<ListenTrendDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching listen trend: {ex.Message}");
            return new List<ListenTrendDto>();
        }
    }

    public async Task<PoiDetailDto?> GetPoiByIdAsync(int id)
    {
        try
        {
            var result = await _apiClient.GetAsync<PoiDetailDto>($"api/v1/Poi/public/{id}");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching POI details for {id}: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> UpdatePoiWithImageAsync(int id, CreatePoiDto dto, Stream? imageStream, string? imageName)
    {
        try
        {
            var content = new MultipartFormDataContent();
            content.Add(new StringContent(dto.Name ?? string.Empty), "name");
            content.Add(new StringContent(dto.Latitude.ToString(CultureInfo.InvariantCulture)), "latitude");
            content.Add(new StringContent(dto.Longitude.ToString(CultureInfo.InvariantCulture)), "longitude");
            content.Add(new StringContent(dto.Title ?? string.Empty), "title");
            content.Add(new StringContent(dto.Description ?? string.Empty), "description");

            if (imageStream != null && !string.IsNullOrEmpty(imageName))
            {
                content.Add(new StreamContent(imageStream), "imageFile", imageName);
            }

            await _apiClient.PutFormAsync<object>($"api/v1/Poi/owner/{id}", content);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating POI {id}: {ex.Message}");
            return false;
        }
    }

    public async Task<List<OverviewMapPinDto>> GetOverviewMapPinsAsync()
    {
        try
        {
            var result = await _apiClient.GetAsync<List<OverviewMapPinDto>>("api/v1/Poi/overview-pins");
            return result ?? new List<OverviewMapPinDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching overview map pins: {ex.Message}");
            return new List<OverviewMapPinDto>();
        }
    }

    public async Task<bool> DeletePoiAsync(int id)
    {
        try
        {
            await _apiClient.DeleteAsync<object>($"api/v1/Poi/{id}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error deleting POI {id}: {ex.Message}");
            return false;
        }
    }

    public async Task<PoiDto?> GetOwnerPoiByIdAsync(int id)
    {
        try
        {
            return await _apiClient.GetAsync<PoiDto>($"api/v1/Poi/owner/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching owner POI {id}: {ex.Message}");
            return null;
        }
    }

    public async Task<List<QrManageStatDto>> GetOwnerQrStatsAsync()
    {
        try
        {
            var result = await _apiClient.GetAsync<List<QrManageStatDto>>($"api/v1/Poi/owner/qr-manage");
            return result ?? new List<QrManageStatDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching QR stats: {ex.Message}");
            return new List<QrManageStatDto>();
        }
    }

    public async Task<List<QrManageStatDto>> GetAdminQrStatsAsync()
    {
        try
        {
            var result = await _apiClient.GetAsync<List<QrManageStatDto>>($"api/v1/Poi/admin/qr-manage");
            return result ?? new List<QrManageStatDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching system QR stats: {ex.Message}");
            return new List<QrManageStatDto>();
        }
    }

    public async Task<List<HeatmapPointDto>> GetUserHeatmapPointsAsync()
    {
        try
        {
            var result = await _apiClient.GetAsync<List<HeatmapPointDto>>("api/v1/Poi/user-heatmap");
            return result ?? new List<HeatmapPointDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching user heatmap points: {ex.Message}");
            return new List<HeatmapPointDto>();
        }
    }

    public async Task<bool> SeedUserLocationsAsync()
    {
        try
        {
            await _apiClient.PostAsync<object>("api/v1/Poi/seed-user-locations", new { });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error seeding user locations: {ex.Message}");
            return false;
        }
    }
}
