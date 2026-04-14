using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VinhKhanhFoodTour.App.Data;
using VinhKhanhFoodTour.App.Models;
using VinhKhanhFoodTour.App.Services;

namespace VinhKhanhFoodTour.App.PageModels
{
    [QueryProperty(nameof(Poi), "Poi")]
    [QueryProperty(nameof(IsFromQr), "IsFromQr")]
    public partial class ProjectDetailPageModel : ObservableObject
    {
        private readonly AudioGuideService _audioGuideService;
        private readonly ApiService _apiService;

        [ObservableProperty]
        private Poi? poi;

        [ObservableProperty]
        private bool isPlaying;

        [ObservableProperty]
        private bool isLoadingAudio;

        [ObservableProperty]
        private bool isFromQr;

        [ObservableProperty]
        private string displayDescription = "Đang tải thông tin...";

        public ProjectDetailPageModel(AudioGuideService audioGuideService, ApiService apiService)
        {
            _audioGuideService = audioGuideService;
            _apiService = apiService;
        }

        async partial void OnPoiChanged(Poi? value)
        {
            if (value != null)
            {
                await LoadPoiDetailsAsync(value);
            }
        }

        private async Task LoadPoiDetailsAsync(Poi currentPoi)
        {
            try
            {
                // Kiểm tra nếu chưa có bản dịch, tải chi tiết từ API
                if (currentPoi.Translations == null || currentPoi.Translations.Count == 0)
                {
                    var fullPoi = await _apiService.GetPoiDetailAsync(currentPoi.Id);
                    if (fullPoi?.Translations != null)
                    {
                        currentPoi.Translations = fullPoi.Translations;
                    }
                }

                // Lấy bản dịch phù hợp theo ngôn ngữ
                string languageCode = Preferences.Default.Get("PreferredLanguage", Constants.DEFAULT_LANGUAGE_CODE);
                var translation = _audioGuideService.GetPreferredTranslation(currentPoi, languageCode);
                
                if (translation != null)
                {
                    // Ghép Title và Description để hiển thị giống những gì được phát âm
                    DisplayDescription = $"{translation.Title}. {translation.Description}";
                }
                else
                {
                    DisplayDescription = currentPoi.Description ?? "Chưa có thông tin giới thiệu chi tiết cho địa điểm này.";
                }

                // Nếu được điều hướng từ QR Code -> Gắn log Scan Tracker
                if (IsFromQr)
                {
                    var deviceId = await SecureStorage.GetAsync("device_id");
                    if (!string.IsNullOrEmpty(deviceId))
                    {
                        var reqData = new
                        {
                            PoiId = currentPoi.Id,
                            DeviceId = deviceId,
                            Timestamp = DateTime.UtcNow
                        };
                        // Đẩy không chờ kết quả để trải nghiệm UX không bị block
                        _ = _apiService.PostAsync("public/logs/scan", reqData);
                    }
                    IsFromQr = false; // Tránh tracking 2 lần nếu user refresh lại trang này
                }
            }
            catch
            {
                DisplayDescription = currentPoi.Description ?? "Không thể tải thông tin giới thiệu.";
            }
        }

        /// <summary>
        /// Bật/Tắt thuyết minh (Audio Guide)
        /// </summary>
        [RelayCommand(AllowConcurrentExecutions = true)]
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