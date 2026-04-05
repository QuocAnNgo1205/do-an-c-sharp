using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.App;
using Android.Gms.Location;
using System.Diagnostics;
using VinhKhanhFoodTour.App.Services;

namespace VinhKhanhFoodTour.App.Platforms.Android
{
    [Service(ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeLocation)]
    public class LocationTrackingService : Service
    {
        private IFusedLocationProviderClient? _fusedLocationProviderClient;
        private LocationCallback? _locationCallback;
        private const string NOTIFICATION_CHANNEL_ID = "location_tracking_channel";
        private const int NOTIFICATION_ID = 1001;

        public static ILocationTrackingService? Instance { get; set; }

        public override IBinder? OnBind(Intent? intent) => null;

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            System.Diagnostics.Debug.WriteLine("[AndroidService] Starting Foreground Service...");
            
            CreateNotificationChannel();
            var notification = CreateNotification();
            
            StartForeground(NOTIFICATION_ID, notification);
            
            StartLocationUpdates();

            return StartCommandResult.Sticky;
        }

        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(NOTIFICATION_CHANNEL_ID, "GPS Tracking", NotificationImportance.Low)
                {
                    Description = "Duy trì vị trí để tự động phát thuyết minh"
                };
                var notificationManager = (NotificationManager)GetSystemService(NotificationService)!;
                notificationManager.CreateNotificationChannel(channel);
            }
        }

        private Notification CreateNotification()
        {
            var intent = new Intent(this, typeof(MainActivity));
            var pendingIntent = PendingIntent.GetActivity(this, 0, intent, PendingIntentFlags.Immutable);

            return new NotificationCompat.Builder(this, NOTIFICATION_CHANNEL_ID)
                .SetContentTitle("📍 Đang bật Tự động thuyết minh")
                .SetContentText("Ứng dụng đang theo dõi vị trí để gợi ý quán ăn gần bạn.")
                .SetSmallIcon(global::Android.Resource.Drawable.IcMenuMyLocation)
                .SetOngoing(true)
                .SetContentIntent(pendingIntent)
                .Build();
        }

        private void StartLocationUpdates()
        {
            _fusedLocationProviderClient = LocationServices.GetFusedLocationProviderClient(this);
            
            var locationRequest = new LocationRequest.Builder(Priority.PriorityHighAccuracy, 5000) // 5 giây/lần
                .SetMinUpdateIntervalMillis(2000)
                .SetMinUpdateDistanceMeters(10) // 10 mét mới cập nhật
                .Build();

            _locationCallback = new TrackingLocationCallback();
            
            try 
            {
                _fusedLocationProviderClient.RequestLocationUpdates(locationRequest, _locationCallback, Looper.MainLooper);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AndroidService ERROR] Failed to start updates: {ex.Message}");
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (_fusedLocationProviderClient != null && _locationCallback != null)
            {
                _fusedLocationProviderClient.RemoveLocationUpdates(_locationCallback);
            }
            StopForeground(StopForegroundFlags.Remove);
            System.Diagnostics.Debug.WriteLine("[AndroidService] Stopped.");
        }
    }

    public class TrackingLocationCallback : LocationCallback
    {
        public override void OnLocationResult(LocationResult result)
        {
            base.OnLocationResult(result);
            if (result.LastLocation != null)
            {
                // Truyền tọa độ về Interface dùng chung
                var args = new global::VinhKhanhFoodTour.App.Services.LocationEventArgs 
                { 
                    Latitude = result.LastLocation.Latitude, 
                    Longitude = result.LastLocation.Longitude 
                };
                
                // Trigger event trong Singleton (AndroidLocationTrackingService implementation)
                // Chúng ta sẽ cần một class implementation cho interface ILocationTrackingService
                if (AndroidLocationTrackingService.Current != null)
                {
                    AndroidLocationTrackingService.Current.RaiseLocationUpdated(args);
                }
            }
        }
    }
}
