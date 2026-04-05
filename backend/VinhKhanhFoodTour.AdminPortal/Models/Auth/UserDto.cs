namespace VinhKhanhFoodTour.AdminPortal.Models.Auth;

/// <summary>
/// DTO representing a user returned by GET /api/v1/Users
/// </summary>
public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO matching backend's toggle status response
/// </summary>
public class UserToggleResponseDto
{
    public string Message { get; set; } = string.Empty;
}
