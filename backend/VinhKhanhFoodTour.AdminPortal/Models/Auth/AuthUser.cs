namespace VinhKhanhFoodTour.AdminPortal.Models.Auth;

public class AuthUser
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // "Admin" or "Owner"
}
