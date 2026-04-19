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
        Routing.RegisterRoute("tourdetail", typeof(Pages.TourDetailPage));

        // 🛡️ Kiểm tra Login khi App khởi động (bắt đầu từ SplashPage)
        Task.Run(async () => await CheckLoginStatusAsync());
    }

    public bool IsInitialized { get; private set; } = false;

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
                // Nếu token rỗng HOẶC đã hết hạn -> Tự động đăng nhập Guest
                SecureStorage.Remove("jwt_token");
                SecureStorage.Remove("token_expiration");

                var deviceId = await SecureStorage.GetAsync("device_id");
                if (string.IsNullOrEmpty(deviceId))
                {
                    deviceId = Guid.NewGuid().ToString();
                    await SecureStorage.SetAsync("device_id", deviceId);
                }

                var authService = new Services.AuthService();
                await authService.GuestLoginAsync(deviceId);

                IsInitialized = true; // Đánh dấu đã khởi tạo xong

                await MainThread.InvokeOnMainThreadAsync(async () => 
                {
                    await Shell.Current.GoToAsync("//MainPage");
                });
            }
            else
            {
                IsInitialized = true; // Đánh dấu đã khởi tạo xong

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

            // Đợi luồng CheckLoginStatus (Guest Auto Login) kết thúc
            while (!IsInitialized)
            {
                await Task.Delay(100);
            }

            // Đảm bảo đang ở Main Thread trước khi navigate
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                // Truyền một Poi "stub" chỉ có Id, trang detail sẽ tự gọi API lấy đủ thông tin
                var stubPoi = new Models.Poi { Id = poiId, Name = "Đang tải..." };
                await Shell.Current.GoToAsync(
                    "ProjectDetailPage",
                    new Dictionary<string, object> 
                    { 
                        ["Poi"] = stubPoi,
                        ["IsFromQr"] = true
                    }
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