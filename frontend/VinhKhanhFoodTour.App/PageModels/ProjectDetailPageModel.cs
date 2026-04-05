using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VinhKhanhFoodTour.App.Models;
using VinhKhanhFoodTour.App.Services;

namespace VinhKhanhFoodTour.App.PageModels
{
    [QueryProperty(nameof(Poi), "Poi")]
    public partial class ProjectDetailPageModel : ObservableObject
    {
        private readonly AudioGuideService _audioGuideService;

        [ObservableProperty]
        private Poi? poi;

        [ObservableProperty]
        private bool isPlaying;

        [ObservableProperty]
        private bool isLoadingAudio;

        public ProjectDetailPageModel(AudioGuideService audioGuideService)
        {
            _audioGuideService = audioGuideService;
        }

        /// <summary>
        /// Bật/Tắt thuyết minh (Audio Guide)
        /// </summary>
        [RelayCommand]
        private async Task ToggleAudio()
        {
            if (Poi == null) return;

            if (IsPlaying)
            {
                await StopAudio();
            }
            else
            {
                await StartAudio();
            }
        }

        private async Task StartAudio()
        {
            if (Poi == null) return;

            IsLoadingAudio = true;
            try 
            {
                // Gọi service xử lý fallback MP3 -> TTS
                // Ngôn ngữ được lấy từ Preferences bên trong Service
                await _audioGuideService.PlayAudioAsync(Poi, isPlaying => 
                {
                    IsPlaying = isPlaying;
                });
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Lỗi", "Không thể phát âm thanh: " + ex.Message, "OK");
            }
            finally
            {
                IsLoadingAudio = false;
            }
        }

        [RelayCommand]
        public async Task StopAudio()
        {
            await _audioGuideService.StopAudioAsync();
            IsPlaying = false;
        }

        /// <summary>
        /// Mở bản đồ tích hợp
        /// </summary>
        [RelayCommand]
        private async Task OpenMap()
        {
            if (Poi == null || Poi.Latitude == 0 || Poi.Longitude == 0)
            {
                await Shell.Current.DisplayAlertAsync("Thông báo", "Chưa có dữ liệu tọa độ cho quán này!", "OK");
                return;
            }

            try
            {
                await Shell.Current.GoToAsync($"///MapPage?Lat={Poi.Latitude}&Lon={Poi.Longitude}&Name={Uri.EscapeDataString(Poi.Name ?? "Địa điểm")}");
            }
            catch (Exception)
            {
                await Shell.Current.DisplayAlertAsync("Lỗi", "Không thể mở bản đồ!", "OK");
            }
        }

        // Cũ: Giữ lại để không lỗi nếu XAML chưa cập nhật ngay
        [RelayCommand]
        private async Task Speak() => await ToggleAudio();
    }
}