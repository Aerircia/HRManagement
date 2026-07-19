using System.Windows.Input;
using HRManagement.Models;
using HRManagement.Services.Interfaces;
using HRManagement.Utilities;

namespace HRManagement.ViewModels;

public class SidebarViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly ISessionService _sessionService;

    public ICommand NavigateCommand { get; }

    public bool IsManager =>
        (_sessionService.CurrentAccount?.RoleID ?? 1) >= (int)UserRole.Manager;

    public bool IsHR =>
        (_sessionService.CurrentAccount?.RoleID ?? 1) >= (int)UserRole.HR;

    public SidebarViewModel(
        INavigationService navigationService,
        ISessionService sessionService)
    {
        _navigationService = navigationService;
        _sessionService = sessionService;

        NavigateCommand = new RelayCommand(Navigate);

        _sessionService.PropertyChanged += (_, __) =>
        {
            OnPropertyChanged(nameof(IsManager));
            OnPropertyChanged(nameof(IsHR));
        };
    }

    private void Navigate(object? parameter)
    {
        if (parameter is Type viewModelType)
        {
            _navigationService.Navigate(viewModelType);
        }
    }
}