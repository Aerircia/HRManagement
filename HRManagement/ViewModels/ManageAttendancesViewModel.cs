using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using HRManagement.Data;
using HRManagement.Models;
using HRManagement.Repositories;
using HRManagement.Services;
using HRManagement.Utilities;

namespace HRManagement.ViewModels;

public class ManageAttendancesViewModel : PageViewModel
{
    private readonly SessionManager _sessionManager;
    private readonly EmployeeRepository _employeeRepository;
    private readonly DepartmentRepository _departmentRepository = new();
    private readonly AttendanceRepository _attendanceRepository;

    public ManageAttendancesViewModel(SessionManager sessionManager, EmployeeRepository employeeRepository, AttendanceRepository attendanceRepository)
    {
        _sessionManager = sessionManager;
        _employeeRepository = employeeRepository;
        _attendanceRepository = attendanceRepository;

        Departments = new ObservableCollection<Department>();
        Employees = new ObservableCollection<ManageEmployeeRowViewModel>();

        ChildAttendanceViewModel = new AttendanceViewModel(_sessionManager, _attendanceRepository);

        LoadDepartmentsCommand = new RelayCommand(_ => LoadDepartments());
        LoadEmployeesCommand = new RelayCommand(_ => LoadEmployees());
        SelectEmployeeCommand = new RelayCommand(p => SelectEmployee(p));

        // initial load
        LoadDepartments();
        LoadEmployees();
    }

    public override string Title => "Manage Attendances";

    public bool IsAdmin => _sessionManager.CurrentUser != null && _sessionManager.CurrentUser.Role.RoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase);

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
    public Department? SelectedDepartment { get => _selectedDepartment; set { SetProperty(ref _selectedDepartment, value); LoadEmployees(); } }

    private string _employeeIdFilter = string.Empty;
    public string EmployeeIdFilter { get => _employeeIdFilter; set { if (SetProperty(ref _employeeIdFilter, value)) LoadEmployees(); } }

    public ICommand LoadDepartmentsCommand { get; }
    public ICommand LoadEmployeesCommand { get; }
    public ICommand SelectEmployeeCommand { get; }

    private void LoadDepartments()
    {
        Departments.Clear();
        Departments.Add(new Department { DepartmentId = 0, DepartmentName = "All Departments" });

        if (IsAdmin)
        {
            var depts = _departmentRepository.GetAll();
            foreach (var d in depts) Departments.Add(d);
        }
        else
        {
            // for non-admin (manager), only include the manager's department so selection/filtering can still work
            if (_sessionManager.CurrentUser != null)
            {
                var deptId = _sessionManager.CurrentUser.Employee.DepartmentId;
                var dept = _departmentRepository.GetAll().FirstOrDefault(d => d.DepartmentId == deptId);
                Departments.Add(dept ?? new Department { DepartmentId = deptId, DepartmentName = $"Department {deptId}" });
            }
        }

        // pre-select appropriate department
        if (_sessionManager.CurrentUser != null && _sessionManager.CurrentUser.Role.RoleName.Equals("Manager", StringComparison.OrdinalIgnoreCase))
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
        if (_sessionManager.CurrentUser != null && _sessionManager.CurrentUser.Role.RoleName.Equals("Manager", StringComparison.OrdinalIgnoreCase))
        {
            var dept = _sessionManager.CurrentUser.Employee.DepartmentId;
            list = _employeeRepository.GetByDepartment(dept);
        }
        else
        {
            if (SelectedDepartment != null && SelectedDepartment.DepartmentId != 0)
                list = _employeeRepository.GetByDepartment(SelectedDepartment.DepartmentId);
            else
                list = _employeeRepository.GetAll();
        }

        foreach (var e in list)
        {
            // apply employee id filter if provided
            if (!string.IsNullOrWhiteSpace(EmployeeIdFilter))
            {
                if (!int.TryParse(EmployeeIdFilter, out var fid) || fid != e.EmployeeId) continue;
            }

            var summary = ComputeSummaryForEmployeeMonth(e, DateTime.Now.Year, DateTime.Now.Month);

            var vm = new ManageEmployeeRowViewModel
            {
                EmployeeId = e.EmployeeId,
                EmployeeName = e.FullName,
                WorkingDays = summary.TotalWorkingDays,
                OnTimeDays = summary.OnTimeDays,
                LateDays = summary.LateDays,
                AbsentDays = summary.AbsentDays
            };

            Employees.Add(vm);
        }
    }

    private (int TotalWorkingDays, int OnTimeDays, int LateDays, int AbsentDays) ComputeSummaryForEmployeeMonth(Employee e, int year, int month)
    {
        var hire = e.HireDate.Date;
        var empId = e.EmployeeId;

        var allAtts = new System.Collections.Generic.List<Attendance>();
        var m = new DateTime(hire.Year, hire.Month, 1);
        var searchEnd = DateTime.Now.Date;
        while (m <= searchEnd)
        {
            allAtts.AddRange(_attendanceRepository.GetAttendancesForEmployeeMonth(empId, m.Year, m.Month));
            m = m.AddMonths(1);
        }

        var viewMonthStart = new DateTime(year, month, 1);
        var attUpTo = allAtts.Where(a => ((a.CheckIn ?? a.CheckOut ?? DateTime.Now).Date) >= viewMonthStart && ((a.CheckIn ?? a.CheckOut ?? DateTime.Now).Date) <= viewMonthStart.AddMonths(1).AddDays(-1)).ToList();

        int onTime = 0, late = 0, absent = 0, totalWorkingDays = 0;
        for (var d = viewMonthStart; d <= viewMonthStart.AddMonths(1).AddDays(-1); d = d.AddDays(1))
        {
            if (d.DayOfWeek == System.DayOfWeek.Saturday || d.DayOfWeek == System.DayOfWeek.Sunday) continue;
            if (d < hire) continue;
            totalWorkingDays++;
            var dayAtt = attUpTo.Where(a => ((a.CheckIn ?? a.CheckOut ?? DateTime.Now).Date) == d.Date).ToList();
            if (!dayAtt.Any() || !dayAtt.Any(a => a.CheckIn.HasValue)) { absent++; continue; }
            var earliest = dayAtt.Where(a => a.CheckIn.HasValue).Select(a => a.CheckIn!.Value).OrderBy(x => x).FirstOrDefault();
            var minutesLate = (int)Math.Round((earliest - d.AddHours(8)).TotalMinutes);
            if (minutesLate <= 5) onTime++; else late++;
        }

        return (totalWorkingDays, onTime, late, absent);
    }

    private void SelectEmployee(object? param)
    {
        if (param is ManageEmployeeRowViewModel row)
        {
            SelectedEmployee = row;
        }
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
