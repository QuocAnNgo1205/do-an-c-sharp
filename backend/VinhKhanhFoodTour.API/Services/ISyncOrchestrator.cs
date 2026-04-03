namespace VinhKhanhFoodTour.API.Services
{
    public interface ISyncOrchestrator
    {
        Task TryRefreshOfflinePackAsync();
    }
}
