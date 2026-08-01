using System.Windows.Input;
using HRManagement.Resources;
using HRManagement.Services.Interfaces;
using HRManagement.Utilities;

namespace HRManagement.ViewModels;

public class LoginViewModel : ViewModelBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IWindowService _windowService;
    private readonly INavigationService _navigationService;
    private readonly ISettingService _settingService;

    public LoginViewModel(
        IAuthenticationService authenticationService,
        IWindowService windowService,
        INavigationService navigationService,
        ISettingService settingService)
    {
        _authenticationService = authenticationService;
        _windowService = windowService;
        _navigationService = navigationService;
        _settingService = settingService;

        // The rest of the app (SettingService.Initialize, called from
        // App.xaml.cs on startup) already applies the persisted theme before
        // the login window shows, so this just reflects that current state
        // rather than assuming light mode - otherwise the toggle would show
        // as "off" even when dark mode was already active from last session.
        _isDarkTheme = _settingService.CurrentTheme == SettingResources.ThemeDark;

        LoginCommand = new RelayCommand(Login);
        ToggleThemeCommand = new RelayCommand(_ => IsDarkTheme = !IsDarkTheme);
    }

    private bool _isDarkTheme;
    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            if (SetProperty(ref _isDarkTheme, value))
            {
                if (value)
                    _settingService.SetDarkTheme();
                else
                    _settingService.SetLightTheme();
            }
        }
    }

    public ICommand ToggleThemeCommand { get; }

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