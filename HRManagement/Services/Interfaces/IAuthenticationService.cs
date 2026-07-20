using HRManagement.Models;

namespace HRManagement.Services.Interfaces;

public interface IAuthenticationService
{
    CurrentUser? Login(string username, string password);

    void Logout();
}