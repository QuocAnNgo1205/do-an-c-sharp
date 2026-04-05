using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace VinhKhanhFoodTour.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // 🔗 Đăng ký Route cho các trang phụ (không nằm trong TabBar)
        Routing.RegisterRoute("RegisterPage", typeof(Pages.RegisterPage));

        // 🛡️ Kiểm tra Login khi App khởi động
        Task.Run(async () => await CheckLoginStatusAsync());
    }

    private async Task CheckLoginStatusAsync()
    {
        try
        {
            var token = await SecureStorage.GetAsync("jwt_token");
            if (string.IsNullOrEmpty(token))
            {
                // Nếu chưa login -> Nhảy sang trang Login
                await MainThread.InvokeOnMainThreadAsync(async () => 
                {
                    await Shell.Current.GoToAsync("//LoginPage");
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Auth] Error checking login: {ex.Message}");
        }
    }

    // Đã thêm chữ "static" để các màn hình khác có thể gọi được
    public static async Task DisplayToastAsync(string message)
    {
        var toast = Toast.Make(message, ToastDuration.Short, 14);
        await toast.Show();
    }
}