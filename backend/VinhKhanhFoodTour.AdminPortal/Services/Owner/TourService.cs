using VinhKhanhFoodTour.AdminPortal.Services.Http;
using VinhKhanhFoodTour.AdminPortal.Models.Tour;
using Microsoft.Extensions.Logging;

namespace VinhKhanhFoodTour.AdminPortal.Services.Owner;

public class TourService : ITourService
{
    private readonly ApiClient _apiClient;
    private readonly ILogger<TourService> _logger;

    public TourService(ApiClient apiClient, ILogger<TourService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<List<PoiSummaryDto>> GetPoiPoolAsync(string sortBy = "popularity")
    {
        try
        {
            var result = await _apiClient.GetAsync<List<PoiSummaryDto>>(
                $"api/v1/Poi/builder-pool?sortBy={sortBy}");
            return result ?? new List<PoiSummaryDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError("Error fetching POI pool: {msg}", ex.Message);
            return new List<PoiSummaryDto>();
        }
    }

    public async Task<List<TourDto>> GetToursAsync()
    {
        try
        {
            var result = await _apiClient.GetAsync<List<TourDto>>("api/v1/Tour");
            return result ?? new List<TourDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError("Error fetching tours: {msg}", ex.Message);
            return new List<TourDto>();
        }
    }

    public async Task<TourDto?> GetTourByIdAsync(int id)
    {
        try
        {
            return await _apiClient.GetAsync<TourDto>($"api/v1/Tour/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error fetching tour {id}: {msg}", id, ex.Message);
            return null;
        }
    }

    public async Task<bool> CreateTourAsync(TourCreateRequest request)
    {
        try
        {
            await _apiClient.PostAsync<object>("api/v1/Tour", request);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error creating tour: {msg}", ex.Message);
            return false;
        }
    }

    public async Task<bool> UpdateTourAsync(int id, TourUpdateRequest request)
    {
        try
        {
            await _apiClient.PutAsync<object>($"api/v1/Tour/{id}", request);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error updating tour {id}: {msg}", id, ex.Message);
            return false;
        }
    }

    public async Task<bool> DeleteTourAsync(int id)
    {
        try
        {
            await _apiClient.DeleteAsync<object>($"api/v1/Tour/{id}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error deleting tour {id}: {msg}", id, ex.Message);
            return false;
        }
    }

    public async Task<List<SuggestRouteItem>> GetSuggestedRouteAsync(List<int> poiIds)
    {
        try
        {
            var result = await _apiClient.PostAsync<List<SuggestRouteItem>>(
                "api/v1/Tour/suggest-route",
                new SuggestRouteRequest { PoiIds = poiIds });
            return result ?? new List<SuggestRouteItem>();
        }
        catch (Exception ex)
        {
            _logger.LogError("Error fetching suggested route: {msg}", ex.Message);
            return new List<SuggestRouteItem>();
        }
    }
}
