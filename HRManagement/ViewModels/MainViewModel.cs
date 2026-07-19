using System.ComponentModel;
using HRManagement.Services.Interfaces;
using HRManagement.Utilities;

namespace HRManagement.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;

    public PageViewModel? CurrentPage =>
        _navigationService.CurrentView as PageViewModel;

    public string WindowTitle =>
        CurrentPage?.Title ?? "HR Management";

    public MainViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;

        _navigationService.PropertyChanged += NavigationChanged;

        _navigationService.Navigate<DashboardViewModel>();
    }

    private void NavigationChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(INavigationService.CurrentView))
        {
            OnPropertyChanged(nameof(CurrentPage));
            OnPropertyChanged(nameof(WindowTitle));
        }
    }
}