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
        Routing.RegisterRoute("ProjectDetailPage", typeof(Pages.ProjectDetailPage));

        // 🛡️ Kiểm tra Login khi App khởi động (bắt đầu từ SplashPage)
        Task.Run(async () => await CheckLoginStatusAsync());
    }

    private async Task CheckLoginStatusAsync()
    {
        try
        {
            var token = await SecureStorage.GetAsync("jwt_token");
            var expirationStr = await SecureStorage.GetAsync("token_expiration");
            
            bool isExpired = true; // Mặc định là hết hạn nếu không parse được
            if (!string.IsNullOrEmpty(expirationStr) && DateTime.TryParse(expirationStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var expDate))
            {
                // Nếu giờ TCG quốc tế nhỏ hơn TCG hết hạn -> Còn hạn
                if (DateTime.UtcNow < expDate)
                {
                    isExpired = false;
                }
            }

            if (string.IsNullOrEmpty(token) || isExpired)
            {
                // Nếu token rỗng HOẶC đã hết hạn -> Thu hồi thông tin cũ và nhảy sang Login
                SecureStorage.Remove("jwt_token");
                SecureStorage.Remove("token_expiration");
                await MainThread.InvokeOnMainThreadAsync(async () => 
                {
                    await Shell.Current.GoToAsync("//LoginPage");
                });
            }
            else
            {
                // Nếu đã login (CÓ JWT_TOKEN VÀ CHƯA HẾT HẠN) -> Nhảy thẳng vào trang chủ
                await MainThread.InvokeOnMainThreadAsync(async () => 
                {
                    await Shell.Current.GoToAsync("//MainPage");
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