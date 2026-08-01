using System;
using System.Reflection;
using System.Windows.Input;
using HRManagement.Repositories.Interfaces;
using HRManagement.Resources;
using HRManagement.Services;
using HRManagement.Services.Interfaces;
using HRManagement.Utilities;

namespace HRManagement.ViewModels;

public class SettingsViewModel : PageViewModel
{
    private readonly ISettingService _settingService;
    private readonly IAccountRepository _accountRepository;
    private readonly SessionManager _sessionService;
    public override string Title => "Settings";
    public SettingsViewModel(
    ISettingService settingService,
    IAccountRepository accountRepository,
    SessionManager sessionService)
    {
        _settingService = settingService;
        _accountRepository = accountRepository;
        _sessionService = sessionService;

        _isDarkTheme = _settingService.CurrentTheme == SettingResources.ThemeDark;

        ToggleThemeCommand = new RelayCommand(_ => ToggleTheme());
        SavePasswordCommand = new RelayCommand(_ => SavePassword(), _ => CanSavePassword());

        LoadAboutInfo();
    }

    #region Accessibility

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

    private void ToggleTheme() => IsDarkTheme = !IsDarkTheme;

    #endregion

    #region Privacy - Change Password

    private string _currentPassword = string.Empty;
    public string CurrentPassword
    {
        get => _currentPassword;
        set
        {
            if (SetProperty(ref _currentPassword, value))
                ((RelayCommand)SavePasswordCommand).RaiseCanExecuteChanged();
        }
    }

    private string _newPassword = string.Empty;
    public string NewPassword
    {
        get => _newPassword;
        set
        {
            if (SetProperty(ref _newPassword, value))
                ((RelayCommand)SavePasswordCommand).RaiseCanExecuteChanged();
        }
    }

    private string _confirmPassword = string.Empty;
    public string ConfirmPassword
    {
        get => _confirmPassword;
        set
        {
            if (SetProperty(ref _confirmPassword, value))
                ((RelayCommand)SavePasswordCommand).RaiseCanExecuteChanged();
        }
    }

    private string _passwordStatusMessage = string.Empty;
    public string PasswordStatusMessage
    {
        get => _passwordStatusMessage;
        set => SetProperty(ref _passwordStatusMessage, value);
    }

    private bool _passwordChangeSucceeded;
    public bool PasswordChangeSucceeded
    {
        get => _passwordChangeSucceeded;
        set => SetProperty(ref _passwordChangeSucceeded, value);
    }

    public ICommand SavePasswordCommand { get; }

    private bool CanSavePassword()
    {
        return !string.IsNullOrWhiteSpace(CurrentPassword)
            && !string.IsNullOrWhiteSpace(NewPassword)
            && !string.IsNullOrWhiteSpace(ConfirmPassword);
    }

    private void SavePassword()
    {
        var accountId = _sessionService.CurrentUser!.Account.AccountId;
        PasswordChangeSucceeded = false;

        if (NewPassword != ConfirmPassword)
        {
            PasswordStatusMessage = "New password and confirmation do not match.";
            return;
        }

        if (NewPassword.Length < 8)
        {
            PasswordStatusMessage = "New password must be at least 8 characters long.";
            return;
        }

        if (NewPassword == CurrentPassword)
        {
            PasswordStatusMessage = "New password must be different from the current password.";
            return;
        }

        var success = _accountRepository.ChangePassword(accountId, CurrentPassword, NewPassword);

        if (!success)
        {
            PasswordStatusMessage = "Current password is incorrect.";
            return;
        }

        PasswordChangeSucceeded = true;
        PasswordStatusMessage = "Password updated successfully.";

        CurrentPassword = string.Empty;
        NewPassword = string.Empty;
        ConfirmPassword = string.Empty;
    }

    #endregion

    #region About

    public string ApplicationName { get; private set; } = "HR Management";
    public string Version { get; private set; } = string.Empty;
    public string DotNetVersion { get; private set; } = string.Empty;
    public string DatabaseName { get; private set; } = "SQL Server";
    public string Developer { get; private set; } = string.Empty;
    public string Copyright { get; private set; } = string.Empty;

    private void LoadAboutInfo()
    {
        var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;

        Version = assemblyVersion != null
            ? $"v{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}"
            : "v1.0.0";

        DotNetVersion = Environment.Version.ToString();
        Developer = "HR Management Team";
        Copyright = $"© {DateTime.Now.Year} HR Management. All rights reserved.";
    }

    #endregion
}
