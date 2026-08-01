using System.Windows.Input;
using HRManagement.Services.Interfaces;
using HRManagement.Utilities;

namespace HRManagement.ViewModels;

public class LoginViewModel : ViewModelBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IWindowService _windowService;
    private readonly INavigationService _navigationService;

    public LoginViewModel(
        IAuthenticationService authenticationService,
        IWindowService windowService,
        INavigationService navigationService)
    {
        _authenticationService = authenticationService;
        _windowService = windowService;
        _navigationService = navigationService;

        LoginCommand = new RelayCommand(Login);
    }

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

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public ICommand LoginCommand { get; }

    private void Login(object? parameter)
    {
        ErrorMessage = string.Empty;

        var user = _authenticationService.Login(
            Username.Trim(),
            Password);

        if (user == null)
        {
            ErrorMessage = "Invalid username or password.";
            return;
        }
        _navigationService.Navigate<DashboardViewModel>();
        _windowService.ShowMainWindow();
    }
}