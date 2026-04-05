using Android.Content;
using System.Diagnostics;
using VinhKhanhFoodTour.App.Services;

namespace VinhKhanhFoodTour.App.Platforms.Android
{
    public class AndroidLocationTrackingService : ILocationTrackingService
    {
        public static AndroidLocationTrackingService? Current { get; private set; }
        
        public event EventHandler<LocationEventArgs>? LocationUpdated;
        private bool _isTracking;
        public bool IsTracking => _isTracking;

        public AndroidLocationTrackingService()
        {
            Current = this;
        }

        public void StartTracking()
        {
            if (_isTracking) return;
            
            var intent = new Intent(Platform.CurrentActivity, typeof(LocationTrackingService));
            if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O)
            {
                Platform.CurrentActivity?.StartForegroundService(intent);
            }
            else
            {
                Platform.CurrentActivity?.StartService(intent);
            }
            
            _isTracking = true;
            Debug.WriteLine("[AndroidWrapper] Foreground Service Started.");
        }

        public void StopTracking()
        {
            if (!_isTracking) return;

            var intent = new Intent(Platform.CurrentActivity, typeof(LocationTrackingService));
            Platform.CurrentActivity?.StopService(intent);
            
            _isTracking = false;
            Debug.WriteLine("[AndroidWrapper] Foreground Service Stopped.");
        }

        public void RaiseLocationUpdated(LocationEventArgs e)
        {
            LocationUpdated?.Invoke(this, e);
        }
    }
}
