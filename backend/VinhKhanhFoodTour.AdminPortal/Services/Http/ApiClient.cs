using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using VinhKhanhFoodTour.AdminPortal.Services.Auth;

namespace VinhKhanhFoodTour.AdminPortal.Services.Http;

public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly AuthState _authState;
    private readonly NavigationManager _navigationManager;
    private readonly ILogger<ApiClient> _logger;

    private const string AuthTokenKey = "authToken";
    private const string AuthUserKey = "authUser";

    public ApiClient(
        HttpClient httpClient,
        ILocalStorageService localStorage,
        AuthState authState,
        NavigationManager navigationManager,
        ILogger<ApiClient> logger)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
        _authState = authState;
        _navigationManager = navigationManager;
        _logger = logger;

        // Lưu ý: BaseAddress đã được cấu hình trong Program.cs qua builder.Services.AddHttpClient<ApiClient>
        if (_httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "VinhKhanhFoodTour.AdminPortal");
        }
    }

    /// <summary>
    /// Attach JWT token to request header
    /// </summary>
    private async Task AttachTokenAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>(AuthTokenKey);
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to attach token: {ex.Message}");
        }
    }

    /// <summary>
    /// Generic GET request
    /// </summary>
    public async Task<T?> GetAsync<T>(string endpoint)
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>(AuthTokenKey);
            
            var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await _httpClient.SendAsync(request);
            return await HandleResponseAsync<T>(response);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError($"GET {endpoint} failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Generic POST request with JSON body
    /// </summary>
    public async Task<T?> PostAsync<T>(string endpoint, object body)
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>(AuthTokenKey);
            
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            
            request.Content = JsonContent.Create(body);

            var response = await _httpClient.SendAsync(request);
            return await HandleResponseAsync<T>(response);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError($"POST {endpoint} failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Generic DELETE request
    /// </summary>
    public async Task<T?> DeleteAsync<T>(string endpoint)
    {
        try
        {
            await AttachTokenAsync();
            var response = await _httpClient.DeleteAsync(endpoint);
            return await HandleResponseAsync<T>(response);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError($"DELETE {endpoint} failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Generic PUT request with JSON body
    /// </summary>
    public async Task<T?> PutAsync<T>(string endpoint, object body)
    {
        try
        {
            await AttachTokenAsync();
            var content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PutAsync(endpoint, content);
            return await HandleResponseAsync<T>(response);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError($"PUT {endpoint} failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Generic POST with multipart/form-data (for file uploads)
    /// </summary>
    public async Task<T?> PostFormAsync<T>(string endpoint, MultipartFormDataContent content)
    {
        try
        {
            await AttachTokenAsync();
            
            // 🔴 DEBUG: Log Content-Type to see if boundary is present
            _logger.LogInformation($"[PostFormAsync] Sending multipart form to {endpoint}. Content-Type: {content.Headers.ContentType}");
            
            var response = await _httpClient.PostAsync(endpoint, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogError($"[PostFormAsync] Request to {endpoint} failed with {response.StatusCode}: {responseBody}");
            }
            
            return await HandleResponseAsync<T>(response);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError($"POST {endpoint} (form) failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Generic PUT with multipart/form-data (for file updates)
    /// </summary>
    public async Task<T?> PutFormAsync<T>(string endpoint, MultipartFormDataContent content)
    {
        try
        {
            await AttachTokenAsync();
            
            _logger.LogInformation($"[PutFormAsync] Sending multipart form to {endpoint}.");
            
            var response = await _httpClient.PutAsync(endpoint, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogError($"[PutFormAsync] Request to {endpoint} failed with {response.StatusCode}: {responseBody}");
            }
            
            return await HandleResponseAsync<T>(response);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError($"PUT {endpoint} (form) failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Handle HTTP response with 401 interceptor
    /// </summary>
    private async Task<T?> HandleResponseAsync<T>(HttpResponseMessage response)
    {
        // 401 Unauthorized - Clear auth and redirect to login
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("Received 401 Unauthorized - Logging out user");
            await ClearAuthAsync();
            _navigationManager.NavigateTo("/login", forceLoad: true);
            return default;
        }

        // Check for success
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError($"HTTP {response.StatusCode}: {errorContent}");
            throw new HttpRequestException($"HTTP {response.StatusCode}: {errorContent}");
        }

        // Parse response
        var jsonContent = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(jsonContent))
            return default;

        return JsonSerializer.Deserialize<T>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    /// <summary>
    /// Clear auth state on logout or 401
    /// </summary>
    public async Task ClearAuthAsync()
    {
        try
        {
            await _localStorage.RemoveItemAsync(AuthTokenKey);
            await _localStorage.RemoveItemAsync(AuthUserKey);
            _authState.Logout();
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to clear auth: {ex.Message}");
        }
    }

    /// <summary>
    /// Helper to get full URL for media files
    /// </summary>
    public string GetFullUrl(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath)) return string.Empty;
        if (relativePath.StartsWith("http")) return relativePath;

        var baseAddress = _httpClient.BaseAddress?.ToString() ?? "";
        return $"{baseAddress.TrimEnd('/')}/{relativePath.TrimStart('/')}";
    }
}
