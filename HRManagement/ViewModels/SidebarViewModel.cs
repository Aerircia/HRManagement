using System;
using System.Windows.Input;
using HRManagement.Services.Interfaces;
using HRManagement.Utilities;

namespace HRManagement.ViewModels;

public class SidebarViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IAuthorizationService _authorizationService;

    public SidebarViewModel(
        INavigationService navigationService,
        IAuthorizationService authorizationService)
    {
        _navigationService = navigationService;
        _authorizationService = authorizationService;

        NavigateCommand = new RelayCommand(Navigate);
    }

    public ICommand NavigateCommand { get; }

    private void Navigate(object? parameter)
    {
        if (parameter is Type viewModelType)
        {
            _navigationService.Navigate(viewModelType);
        }
    }

    #region Authorization

    public bool IsAdmin => _authorizationService.IsAdmin;

    public bool IsManager => _authorizationService.IsManager;

    public bool IsEmployee => _authorizationService.IsEmployee;

    #endregion
}
