namespace VinhKhanhFoodTour.AdminPortal.Models.Auth;

public class AuthToken
{
    public string Token { get; set; } = string.Empty;
    public long UserId { get; set; }
    public string Username { get; set; } = string.Empty;
}
