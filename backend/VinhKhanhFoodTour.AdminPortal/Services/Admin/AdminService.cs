using VinhKhanhFoodTour.AdminPortal.Services.Http;
using Microsoft.Extensions.Logging;
using VinhKhanhFoodTour.AdminPortal.Models.Auth;
using VinhKhanhFoodTour.AdminPortal.Models.Admin;

namespace VinhKhanhFoodTour.AdminPortal.Services.Admin;

public class AdminService : IAdminService
{
    private readonly ApiClient _apiClient;
    private readonly ILogger<AdminService> _logger;

    public AdminService(ApiClient apiClient, ILogger<AdminService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<List<AdminPoiDto>> GetAllPendingPoisAsync()
    {
        try
        {
            // Backend route: [Route("api/v1/[controller]")] + [HttpGet("pending")]
            // = GET api/v1/Poi/pending
            var result = await _apiClient.GetAsync<List<AdminPoiDto>>("api/v1/Poi/pending");
            return result ?? new List<AdminPoiDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching pending POIs: {ex.Message}");
            throw;
        }
    }

    public async Task<PoiActionResponseDto> ApprovePoisAsync(int id)
    {
        try
        {
            // Backend: PUT api/v1/Poi/{id}/approve → returns { Message, Id }
            var result = await _apiClient.PutAsync<PoiActionResponseDto>($"api/v1/Poi/{id}/approve", new { });
            return result ?? new PoiActionResponseDto { Message = "Approved successfully" };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error approving POI {id}: {ex.Message}");
            throw;
        }
    }

    public async Task<PoiActionResponseDto> RejectPoiAsync(int id, string reason)
    {
        try
        {
            // Backend: PUT api/v1/Poi/{id}/reject → body: { reason } → returns { Message, Id }
            var dto = new RejectPoiDto { Reason = reason };
            var result = await _apiClient.PutAsync<PoiActionResponseDto>($"api/v1/Poi/{id}/reject", dto);
            return result ?? new PoiActionResponseDto { Message = "Rejected successfully" };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error rejecting POI {id}: {ex.Message}");
            throw;
        }
    }

    public async Task<List<UserDto>> GetUsersAsync()
    {
        try
        {
            var result = await _apiClient.GetAsync<List<UserDto>>("api/v1/Users");
            return result ?? new List<UserDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching users: {ex.Message}");
            throw;
        }
    }

    public async Task<UserToggleResponseDto> ToggleUserStatusAsync(int id)
    {
        try
        {
            var result = await _apiClient.PutAsync<UserToggleResponseDto>($"api/v1/Users/{id}/toggle-status", new { });
            return result ?? new UserToggleResponseDto { Message = "User status toggled successfully." };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error toggling user {id}: {ex.Message}");
            throw;
        }
    }

    public async Task<UserActionResponseDto> DeleteUserAsync(int id)
    {
        try
        {
            var result = await _apiClient.DeleteAsync<UserActionResponseDto>($"api/v1/Users/{id}");
            return result ?? new UserActionResponseDto { Message = "User deleted successfully." };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error deleting user {id}: {ex.Message}");
            throw;
        }
    }

    public async Task<UserActionResponseDto> AddUserAsync(CreateUserDto dto)
    {
        try
        {
            var result = await _apiClient.PostAsync<UserActionResponseDto>("api/v1/Users", dto);
            return result ?? new UserActionResponseDto { Message = "User added successfully." };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error adding user: {ex.Message}");
            throw;
        }
    }

    public async Task<UserStatsDto?> GetUserStatsAsync()
    {
        try
        {
            var result = await _apiClient.GetAsync<UserStatsDto>("api/v1/Users/stats");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching user stats: {ex.Message}");
            throw;
        }
    }

    public async Task<List<UserDeviceDto>> GetActiveDevicesAsync(int userId)
    {
        try
        {
            var result = await _apiClient.GetAsync<List<UserDeviceDto>>($"api/v1/Users/{userId}/devices");
            return result ?? new List<UserDeviceDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching devices for user {userId}: {ex.Message}");
            throw;
        }
    }

    public async Task<UserActionResponseDto> RevokeDeviceAsync(int userId, string deviceId)
    {
        try
        {
            var result = await _apiClient.DeleteAsync<UserActionResponseDto>($"api/v1/Users/{userId}/devices/{deviceId}");
            return result ?? new UserActionResponseDto { Message = "Device access revoked successfully." };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error revoking device {deviceId} for user {userId}: {ex.Message}");
            throw;
        }
    }

    public async Task<List<AdminAudioDto>> GetAudioFilesAsync()
    {
        try
        {
            var result = await _apiClient.GetAsync<List<AdminAudioDto>>("api/v1/Media/audio");
            return result ?? new List<AdminAudioDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching audio files: {ex.Message}");
            throw;
        }
    }

    public async Task<UserActionResponseDto> DeleteAudioFileAsync(string fileName)
    {
        try
        {
            var result = await _apiClient.DeleteAsync<UserActionResponseDto>($"api/v1/Media/audio/{fileName}");
            return result ?? new UserActionResponseDto { Message = "Audio file deleted successfully." };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error deleting audio file {fileName}: {ex.Message}");
            throw;
        }
    }
}

/// <summary>
/// DTO for rejecting a POI - matches backend's RejectPoiDto
/// </summary>
public class RejectPoiDto
{
    public string Reason { get; set; } = string.Empty;
}
