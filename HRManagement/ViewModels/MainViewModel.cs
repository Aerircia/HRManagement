using System.Windows.Input;
using HRManagement.Utilities;
using HRManagement.ViewModels.Pages;

namespace HRManagement.ViewModels;

public class MainViewModel : ViewModelBase
{
    private object? _currentView;

    public object? CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    public ICommand ShowDashboardCommand { get; }

    public ICommand ShowProfileCommand { get; }

    public MainViewModel()
    {
        CurrentView = new DashboardViewModel();


        ShowDashboardCommand = new RelayCommand(_ =>
        {
            CurrentView = new DashboardViewModel();
        });


        ShowProfileCommand = new RelayCommand(_ =>
        {
            CurrentView = new ProfileViewModel();
        });
    }
}