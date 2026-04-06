using System.Net.Http.Json;
using System.Diagnostics;
using VinhKhanhFoodTour.App.Models;
using VinhKhanhFoodTour.App.Data;

namespace VinhKhanhFoodTour.App.Services;

/// <summary>
/// Service chung cho tất cả các gọi API tới Backend
/// 
/// Tính năng:
/// - Cấu hình Base URL tự động (Android Emulator / iOS Simulator)
/// - Xử lý lỗi toàn cầu (timeout, network error, server error)
/// - Logging để debug
/// </summary>
public class ApiService
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Base URL tự động được cấu hình từ Constants
    /// - Android Emulator: http://10.0.2.2:5007/api/v1
    /// - iOS Simulator: http://127.0.0.1:5007/api/v1
    /// </summary>
    public static string BaseAddress => Constants.API_BASE_URL;

    public ApiService()
    {
        _httpClient = new HttpClient();

        // Đảm bảo BaseAddress chuẩn hóa (không bị dư dấu '/')
        var finalAddress = BaseAddress.EndsWith("/") ? BaseAddress : BaseAddress + "/";
        _httpClient.BaseAddress = new Uri(finalAddress);

        // Timeout dựa trên cấu hình Constants
        _httpClient.Timeout = TimeSpan.FromSeconds(Constants.HTTP_TIMEOUT_SECONDS);

        Debug.WriteLine($"[API] Initialized with BaseAddress: {finalAddress}");
    }

    private async Task SetAuthHeaderAsync()
    {
        try
        {
            var token = await SecureStorage.GetAsync("jwt_token");
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[API ERROR] Failed to set auth header: {ex.Message}");
        }
    }

    // ====================================================================
    // ENDPOINT 1: Lấy danh sách các POI công khai (Public)
    // ====================================================================
    /// <summary>
    /// Lấy danh sách các POI công khai (Public)
    /// - Danh sách Poi với các trường cơ bản (Id, Name, Latitude, Longitude, ImageUrl, Status)
    /// - Không bao giờ bao gồm Translations
    /// </summary>
    public async Task<List<Poi>?> GetPoisAsync(bool showErrorAlert = true)
    {
        try
        {
            Debug.WriteLine($"[API] GET /Poi");
            await SetAuthHeaderAsync();
            var response = await _httpClient.GetAsync("Poi");

            if (!response.IsSuccessStatusCode)
            {
                if (showErrorAlert)
                    await HandleErrorResponse(response, "Lấy danh sách quán");
                return null;
            }

            var pois = await response.Content.ReadFromJsonAsync<List<Poi>>();
            Debug.WriteLine($"[API] ✓ Retrieved {pois?.Count ?? 0} POIs");
            return pois ?? new List<Poi>();
        }
        catch (HttpRequestException ex) when (ex.InnerException is TimeoutException)
        {
            if (showErrorAlert)
                await ShowErrorAlert("⏱️ Hết Thời Chờ", "Backend không phản hồi trong 15 giây. Kiểm tra kết nối mạng và xác nhận Backend đang chạy.");
            return null;
        }
        catch (HttpRequestException ex)
        {
            Debug.WriteLine($"[API ERROR] Network error: {ex.Message}");
            if (showErrorAlert)
                await ShowErrorAlert("🔌 Lỗi Mạng", 
                    $"Không kết nối được tới Backend ({BaseAddress}). Chi tiết: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[API ERROR] Unexpected error: {ex}");
            if (showErrorAlert)
                await ShowErrorAlert("❌ Lỗi Bất Ngờ", $"Đã xảy ra lỗi khi lấy danh sách quán: {ex.Message}");
            return null;
        }
    }

    // ====================================================================
    // ENDPOINT 2: Lấy chi tiết một POI bao gồm Translations
    // ====================================================================
    /// <summary>
    /// Gọi GET /api/v1/Poi/{id} để lấy chi tiết của một quán ăn
    /// 
    /// Endpoint trả về:
    /// - Poi với Id, Name, Latitude, Longitude, ImageUrl, Status
    /// - 🔴 QUAN TRỌNG: Bao gồm danh sách Translations đầy đủ (kèm AudioFilePath)
    /// 
    /// Ví dụ Response:
    /// {
    ///   "id": 1,
    ///   "name": "Quán Phở",
    ///   "latitude": 21.028511,
    ///   "longitude": 105.854007,
    ///   "imageUrl": "https://...",
    ///   "status": 1,
    ///   "translations": [
    ///     {
    ///       "id": 10,
    ///       "languageCode": "vi",
    ///       "title": "Phở Gia Truyền",
    ///       "description": "Quán phở nổi tiếng...",
    ///       "audioFilePath": "/media/vi/poi_1.mp3",
    ///       "imageUrl": null
    ///     }
    ///   ]
    /// }
    /// </summary>
    public async Task<Poi?> GetPoiDetailAsync(int poiId, bool showErrorAlert = true)
    {
        try
        {
            Debug.WriteLine($"[API] GET /Poi/{poiId}");
            await SetAuthHeaderAsync();
            var response = await _httpClient.GetAsync($"Poi/public/{poiId}");

            if (!response.IsSuccessStatusCode)
            {
                if (showErrorAlert)
                    await HandleErrorResponse(response, $"Lấy chi tiết quán (ID: {poiId})");
                return null;
            }

            var poi = await response.Content.ReadFromJsonAsync<Poi>();
            Debug.WriteLine($"[API] ✓ Retrieved POI detail with {poi?.Translations?.Count ?? 0} translations");
            return poi;
        }
        catch (HttpRequestException ex) when (ex.InnerException is TimeoutException)
        {
            if (showErrorAlert)
                await ShowErrorAlert("⏱️ Hết Thời Chờ", "Backend không phản hồi trong 15 giây.");
            return null;
        }
        catch (HttpRequestException ex)
        {
            Debug.WriteLine($"[API ERROR] Network error: {ex.Message}");
            if (showErrorAlert)
                await ShowErrorAlert("🔌 Lỗi Mạng", $"Không thể kết nối tới Backend: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[API ERROR] Unexpected error: {ex}");
            if (showErrorAlert)
                await ShowErrorAlert("❌ Lỗi Bất Ngờ", $"Lỗi khi lấy chi tiết quán: {ex.Message}");
            return null;
        }
    }

    // ====================================================================
    // ENDPOINT 3: Gửi dữ liệu lên Server (Analytics, Logging, v.v.)
    // ====================================================================
    /// <summary>
    /// Gửi dữ liệu POST tới Backend (ví dụ: Log lượt nghe)
    /// </summary>
    public async Task<bool> PostAsync<T>(string endpoint, T data)
    {
        try
        {
            Debug.WriteLine($"[API] POST /{endpoint}");
            await SetAuthHeaderAsync();
            var response = await _httpClient.PostAsJsonAsync(endpoint, data);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"[API ERROR] POST /{endpoint} failed: {response.StatusCode} - {errorBody}");
                return false;
            }

            Debug.WriteLine($"[API] ✓ POST /{endpoint} successful");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[API ERROR] POST /{endpoint} failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> PutAsync<T>(string endpoint, T data)
    {
        try
        {
            Debug.WriteLine($"[API] PUT /{endpoint}");
            await SetAuthHeaderAsync();
            var response = await _httpClient.PutAsJsonAsync(endpoint, data);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"[API ERROR] PUT /{endpoint} failed: {response.StatusCode} - {errorBody}");
                return false;
            }

            Debug.WriteLine($"[API] ✓ PUT /{endpoint} successful");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[API ERROR] PUT /{endpoint} failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteAsync(string endpoint)
    {
        try
        {
            Debug.WriteLine($"[API] DELETE /{endpoint}");
            await SetAuthHeaderAsync();
            var response = await _httpClient.DeleteAsync(endpoint);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"[API ERROR] DELETE /{endpoint} failed: {response.StatusCode} - {errorBody}");
                return false;
            }

            Debug.WriteLine($"[API] ✓ DELETE /{endpoint} successful");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[API ERROR] DELETE /{endpoint} failed: {ex.Message}");
            return false;
        }
    }

    // ====================================================================
    // TIỆN ÍCH: Hàm xử lý lỗi Server
    // ====================================================================
    private async Task HandleErrorResponse(HttpResponseMessage response, string context)
    {
        string errorMessage = context;

        try
        {
            // Cố gắng đọc error message từ response body
            var errorContent = await response.Content.ReadAsStringAsync();
            Debug.WriteLine($"[API ERROR] {response.StatusCode}: {errorContent}");
            errorMessage += $"\n\nMã lỗi: {response.StatusCode}";
        }
        catch { }

        await ShowErrorAlert("❌ Lỗi Server", $"{errorMessage}");
    }

    private async Task ShowErrorAlert(string title, string message)
    {
        try
        {
            var currentPage = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (currentPage != null)
                await currentPage.DisplayAlertAsync(title, message, "OK");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] Cannot show alert: {ex.Message}");
        }
    }
}