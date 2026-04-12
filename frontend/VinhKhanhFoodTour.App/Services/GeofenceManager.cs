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
        private readonly PoiCacheService _poiCache;
        
        // Cấu hình Geofence
        private const double TRIGGER_RADIUS_METERS = 30; 
        private const int COOLDOWN_MINUTES = 30;

        // Lưu vết thời gian phát audio cuối cùng cho mỗi POI để tránh spam
        private readonly Dictionary<int, DateTime> _lastPlayedPois = new();
        private List<Poi>? _allPois;

        public GeofenceManager(
            ILocationTrackingService locationService, 
            ApiService apiService, 
            AudioGuideService audioService,
            PoiCacheService poiCache)
        {
            _locationService = locationService;
            _apiService = apiService;
            _audioService = audioService;
            _poiCache = poiCache;

            // Đăng ký nhận tọa độ từ Service chạy ngầm
            _locationService.LocationUpdated += OnLocationUpdated;
        }

        private async void OnLocationUpdated(object? sender, LocationEventArgs e)
        {
            try 
            {
                // 1. Nếu chưa có danh sách POI, nạp từ SQLite Cache (Offline-ready)
                if (_allPois == null || _allPois.Count == 0)
                {
                    _allPois = await _poiCache.GetCachedPoisAsync();
                    
                    // Fallback nếu Cache trống
                    if (_allPois == null || _allPois.Count == 0)
                    {
                        var onlinePois = await _apiService.GetPoisAsync();
                        if (onlinePois != null && onlinePois.Count > 0)
                        {
                            _allPois = onlinePois;
                            _ = _poiCache.SavePoisAsync(_allPois);
                        }
                        else return;
                    }
                }

                // NẾU ĐANG PHÁT AUDIO THÌ KHÔNG LÀM PHIỀN
                if (_audioService.IsPlaying)
                {
                    return;
                }

                var userLocation = new Location(e.Latitude, e.Longitude);
                
                Poi? closestPoi = null;
                double minDistance = double.MaxValue;

                // 2. Quét qua tất cả quán ăn để tìm quán GẦN NHẤT trong bán kính
                foreach (var poi in _allPois)
                {
                    if (poi.Latitude == 0 || poi.Longitude == 0) continue;

                    var poiLocation = new Location(poi.Latitude, poi.Longitude);
                    double distanceKm = userLocation.CalculateDistance(poiLocation, DistanceUnits.Kilometers);
                    double distanceMeters = distanceKm * 1000;

                    // 3. Nếu khoảng cách < 30m và có thể phát (kiểm tra Cooldown)
                    if (distanceMeters <= TRIGGER_RADIUS_METERS && CanPlayAudio(poi.Id))
                    {
                        if (distanceMeters < minDistance)
                        {
                            minDistance = distanceMeters;
                            closestPoi = poi;
                        }
                    }
                }

                // 4. Phát cho quán gần nhất tìm được
                if (closestPoi != null)
                {
                    Debug.WriteLine($"[Geofence] 🎯 USER IS NEAR CLOSEST POI: {closestPoi.Name} ({minDistance:F1}m). Triggering Audio...");
                    
                    // Ghi sổ ngay lập tức để block các call tiếp theo
                    _lastPlayedPois[closestPoi.Id] = DateTime.UtcNow;

                    if (closestPoi.Translations != null && closestPoi.Translations.Count > 0)
                    {
                        // Cache đã có Translations — phát ngay, không cần gọi API
                        Debug.WriteLine($"[Geofence] 🎯 TRIGGERING AUDIO FOR: {closestPoi.Name} (Offline Mode: OK)");
                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            try
                            {
                                await _audioService.PlayAudioAsync(closestPoi, showErrors: false);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"[Geofence THREAD ERROR] {ex.Message}");
                            }
                        });
                    }
                    else
                    {
                        // Fallback: gọi API từ BACKGROUND THREAD (đây là context hiện tại),
                        // sau đó mới vào Main Thread để phát audio — KHÔNG block UI.
                        try
                        {
                            var detail = await _apiService.GetPoiDetailAsync(closestPoi.Id, showErrorAlert: false);
                            if (detail != null)
                            {
                                Debug.WriteLine($"[Geofence] 🎯 TRIGGERING AUDIO FOR: {detail.Name} (Fallback API)");
                                MainThread.BeginInvokeOnMainThread(async () =>
                                {
                                    try
                                    {
                                        await _audioService.PlayAudioAsync(detail, showErrors: false);
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"[Geofence THREAD ERROR] {ex.Message}");
                                    }
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[Geofence API Fallback ERROR] {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Geofence ERROR] {ex.Message}");
            }
        }

        public void RegisterManualPlay(int poiId)
        {
            _lastPlayedPois[poiId] = DateTime.UtcNow;
            Debug.WriteLine($"[Geofence] 📝 Registered manual play for POI {poiId}. Cooldown started.");
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
