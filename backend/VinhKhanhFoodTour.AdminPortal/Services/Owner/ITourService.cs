using VinhKhanhFoodTour.AdminPortal.Models.Tour;

namespace VinhKhanhFoodTour.AdminPortal.Services.Owner;

public interface ITourService
{
    Task<List<PoiSummaryDto>> GetPoiPoolAsync(string sortBy = "popularity");
    Task<List<TourDto>> GetToursAsync();
    Task<TourDto?> GetTourByIdAsync(int id);
    Task<bool> CreateTourAsync(TourCreateRequest request);
    Task<bool> UpdateTourAsync(int id, TourUpdateRequest request);
    Task<bool> DeleteTourAsync(int id);
    Task<List<SuggestRouteItem>> GetSuggestedRouteAsync(List<int> poiIds);
}
