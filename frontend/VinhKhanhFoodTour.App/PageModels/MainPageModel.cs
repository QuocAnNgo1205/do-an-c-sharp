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
        private bool _isGeofenceStarted = false; // Guard: tránh Start() bị gọi nhiều lần

        [ObservableProperty]
        private ObservableCollection<Poi> restaurants = new();

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private bool isRefreshing;
        [ObservableProperty] private string searchText = string.Empty;

        // 🗺️ MỚI: Dữ liệu cho Danh sách Tour
        [ObservableProperty] private ObservableCollection<Tour> toursList = new();

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
                        UpdateMockToursData();
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
                        UpdateMockToursData();
                    });
                    _ = UpdateDistancesAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainPage] Lỗi load data: {ex.Message}");
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Shell.Current.DisplayAlertAsync("Lỗi kết nối", "Không thể cập nhật danh sách địa điểm. Có thể do mạng yếu hoặc máy chủ bị gián đoạn.", "Thử lại");
                });
            }
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

        private void UpdateMockToursData()
        {
            if (ToursList.Count > 0) return; // Đã có data rồi thì khỏi load lại

            var allPoiIds = Restaurants.Select(r => r.Id).ToList();

            ToursList.Add(new Tour
            {
                Id = 1,
                Title = "Grill & Neon Lights",
                NumberOfStops = 5,
                Duration = "85",
                ImageUrl = "https://images.unsplash.com/photo-1555939594-58d7cb561ad1",
                PoiIds = allPoiIds.Take(5).ToList() // Lấy 5 quán đầu làm Tour thịt nướng
            });

            ToursList.Add(new Tour
            {
                Id = 2,
                Title = "The Street Food Fusion",
                NumberOfStops = 4,
                Duration = "60",
                ImageUrl = "https://images.unsplash.com/photo-1565299624946-b28f40a0ae38",
                PoiIds = allPoiIds.Skip(3).Take(4).ToList() // Trộn 4 quán ngẫu nhiên
            });

            ToursList.Add(new Tour
            {
                Id = 3,
                Title = "Hẻm Ốc Đêm Sài Thành",
                NumberOfStops = 6,
                Duration = "120",
                ImageUrl = "https://images.unsplash.com/photo-1549488344-c102df0827f3",
                PoiIds = allPoiIds.OrderBy(x => Guid.NewGuid()).Take(6).ToList()
            });
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

            // 🛰️ BUG FIX: Khởi động GPS Geofencing nếu chưa Start
            if (!_isGeofenceStarted)
            {
                _geofenceManager.Start();
                _isGeofenceStarted = true;
                System.Diagnostics.Debug.WriteLine("[MainPage] ✅ GeofenceManager Started.");
            }
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

        [RelayCommand]
        private async Task GoToTour(Tour selectedTour)
        {
            if (selectedTour == null) return;
            // Nhảy sang trang Tour Detail và truyền dữ liệu Tour qua
            await Shell.Current.GoToAsync("tourdetail", new Dictionary<string, object>
            {
                { "TourData", selectedTour }
            });
        }
    }
}