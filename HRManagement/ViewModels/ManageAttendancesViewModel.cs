using System.Collections.ObjectModel;
using System.Windows.Input;
using HRManagement.Models;
using HRManagement.Repositories.Interfaces;
using HRManagement.Services;
using HRManagement.Services.Interfaces;
using HRManagement.Utilities;

namespace HRManagement.ViewModels;

public class ManageAttendancesViewModel : PageViewModel
{
    private readonly SessionManager _sessionManager;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IAttendanceService _attendanceService;

    public ManageAttendancesViewModel(
        SessionManager sessionManager,
        IEmployeeRepository employeeRepository,
        IDepartmentRepository departmentRepository,
        IAttendanceService attendanceService)
    {
        _sessionManager = sessionManager;
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _attendanceService = attendanceService;

        Departments = new ObservableCollection<Department>();
        Employees = new ObservableCollection<ManageEmployeeRowViewModel>();

        SelectOtCommand = new RelayCommand(p => OpenOtForm(p as ManageEmployeeRowViewModel));
        SaveOtCommand = new RelayCommand(_ => SaveOt());
        CancelOtCommand = new RelayCommand(_ => CloseOtForm());

        LoadDepartments();
        LoadEmployees();

        _attendanceService.AttendanceChanged += (_, e) => RefreshEmployeeRow(e.EmployeeId);
    }

    public override string Title => "Manage Attendances";

    public bool IsAdmin =>
        _sessionManager.CurrentUser != null &&
        _sessionManager.CurrentUser.Role.RoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase);

    public ObservableCollection<Department> Departments { get; }
    public ObservableCollection<ManageEmployeeRowViewModel> Employees { get; }

    private Department? _selectedDepartment;
    public Department? SelectedDepartment
    {
        get => _selectedDepartment;
        set
        {
            if (SetProperty(ref _selectedDepartment, value))
                LoadEmployees();
        }
    }

    public ICommand SelectOtCommand { get; }
    public ICommand SaveOtCommand { get; }
    public ICommand CancelOtCommand { get; }

    // OT modal (replaces OtDialog window)

    private bool _isOtFormOpen;
    public bool IsOtFormOpen { get => _isOtFormOpen; set => SetProperty(ref _isOtFormOpen, value); }

    private ManageEmployeeRowViewModel? _otTargetRow;

    private DateTime? _otDate = DateTime.Today;
    public DateTime? OtDate { get => _otDate; set => SetProperty(ref _otDate, value); }

    private string _otStartTime = "18:00";
    public string OtStartTime { get => _otStartTime; set => SetProperty(ref _otStartTime, value); }

    private string _otEndTime = "20:00";
    public string OtEndTime { get => _otEndTime; set => SetProperty(ref _otEndTime, value); }

    private string? _otErrorMessage;
    public string? OtErrorMessage
    {
        get => _otErrorMessage;
        set
        {
            if (SetProperty(ref _otErrorMessage, value))
                OnPropertyChanged(nameof(HasOtError));
        }
    }

    public bool HasOtError => !string.IsNullOrWhiteSpace(OtErrorMessage);

    private void LoadDepartments()
    {
        Departments.Clear();
        Departments.Add(new Department { DepartmentId = 0, DepartmentName = "All Departments" });

        foreach (var department in _departmentRepository.GetAll())
            Departments.Add(department);

        if (_sessionManager.CurrentUser != null &&
            _sessionManager.CurrentUser.Role.RoleName.Equals("Manager", StringComparison.OrdinalIgnoreCase))
        {
            var deptId = _sessionManager.CurrentUser.Employee.DepartmentId;
            SelectedDepartment = Departments.FirstOrDefault(d => d.DepartmentId == deptId) ?? Departments.First();
        }
        else
        {
            SelectedDepartment = Departments.First();
        }
    }

    private void LoadEmployees()
    {
        Employees.Clear();

        var isManager = _sessionManager.CurrentUser != null &&
            _sessionManager.CurrentUser.Role.RoleName.Equals("Manager", StringComparison.OrdinalIgnoreCase);

        var list = isManager
            ? _employeeRepository.GetByDepartment(_sessionManager.CurrentUser!.Employee.DepartmentId)
            : SelectedDepartment is { DepartmentId: > 0 }
                ? _employeeRepository.GetByDepartment(SelectedDepartment.DepartmentId)
                : _employeeRepository.GetAll();

        foreach (var employee in list)
            Employees.Add(BuildRow(employee));
    }

    private ManageEmployeeRowViewModel BuildRow(Employee employee)
    {
        var summary = _attendanceService.GetMonthSummary(employee.EmployeeId, employee.HireDate, DateTime.Now);

        return new ManageEmployeeRowViewModel
        {
            EmployeeId = employee.EmployeeId,
            EmployeeName = employee.FullName,
            WorkingDays = summary.TotalDaysWorked,
            OnTimeDays = summary.TotalOnTime,
            LateDays = summary.TotalLate,
            AbsentDays = summary.TotalAbsent
        };
    }

    private void RefreshEmployeeRow(int employeeId)
    {
        var row = Employees.FirstOrDefault(e => e.EmployeeId == employeeId);
        var employee = _employeeRepository.GetById(employeeId);

        if (row == null || employee == null)
            return;

        var summary = _attendanceService.GetMonthSummary(employeeId, employee.HireDate, DateTime.Now);
        row.WorkingDays = summary.TotalDaysWorked;
        row.OnTimeDays = summary.TotalOnTime;
        row.LateDays = summary.TotalLate;
        row.AbsentDays = summary.TotalAbsent;
    }

    private void OpenOtForm(ManageEmployeeRowViewModel? row)
    {
        if (row == null)
            return;

        _otTargetRow = row;
        OtDate = DateTime.Today;
        OtStartTime = "18:00";
        OtEndTime = "20:00";
        OtErrorMessage = null;
        IsOtFormOpen = true;
    }

    private void CloseOtForm()
    {
        IsOtFormOpen = false;
        _otTargetRow = null;
    }

    private void SaveOt()
    {
        if (_otTargetRow == null || OtDate == null)
        {
            OtErrorMessage = "Please pick a date.";
            return;
        }

        if (!TimeSpan.TryParse(OtStartTime, out var start) || !TimeSpan.TryParse(OtEndTime, out var end))
        {
            OtErrorMessage = "Use the HH:mm time format.";
            return;
        }

        if (end <= start)
        {
            OtErrorMessage = "End time must be after start time.";
            return;
        }

        _attendanceService.ScheduleOt(_otTargetRow.EmployeeId, OtDate.Value, start, end);

        IsOtFormOpen = false;
        _otTargetRow = null;
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
}