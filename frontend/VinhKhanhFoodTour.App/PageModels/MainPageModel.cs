using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VinhKhanhFoodTour.App.Models;
using VinhKhanhFoodTour.App.Services;

namespace VinhKhanhFoodTour.App.PageModels
{
    public partial class MainPageModel : ObservableObject
    {
        private readonly ApiService _apiService;
        private readonly AudioGuideService _audioGuideService;
        private readonly GeofenceManager _geofenceManager;
        private readonly PoiCacheService _poiCache;

        [ObservableProperty]
        private ObservableCollection<Poi> restaurants = new();

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private bool isRefreshing;
        [ObservableProperty] private string searchText = string.Empty;

        // 📊 MỚI: Dữ liệu cho biểu đồ thống kê quán ăn
        [ObservableProperty] private ObservableCollection<PoiCategory> categoryData = new();
        [ObservableProperty] private List<Brush> categoryColors = new();

        public MainPageModel(ApiService apiService, AudioGuideService audioGuideService, GeofenceManager geofenceManager, PoiCacheService poiCache)
        {
            _apiService = apiService;
            _audioGuideService = audioGuideService;
            _geofenceManager = geofenceManager;
            _poiCache = poiCache;

            // 🛑 MỚI: Đồng bộ trạng thái UI tĩnh mượt mà bất kể ai gọi (Kể cả Geofence)
            _audioGuideService.PlaybackStateChanged += (s, e) => 
            {
                var poi = Restaurants.FirstOrDefault(p => p.Id == e.PoiId);
                if (poi != null)
                {
                    MainThread.BeginInvokeOnMainThread(() => 
                    {
                        poi.IsPlaying = e.IsPlaying;
                        if (!e.IsPlaying) poi.IsLoadingAudio = false;
                    });
                }
            };
        }

        private async Task LoadDataAsync(string? query = null)
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                // ⚡ BƯỚC 1: Đọc từ SQLite Cache ngay lập tức (không cần mạng)
                var cached = await _poiCache.GetCachedPoisAsync();
                if (cached.Count > 0 && string.IsNullOrWhiteSpace(query))
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Restaurants.Clear();
                        foreach (var item in cached) Restaurants.Add(item);
                        UpdateChartData();
                    });
                    _ = UpdateDistancesAsync();
                }

                // 🌐 BƯỚC 2: Gọi API để lấy dữ liệu mới nhất (chạy ngầm)
                IsRefreshing = true;
                var data = await _apiService.GetPoisAsync(showErrorAlert: cached.Count == 0);
                if (data != null)
                {
                    // Lưu vào cache để lần sau không cần chờ
                    _ = _poiCache.SavePoisAsync(data);

                    if (!string.IsNullOrWhiteSpace(query))
                    {
                        data = data.Where(p =>
                            (p.Name != null && p.Name.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                            (p.Title != null && p.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
                        ).ToList();
                    }

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Restaurants.Clear();
                        foreach (var item in data) Restaurants.Add(item);
                        UpdateChartData();
                    });
                    _ = UpdateDistancesAsync();
                }
            }
            catch (Exception) { }
            finally
            {
                IsRefreshing = false;
                IsBusy = false;
            }
        }

        private void UpdateChartData()
        {
            if (Restaurants == null || Restaurants.Count == 0) return;

            // Ví dụ phân loại theo Title (hoặc bạn có thể dùng Grouping thực tế)
            var groups = Restaurants
                .GroupBy(r => (r.Title?.Contains("Cafe") ?? false) ? "Cà phê" :
                             (r.Title?.Contains("Hải sản") ?? false) ? "Hải sản" :
                             (r.Title?.Contains("Ốc") ?? false) ? "Ốc & Ăn vặt" : "Khác")
                .Select(g => new PoiCategory { Title = g.Key, Count = g.Count() })
                .ToList();

            CategoryData = new ObservableCollection<PoiCategory>(groups);

            // Bảng màu rực rỡ
            CategoryColors = new List<Brush>
            {
                new SolidColorBrush(Color.FromArgb("#FF5722")),
                new SolidColorBrush(Color.FromArgb("#FFC107")),
                new SolidColorBrush(Color.FromArgb("#2E7D32")),
                new SolidColorBrush(Color.FromArgb("#2196F3"))
            };
        }

        private async Task UpdateDistancesAsync()
        {
            try
            {
                var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium));
                if (location == null) return;

                foreach (var poi in Restaurants)
                {
                    var poiLocation = new Location(poi.Latitude, poi.Longitude);
                    double distance = Location.CalculateDistance(location, poiLocation, DistanceUnits.Kilometers);

                    poi.DistanceDisplay = distance < 1 ? $"{(distance * 1000):F0} m" : $"{distance:F1} km";
                }
            }
            catch { }
        }

        // --- TÍNH NĂNG MỚI: THUYẾT MINH ĐỒNG BỘ ---
        [RelayCommand(AllowConcurrentExecutions = true)]
        private async Task Speak(Poi poi)
        {
            if (poi == null) return;

            // Nếu NHẤN KHI ĐANG PHÁT, ta Dừng
            if (poi.IsPlaying)
            {
                await StopSpeak();
                return;
            }

            // Nếu đang phát âm thanh khác thì Dừng tất cả cái cũ trc khi mở cái mới
            await StopSpeak();

            try
            {
                poi.IsLoadingAudio = true;

                // 🛑 MỚI: Kiểm tra nếu chưa có bản dịch (Translations rỗng), tải chi tiết từ API
                if (poi.Translations == null || poi.Translations.Count == 0)
                {
                    var fullPoi = await _apiService.GetPoiDetailAsync(poi.Id);
                    if (fullPoi?.Translations != null)
                    {
                        poi.Translations = fullPoi.Translations;
                    }
                }
                
                // Đồng bộ bộ đếm với tính năng AutoPlay
                _geofenceManager.RegisterManualPlay(poi.Id);
                
                await _audioGuideService.PlayAudioAsync(poi);
            }
            catch (Exception)
            {
                // Lỗi đã được Service hiển thị Alert
            }
            finally
            {
                // IsLoadingAudio false should be handled by event or fallback here
                if (!poi.IsPlaying) poi.IsLoadingAudio = false;
            }
        }

        [RelayCommand]
        private async Task StopSpeak()
        {
            await _audioGuideService.StopAudioAsync();
        }

        // --- GIỮ NGUYÊN TOÀN BỘ CÁC LỆNH GỐC ---
        [RelayCommand]
        private async Task Appearing()
        {
            await LoadDataAsync();
        }

        [RelayCommand]
        private async Task Refresh() => await LoadDataAsync();

        [RelayCommand]
        private async Task Search() => await LoadDataAsync(SearchText);

        [RelayCommand]
        private async Task GoToDetail(Poi selectedPoi)
        {
            if (selectedPoi == null) return;
            await Shell.Current.GoToAsync("project", new Dictionary<string, object> { { "Poi", selectedPoi } });
        }

        [RelayCommand]
        private async Task GoToMap(Poi selectedPoi)
        {
            if (selectedPoi == null) return;
            await Shell.Current.GoToAsync($"///MapPage?Lat={selectedPoi.Latitude}&Lon={selectedPoi.Longitude}&Name={Uri.EscapeDataString(selectedPoi.Name)}");
        }
    }
}