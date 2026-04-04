using VinhKhanhFoodTour.AdminPortal.Services.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

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
            var result = await _apiClient.PostAsync<CreatePoiResponseDto>("api/v1/Poi/owner/create", dto);
            return result ?? new CreatePoiResponseDto { Message = "POI created successfully" };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating POI: {ex.Message}");
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

    public async Task<List<PoiListenStatsDto>> GetListenStatsAsync()
    {
        try
        {
            var result = await _apiClient.GetAsync<List<PoiListenStatsDto>>("api/v1/Sync/owner/stats/listens");
            return result ?? new List<PoiListenStatsDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching listen stats: {ex.Message}");
            throw;
        }
    }
}
