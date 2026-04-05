using System.Diagnostics;
using VinhKhanhFoodTour.App.Models;

namespace VinhKhanhFoodTour.App.Services
{
    /// <summary>
    /// Bộ não xử lý Geofencing (Vùng địa lý ảo)
    /// Tự động tính toán khoảng cách và kích hoạt audio guide khi ở gần POI.
    /// </summary>
    public class GeofenceManager
    {
        private readonly ILocationTrackingService _locationService;
        private readonly ApiService _apiService;
        private readonly AudioGuideService _audioService;
        
        // Cấu hình Geofence
        private const double TRIGGER_RADIUS_METERS = 30; 
        private const int COOLDOWN_MINUTES = 30;

        // Lưu vết thời gian phát audio cuối cùng cho mỗi POI để tránh spam
        private readonly Dictionary<int, DateTime> _lastPlayedPois = new();
        private List<Poi>? _allPois;

        public GeofenceManager(
            ILocationTrackingService locationService, 
            ApiService apiService, 
            AudioGuideService audioService)
        {
            _locationService = locationService;
            _apiService = apiService;
            _audioService = audioService;

            // Đăng ký nhận tọa độ từ Service chạy ngầm
            _locationService.LocationUpdated += OnLocationUpdated;
        }

        private async void OnLocationUpdated(object? sender, LocationEventArgs e)
        {
            try 
            {
                // 1. Nếu chưa có danh sách POI, nạp từ API
                if (_allPois == null || _allPois.Count == 0)
                {
                    _allPois = await _apiService.GetPoisAsync();
                    if (_allPois == null) return;
                }

                var userLocation = new Location(e.Latitude, e.Longitude);

                // 2. Quét qua tất cả quán ăn để kiểm tra khoảng cách
                foreach (var poi in _allPois)
                {
                    if (poi.Latitude == 0 || poi.Longitude == 0) continue;

                    var poiLocation = new Location(poi.Latitude, poi.Longitude);
                    double distanceKm = userLocation.CalculateDistance(poiLocation, DistanceUnits.Kilometers);
                    double distanceMeters = distanceKm * 1000;

                    // 3. Nếu khoảng cách < 30m
                    if (distanceMeters <= TRIGGER_RADIUS_METERS)
                    {
                        Debug.WriteLine($"[GEOFENCE TRIGGERED] Khoảng cách: {distanceMeters:F1}m đến {poi.Name}. Đang gọi Audio...");
                        
                        // 4. TẠM TẮT Cool-down (30 phút qua đã phát chưa?) ĐỂ TEST
                        // if (CanPlayAudio(poi.Id))
                        if (true) // Bỏ qua cooldown để phát lặp lại khi ở gần
                        {
                            Debug.WriteLine($"[Geofence] 🎯 USER IS NEAR: {poi.Name} ({distanceMeters:F1}m). Triggering Audio...");
                            
                            // Phát Audio Guide (Hàm này đã tự xử lý Fallback & Language)
                            // Sử dụng MainThread để gọi AudioService
                            MainThread.BeginInvokeOnMainThread(async () => 
                            {
                                try 
                                {
                                    // Lấy chi tiết quán (để có bản dịch đầy đủ) - Tắt Alert khi chạy ngầm
                                    var detail = await _apiService.GetPoiDetailAsync(poi.Id, showErrorAlert: false);
                                    if (detail != null)
                                    {
                                        Debug.WriteLine($"[Geofence] 🎯 TRIGGERING AUDIO FOR: {detail.Name}");
                                        await _audioService.PlayAudioAsync(detail, showErrors: false);
                                    }
                                    else 
                                    {
                                        Debug.WriteLine($"[Geofence ERROR] Could not fetch detail for POI ID: {poi.Id}");
                                        ShowToast($"Không thể tải dữ liệu cho: {poi.Name}");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"[Geofence THREAD ERROR] {ex.Message}");
                                }
                            });

                            // Lưu mốc thời gian đã phát
                            _lastPlayedPois[poi.Id] = DateTime.UtcNow;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Geofence ERROR] {ex.Message}");
            }
        }

        private bool CanPlayAudio(int poiId)
        {
            if (!_lastPlayedPois.TryGetValue(poiId, out var lastTime))
            {
                return true; // Chưa phát bao giờ
            }

            // Đã phát, kiểm tra thời gian trôi qua
            return (DateTime.UtcNow - lastTime).TotalMinutes >= COOLDOWN_MINUTES;
        }

        public void Start() => _locationService.StartTracking();
        public void Stop() 
        {
            _locationService.StopTracking();
            _ = _audioService.StopAudioAsync();
        }

        private void ShowToast(string message)
        {
            MainThread.BeginInvokeOnMainThread(async () => 
            {
                var toast = CommunityToolkit.Maui.Alerts.Toast.Make(message);
                await toast.Show();
            });
        }
    }
}
