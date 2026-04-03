namespace VinhKhanhFoodTour.API.Services
{
    public class SyncOrchestrator : ISyncOrchestrator
    {
        private readonly ISyncService _syncService;
        private readonly ILogger<SyncOrchestrator> _logger;

        public SyncOrchestrator(ISyncService syncService, ILogger<SyncOrchestrator> logger)
        {
            _syncService = syncService;
            _logger = logger;
        }

        public async Task TryRefreshOfflinePackAsync()
        {
            try
            {
                await _syncService.GenerateOfflinePackAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to regenerate offline pack.");
            }
        }
    }
}
