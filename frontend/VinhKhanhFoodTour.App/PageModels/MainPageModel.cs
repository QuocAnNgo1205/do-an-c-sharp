using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VinhKhanhFoodTour.App.Models;
using VinhKhanhFoodTour.App.Services;
using System.Threading; // 👉 Cần cho quản lý dừng giọng nói

namespace VinhKhanhFoodTour.App.PageModels
{
    public partial class MainPageModel : ObservableObject
    {
        private readonly ApiService _apiService;

        // Biến quản lý dừng thuyết minh (Cách làm của Senior để tránh lỗi pin/memory)
        private CancellationTokenSource? _speechCancellation;

        [ObservableProperty]
        private ObservableCollection<Poi> restaurants = new();

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private bool isRefreshing;
        [ObservableProperty] private string searchText = string.Empty;

        public MainPageModel(ApiService apiService)
        {
            _apiService = apiService;
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
                }
            }
            catch (Exception) { }
            finally
            {
                IsRefreshing = false;
                IsBusy = false;
            }
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

        // --- TÍNH NĂNG MỚI: THUYẾT MINH ---
        [RelayCommand]
        private async Task Speak(Poi poi)
        {
            if (poi == null || string.IsNullOrWhiteSpace(poi.Description)) return;

            try
            {
                StopSpeak(); // Dừng nếu đang đọc quán cũ
                _speechCancellation = new CancellationTokenSource();

                await TextToSpeech.Default.SpeakAsync(poi.Description, new SpeechOptions
                {
                    Pitch = 1.0f,
                    Volume = 1.0f
                }, _speechCancellation.Token);
            }
            catch (OperationCanceledException) { }
            catch (Exception) { }
        }

        [RelayCommand]
        private void StopSpeak()
        {
            if (_speechCancellation != null && !_speechCancellation.IsCancellationRequested)
            {
                _speechCancellation.Cancel();
                _speechCancellation.Dispose();
                _speechCancellation = null;
            }
        }

        // --- GIỮ NGUYÊN TOÀN BỘ CÁC LỆNH GỐC CỦA BẠN ---
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