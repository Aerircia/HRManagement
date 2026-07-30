using HRManagement.Models;
using HRManagement.Repositories;
using HRManagement.Repositories.Interfaces;
using HRManagement.Services;
using HRManagement.Services.Interfaces;
using HRManagement.Utilities;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace HRManagement.ViewModels;

public class DashboardViewModel : PageViewModel
{
    private readonly SessionManager _sessionManager;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAttendanceService _attendanceService;
    private readonly IRequestService _requestService;
    private readonly IEmployeeEvaluationRepository _evaluationRepository;
    private readonly IAnnouncementRepository _announcementRepository;
    private readonly IDashboardService _dashboardService;
    private readonly INavigationService _navigationService;
    private readonly IAuthorizationService _authService;

    public override string Title => "Dashboard";

    public DashboardViewModel(
        SessionManager sessionManager,
        IEmployeeRepository employeeRepository,
        IAttendanceService attendanceService,
        IRequestService requestService,
        IEmployeeEvaluationRepository evaluationRepository,
        IAnnouncementRepository announcementRepository,
        IDashboardService dashboardService,
        INavigationService navigationService,
        IAuthorizationService authService)
    {
        _sessionManager = sessionManager;
        _employeeRepository = employeeRepository;
        _attendanceService = attendanceService;
        _requestService = requestService;
        _evaluationRepository = evaluationRepository;
        _announcementRepository = announcementRepository;
        _dashboardService = dashboardService;
        _navigationService = navigationService;
        _authService = authService;

        Announcements = [];
        PendingRequests = [];

        // LiveCharts Fix: Initialize collections once
        PayoutSeries = [];
        _myPayoutValues = [];
        _teamPayoutValues = [];

        GoToAttendanceCommand = new RelayCommand(_ => _navigationService.Navigate<AttendanceViewModel>());
        GoToRequestsCommand = new RelayCommand(_ => _navigationService.Navigate<RequestsViewModel>());
        GoToSalaryCommand = new RelayCommand(_ => _navigationService.Navigate<SalaryViewModel>());
        CheckInCommand = new RelayCommand(_ => CheckIn(), _ => CanCheckIn());
        CheckOutCommand = new RelayCommand(_ => CheckOut(), _ => CanCheckOut());
        GoToManageAttendancesCommand = new RelayCommand(_ => _navigationService.Navigate<ManageAttendancesViewModel>());
        GoToManageRequestsCommand = new RelayCommand(_ => _navigationService.Navigate<ManageRequestsViewModel>());

        Load();
    }

    // ===== Role scope =====
    public bool IsAdmin => _authService.IsAdmin;
    public bool IsManager => _authService.IsManager;
    public bool IsEmployee => _authService.IsEmployee;

    // ===== Header =====
    public string UserName => _sessionManager.CurrentUser?.Employee.FullName ?? "";
    public string GreetingMessage
    {
        get
        {
            var hour = DateTime.Now.Hour;
            var timeGreeting = hour switch
            {
                < 12 => "Good morning",
                < 17 => "Good afternoon",
                _ => "Good evening"
            };
            return $"{timeGreeting}, {UserName}";
        }
    }
    public string ScopeLabel { get; private set; } = string.Empty;

    // ===== Quick actions (Employee) =====
    public ICommand GoToAttendanceCommand { get; }
    public ICommand GoToRequestsCommand { get; }
    public ICommand GoToSalaryCommand { get; }
    public ICommand CheckInCommand { get; }
    public ICommand CheckOutCommand { get; }
    public ICommand GoToManageRequestsCommand { get; }
    public ICommand GoToManageAttendancesCommand { get; }

    // ===== Employee: Personal Stats =====
    private int _myLoggedHoursThisMonth;
    public int MyLoggedHoursThisMonth { get => _myLoggedHoursThisMonth; private set => SetProperty(ref _myLoggedHoursThisMonth, value); }

    private int _myTargetHoursThisMonth;
    public int MyTargetHoursThisMonth { get => _myTargetHoursThisMonth; private set => SetProperty(ref _myTargetHoursThisMonth, value); }

    public double MyHoursProgressPercentage =>
        MyTargetHoursThisMonth <= 0 ? 0 : Math.Min(100, (double)MyLoggedHoursThisMonth / MyTargetHoursThisMonth * 100);

    private string _todayAttendanceStatus = "Not checked in";
    public string TodayAttendanceStatus { get => _todayAttendanceStatus; private set => SetProperty(ref _todayAttendanceStatus, value); }

    private string _latestEvaluationDisplay = "No evaluations on file";
    public string LatestEvaluationDisplay { get => _latestEvaluationDisplay; private set => SetProperty(ref _latestEvaluationDisplay, value); }

    private string _latestEvaluationAmountDisplay = "—";
    public string LatestEvaluationAmountDisplay { get => _latestEvaluationAmountDisplay; private set => SetProperty(ref _latestEvaluationAmountDisplay, value); }

    // ===== Manager/Admin: Team Stats =====
    private int _teamLoggedHoursThisMonth;
    public int TeamLoggedHoursThisMonth { get => _teamLoggedHoursThisMonth; private set => SetProperty(ref _teamLoggedHoursThisMonth, value); }

    private int _teamTargetHoursThisMonth;
    public int TeamTargetHoursThisMonth { get => _teamTargetHoursThisMonth; private set => SetProperty(ref _teamTargetHoursThisMonth, value); }

    private int _todayCheckedIn;
    public int TodayCheckedIn { get => _todayCheckedIn; private set => SetProperty(ref _todayCheckedIn, value); }

    private int _todayTotalEmployees;
    public int TodayTotalEmployees { get => _todayTotalEmployees; private set => SetProperty(ref _todayTotalEmployees, value); }

    public ObservableCollection<RequestFormSummary> PendingRequests { get; }

    private int _pendingRequestsCount;
    public int PendingRequestsCount { get => _pendingRequestsCount; private set => SetProperty(ref _pendingRequestsCount, value); }

    // ===== Dual-Line Payout Chart =====
    public ObservableCollection<ISeries> PayoutSeries { get; }
    private readonly ObservableCollection<decimal> _myPayoutValues;
    private readonly ObservableCollection<decimal> _teamPayoutValues;

    private static readonly string[] MonthLabels = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

    private Axis[] _payoutXAxes = [new Axis { Labels = MonthLabels }];
    public Axis[] PayoutXAxes { get => _payoutXAxes; private set => SetProperty(ref _payoutXAxes, value); }

    private Axis[] _payoutYAxes = [new Axis { Labeler = v => v.ToString("N0") }];
    public Axis[] PayoutYAxes { get => _payoutYAxes; private set => SetProperty(ref _payoutYAxes, value); }

    // ===== Announcements =====
    public ObservableCollection<Announcement> Announcements { get; }

    // ===== Loading =====
    private void Load()
    {
        var currentUser = _sessionManager.CurrentUser;
        if (currentUser == null)
            return;

        LoadAnnouncements();
        InitializeChartSeries();

        // Everyone runs personal view
        LoadEmployeeView(currentUser.Employee);

        // Scope expansions
        if (IsAdmin)
        {
            ScopeLabel = "Organization overview";
            var employeeIds = _employeeRepository.GetAll().Select(e => e.EmployeeId).ToList();
            LoadManagementView(employeeIds);
        }
        else if (IsManager)
        {
            ScopeLabel = "Department overview";
            var employeeIds = _employeeRepository
                .GetByDepartment(currentUser.Employee.DepartmentId)
                .Select(e => e.EmployeeId)
                .ToList();
            LoadManagementView(employeeIds);
        }

        OnPropertyChanged(nameof(IsAdmin));
        OnPropertyChanged(nameof(IsManager));
        OnPropertyChanged(nameof(IsEmployee));
    }

    private void InitializeChartSeries()
    {
        PayoutSeries.Clear();

        // LiveCharts renders through SkiaSharp, which is entirely outside
        // WPF's resource system - StaticResource/DynamicResource bindings
        // in XAML never reach it. Colors have to be resolved from the
        // current theme's brush dictionary here in code and re-applied
        // any time the theme could have changed (see RefreshChartTheme).
        var primaryColor = ResolveThemeColor("PrimaryBrush", 0x3E, 0x63, 0xDD);
        var accentColor = ResolveThemeColor("AccentBrush", 0xFF, 0x99, 0x00);
        var axisTextColor = ResolveThemeColor("SecondaryTextBrush", 0x8A, 0x93, 0xB8);

        // Line 1: Personal Payout
        PayoutSeries.Add(new LineSeries<decimal>
        {
            Name = "My Payout",
            Values = _myPayoutValues,
            GeometrySize = 10,
            LineSmoothness = 0.6,
            Fill = null, // Transparent fill for clear multi-line view
            Stroke = new SolidColorPaint(primaryColor) { StrokeThickness = 3 },
            GeometryStroke = new SolidColorPaint(primaryColor) { StrokeThickness = 3 }
        });

        // Line 2: Team/Org Payout
        if (IsManager)
        {
            PayoutSeries.Add(new LineSeries<decimal>
            {
                Name = IsAdmin ? "Org Total Payout" : "Dept Total Payout",
                Values = _teamPayoutValues,
                GeometrySize = 10,
                LineSmoothness = 0.6,
                Fill = null,
                Stroke = new SolidColorPaint(accentColor) { StrokeThickness = 3 },
                GeometryStroke = new SolidColorPaint(accentColor) { StrokeThickness = 3 }
            });
        }

        // Axis label color also has to be set explicitly - Labeler only
        // controls the text format, not its paint, so it silently stayed
        // on LiveCharts' own default (dark) color regardless of theme.
        PayoutXAxes = new Axis[]
        {
            new Axis { Labels = MonthLabels, LabelsPaint = new SolidColorPaint(axisTextColor) }
        };

        PayoutYAxes = new Axis[]
        {
            new Axis { Labeler = v => v.ToString("N0"), LabelsPaint = new SolidColorPaint(axisTextColor) }
        };
    }

    // Resolves a themed WPF brush's color into a SkiaSharp color LiveCharts
    // can consume. Falls back to a hardcoded color if the key can't be
    // found (e.g. design-time), so the chart never throws.
    private static SKColor ResolveThemeColor(string resourceKey, byte fallbackR, byte fallbackG, byte fallbackB)
    {
        if (Application.Current?.TryFindResource(resourceKey) is SolidColorBrush brush)
        {
            var c = brush.Color;
            return new SKColor(c.R, c.G, c.B, c.A);
        }

        return new SKColor(fallbackR, fallbackG, fallbackB);
    }

    // Re-resolves chart colors from the current theme and rebuilds the
    // series/axes in place. DashboardViewModel is registered AddTransient
    // (see App.xaml.cs), so simply navigating away from and back to
    // Dashboard after a theme toggle already picks up new colors via a
    // fresh instance - this method exists for the case where the view is
    // still on screen (or as a hook for wiring to a theme-changed event).
    public void RefreshChartTheme()
    {
        InitializeChartSeries();
    }

    private void LoadEmployeeView(Employee employee)
    {
        var now = DateTime.Now;

        var summary = _attendanceService.GetMonthSummary(employee.EmployeeId, employee.HireDate.Date, now);
        MyLoggedHoursThisMonth = (summary.TotalOnTime + summary.TotalLate) * 8;

        var workingDaysThisMonth = Enumerable.Range(1, DateTime.DaysInMonth(now.Year, now.Month))
            .Select(d => new DateTime(now.Year, now.Month, d))
            .Count(d => d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && d <= now.Date);
        MyTargetHoursThisMonth = workingDaysThisMonth * 8;

        var todayRecord = _attendanceService
            .BuildMonth(employee.EmployeeId, employee.HireDate.Date, new DateTime(now.Year, now.Month, 1))
            .FirstOrDefault(d => d.Date.Date == now.Date);
        TodayAttendanceStatus = todayRecord?.Status switch
        {
            "Present" => "Checked in — on time",
            "Late" => "Checked in — late",
            "OT" => "Checked in — overtime",
            "Absent" => "Not checked in",
            "Weekend" => "Weekend",
            _ => "Not checked in"
        };

        RaiseCheckCommands();

        var latestEvaluation = _evaluationRepository.GetLatestByEmployeeId(employee.EmployeeId);
        if (latestEvaluation != null)
        {
            var type = latestEvaluation.EvaluationType ?? latestEvaluation.BonusType ?? "Evaluation";
            LatestEvaluationDisplay = $"{type} · {latestEvaluation.BonusDate:MMM dd, yyyy}";
            LatestEvaluationAmountDisplay = latestEvaluation.Amount.ToString("C0");
        }
        else
        {
            LatestEvaluationDisplay = "No evaluations on file";
            LatestEvaluationAmountDisplay = "—";
        }

        // Populate personal payout line safely
        var points = _dashboardService.GetMonthlyPayoutTotals([employee.EmployeeId], now.Year);
        _myPayoutValues.Clear();
        foreach (var p in points) _myPayoutValues.Add(p.Total);
    }

    private void LoadManagementView(List<int> employeeIds)
    {
        var now = DateTime.Now;

        var stat = _dashboardService.GetTodayAttendanceStat(employeeIds);
        TodayCheckedIn = stat.CheckedIn;
        TodayTotalEmployees = stat.TotalEmployees;

        var allRequests = IsAdmin
            ? _requestService.GetAllRequests()
            : _requestService.GetRequestsByDepartment(_sessionManager.CurrentUser!.Employee.DepartmentId);

        var pending = allRequests.Where(r => r.Status == "Pending").OrderByDescending(r => r.SubmitDate).ToList();

        PendingRequests.Clear();
        foreach (var request in pending.Take(5))
            PendingRequests.Add(request);

        PendingRequestsCount = pending.Count;

        var totalLogged = 0;
        var totalTarget = 0;
        var workingDaysThisMonth = Enumerable.Range(1, DateTime.DaysInMonth(now.Year, now.Month))
            .Select(d => new DateTime(now.Year, now.Month, d))
            .Count(d => d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && d <= now.Date);

        foreach (var employeeId in employeeIds)
        {
            var employee = _employeeRepository.GetById(employeeId);
            if (employee == null) continue;

            var summary = _attendanceService.GetMonthSummary(employeeId, employee.HireDate.Date, now);
            totalLogged += (summary.TotalOnTime + summary.TotalLate) * 8;
            totalTarget += workingDaysThisMonth * 8;
        }

        TeamLoggedHoursThisMonth = totalLogged;
        TeamTargetHoursThisMonth = totalTarget;

        // Populate team payout line safely
        var points = _dashboardService.GetMonthlyPayoutTotals(employeeIds, now.Year);
        _teamPayoutValues.Clear();
        foreach (var p in points) _teamPayoutValues.Add(p.Total);
    }

    private void LoadAnnouncements()
    {
        Announcements.Clear();
        foreach (var announcement in _announcementRepository.GetActive())
            Announcements.Add(announcement);
    }
    private bool CanCheckIn()
    {
        var employee = _sessionManager.CurrentUser?.Employee;
        if (employee == null)
            return false;

        return _attendanceService.CanCheckIn(
            employee.EmployeeId,
            employee.HireDate.Date);
    }

    private bool CanCheckOut()
    {
        var employee = _sessionManager.CurrentUser?.Employee;
        if (employee == null)
            return false;

        return _attendanceService.CanCheckOut(
            employee.EmployeeId,
            employee.HireDate.Date);
    }

    private void CheckIn()
    {
        if (_sessionManager.CurrentUser?.Employee is { } emp)
        {
            _attendanceService.CheckIn(emp.EmployeeId);
            LoadEmployeeView(emp);
        }
    }

    private void CheckOut()
    {
        if (_sessionManager.CurrentUser?.Employee is { } emp)
        {
            _attendanceService.CheckOut(emp.EmployeeId);
            LoadEmployeeView(emp);
        }
    }

    private void RaiseCheckCommands()
    {
        (CheckInCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (CheckOutCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }
}