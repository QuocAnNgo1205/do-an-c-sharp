using VinhKhanhFoodTour.DTOs;

namespace VinhKhanhFoodTour.API.Services
{
    public interface ISyncService
    {
        Task GenerateOfflinePackAsync();
        Task<OfflinePackManifestDto?> GetManifestAsync();
        Task<SyncStatusDto> GetStatusAsync();
        string GetOfflinePackPath();
    }
}
