using System.Collections.ObjectModel;
using HRManagement.Models;
using HRManagement.Models.Dashboard;
using HRManagement.Services.Interfaces;
using HRManagement.Utilities;

namespace HRManagement.ViewModels;

public class DashboardViewModel : PageViewModel
{
    private readonly IDashboardService _dashboardService;
    private readonly IAuthorizationService _authorizationService;
    private readonly INavigationService _navigationService;

    public DashboardViewModel(
        IDashboardService dashboardService,
        IAuthorizationService authorizationService,
        INavigationService navigationService)
    {
        _dashboardService = dashboardService;
        _authorizationService = authorizationService;
        _navigationService = navigationService;

        CheckInCommand = new RelayCommand(_ => CheckIn(), _ => CanCheckIn);
        CheckOutCommand = new RelayCommand(_ => CheckOut(), _ => CanCheckOut);
        SendRequestCommand = new RelayCommand(_ => _navigationService.Navigate<RequestsViewModel>());
        ViewPayslipCommand = new RelayCommand(_ => _navigationService.Navigate<SalaryViewModel>());

        Load();
    }

    #region Bound Properties

    private ObservableCollection<WeekDayItem> _weekDays = [];
    public ObservableCollection<WeekDayItem> WeekDays
    {
        get => _weekDays;
        private set => SetProperty(ref _weekDays, value);
    }

    private ObservableCollection<WeeklyHourPoint> _weeklyHours = [];
    public ObservableCollection<WeeklyHourPoint> WeeklyHours
    {
        get => _weeklyHours;
        private set => SetProperty(ref _weeklyHours, value);
    }

    private ObservableCollection<Announcement> _announcements = [];
    public ObservableCollection<Announcement> Announcements
    {
        get => _announcements;
        private set => SetProperty(ref _announcements, value);
    }

    private EmployeeAnalytics _employeeAnalytics = new();
    public EmployeeAnalytics EmployeeAnalytics
    {
        get => _employeeAnalytics;
        private set => SetProperty(ref _employeeAnalytics, value);
    }

    private ManagerAnalytics? _managerAnalytics;
    public ManagerAnalytics? ManagerAnalytics
    {
        get => _managerAnalytics;
        private set => SetProperty(ref _managerAnalytics, value);
    }

    private Attendance? _todayAttendance;
    public Attendance? TodayAttendance
    {
        get => _todayAttendance;
        private set => SetProperty(ref _todayAttendance, value);
    }

    /// <summary>Second analytics row is only shown for managers/admins.</summary>
    public bool ShowManagerAnalytics =>
        _authorizationService.IsManager || _authorizationService.IsAdmin;

    private bool _canCheckIn;
    public bool CanCheckIn
    {
        get => _canCheckIn;
        private set => SetProperty(ref _canCheckIn, value);
    }

    private bool _canCheckOut;
    public bool CanCheckOut
    {
        get => _canCheckOut;
        private set => SetProperty(ref _canCheckOut, value);
    }

    #endregion

    #region Commands

    public RelayCommand CheckInCommand { get; }
    public RelayCommand CheckOutCommand { get; }
    public RelayCommand SendRequestCommand { get; }
    public RelayCommand ViewPayslipCommand { get; }

    public override string Title => "Dashboard";

    #endregion

    #region Actions

    private void CheckIn()
    {
        _dashboardService.CheckIn();
        Load();
    }

    private void CheckOut()
    {
        _dashboardService.CheckOut();
        Load();
    }

    #endregion

    #region Loading

    /// <summary>
    /// Reloads all dashboard data in one shot (per the design summary:
    /// "Refresh logic should simply reload DashboardData instead of
    /// maintaining duplicated loading paths").
    /// </summary>
    private void Load()
    {
        var data = _dashboardService.LoadDashboard();

        WeekDays = data.WeekDays;
        WeeklyHours = data.WeeklyHours;
        Announcements = data.Announcements;
        EmployeeAnalytics = data.EmployeeAnalytics;
        ManagerAnalytics = data.ManagerAnalytics;
        TodayAttendance = data.TodayAttendance;

        CanCheckIn = _dashboardService.CanCheckIn();
        CanCheckOut = _dashboardService.CanCheckOut();

        OnPropertyChanged(nameof(ShowManagerAnalytics));

        CheckInCommand.RaiseCanExecuteChanged();
        CheckOutCommand.RaiseCanExecuteChanged();
    }

    #endregion
}