using System;
using VinhKhanhFoodTour.App.Services;

namespace VinhKhanhFoodTour.App.Services
{
    public class MockLocationService : ILocationTrackingService
    {
        public event EventHandler<LocationEventArgs>? LocationUpdated;
        private bool _isTracking;
        public bool IsTracking => _isTracking;

        public void StartTracking()
        {
            _isTracking = true;
            // No-op for mock
        }

        public void StopTracking()
        {
            _isTracking = false;
        }
    }
}
