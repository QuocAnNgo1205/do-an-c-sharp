using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VinhKhanhFoodTour.App.Services;

namespace VinhKhanhFoodTour.App.PageModels;

public partial class RegisterViewModel : ObservableObject
{
    private readonly AuthService _authService;

    [ObservableProperty]
    private string username = string.Empty;

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private string confirmPassword = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    public RegisterViewModel(AuthService authService)
    {
        _authService = authService;
    }

    [RelayCommand]
    private async Task Register()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            await Shell.Current.DisplayAlertAsync("Lỗi", "Vui lòng điền đầy đủ các thông tin.", "OK");
            return;
        }

        if (Password != ConfirmPassword)
        {
            await Shell.Current.DisplayAlertAsync("Lỗi", "Mật khẩu xác nhận không khớp.", "OK");
            return;
        }

        IsBusy = true;
        try
        {
            var (Success, Message) = await _authService.RegisterAsync(Username, Email, Password);
            if (Success)
            {
                await Shell.Current.DisplayAlertAsync("Thành công 🎉", Message, "Đến Đăng nhập");
                await GoToLogin();
            }
            else
            {
                await Shell.Current.DisplayAlertAsync("Lỗi đăng ký", Message, "OK");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GoToLogin()
    {
        // Quay lại trang Đăng nhập
        await Shell.Current.GoToAsync("..");
    }
}
