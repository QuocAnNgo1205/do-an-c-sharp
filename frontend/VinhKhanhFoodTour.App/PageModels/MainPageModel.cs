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

        [ObservableProperty]
        private ObservableCollection<Poi> restaurants = new();

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private bool isRefreshing;
        [ObservableProperty] private string searchText = string.Empty;

        // 📊 MỚI: Dữ liệu cho biểu đồ thống kê quán ăn
        [ObservableProperty] private ObservableCollection<PoiCategory> categoryData = new();
        [ObservableProperty] private List<Brush> categoryColors = new();

        public MainPageModel(ApiService apiService, AudioGuideService audioGuideService)
        {
            _apiService = apiService;
            _audioGuideService = audioGuideService;
        }

        private async Task LoadDataAsync(string? query = null)
        {
            if (IsBusy) return;
            IsBusy = true;
            IsRefreshing = true;

            try
            {
                var data = await _apiService.GetPoisAsync();
                if (data != null)
                {
                    if (!string.IsNullOrWhiteSpace(query))
                    {
                        data = data.Where(p =>
                            (p.Name != null && p.Name.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                            (p.Title != null && p.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
                        ).ToList();
                    }

                    Restaurants.Clear();
                    foreach (var item in data)
                    {
                        Restaurants.Add(item);
                    }

                    _ = UpdateDistancesAsync();
                    
                    // Cập nhật biểu đồ sau khi nạp dữ liệu xong
                    UpdateChartData();
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
        [RelayCommand]
        private async Task Speak(Poi poi)
        {
            if (poi == null) return;

            // Nếu đang phát chính quán này -> Dừng
            if (poi.IsPlaying)
            {
                await StopSpeak();
                return;
            }

            // Dừng mọi quán khác đang phát trước khi phát quán mới
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
                
                // PlayAudioAsync hiện đã nhận callback để đồng bộ trạng thái UI
                // Ngôn ngữ được tự động lấy từ Preferences bên trong Service
                await _audioGuideService.PlayAudioAsync(poi, isPlaying => 
                {
                    poi.IsPlaying = isPlaying;
                    if (!isPlaying) poi.IsLoadingAudio = false;
                });
            }
            catch (Exception)
            {
                // Lỗi đã được Service hiển thị Alert
            }
            finally
            {
                poi.IsLoadingAudio = false;
            }
        }

        [RelayCommand]
        private async Task StopSpeak()
        {
            await _audioGuideService.StopAudioAsync();
            
            // Reset trạng thái hiển thị cho tất cả các quán trong danh sách
            foreach (var r in Restaurants)
            {
                r.IsPlaying = false;
                r.IsLoadingAudio = false;
            }
        }

        // --- GIỮ NGUYÊN TOÀN BỘ CÁC LỆNH GỐC ---
        [RelayCommand]
        private async Task Appearing() => await LoadDataAsync();

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