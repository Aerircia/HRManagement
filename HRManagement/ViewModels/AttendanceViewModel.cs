using System.Collections.ObjectModel;
using System.Windows.Input;
using HRManagement.Models;
using HRManagement.Services;
using HRManagement.Services.Interfaces;
using HRManagement.Utilities;

namespace HRManagement.ViewModels;

public class AttendanceViewModel : PageViewModel
{
    private readonly IAttendanceService _attendanceService;
    private readonly SessionManager _sessionManager;

    private DateTime? _hireDate;

    public AttendanceViewModel(SessionManager sessionManager, IAttendanceService attendanceService)
    {
        _sessionManager = sessionManager;
        _attendanceService = attendanceService;

        var employee = _sessionManager.CurrentUser?.Employee;
        _hireDate = employee?.HireDate.Date;
        CurrentMonth = StartOfMonth(_hireDate);

        Days = new ObservableCollection<AttendanceDayViewModel>();

        _prevCommand = new RelayCommand(_ => ChangeMonth(-1));
        _nextCommand = new RelayCommand(_ => ChangeMonth(1));
        _checkInCommand = new RelayCommand(_ => CheckIn(), _ => CanCheckIn());
        _checkOutCommand = new RelayCommand(_ => CheckOut(), _ => CanCheckOut());

        _sessionManager.OnUserChanged += (_, _) => OnUserChanged();
        _attendanceService.AttendanceChanged += OnAttendanceChanged;

        Refresh();
    }

    public override string Title => "Attendance";

    private DateTime _currentMonth;
    public DateTime CurrentMonth
    {
        get => _currentMonth;
        set => SetProperty(ref _currentMonth, value);
    }

    public ObservableCollection<AttendanceDayViewModel> Days { get; }

    private readonly RelayCommand _prevCommand;
    private readonly RelayCommand _nextCommand;
    private readonly RelayCommand _checkInCommand;
    private readonly RelayCommand _checkOutCommand;

    public ICommand PrevMonthCommand => _prevCommand;
    public ICommand NextMonthCommand => _nextCommand;
    public ICommand CheckInCommand => _checkInCommand;
    public ICommand CheckOutCommand => _checkOutCommand;

    private int _totalDaysWorked;
    public int TotalDaysWorked { get => _totalDaysWorked; set => SetProperty(ref _totalDaysWorked, value); }

    private int _totalOnTime;
    public int TotalOnTime { get => _totalOnTime; set => SetProperty(ref _totalOnTime, value); }

    private int _totalLate;
    public int TotalLate { get => _totalLate; set => SetProperty(ref _totalLate, value); }

    private int _totalAbsent;
    public int TotalAbsent { get => _totalAbsent; set => SetProperty(ref _totalAbsent, value); }

    private static DateTime StartOfMonth(DateTime? date) =>
        date.HasValue ? new DateTime(date.Value.Year, date.Value.Month, 1) : new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

    private void ChangeMonth(int delta)
    {
        var candidate = CurrentMonth.AddMonths(delta);

        if (_hireDate.HasValue)
        {
            var hireMonth = StartOfMonth(_hireDate);
            if (candidate < hireMonth)
                candidate = hireMonth;
        }

        CurrentMonth = candidate;
        Refresh();
    }

    private void Refresh()
    {
        var employeeId = _sessionManager.CurrentUser?.Employee.EmployeeId;
        var hire = _hireDate ?? DateTime.Today;

        Days.Clear();

        if (employeeId != null)
        {
            foreach (var day in _attendanceService.BuildMonth(employeeId.Value, hire, CurrentMonth))
                Days.Add(new AttendanceDayViewModel(day));

            var summary = _attendanceService.GetMonthSummary(employeeId.Value, hire, CurrentMonth);
            TotalDaysWorked = summary.TotalDaysWorked;
            TotalOnTime = summary.TotalOnTime;
            TotalLate = summary.TotalLate;
            TotalAbsent = summary.TotalAbsent;
        }

        _checkInCommand.RaiseCanExecuteChanged();
        _checkOutCommand.RaiseCanExecuteChanged();
    }

    private bool CanCheckIn()
    {
        var employeeId = _sessionManager.CurrentUser?.Employee.EmployeeId;
        return employeeId != null && _hireDate.HasValue && _attendanceService.CanCheckIn(employeeId.Value, _hireDate.Value);
    }

    private bool CanCheckOut()
    {
        var employeeId = _sessionManager.CurrentUser?.Employee.EmployeeId;
        return employeeId != null && _hireDate.HasValue && _attendanceService.CanCheckOut(employeeId.Value, _hireDate.Value);
    }

    private void CheckIn()
    {
        var employeeId = _sessionManager.CurrentUser?.Employee.EmployeeId;
        if (employeeId == null)
            return;

        _attendanceService.CheckIn(employeeId.Value);
        Refresh();
    }

    private void CheckOut()
    {
        var employeeId = _sessionManager.CurrentUser?.Employee.EmployeeId;
        if (employeeId == null)
            return;

        _attendanceService.CheckOut(employeeId.Value);
        Refresh();
    }

    private void OnUserChanged()
    {
        var employee = _sessionManager.CurrentUser?.Employee;
        _hireDate = employee?.HireDate.Date;
        CurrentMonth = StartOfMonth(_hireDate);

        Refresh();
    }

    // Replaces the old cross-viewmodel App.Services lookup + RefreshIfEmployee call
    // that ManageAttendancesViewModel used to reach into this ViewModel directly.
    // Both ViewModels now just listen to the same service-level event independently.
    private void OnAttendanceChanged(object? sender, AttendanceChangedEventArgs e)
    {
        var employeeId = _sessionManager.CurrentUser?.Employee.EmployeeId;
        if (employeeId != e.EmployeeId)
            return;

        if (e.Date.Year == CurrentMonth.Year && e.Date.Month == CurrentMonth.Month)
            Refresh();
    }

    public class AttendanceDayViewModel
    {
        public AttendanceDayViewModel(AttendanceDayModel model)
        {
            Date = model.Date;
            IsCurrentMonth = model.IsCurrentMonth;
            IsBeforeHireDate = model.IsBeforeHireDate;
            Status = model.Status;
            LatenessMinutes = model.LatenessMinutes;
        }

        public DateTime Date { get; }
        public bool IsCurrentMonth { get; }
        public bool IsBeforeHireDate { get; }
        public string Status { get; }
        public int? LatenessMinutes { get; }

        public bool ShouldShowTimes => IsCurrentMonth && !IsBeforeHireDate && !string.IsNullOrEmpty(Status);

        public string DisplayStatus =>
            Status == "Late" && LatenessMinutes is > 0
                ? $"Late · {LatenessMinutes}m"
                : Status;
    }
}