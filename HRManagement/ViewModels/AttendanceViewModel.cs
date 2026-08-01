using HRManagement.Models;
using HRManagement.Services;
using HRManagement.Services.Interfaces;
using HRManagement.Utilities;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace HRManagement.ViewModels;

// Thin presentation layer over IAttendanceService. All day-grid building,
// status/lateness resolution, and summary math live in AttendanceService -
// this class only tracks UI state (which employee/month is displayed) and
// exposes bindable collections/commands.
public class AttendanceViewModel : PageViewModel
{
    private readonly IAttendanceService _attendanceService;
    private readonly SessionManager _sessionManager;
    private readonly ISettingService? _settingService;

    private int? _displayEmployeeId;
    private DateTime? _displayHireDate;

    public AttendanceViewModel(
        SessionManager sessionManager,
        IAttendanceService attendanceService,
        ISettingService? settingService = null)
    {
        _sessionManager = sessionManager;
        _attendanceService = attendanceService;
        _settingService = settingService;
        _attendanceService.AttendanceChanged += AttendanceService_OnAttendanceChanged;
        _sessionManager.OnUserChanged += SessionManager_OnUserChanged;

        if (_settingService != null)
            _settingService.ThemeChanged += SettingService_OnThemeChanged;

        Days = [];

        _prevCommand = new RelayCommand(_ => ChangeMonth(-1));
        _nextCommand = new RelayCommand(_ => ChangeMonth(1));
        _checkInCommand = new RelayCommand(_ => CheckIn(), _ => CanCheckIn());
        _checkOutCommand = new RelayCommand(_ => CheckOut(), _ => CanCheckOut());

        CurrentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        Refresh();
    }

    public override string Title => "Attendance";

    private DateTime _currentMonth;
    public DateTime CurrentMonth { get => _currentMonth; set => SetProperty(ref _currentMonth, value); }

    public ObservableCollection<AttendanceDayViewModel> Days { get; }

    private readonly RelayCommand _prevCommand;
    private readonly RelayCommand _nextCommand;
    private readonly RelayCommand _checkInCommand;
    private readonly RelayCommand _checkOutCommand;

    public ICommand PrevMonthCommand => _prevCommand;
    public ICommand NextMonthCommand => _nextCommand;
    public ICommand CheckInCommand => _checkInCommand;
    public ICommand CheckOutCommand => _checkOutCommand;

    private int? EmployeeId => _displayEmployeeId ?? _sessionManager.CurrentUser?.Employee.EmployeeId;

    private DateTime? HireDate => _displayHireDate ?? _sessionManager.CurrentUser?.Employee.HireDate.Date;

    // Lets a host (e.g. ManageAttendancesViewModel) point this calendar at
    // a specific employee instead of the logged-in session user.
    public void SetDisplayedEmployee(int? employeeId, DateTime? hireDate)
    {
        _displayEmployeeId = employeeId;
        _displayHireDate = hireDate;

        CurrentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        if (HireDate.HasValue)
        {
            var hireMonth = new DateTime(HireDate.Value.Year, HireDate.Value.Month, 1);

            if (CurrentMonth < hireMonth)
                CurrentMonth = hireMonth;
        }

        Refresh();
    }

    private void ChangeMonth(int delta)
    {
        var candidate = CurrentMonth.AddMonths(delta);

        if (HireDate.HasValue)
        {
            var hireMonth = new DateTime(HireDate.Value.Year, HireDate.Value.Month, 1);
            if (candidate < hireMonth)
                candidate = hireMonth;
        }

        CurrentMonth = candidate;
        Refresh();
    }

    private void Refresh()
    {
        var empId = EmployeeId;
        var hire = HireDate;

        if (!empId.HasValue || !hire.HasValue)
        {
            Days.Clear();
            ClearSummary();
            RaiseCanExecuteChanged();
            return;
        }

        var monthStart = new DateTime(CurrentMonth.Year, CurrentMonth.Month, 1);

        var built = _attendanceService.BuildMonth(empId.Value, hire.Value, monthStart);
        SyncDays(built);

        var summary = _attendanceService.GetMonthSummary(empId.Value, hire.Value, CurrentMonth);
        ApplySummary(summary);

        RaiseCanExecuteChanged();
    }

    // Reuses existing AttendanceDayViewModel instances where possible so
    // bindings don't churn every refresh; only resizes when the grid's
    // cell count actually changes (5-row vs 6-row month).
    private void SyncDays(List<AttendanceDayModel> built)
    {
        if (Days.Count != built.Count)
        {
            Days.Clear();
            foreach (var model in built)
                Days.Add(new AttendanceDayViewModel(model));
            return;
        }

        for (var i = 0; i < built.Count; i++)
            Days[i].UpdateFrom(built[i]);
    }

    private void ClearSummary()
    {
        TotalDaysWorked = 0;
        TotalOnTime = 0;
        TotalLate = 0;
        TotalLateMinutes = 0;
        TotalAbsent = 0;
        TotalOtDays = 0;
        TotalDayOffDays = 0;
        TotalWorkedMinutes = 0;
    }

    private void ApplySummary(AttendanceMonthSummary summary)
    {
        TotalDaysWorked = summary.TotalDaysWorked;
        TotalOnTime = summary.TotalOnTime;
        TotalLate = summary.TotalLate;
        TotalLateMinutes = summary.TotalLateMinutes;
        TotalAbsent = summary.TotalAbsent;
        TotalOtDays = summary.TotalOtDays;
        TotalDayOffDays = summary.TotalDayOffDays;
        TotalWorkedMinutes = summary.TotalWorkedMinutes;
    }

    private bool CanCheckIn()
    {
        var empId = EmployeeId;
        var hire = HireDate;
        return empId.HasValue && hire.HasValue && _attendanceService.CanCheckIn(empId.Value, hire.Value);
    }

    private bool CanCheckOut()
    {
        var empId = EmployeeId;
        var hire = HireDate;
        return empId.HasValue && hire.HasValue && _attendanceService.CanCheckOut(empId.Value, hire.Value);
    }

    private void CheckIn()
    {
        if (!EmployeeId.HasValue) return;
        _attendanceService.CheckIn(EmployeeId.Value);
        // Refresh happens via AttendanceChanged event, but call directly too
        // in case the repository event doesn't fire synchronously for this employee/month.
        Refresh();
    }

    private void CheckOut()
    {
        if (!EmployeeId.HasValue) return;
        _attendanceService.CheckOut(EmployeeId.Value);
        Refresh();
    }

    private void RaiseCanExecuteChanged()
    {
        _checkInCommand.RaiseCanExecuteChanged();
        _checkOutCommand.RaiseCanExecuteChanged();
    }

    private void SettingService_OnThemeChanged(object? sender, EventArgs e)
    {
        foreach (var day in Days)
            day.RefreshThemeDependentDisplay();
    }

    private void SessionManager_OnUserChanged(object? sender, EventArgs e)
    {
        // Only reset to the session user's own calendar if nobody has
        // explicitly pinned this view to another employee (e.g. admin
        // browsing via Manage Attendances).
        if (_displayEmployeeId.HasValue)
            return;

        CurrentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        Refresh();
    }

    private void AttendanceService_OnAttendanceChanged(object? sender, AttendanceChangedEventArgs e)
    {
        if (EmployeeId != e.EmployeeId)
            return;

        if (e.Date.Year == CurrentMonth.Year && e.Date.Month == CurrentMonth.Month)
            Refresh();
    }

    private int _totalDaysWorked;
    public int TotalDaysWorked { get => _totalDaysWorked; private set => SetProperty(ref _totalDaysWorked, value); }

    private int _totalLate;
    public int TotalLate { get => _totalLate; private set => SetProperty(ref _totalLate, value); }

    private int _totalLateMinutes;
    public int TotalLateMinutes { get => _totalLateMinutes; private set => SetProperty(ref _totalLateMinutes, value); }

    private int _totalAbsent;
    public int TotalAbsent { get => _totalAbsent; private set => SetProperty(ref _totalAbsent, value); }

    private int _totalOnTime;
    public int TotalOnTime { get => _totalOnTime; private set => SetProperty(ref _totalOnTime, value); }

    private int _totalOtDays;
    public int TotalOtDays { get => _totalOtDays; private set => SetProperty(ref _totalOtDays, value); }

    private int _totalDayOffDays;
    public int TotalDayOffDays { get => _totalDayOffDays; private set => SetProperty(ref _totalDayOffDays, value); }

    private int _totalWorkedMinutes;
    public int TotalWorkedMinutes
    {
        get => _totalWorkedMinutes;
        private set
        {
            if (SetProperty(ref _totalWorkedMinutes, value))
                OnPropertyChanged(nameof(TotalHoursThisMonthDisplay));
        }
    }

    // "142h 30m" style display for the new "Total Hours This Month" card.
    public string TotalHoursThisMonthDisplay =>
        $"{TotalWorkedMinutes / 60}h {TotalWorkedMinutes % 60}m";
}
