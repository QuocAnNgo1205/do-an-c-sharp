using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VinhKhanhFoodTour.App.Services;

namespace VinhKhanhFoodTour.App.PageModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly AuthService _authService;
    private readonly ApiService _apiService;

    [ObservableProperty]
    private string username = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    public LoginViewModel(AuthService authService, ApiService apiService)
    {
        _authService = authService;
        _apiService = apiService;
    }

    [RelayCommand]
    private async Task Login()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            await Shell.Current.DisplayAlert("⚠️ Thiếu thông tin", "Vui lòng nhập tên đăng nhập và mật khẩu.", "OK");
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _authService.LoginAsync(Username, Password);
            if (result.Success)
            {
                // Điều hướng vào App chính (Reset Stack)
                await Shell.Current.GoToAsync("//MainPage");
            }
            else
            {
                await Shell.Current.DisplayAlert("❌ Thất bại", result.Message, "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("❌ Lỗi", $"Đã xảy ra lỗi: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GoToRegister()
    {
        // Điều hướng tới trang đăng ký
        await Shell.Current.GoToAsync("RegisterPage");
    }
}
