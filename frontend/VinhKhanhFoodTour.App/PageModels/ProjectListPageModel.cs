using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using VinhKhanhFoodTour.App.Models;
using VinhKhanhFoodTour.App.Services;

namespace VinhKhanhFoodTour.App.PageModels
{
    public partial class ProjectListPageModel : ObservableObject
    {
        // Sử dụng Service để gọi API lấy danh sách quán ăn
        private readonly IPoiService _poiService;

        [ObservableProperty]
        private ObservableCollection<Poi> _pois = new();

        [ObservableProperty]
        private Poi? _selectedPoi;

        // Sửa lỗi "searchText" bị Null mà bạn gặp lúc nãy
        [ObservableProperty]
        private string _searchText = string.Empty;

        public ProjectListPageModel(IPoiService poiService)
        {
            _poiService = poiService;
        }

        // Hàm này chạy mỗi khi bạn mở màn hình danh sách
        [RelayCommand]
        private async Task Appearing()
        {
            await LoadData();
        }

        private async Task LoadData()
        {
            try
            {
                var list = await _poiService.GetPoisAsync();
                Pois = new ObservableCollection<Poi>(list);
            }
            catch (Exception ex)
            {
                // Nếu lỗi API, thông báo cho người dùng
                await Shell.Current.DisplayAlert("Lỗi", "Không thể tải danh sách quán ăn", "OK");
            }
        }

        // Khi người dùng bấm vào một quán ăn
        [RelayCommand]
        private async Task NavigateToProject(Poi poi)
        {
            if (poi == null) return;

            // Chuyển sang trang chi tiết và truyền dữ liệu quán ăn sang
            await Shell.Current.GoToAsync("project", new Dictionary<string, object>
            {
                { "Poi", poi }
            });
        }

        // Chức năng tìm kiếm quán ăn
        [RelayCommand]
        private async Task Search()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                await LoadData();
                return;
            }

            var filtered = Pois.Where(p => p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();
            Pois = new ObservableCollection<Poi>(filtered);
        }
    }
}