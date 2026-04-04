using VinhKhanhFoodTour.AdminPortal.Services.Http;
using Microsoft.Extensions.Logging;

namespace VinhKhanhFoodTour.AdminPortal.Services.Admin;

public class SyncService : ISyncService
{
    private readonly ApiClient _apiClient;
    private readonly ILogger<SyncService> _logger;

    public SyncService(ApiClient apiClient, ILogger<SyncService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<SyncStatusDto> GetSyncStatusAsync()
    {
        try
        {
            var result = await _apiClient.GetAsync<SyncStatusDto>("api/v1/Sync/admin/sync/status");
            return result ?? new SyncStatusDto 
            { 
                HasSuccess = false, 
                IsRunning = false,
                LastError = "Unable to fetch sync status"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching sync status: {ex.Message}");
            return new SyncStatusDto 
            { 
                HasSuccess = false, 
                IsRunning = false,
                LastError = $"Error: {ex.Message}"
            };
        }
    }
}
