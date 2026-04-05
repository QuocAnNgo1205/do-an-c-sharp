namespace VinhKhanhFoodTour.AdminPortal.Models.Auth;

/// <summary>
/// Data for a new user registration request
/// </summary>
public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Response message from registration endpoint
/// </summary>
public class RegisterResponse
{
    public string Message { get; set; } = string.Empty;
}
