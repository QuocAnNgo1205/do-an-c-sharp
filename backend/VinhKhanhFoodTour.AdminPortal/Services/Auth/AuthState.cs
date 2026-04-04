using System;
using VinhKhanhFoodTour.AdminPortal.Models.Auth;

namespace VinhKhanhFoodTour.AdminPortal.Services.Auth;

public class AuthState
{
    private AuthUser? _currentUser;
    public event Action? OnStateChanged;

    public AuthUser? CurrentUser
    {
        get => _currentUser;
        private set
        {
            if (_currentUser != value)
            {
                _currentUser = value;
                NotifyStateChanged();
            }
        }
    }

    public bool IsAuthenticated => CurrentUser != null;

    public bool IsAdmin => CurrentUser?.Role == "Admin";
    public bool IsOwner => CurrentUser?.Role == "Owner";

    public void Login(AuthUser user)
    {
        CurrentUser = user;
    }

    public void Logout()
    {
        CurrentUser = null;
    }

    public void SetCurrentUser(AuthUser? user)
    {
        CurrentUser = user;
    }

    private void NotifyStateChanged() => OnStateChanged?.Invoke();
}
