namespace HRManagement.Services.Interfaces;

public interface IAuthorizationService
{
    bool IsLoggedIn { get; }

    bool IsAdmin { get; }

    bool IsManager { get; }

    bool IsEmployee { get; }
}