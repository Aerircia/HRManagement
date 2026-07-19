using HRManagement.Models;
using HRManagement.Services.Interfaces;
using HRManagement.Utilities;

namespace HRManagement.Services;

public class SessionService : ViewModelBase, ISessionService
{
    private Account? _currentAccount;

    public Account? CurrentAccount
    {
        get => _currentAccount;
        private set
        {
            if (SetProperty(ref _currentAccount, value))
            {
                OnPropertyChanged(nameof(IsLoggedIn));
            }
        }
    }

    public bool IsLoggedIn => CurrentAccount != null;

    public void Login(Account account)
    {
        CurrentAccount = account;
    }

    public void Logout()
    {
        CurrentAccount = null;
    }
}