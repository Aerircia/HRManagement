using HRManagement.Services.Interfaces;
using HRManagement.Utilities;
using HRManagement.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace HRManagement.Services;

public class NavigationService(IServiceProvider serviceProvider) : ViewModelBase, INavigationService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    private ViewModelBase? _currentView;

    public ViewModelBase? CurrentView
    {
        get => _currentView;
        private set => SetProperty(ref _currentView, value);
    }

    public void Navigate<TViewModel>()
        where TViewModel : ViewModelBase
    {
        CurrentView = _serviceProvider.GetRequiredService<TViewModel>();
    }
    public void Navigate(Type viewModelType)
    {
        var viewModel = _serviceProvider.GetRequiredService(viewModelType);

        if (viewModel is ViewModelBase vm)
        {
            CurrentView = vm;
        }
    }
    public void Reset()
    {
        // 1. Clear the view
        CurrentView = null;

    }
}