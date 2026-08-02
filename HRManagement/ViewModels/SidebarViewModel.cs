using System;
using System.ComponentModel;
using System.Windows.Input;
using HRManagement.Services.Interfaces;
using HRManagement.Utilities;

namespace HRManagement.ViewModels;

public class SidebarViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IAuthorizationService _authorizationService;
    private readonly ISettingService _settingService;

    public SidebarViewModel(
        INavigationService navigationService,
        IAuthorizationService authorizationService,
        ISettingService settingService)
    {
        _navigationService = navigationService;
        _authorizationService = authorizationService;
        _settingService = settingService;

        NavigateCommand = new RelayCommand(Navigate);

        _navigationService.PropertyChanged += NavigationService_OnPropertyChanged;
        _settingService.ThemeChanged += SettingService_OnThemeChanged;
        ToggleCollapseCommand = new RelayCommand(_ => IsCollapsed = !IsCollapsed);
        CurrentPageType = _navigationService.CurrentView?.GetType();
    }

    // Each nav Button's Background/Foreground come from a MultiBinding
    // (CurrentPageType, CommandParameter) through NavItemBrushConverter,
    // which - like AttendanceStatusToBrushConverter - resolves brushes live
    // via Application.Current.FindResource but only re-runs when one of its
    // OWN bound values changes. A theme swap changes neither CurrentPageType
    // nor CommandParameter, so without this the selected nav pill (and every
    // other item's text color) stayed on the old palette until the next
    // actual navigation re-touched CurrentPageType. Re-raising
    // CurrentPageType's PropertyChanged (with the same value) forces the
    // MultiBinding - and therefore the converter - to re-evaluate now.
    private void SettingService_OnThemeChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(CurrentPageType));
    }

    public ICommand NavigateCommand { get; }

    // Bound by each nav Button's Style (via a DataTrigger comparing this to
    // that button's CommandParameter) so the currently active page renders
    // as a selected pill instead of every nav item looking identical.
    private Type? _currentPageType;
    public Type? CurrentPageType
    {
        get => _currentPageType;
        private set => SetProperty(ref _currentPageType, value);
    }

    private void NavigationService_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(INavigationService.CurrentView))
            CurrentPageType = _navigationService.CurrentView?.GetType();
    }

    private void Navigate(object? parameter)
    {
        if (parameter is Type viewModelType)
        {
            _navigationService.Navigate(viewModelType);
        }
    }

    #region Collapse rail

    public ICommand ToggleCollapseCommand { get; }

    // Drives both this sidebar's own content (icon-only rail) and the host
    // window's column width (via CollapsedToWidthConverter in MainWindow.xaml),
    // so the whole layout reflows together instead of just this control
    // clipping its content.
    private bool _isCollapsed;
    public bool IsCollapsed
    {
        get => _isCollapsed;
        set
        {
            if (SetProperty(ref _isCollapsed, value))
                OnPropertyChanged(nameof(IsExpanded));
        }
    }

    // Convenience for XAML bindings that read more naturally as "expanded".
    public bool IsExpanded => !IsCollapsed;

    #endregion

    #region Search

    // Filters the nav item list by label text. Only affects sidebar
    // navigation items for now (not a global/cross-page search - that
    // lives on the TopBar search box instead).
    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    #endregion

    #region Authorization

    public bool IsAdmin => _authorizationService.IsAdmin;

    public bool IsManager => _authorizationService.IsManager;

    public bool IsEmployee => _authorizationService.IsEmployee;

    #endregion
}
