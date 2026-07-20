using HRManagement.Models;

namespace HRManagement.Services;

public class SessionManager
{
    public CurrentUser? CurrentUser { get; private set; }

    public bool IsLoggedIn => CurrentUser != null;

    public void Login(CurrentUser user)
    {
        CurrentUser = user;
    }

    public void Logout()
    {
        CurrentUser = null;
    }
}