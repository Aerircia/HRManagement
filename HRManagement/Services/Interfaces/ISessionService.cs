using HRManagement.Models;
using System.ComponentModel;

namespace HRManagement.Services.Interfaces;

public interface ISessionService : INotifyPropertyChanged
{
    Account? CurrentAccount { get; }

    bool IsLoggedIn { get; }

    void Login(Account account);

    void Logout();
}