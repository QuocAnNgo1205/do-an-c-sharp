using CoreLocation;
using System.Diagnostics;
using VinhKhanhFoodTour.App.Services;

namespace VinhKhanhFoodTour.App.Platforms.iOS
{
    public class iOSLocationTrackingService : ILocationTrackingService
    {
        private CLLocationManager? _locationManager;
        public event EventHandler<LocationEventArgs>? LocationUpdated;
        private bool _isTracking;
        public bool IsTracking => _isTracking;

        public void StartTracking()
        {
            if (_isTracking) return;

            _locationManager = new CLLocationManager();
            _locationManager.DesiredAccuracy = CLLocation.AccuracyBest;
            _locationManager.DistanceFilter = 10; // Cập nhật sau 10m di chuyển

            // 🛑 Cấu hình chạy ngầm (Background)
            _locationManager.AllowsBackgroundLocationUpdates = true;
            _locationManager.PausesLocationUpdatesAutomatically = false;
            
            // Hiện icon location ở Status bar khi chạy ngầm
            _locationManager.ShowsBackgroundLocationIndicator = true;

            _locationManager.LocationsUpdated += (s, e) =>
            {
                var location = e.Locations[e.Locations.Length - 1];
                RaiseLocationUpdated(new LocationEventArgs
                {
                    Latitude = location.Coordinate.Latitude,
                    Longitude = location.Coordinate.Longitude
                });
            };

            _locationManager.StartUpdatingLocation();
            _isTracking = true;
            Debug.WriteLine("[iOSWrapper] Background Location Tracking Started.");
        }

        public void StopTracking()
        {
            if (!_isTracking || _locationManager == null) return;

            _locationManager.StopUpdatingLocation();
            _isTracking = false;
            Debug.WriteLine("[iOSWrapper] Background Location Tracking Stopped.");
        }

        private void RaiseLocationUpdated(LocationEventArgs e)
        {
            LocationUpdated?.Invoke(this, e);
        }
    }
}
