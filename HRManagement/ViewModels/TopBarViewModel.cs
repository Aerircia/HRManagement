using System.Windows.Input;
using HRManagement.Services;
using HRManagement.Services.Interfaces;
using HRManagement.Utilities;

namespace HRManagement.ViewModels;

public class TopBarViewModel : ViewModelBase
{
    private readonly SessionManager _sessionManager;
    private readonly IAuthenticationService _authenticationService;
    private readonly IWindowService _windowService;
    private readonly INavigationService _navigationService;

    public TopBarViewModel(
        SessionManager sessionManager,
        IAuthenticationService authenticationService,
        IWindowService windowService,
        INavigationService navigationService)
    {
        _sessionManager = sessionManager;
        _authenticationService = authenticationService;
        _windowService = windowService;
        _navigationService = navigationService;

        LogoutCommand = new RelayCommand(Logout);
    }

    public string UserName =>
        _sessionManager.CurrentUser?.Employee.FullName ?? "";

    public string RoleName =>
        _sessionManager.CurrentUser?.Role.RoleName ?? "";

    public string? Avatar =>
        _sessionManager.CurrentUser?.Employee.Avatar;

    public ICommand LogoutCommand { get; }

    private void Logout(object? parameter)
    {
        _authenticationService.Logout();

        _navigationService.Reset();

        _windowService.ShowLoginWindow();
    }
}