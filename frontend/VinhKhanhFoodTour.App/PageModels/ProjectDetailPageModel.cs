using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using VinhKhanhFoodTour.App.Models;

namespace VinhKhanhFoodTour.App.PageModels
{
    [QueryProperty(nameof(Poi), "Poi")]
    public partial class ProjectDetailPageModel : ObservableObject
    {
        [ObservableProperty]
        private Poi? poi;

        // Tính năng 1: Đọc Thuyết minh (TTS)
        [RelayCommand]
        private async Task Speak()
        {
            // Kiểm tra an toàn: Poi hoặc Description không được null
            if (Poi == null || string.IsNullOrWhiteSpace(Poi.Description))
            {
                await Shell.Current.DisplayAlert("Thông báo", "Quán này chưa có bài thuyết minh!", "OK");
                return;
            }

            try
            {
                await TextToSpeech.Default.SpeakAsync(Poi.Description, new SpeechOptions
                {
                    Pitch = 1.0f,
                    Volume = 1.0f
                });
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlert("Lỗi", "Không thể phát giọng nói trên máy này!", "OK");
            }
        }

        // Tính năng 2: Mở bản đồ tích hợp thay vì nhảy ra Google Maps ngoài
        [RelayCommand]
        private async Task OpenMap()
        {
            // Kiểm tra tọa độ từ Backend truyền xuống
            if (Poi == null || Poi.Latitude == 0 || Poi.Longitude == 0)
            {
                await Shell.Current.DisplayAlert("Thông báo", "Chưa có dữ liệu tọa độ cho quán này!", "OK");
                return;
            }

            try
            {
                // ĐOẠN ĐÃ SỬA:
                // Thay vì dùng Map.Default.OpenAsync (gọi App ngoài), 
                // ta dùng ///MapPage để nhảy thẳng sang Tab Bản đồ bên trong App và cắm ghim.
                await Shell.Current.GoToAsync($"///MapPage?Lat={Poi.Latitude}&Lon={Poi.Longitude}&Name={Uri.EscapeDataString(Poi.Name ?? "Địa điểm")}");
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlert("Lỗi", "Không thể mở bản đồ!", "OK");
            }
        }
    }
}