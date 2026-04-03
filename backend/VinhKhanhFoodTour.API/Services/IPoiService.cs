using VinhKhanhFoodTour.DTOs;
using VinhKhanhFoodTour.Models;

namespace VinhKhanhFoodTour.API.Services
{
    public interface IPoiService
    {
        Task<Poi> CreatePoiAsync(int ownerId, CreatePoiRequest request);
        Task<List<PoiDto>> GetPendingPoisAsync();
        Task<Poi> ApprovePoiAsync(int id);
        Task<Poi> RejectPoiAsync(int id, string reason);
        Task<Poi> CreateOwnerPoiAsync(int ownerId, CreatePoiDto request);
        Task<Poi> UpdatePoiAsync(int id, int ownerId, UpdatePoiDto request);
        Task<List<PoiDto>> GetOwnerPoisAsync(int ownerId);
        Task<List<PoiDto>> GetPublicPoisAsync();
        Task<List<MapPinDto>> GetMapPinsAsync();
        Task<List<PoiDto>> GetNearbyPoisAsync(double userLat, double userLng, double radiusInMeters);
        Task<(Poi poi, PoiTranslation translation)> GetOwnerTranslationAsync(int poiId, string languageCode, int ownerId);
        Task SaveTranslationAsync(PoiTranslation translation);
    }
}
