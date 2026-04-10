using System;
using System.Threading;
using VinhKhanhFoodTour.App.Services;

namespace VinhKhanhFoodTour.App.Services
{
    public class MockLocationService : ILocationTrackingService
    {
        public event EventHandler<LocationEventArgs>? LocationUpdated;
        private bool _isTracking;
        private Timer? _timer;
        
        // Bắt đầu đi bộ từ đầu đường Vĩnh Khánh
        private double _currentLat = 10.760446;
        private double _currentLon = 106.700140;

        public bool IsTracking => _isTracking;

        public void StartTracking()
        {
            _isTracking = true;
            
            // Giả lập đi dọc theo đường Vĩnh Khánh mỗi 3 giây
            _timer = new Timer(SimulateLocation, null, 0, 3000);
        }

        private void SimulateLocation(object? state)
        {
            if (!_isTracking) return;

            // Di chuyển 1 chút về phía nam/đông nam (Dọc đường Vĩnh Khánh)
            _currentLat -= 0.00010;
            _currentLon += 0.00005;

            LocationUpdated?.Invoke(this, new LocationEventArgs
            {
                Latitude = _currentLat,
                Longitude = _currentLon
            });
        }

        public void StopTracking()
        {
            _isTracking = false;
            _timer?.Dispose();
            _timer = null;
        }
    }
}
