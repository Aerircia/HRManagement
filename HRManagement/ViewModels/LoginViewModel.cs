using System.Windows.Input;
using HRManagement.Services.Interfaces;
using HRManagement.Utilities;

namespace HRManagement.ViewModels;

public class LoginViewModel : ViewModelBase
{
    private readonly ISessionService _sessionService;
    private readonly INavigationService _navigationService;
    private readonly IWindowService _windowService;

    private string _username = string.Empty;
    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    private string _password = string.Empty;
    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public ICommand LoginCommand { get; }

    public LoginViewModel(
        ISessionService sessionService,
        INavigationService navigationService,
        IWindowService windowService)
    {
        _sessionService = sessionService;
        _navigationService = navigationService;
        _windowService = windowService;

        LoginCommand = new RelayCommand(_ => Login());
    }

    private void Login()
    {
        // Temporary until AuthenticationService exists

        _windowService.ShowMainWindow();
    }
}