using System.Net.Http.Json;
using System.Diagnostics;
using VinhKhanhFoodTour.App.Models;

namespace VinhKhanhFoodTour.App.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;

    // Giữ nguyên cấu hình kết nối ổn định cho Máy ảo và Máy thật
    public static string BaseAddress =
        DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5007" : "http://localhost:5007";

    public ApiService()
    {
        _httpClient = new HttpClient();

        // Đảm bảo BaseAddress chuẩn hóa
        var finalAddress = BaseAddress.EndsWith("/") ? BaseAddress : BaseAddress + "/";
        _httpClient.BaseAddress = new Uri(finalAddress);

        // Timeout 15 giây để tránh chờ đợi quá lâu nếu Backend chưa bật
        _httpClient.Timeout = TimeSpan.FromSeconds(15);
    }

    /// <summary>
    /// ĐÃ XÓA HÀM LOGIN: Theo đúng đặc tả, khách du lịch vào App là dùng ngay, 
    /// không cần đăng nhập. Việc quản lý dữ liệu sẽ làm ở phía Web/Swagger.
    /// </summary>

    public async Task<List<Poi>?> GetPoisAsync()
    {
        try
        {
            // BỎ HOÀN TOÀN việc lấy Token từ SecureStorage. 
            // App bây giờ dùng quyền "Khách vãng lai" nên không cần gửi Header Authorization nữa.

            // Gọi đúng đầu API công khai (Public)
            var response = await _httpClient.GetAsync("api/v1/Poi/public");

            if (!response.IsSuccessStatusCode)
            {
                // Thông báo lỗi nếu Server từ chối (ví dụ: 404 hoặc 500)
                var currentPage = Application.Current?.Windows.FirstOrDefault()?.Page;
                if (currentPage != null)
                    await currentPage.DisplayAlert("Lỗi Server", $"Backend không phản hồi đúng. Mã lỗi: {response.StatusCode}", "OK");
                return null;
            }

            // Trả về danh sách quán ăn cho MainPage hiển thị
            return await response.Content.ReadFromJsonAsync<List<Poi>>();
        }
        catch (Exception ex)
        {
            // Thông báo nếu mất mạng hoặc Backend chưa chạy
            Debug.WriteLine($"[API ERROR]: {ex.Message}");
            var currentPage = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (currentPage != null)
                await currentPage.DisplayAlert("Lỗi Kết Nối", "Không thể kết nối tới máy chủ. Hãy đảm bảo Backend đã được bật!", "Đã hiểu");
            return null;
        }
    }
}