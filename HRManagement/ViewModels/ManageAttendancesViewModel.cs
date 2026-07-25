using HRManagement.Models;
using HRManagement.Repositories.Interfaces;
using HRManagement.Services;
using HRManagement.Services.Interfaces;
using HRManagement.Utilities;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

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
        IAttendanceService attendanceService,
        AttendanceViewModel childAttendanceViewModel)
    {
        _sessionManager = sessionManager;
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _attendanceService = attendanceService;

        Departments = new ObservableCollection<Department>();
        Employees = new ObservableCollection<ManageEmployeeRowViewModel>();

        // Injected by DI instead of being newed up here, so it shares the
        // same IAttendanceService instance (and its AttendanceChanged
        // event wiring) as the rest of the app.
        ChildAttendanceViewModel = childAttendanceViewModel;

        LoadDepartmentsCommand = new RelayCommand(_ => LoadDepartments());
        LoadEmployeesCommand = new RelayCommand(_ => LoadEmployees());
        SelectEmployeeCommand = new RelayCommand(p => SelectEmployee(p));

        LoadDepartments();
        LoadEmployees();
    }

    public override string Title => "Manage Attendances";

    public bool IsAdmin => _sessionManager.CurrentUser != null &&
                            _sessionManager.CurrentUser.Role.RoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase);

    public ObservableCollection<Department> Departments { get; }
    public ObservableCollection<ManageEmployeeRowViewModel> Employees { get; }

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
                var emp = _employeeRepository.GetById(_selectedEmployee.EmployeeId);
                ChildAttendanceViewModel.SetDisplayedEmployee(_selectedEmployee.EmployeeId, emp?.HireDate.Date);
            }
        }
    }

    public bool IsEmployeeSelected => SelectedEmployee != null;

    private Department? _selectedDepartment;
    public Department? SelectedDepartment
    {
        get => _selectedDepartment;
        set { SetProperty(ref _selectedDepartment, value); LoadEmployees(); }
    }

    private string _employeeIdFilter = string.Empty;
    public string EmployeeIdFilter
    {
        get => _employeeIdFilter;
        set { if (SetProperty(ref _employeeIdFilter, value)) LoadEmployees(); }
    }

    public ICommand LoadDepartmentsCommand { get; }
    public ICommand LoadEmployeesCommand { get; }
    public ICommand SelectEmployeeCommand { get; }

    private void LoadDepartments()
    {
        Departments.Clear();
        Departments.Add(new Department { DepartmentId = 0, DepartmentName = "All Departments" });

        if (IsAdmin)
        {
            foreach (var d in _departmentRepository.GetAll())
                Departments.Add(d);
        }
        else if (_sessionManager.CurrentUser != null)
        {
            // Non-admin (manager): only include the manager's own department
            // so filtering still works without exposing other departments.
            var deptId = _sessionManager.CurrentUser.Employee.DepartmentId;
            var dept = _departmentRepository.GetAll().FirstOrDefault(d => d.DepartmentId == deptId);
            Departments.Add(dept ?? new Department { DepartmentId = deptId, DepartmentName = $"Department {deptId}" });
        }

        if (_sessionManager.CurrentUser != null &&
            _sessionManager.CurrentUser.Role.RoleName.Equals("Manager", StringComparison.OrdinalIgnoreCase))
        {
            var deptId = _sessionManager.CurrentUser.Employee.DepartmentId;
            SelectedDepartment = Departments.FirstOrDefault(x => x.DepartmentId == deptId) ?? Departments.First();
        }
        else
        {
            SelectedDepartment = Departments.First();
        }
    }

    private void LoadEmployees()
    {
        Employees.Clear();

        IEnumerable<Employee> list;
        if (_sessionManager.CurrentUser != null &&
            _sessionManager.CurrentUser.Role.RoleName.Equals("Manager", StringComparison.OrdinalIgnoreCase))
        {
            list = _employeeRepository.GetByDepartment(_sessionManager.CurrentUser.Employee.DepartmentId);
        }
        else if (SelectedDepartment != null && SelectedDepartment.DepartmentId != 0)
        {
            list = _employeeRepository.GetByDepartment(SelectedDepartment.DepartmentId);
        }
        else
        {
            list = _employeeRepository.GetAll();
        }

        var now = DateTime.Now;

        foreach (var e in list)
        {
            if (!string.IsNullOrWhiteSpace(EmployeeIdFilter))
            {
                if (!int.TryParse(EmployeeIdFilter, out var fid) || fid != e.EmployeeId)
                    continue;
            }

            // Single source of truth for the summary math - same service
            // method used by the calendar itself, so the row list and the
            // detail calendar can never disagree.
            var summary = _attendanceService.GetMonthSummary(e.EmployeeId, e.HireDate.Date, now);

            Employees.Add(new ManageEmployeeRowViewModel
            {
                EmployeeId = e.EmployeeId,
                EmployeeName = e.FullName,
                WorkingDays = summary.TotalDaysWorked,
                OnTimeDays = summary.TotalOnTime,
                LateDays = summary.TotalLate,
                AbsentDays = summary.TotalAbsent
            });
        }
    }

    private void SelectEmployee(object? param)
    {
        if (param is ManageEmployeeRowViewModel row)
            SelectedEmployee = row;
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
