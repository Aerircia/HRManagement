using HRManagement.Services.Interfaces;

namespace HRManagement.Services;

public class AuthorizationService(SessionManager sessionManager) : IAuthorizationService
{
    private readonly SessionManager _sessionManager = sessionManager;

    public bool IsLoggedIn =>
        _sessionManager.IsLoggedIn;

    public bool IsAdmin =>
    _sessionManager.CurrentUser?.Role.RoleId == 1;

    public bool IsManager =>
        IsAdmin || _sessionManager.CurrentUser?.Role.RoleId == 2;

    public bool IsEmployee =>
        IsManager || _sessionManager.CurrentUser?.Role.RoleId == 3;

}