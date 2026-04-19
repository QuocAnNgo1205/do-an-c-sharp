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
        
        // BUG FIX: Bắt đầu cách Ốc Oanh (10.760193, 106.702081) khoảng 25m về phía Tây
        // Di chuyển về hướng Đông để vào trong Geofence sau ~12 giây (4 bước × 3s)
        private double _currentLat = 10.760193;
        private double _currentLon = 106.701800; // ~25m về phía Tây của Ốc Oanh

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

            // BUG FIX: Di chuyển về phía Đông để tiếp cận Ốc Oanh (lon ~ 106.702081)
            // Mỗi 0.000020 độ kinh ≈ 2.2m, sau ~4 bước (12s) sẽ vào Geofence 30m
            _currentLon += 0.000020;

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
