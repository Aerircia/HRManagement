using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Input;
using HRManagement.Models;
using HRManagement.Services.Interfaces;
using HRManagement.Utilities;

namespace HRManagement.ViewModels;

public class TopBarViewModel : ViewModelBase
{
    private readonly ISessionService _sessionService;
    private readonly IWindowService _windowService;

    private string _username = "Guest";
    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    private string _role = string.Empty;
    public string Role
    {
        get => _role;
        set => SetProperty(ref _role, value);
    }

    // Will later come from the database
    private ImageSource? _avatar;
    public ImageSource? Avatar
    {
        get => _avatar;
        set => SetProperty(ref _avatar, value);
    }

    public ICommand LogoutCommand { get; }

    public TopBarViewModel(
        ISessionService sessionService,
        IWindowService windowService)
    {
        _sessionService = sessionService;
        _windowService = windowService;

        LogoutCommand = new RelayCommand(_ => Logout());

        UpdateUserInfo();

        _sessionService.PropertyChanged += SessionChanged;
    }

    private void SessionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ISessionService.CurrentAccount))
        {
            UpdateUserInfo();
        }
    }

    private void UpdateUserInfo()
    {
        var account = _sessionService.CurrentAccount;

        if (account == null)
        {
            Username = "Guest";
            Role = string.Empty;
            Avatar = null;
            return;
        }

        Username = account.Username;
        Role = ((UserRole)account.RoleID).ToString();

        // TODO: Load avatar from database
        Avatar = null;
    }

    private void Logout()
    {
        _sessionService.Logout();
        _windowService.ShowLoginWindow();
    }
}