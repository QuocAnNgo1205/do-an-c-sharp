using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using VinhKhanhFoodTour.App.Models;
using VinhKhanhFoodTour.App.Services;

namespace VinhKhanhFoodTour.App.PageModels
{
    public partial class ProfilePageModel : ObservableObject
    {
        private readonly ApiService _apiService;
        private readonly AudioGuideService _audioGuideService;
        private readonly AuthService _authService;

        [ObservableProperty]
        private string? userName;

        [ObservableProperty]
        private string? userRole;

        [ObservableProperty]
        private Poi? featuredPoi;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private ObservableCollection<LanguageInfo> availableLanguages = new();

        [ObservableProperty]
        private LanguageInfo? selectedLanguage;

        private bool _isInitializing;

        public ProfilePageModel(ApiService apiService, AudioGuideService audioGuideService, AuthService authService)
        {
            _apiService = apiService;
            _audioGuideService = audioGuideService;
            _authService = authService;

            // Khởi tạo danh sách ngôn ngữ hỗ trợ
            AvailableLanguages = new ObservableCollection<LanguageInfo>
            {
                new() { Name = "Tiếng Việt", Code = "vi", Flag = "🇻🇳" },
                new() { Name = "Tiếng Anh (English)", Code = "en", Flag = "🇬🇧" },
                new() { Name = "Tiếng Hàn (Korean)", Code = "ko", Flag = "🇰🇷" },
                new() { Name = "Tiếng Nhật (Japanese)", Code = "ja", Flag = "🇯🇵" }
            };

            // Lấy ngôn ngữ đã lưu từ Local
            var savedLang = Preferences.Default.Get("PreferredLanguage", "vi");
            _isInitializing = true;
            SelectedLanguage = AvailableLanguages.FirstOrDefault(l => l.Code == savedLang) 
                              ?? AvailableLanguages.First();
            _isInitializing = false;
        }

        partial void OnSelectedLanguageChanged(LanguageInfo? value)
        {
            if (value == null || _isInitializing) return;
            _ = UpdateLanguageAsync(value.Code);
        }

        private async Task UpdateLanguageAsync(string langCode)
        {
            // 1. Lưu Local ngay lập tức để UX mượt mà
            Preferences.Default.Set("PreferredLanguage", langCode);

            // 2. Đồng bộ lên Server (nếu thất bại cũng không sao vì đã có Local)
            try
            {
                await _apiService.PutAsync("Account/language", new { LanguageCode = langCode });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Profile] Sync language failed: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task SelectLanguage(LanguageInfo language)
        {
            if (language == null) return;
            SelectedLanguage = language;
            // OnSelectedLanguageChanged sẽ được gọi tự động
            
            await Shell.Current.DisplayAlertAsync("Thành công", $"Đã chọn: {language.Name} {language.Flag}", "OK");
        }

        [RelayCommand]
        public async Task LoadProfileData()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                // Lấy danh sách quán và chọn quán đầu tiên làm "Quán tiêu biểu" cho Profile
                var pois = await _apiService.GetPoisAsync();
                if (pois != null && pois.Count > 0)
                {
                    // Lấy chi tiết đầy đủ của quán đầu tiên (để có Description)
                    FeaturedPoi = await _apiService.GetPoiDetailAsync(pois[0].Id);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Profile] Error loading data: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                
                // Cập nhật thông tin user từ Storage
                UserName = await SecureStorage.GetAsync("user_name") ?? "Khách";
                UserRole = await SecureStorage.GetAsync("user_role") ?? "Tourist";
            }
        }

        [RelayCommand]
        private async Task Logout()
        {
            bool confirm = await Shell.Current.DisplayAlertAsync("Đăng xuất", "Bạn có chắc chắn muốn đăng xuất không?", "Đăng xuất", "Hủy");
            if (confirm)
            {
                _authService.Logout();
                await Shell.Current.GoToAsync("//LoginPage");
            }
        }

        [RelayCommand]
        private async Task ToggleAudio()
        {
            if (FeaturedPoi == null) return;

            if (FeaturedPoi.IsPlaying)
            {
                await _audioGuideService.StopAudioAsync();
                FeaturedPoi.IsPlaying = false;
            }
            else
            {
                try
                {
                    FeaturedPoi.IsLoadingAudio = true;
                    // PlayAudioAsync hiện đã nhận callback để đồng bộ trạng thái UI
                    // Ngôn ngữ được tự động lấy từ Preferences bên trong Service
                    await _audioGuideService.PlayAudioAsync(FeaturedPoi, isPlaying => 
                    {
                        FeaturedPoi.IsPlaying = isPlaying;
                        if (!isPlaying) FeaturedPoi.IsLoadingAudio = false;
                    });
                }
                catch { }
                finally
                {
                    FeaturedPoi.IsLoadingAudio = false;
                }
            }
        }

        [RelayCommand]
        private async Task StopAudio()
        {
            await _audioGuideService.StopAudioAsync();
            if (FeaturedPoi != null) FeaturedPoi.IsPlaying = false;
        }
    }

    public class LanguageInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Flag { get; set; } = string.Empty;
    }
}
