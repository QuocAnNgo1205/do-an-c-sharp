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
        private string? userEmail;

        [ObservableProperty]
        private string? userRole;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private ObservableCollection<LanguageInfo> availableLanguages = new();

        [ObservableProperty]
        private LanguageInfo? selectedLanguage;

        [ObservableProperty]
        private bool isLanguageListExpanded;

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
            string userName = await SecureStorage.GetAsync("user_name") ?? "guest";
            Preferences.Default.Set($"PreferredLanguage_{userName}", langCode); // Ghi nhớ riêng cho tài khoản
            Preferences.Default.Set("PreferredLanguage", langCode); // Cập nhật trạng thái chung để đồng bộ vào Audio Service

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
            IsLanguageListExpanded = false; // Tự động thu gọn lại
            
            await Shell.Current.DisplayAlertAsync("Thành công", $"Đã chọn: {language.Name} {language.Flag}", "OK");
        }

        [RelayCommand]
        private void ToggleLanguageList()
        {
            IsLanguageListExpanded = !IsLanguageListExpanded;
        }

        [RelayCommand]
        public async Task LoadProfileData()
        {
            // LUÔN LUÔN cập nhật tên/email ngay từ giây đầu tiên bước vào trang (Bypass IsBusy)
            UserName = await SecureStorage.GetAsync("user_name") ?? "Khách";
            UserEmail = await SecureStorage.GetAsync("user_email") ?? "Chưa cập nhật email";
            UserRole = await SecureStorage.GetAsync("user_role") ?? "Tourist";

            if (IsBusy) return;
            IsBusy = true;

            try
            {
                // LUÔN LẤY LẠI NGÔN NGỮ KHI TRANG HIỂN THỊ
                var savedLang = Preferences.Default.Get("PreferredLanguage", "vi");
                _isInitializing = true;
                SelectedLanguage = AvailableLanguages.FirstOrDefault(l => l.Code == savedLang) 
                                  ?? AvailableLanguages.First();
                _isInitializing = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Profile] Error loading data: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
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
    }

    public class LanguageInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Flag { get; set; } = string.Empty;
    }
}
