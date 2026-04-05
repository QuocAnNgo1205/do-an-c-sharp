using VinhKhanhFoodTour.AdminPortal.Models.Auth;

namespace VinhKhanhFoodTour.AdminPortal.Services.Admin;

/// <summary>
/// DTO for Admin POI - matches backend PoiDto response from GET /api/v1/Poi/pending
/// </summary>
public class AdminPoiDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Status { get; set; }
    public string? RejectionReason { get; set; }
}

/// <summary>
/// DTO matching backend's approval/rejection response: { Message, Id }
/// </summary>
public class PoiActionResponseDto
{
    public string Message { get; set; } = string.Empty;
    public int? Id { get; set; }
}

public class UserActionResponseDto
{
    public string Message { get; set; } = string.Empty;
}

public class CreateUserDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string RoleName { get; set; } = "Owner";
}

/// <summary>
/// Service interface for admin operations on POIs
/// </summary>
public interface IAdminService
{
    /// <summary>
    /// Get all POIs with Pending status
    /// </summary>
    Task<List<AdminPoiDto>> GetAllPendingPoisAsync();

    /// <summary>
    /// Approve a pending POI
    /// </summary>
    Task<PoiActionResponseDto> ApprovePoisAsync(int id);

    /// <summary>
    /// Reject a pending POI with a reason
    /// </summary>
    Task<PoiActionResponseDto> RejectPoiAsync(int id, string reason);

    /// <summary>
    /// Get all users in the system (Admin only)
    /// </summary>
    Task<List<UserDto>> GetUsersAsync();

    /// <summary>
    /// Toggle the IsActive status of a user (Admin only)
    /// </summary>
    Task<UserToggleResponseDto> ToggleUserStatusAsync(int id);

    /// <summary>
    /// Delete a user (Admin only)
    /// </summary>
    Task<UserActionResponseDto> DeleteUserAsync(int id);

    /// <summary>
    /// Add a new user (Admin only)
    /// </summary>
    Task<UserActionResponseDto> AddUserAsync(CreateUserDto dto);
}
