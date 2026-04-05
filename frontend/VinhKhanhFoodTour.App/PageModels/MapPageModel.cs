using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using VinhKhanhFoodTour.App.Models;
using VinhKhanhFoodTour.App.Services;

namespace VinhKhanhFoodTour.App.PageModels
{
    public partial class MapPageModel : ObservableObject
    {
        private readonly ApiService _apiService;

        // Danh sách chứa tất cả các quán ăn để vẽ lên bản đồ
        [ObservableProperty]
        private ObservableCollection<Poi> allRestaurants = new();

        public MapPageModel(ApiService apiService)
        {
            _apiService = apiService;
        }

        // Hàm này sẽ được gọi khi Tab Bản đồ được mở lên
        public async Task LoadMapDataAsync()
        {
            try
            {
                var data = await _apiService.GetPoisAsync();
                if (data != null && data.Any())
                {
                    AllRestaurants.Clear();
                    foreach (var item in data)
                    {
                        AllRestaurants.Add(item);
                    }
                }
            }
            catch (Exception) { }
        }
    }
}