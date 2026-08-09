using HRManagement.Models;
using HRManagement.Services;
using HRManagement.Services.Interfaces;
using HRManagement.Utilities;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace HRManagement.ViewModels;

public class ManageAttendancesViewModel : PageViewModel
{
    private readonly SessionManager _sessionManager;
    private readonly IManageAttendancesService _manageAttendancesService;

    public ManageAttendancesViewModel(
        SessionManager sessionManager,
        IManageAttendancesService manageAttendancesService,
        AttendanceViewModel childAttendanceViewModel)
    {
        _sessionManager = sessionManager;
        _manageAttendancesService = manageAttendancesService;

        Departments = [];
        Employees = [];
        OpenCheckIns = [];

        // Injected by DI instead of being newed up here, so it shares the
        // same IAttendanceService instance (and its AttendanceChanged
        // event wiring) as the rest of the app.
        ChildAttendanceViewModel = childAttendanceViewModel;

        LoadDepartmentsCommand = new RelayCommand(_ => LoadDepartments());
        LoadEmployeesCommand = new RelayCommand(_ => LoadEmployees());
        SelectEmployeeCommand = new RelayCommand(p => SelectEmployee(p));
        ApproveOpenCheckInCommand = new RelayCommand(p => ApproveOpenCheckIn(p));
        DenyOpenCheckInCommand = new RelayCommand(p => DenyOpenCheckIn(p));

        LoadDepartments();
        LoadEmployees();
        LoadOpenCheckIns();
    }

    public override string Title => "Manage Attendances";

    public bool IsAdmin => _sessionManager.CurrentUser != null &&
                            _sessionManager.CurrentUser.Role.RoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase);

    public ObservableCollection<Department> Departments { get; }
    public ObservableCollection<ManageEmployeeRowViewModel> Employees { get; }
    public ObservableCollection<OpenCheckInRowViewModel> OpenCheckIns { get; }

    public AttendanceViewModel ChildAttendanceViewModel { get; }

    private ManageEmployeeRowViewModel? _selectedEmployee;
    public ManageEmployeeRowViewModel? SelectedEmployee
    {
        get => _selectedEmployee;
        set
        {
            SetProperty(ref _selectedEmployee, value);
            OnPropertyChanged(nameof(IsEmployeeSelected));

            if (_selectedEmployee != null)
            {
                var hireDate = _manageAttendancesService.GetEmployeeHireDate(_selectedEmployee.EmployeeId);
                ChildAttendanceViewModel.SetDisplayedEmployee(_selectedEmployee.EmployeeId, hireDate);
            }
        }
    }

    public bool IsEmployeeSelected => SelectedEmployee != null;

    private Department? _selectedDepartment;
    public Department? SelectedDepartment
    {
        get => _selectedDepartment;
        set
        {
            SetProperty(ref _selectedDepartment, value);
            LoadEmployees();
            LoadOpenCheckIns();
        }
    }

    private string _employeeFilter = string.Empty;
    public string EmployeeFilter
    {
        get => _employeeFilter;
        set { if (SetProperty(ref _employeeFilter, value)) LoadEmployees(); }
    }

    public ICommand LoadDepartmentsCommand { get; }
    public ICommand LoadEmployeesCommand { get; }
    public ICommand SelectEmployeeCommand { get; }
    public ICommand ApproveOpenCheckInCommand { get; }
    public ICommand DenyOpenCheckInCommand { get; }

    // ===== Top stat cards =====

    private string _presentRateTodayDisplay = "0%";
    public string PresentRateTodayDisplay { get => _presentRateTodayDisplay; private set => SetProperty(ref _presentRateTodayDisplay, value); }

    private string _lateRateTodayDisplay = "0%";
    public string LateRateTodayDisplay { get => _lateRateTodayDisplay; private set => SetProperty(ref _lateRateTodayDisplay, value); }

    private string _absentRateTodayDisplay = "0%";
    public string AbsentRateTodayDisplay { get => _absentRateTodayDisplay; private set => SetProperty(ref _absentRateTodayDisplay, value); }

    private int _dayOffThisMonthCount;
    public int DayOffThisMonthCount { get => _dayOffThisMonthCount; private set => SetProperty(ref _dayOffThisMonthCount, value); }

    private int _otThisMonthCount;
    public int OtThisMonthCount { get => _otThisMonthCount; private set => SetProperty(ref _otThisMonthCount, value); }

    private string _teamHoursThisMonthDisplay = "0h 0m";
    public string TeamHoursThisMonthDisplay { get => _teamHoursThisMonthDisplay; private set => SetProperty(ref _teamHoursThisMonthDisplay, value); }

    private void LoadDepartments()
    {
        Departments.Clear();

        foreach (var d in _manageAttendancesService.GetDepartmentOptions())
            Departments.Add(d);

        var defaultDepartment = _manageAttendancesService.GetDefaultDepartment([.. Departments]);
        SelectedDepartment = defaultDepartment != null
            ? Departments.FirstOrDefault(x => x.DepartmentId == defaultDepartment.DepartmentId)
            : Departments.FirstOrDefault();
    }

    private void LoadEmployees()
    {
        Employees.Clear();

        var overview = _manageAttendancesService.GetEmployeeAttendanceOverview(
            SelectedDepartment?.DepartmentId,
            EmployeeFilter);

        foreach (var row in overview.Rows)
        {
            Employees.Add(new ManageEmployeeRowViewModel
            {
                EmployeeId = row.EmployeeId,
                EmployeeName = row.EmployeeName,
                WorkingDays = row.WorkingDays,
                OnTimeDays = row.OnTimeDays,
                LateDays = row.LateDays,
                AbsentDays = row.AbsentDays,
                CheckInToday = row.CheckInToday,
                CheckOutToday = row.CheckOutToday
            });
        }

        DayOffThisMonthCount = overview.DayOffThisMonthCount;
        OtThisMonthCount = overview.OtThisMonthCount;

        var teamMinutes = overview.TeamMinutesThisMonth;
        TeamHoursThisMonthDisplay = $"{teamMinutes / 60}h {teamMinutes % 60}m";

        PresentRateTodayDisplay = $"{overview.PresentRateToday:0}%";
        LateRateTodayDisplay = $"{overview.LateRateToday:0}%";
        AbsentRateTodayDisplay = $"{overview.AbsentRateToday:0}%";
    }

    private void LoadOpenCheckIns()
    {
        OpenCheckIns.Clear();

        var rows = _manageAttendancesService.GetOpenCheckIns(SelectedDepartment?.DepartmentId);

        foreach (var row in rows)
        {
            OpenCheckIns.Add(new OpenCheckInRowViewModel
            {
                AttendanceId = row.AttendanceId,
                EmployeeId = row.EmployeeId,
                EmployeeName = row.EmployeeName,
                CheckIn = row.CheckIn,
                Status = row.Status
            });
        }
    }

    private void SelectEmployee(object? param)
    {
        if (param is ManageEmployeeRowViewModel row)
            SelectedEmployee = row;
    }

    private void ApproveOpenCheckIn(object? param)
    {
        if (param is not OpenCheckInRowViewModel row)
            return;

        _manageAttendancesService.ApproveOpenCheckIn(row.AttendanceId);

        // Refresh both grids: approving changes CheckOutToday for that
        // employee if the open check-in happened to be today's record,
        // and always removes the row from the open check-ins list.
        LoadOpenCheckIns();
        LoadEmployees();
    }

    private void DenyOpenCheckIn(object? param)
    {
        if (param is not OpenCheckInRowViewModel row)
            return;

        // Hard delete is destructive and irreversible - confirm before
        // calling the service, per the brief. Kept in the ViewModel (not
        // the service) since it's a UI-facing confirmation, not a business
        // rule; the service performs the delete unconditionally once called.
        var result = MessageBox.Show(
            $"Delete the check-in for {row.EmployeeName} on {row.CheckIn:MMM dd, yyyy}? This cannot be undone.",
            "Deny Check-In",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        _manageAttendancesService.DenyOpenCheckIn(row.AttendanceId);

        LoadOpenCheckIns();
        LoadEmployees();
    }
}

public class ManageEmployeeRowViewModel : ViewModelBase
{
    private int _employeeId;
    public int EmployeeId { get => _employeeId; set => SetProperty(ref _employeeId, value); }

    private string _employeeName = string.Empty;
    public string EmployeeName { get => _employeeName; set => SetProperty(ref _employeeName, value); }

    private int _workingDays;
    public int WorkingDays { get => _workingDays; set => SetProperty(ref _workingDays, value); }

    private int _onTimeDays;
    public int OnTimeDays { get => _onTimeDays; set => SetProperty(ref _onTimeDays, value); }

    private int _lateDays;
    public int LateDays { get => _lateDays; set => SetProperty(ref _lateDays, value); }

    private int _absentDays;
    public int AbsentDays { get => _absentDays; set => SetProperty(ref _absentDays, value); }

    private DateTime? _checkInToday;
    public DateTime? CheckInToday
    {
        get => _checkInToday;
        set
        {
            if (SetProperty(ref _checkInToday, value))
                OnPropertyChanged(nameof(CheckInTodayDisplay));
        }
    }

    private DateTime? _checkOutToday;
    public DateTime? CheckOutToday
    {
        get => _checkOutToday;
        set
        {
            if (SetProperty(ref _checkOutToday, value))
                OnPropertyChanged(nameof(CheckOutTodayDisplay));
        }
    }

    public string CheckInTodayDisplay => CheckInToday.HasValue ? CheckInToday.Value.ToString("HH:mm") : "—";
    public string CheckOutTodayDisplay => CheckOutToday.HasValue ? CheckOutToday.Value.ToString("HH:mm") : "—";

    // Initials used for the avatar-style circle in the employee table
    // (e.g. "John Doe" -> "JD"), since a reliable avatar image isn't
    // available for every employee.
    public string Initials
    {
        get
        {
            if (string.IsNullOrWhiteSpace(EmployeeName))
                return "?";

            var parts = EmployeeName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length switch
            {
                0 => "?",
                1 => parts[0][..1].ToUpperInvariant(),
                _ => (parts[0][..1] + parts[^1][..1]).ToUpperInvariant()
            };
        }
    }
}

// Row VM for the "Open Check-Ins" grid: a manual weekday check-in with no
// check-out yet, awaiting an HR/manager Approve (default 17:00 check-out)
// or Deny (hard delete) decision.
public class OpenCheckInRowViewModel : ViewModelBase
{
    private int _attendanceId;
    public int AttendanceId { get => _attendanceId; set => SetProperty(ref _attendanceId, value); }

    private int _employeeId;
    public int EmployeeId { get => _employeeId; set => SetProperty(ref _employeeId, value); }

    private string _employeeName = string.Empty;
    public string EmployeeName { get => _employeeName; set => SetProperty(ref _employeeName, value); }

    private DateTime _checkIn;
    public DateTime CheckIn
    {
        get => _checkIn;
        set
        {
            if (SetProperty(ref _checkIn, value))
            {
                OnPropertyChanged(nameof(DateDisplay));
                OnPropertyChanged(nameof(CheckInTimeDisplay));
            }
        }
    }

    private string _status = string.Empty;
    public string Status { get => _status; set => SetProperty(ref _status, value); }

    public string DateDisplay => CheckIn.ToString("MMM dd, yyyy");
    public string CheckInTimeDisplay => CheckIn.ToString("HH:mm");

    // Initials, same pattern as ManageEmployeeRowViewModel, for the avatar
    // circle in the Employee column of this grid.
    public string Initials
    {
        get
        {
            if (string.IsNullOrWhiteSpace(EmployeeName))
                return "?";

            var parts = EmployeeName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length switch
            {
                0 => "?",
                1 => parts[0][..1].ToUpperInvariant(),
                _ => (parts[0][..1] + parts[^1][..1]).ToUpperInvariant()
            };
        }
    }
}
