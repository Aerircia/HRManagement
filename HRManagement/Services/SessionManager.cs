using HRManagement.Models;
using HRManagement.Services.Interfaces;

namespace HRManagement.Services;

public class SessionManager
{
    public CurrentUser? CurrentUser { get; private set; }

    public bool IsLoggedIn => CurrentUser != null;

    public void Login(CurrentUser user)
    {
        CurrentUser = user;
        OnUserChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Logout()
    {
        CurrentUser = null;
        OnUserChanged?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? OnUserChanged;
}