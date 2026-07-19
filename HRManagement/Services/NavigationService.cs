using Microsoft.Extensions.DependencyInjection;
using HRManagement.Services.Interfaces;
using HRManagement.Utilities;

namespace HRManagement.Services;

public class NavigationService : ViewModelBase, INavigationService
{
    private readonly IServiceProvider _serviceProvider;

    private ViewModelBase? _currentView;

    public ViewModelBase? CurrentView
    {
        get => _currentView;
        private set => SetProperty(ref _currentView, value);
    }

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
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
}