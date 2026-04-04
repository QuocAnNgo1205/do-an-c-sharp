using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using Blazored.LocalStorage;
using System.Net.Http.Json;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VinhKhanhFoodTour.AdminPortal.Models.Auth;
using VinhKhanhFoodTour.AdminPortal.Services.Http;

namespace VinhKhanhFoodTour.AdminPortal.Services.Auth;

public class AuthService
{
    private readonly ApiClient _apiClient;
    private readonly ILocalStorageService _localStorage;
    private readonly AuthState _authState;
    private readonly ILogger<AuthService> _logger;

    private const string AuthTokenKey = "authToken";
    private const string AuthUserKey = "authUser";

    public AuthService(
        ApiClient apiClient,
        ILocalStorageService localStorage,
        AuthState authState,
        ILogger<AuthService> logger)
    {
        _apiClient = apiClient;
        _localStorage = localStorage;
        _authState = authState;
        _logger = logger;
    }

    /// <summary>
    /// Login with username and password
    /// </summary>
    public async Task<bool> LoginAsync(string username, string password)
    {
        try
        {
            var loginRequest = new LoginRequest
            {
                Username = username,
                Password = password
            };

            var response = await _apiClient.PostAsync<LoginResponse>(
                "/api/v1/Auth/login",
                loginRequest);

            if (response?.Token == null)
            {
                _logger.LogWarning("Login failed: No token in response");
                return false;
            }

            // Parse JWT and extract user info
            var user = ExtractUserFromToken(response.Token);
            if (user == null)
            {
                _logger.LogWarning("Failed to extract user from token");
                return false;
            }

            // Store token and user in localStorage
            await _localStorage.SetItemAsync(AuthTokenKey, response.Token);
            await _localStorage.SetItemAsync(AuthUserKey, user);

            // Update auth state
            _authState.Login(user);
            _logger.LogInformation($"Login successful for user: {user.Username} (Role: {user.Role})");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Login exception: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Logout user
    /// </summary>
    public async Task LogoutAsync()
    {
        try
        {
            await _apiClient.ClearAuthAsync();
            _logger.LogInformation("Logout successful");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Logout exception: {ex.Message}");
        }
    }

    /// <summary>
    /// Check if user is authenticated on app load
    /// </summary>
    public async Task<bool> RestoreAuthAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>(AuthTokenKey);
            var user = await _localStorage.GetItemAsync<AuthUser>(AuthUserKey);

            if (string.IsNullOrEmpty(token) || user == null)
            {
                _authState.Logout();
                return false;
            }

            // Verify token is not expired
            if (!IsTokenValid(token))
            {
                await _apiClient.ClearAuthAsync();
                return false;
            }

            _authState.SetCurrentUser(user);
            _logger.LogInformation($"Auth restored for user: {user.Username}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"RestoreAuth exception: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Extract user info from JWT token using claims
    /// </summary>
    private AuthUser? ExtractUserFromToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            var claims = jwt.Claims;

            var user = new AuthUser
            {
                Id = long.TryParse(
                    claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value,
                    out var id) ? id : 0,
                Username = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? string.Empty,
                Role = claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? string.Empty
            };

            return !string.IsNullOrEmpty(user.Username) ? user : null;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to extract user from token: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Verify JWT token is still valid (not expired)
    /// </summary>
    private bool IsTokenValid(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            return jwt.ValidTo > DateTime.UtcNow;
        }
        catch
        {
            return false;
        }
    }
}

// Response DTO from backend login endpoint
// Must match the JSON returned by AuthController.Login():
//   { "token": "...", "role": "...", "expiration": "..." }
public class LoginResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("expiration")]
    public DateTime Expiration { get; set; }
}
