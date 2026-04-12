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

    /// <summary>
    /// Xử lý Deep Link URI dạng vkfoodtour://poi/{id}.
    /// Được gọi từ App.xaml.cs khi app nhận intent (cả cold start và background resume).
    /// Phải chạy trên Main Thread.
    /// </summary>
    public async Task HandleDeepLinkAsync(Uri uri)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[DeepLink] Received URI: {uri}");

            // Kiểm tra đúng scheme
            if (!uri.Scheme.Equals("vkfoodtour", StringComparison.OrdinalIgnoreCase))
                return;

            // Kiểm tra đúng host (host = "poi" trong vkfoodtour://poi/{id})
            if (!uri.Host.Equals("poi", StringComparison.OrdinalIgnoreCase))
                return;

            // Bóc tách PoiId từ path: "/42" → 42
            var segments = uri.AbsolutePath.Trim('/').Split('/');
            if (segments.Length == 0 || !int.TryParse(segments[0], out int poiId) || poiId <= 0)
            {
                System.Diagnostics.Debug.WriteLine($"[DeepLink] Invalid PoiId in URI: {uri}");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[DeepLink] Navigating to ProjectDetailPage with PoiId={poiId}");

            // Đảm bảo đang ở Main Thread trước khi navigate
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                // Đảm bảo user đã đăng nhập trước khi điều hướng
                var token = await SecureStorage.GetAsync("jwt_token");
                if (string.IsNullOrEmpty(token))
                {
                    // Chưa login → lưu lại intent để xử lý sau khi login xong
                    System.Diagnostics.Debug.WriteLine("[DeepLink] User not logged in, cannot navigate.");
                    await DisplayToastAsync("Vui lòng đăng nhập để xem chi tiết quán ăn.");
                    return;
                }

                // Navigate với PoiId dưới dạng query param — ProjectDetailPageModel nhận qua [QueryProperty]
                // Truyền một Poi "stub" chỉ có Id, trang detail sẽ tự gọi API lấy đủ thông tin
                var stubPoi = new Models.Poi { Id = poiId, Name = "Đang tải..." };
                await Shell.Current.GoToAsync(
                    "ProjectDetailPage",
                    new Dictionary<string, object> { ["Poi"] = stubPoi }
                );
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DeepLink] Error handling deep link: {ex.Message}");
        }
    }

    // Đã thêm chữ "static" để các màn hình khác có thể gọi được
    public static async Task DisplayToastAsync(string message)
    {
        var toast = Toast.Make(message, ToastDuration.Short, 14);
        await toast.Show();
    }
}