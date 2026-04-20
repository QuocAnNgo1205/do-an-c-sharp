using System.Net.Http.Json;
using System.Diagnostics;
using VinhKhanhFoodTour.App.Data;

namespace VinhKhanhFoodTour.App.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;

    public AuthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        Debug.WriteLine($"[AuthService] Initialized with BaseAddress from DI: {_httpClient.BaseAddress}");
    }

    public async Task<(bool Success, string Message)> LoginAsync(string username, string password)
    {
        try
        {
            var loginData = new 
            { 
                Username = username, 
                Password = password,
                DeviceId = DeviceInfo.Current.Model, // Use Model as fallback or identifier if possible, but real DeviceId is better.
                DeviceName = DeviceInfo.Current.Name,
                Os = DeviceInfo.Current.Platform.ToString()
            };
            var response = await _httpClient.PostAsJsonAsync("Auth/login", loginData);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (result != null && !string.IsNullOrEmpty(result.Token))
                {
                    // 🔐 Lưu Token vào SecureStorage
                    await SecureStorage.SetAsync("jwt_token", result.Token);
                    // DÙNG BIẾN username TỪ THAM SỐ ĐÚNG Ở TRÊN VÌ API KHÔNG TRẢ VỀ FIELD NÀY
                    await SecureStorage.SetAsync("user_name", username);
                    await SecureStorage.SetAsync("user_role", result.Role);
                    await SecureStorage.SetAsync("user_email", string.IsNullOrEmpty(result.Email) ? "Chưa cập nhật email" : result.Email);
                    await SecureStorage.SetAsync("token_expiration", result.Expiration.HasValue ? result.Expiration.Value.ToString("o") : DateTime.UtcNow.AddDays(7).ToString("o"));

                    // 🌍 PHỤC HỒI NGÔN NGỮ CỦA RIÊNG TÀI KHOẢN NÀY (Mặc định là 'vi' - tiếng Việt)
                    var userLang = Preferences.Default.Get($"PreferredLanguage_{username}", "vi");
                    Preferences.Default.Set("PreferredLanguage", userLang);

                    Debug.WriteLine("[Auth] Login successful, token saved.");
                    return (true, "Đăng nhập thành công!");
                }
            }

            var error = await response.Content.ReadAsStringAsync();
            Debug.WriteLine($"[Auth] Login failed: {error}");
            return (false, "Tên đăng nhập hoặc mật khẩu không chính xác.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Auth] Login exception: {ex.Message}");
            return (false, "Lỗi kết nối máy chủ. Vui lòng thử lại sau.");
        }
    }

    public async Task<(bool Success, string Message)> GuestLoginAsync(string deviceId)
    {
        try
        {
            var loginData = new 
            { 
                DeviceId = deviceId,
                DeviceName = DeviceInfo.Current.Name,
                Os = DeviceInfo.Current.Platform.ToString()
            };
            var response = await _httpClient.PostAsJsonAsync("Auth/guest-login", loginData);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (result != null && !string.IsNullOrEmpty(result.Token))
                {
                    // 🔐 Lưu Token vào SecureStorage
                    await SecureStorage.SetAsync("jwt_token", result.Token);
                    await SecureStorage.SetAsync("user_name", result.Username);
                    await SecureStorage.SetAsync("user_role", result.Role);
                    await SecureStorage.SetAsync("token_expiration", result.Expiration.HasValue ? result.Expiration.Value.ToString("o") : DateTime.UtcNow.AddDays(365).ToString("o"));

                    // 🌍 PHỤC HỒI NGÔN NGỮ (Mặc định 'vi')
                    var userLang = Preferences.Default.Get($"PreferredLanguage_{result.Username}", "vi");
                    Preferences.Default.Set("PreferredLanguage", userLang);

                    Debug.WriteLine("[Auth] Guest Login successful, token saved.");
                    return (true, "Đăng nhập Guest thành công!");
                }
            }

            var error = await response.Content.ReadAsStringAsync();
            Debug.WriteLine($"[Auth] Guest Login failed: {error}");
            return (false, "Không thể tạo tài khoản Guest.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Auth] Guest Login exception: {ex.Message}");
            return (false, "Lỗi kết nối máy chủ. Vui lòng thử lại sau.");
        }
    }

    public async Task<(bool Success, string Message)> RegisterAsync(string username, string email, string password)
    {
        try
        {
            var registerData = new 
            { 
                Username = username, 
                Email = email, 
                Password = password,
                Role = "Tourist" // Mặc định cho App
            };
            
            var response = await _httpClient.PostAsJsonAsync("Auth/register", registerData);

            if (response.IsSuccessStatusCode)
            {
                return (true, "Đăng ký thành công! Bạn có thể đăng nhập ngay.");
            }

            var error = await response.Content.ReadAsStringAsync();
            Debug.WriteLine($"[Auth] Register failed: {error}");
            return (false, "Đăng ký thất bại. Tên đăng nhập hoặc Email có thể đã tồn tại.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Auth] Register exception: {ex.Message}");
            return (false, "Lỗi kết nối máy chủ.");
        }
    }

    public void Logout()
    {
        SecureStorage.Remove("jwt_token");
        SecureStorage.Remove("user_name");
        SecureStorage.Remove("user_role");
        SecureStorage.Remove("user_email");
        SecureStorage.Remove("token_expiration");
        
        // Reset lại cờ chọn ngôn ngữ để acc sau (hoặc đăng ký mới) đăng nhập vào sẽ được hỏi lại
        Preferences.Default.Remove("HasSelectedLanguageFirstTime");
    }

    public async Task PingActivityAsync()
    {
        if (!await IsLoggedInAsync()) return;

        try
        {
            var pingData = new 
            { 
                DeviceId = DeviceInfo.Current.Model,
                DeviceName = DeviceInfo.Current.Name,
                Os = DeviceInfo.Current.Platform.ToString()
            };
            
            // Note: HttpClient should already have the Auth header if configured in ApiService/DI
            var response = await _httpClient.PostAsJsonAsync("Auth/ping", pingData);
            if (response.IsSuccessStatusCode)
            {
                Debug.WriteLine("[Auth] Activity ping sent successfully.");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Auth] Activity ping failed: {ex.Message}");
        }
    }

    public async Task<bool> IsLoggedInAsync()
    {
        var token = await SecureStorage.GetAsync("jwt_token");
        return !string.IsNullOrEmpty(token);
    }
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime? Expiration { get; set; }
}
