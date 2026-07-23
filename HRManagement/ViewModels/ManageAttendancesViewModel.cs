using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using HRManagement.Data;
using HRManagement.Models;
using HRManagement.Repositories;
using HRManagement.Services;
using HRManagement.Utilities;
using HRManagement.Views.Windows;
using System.Windows;

namespace HRManagement.ViewModels;

public class ManageAttendancesViewModel : PageViewModel
{
    private readonly SessionManager _sessionManager;
    private readonly EmployeeRepository _employeeRepository;
    private readonly DepartmentRepository _departmentRepository = new();
    private readonly AttendanceRepository _attendanceRepository;
    public bool IsAdmin => _sessionManager.CurrentUser != null && _sessionManager.CurrentUser.Role.RoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase);

    public ManageAttendancesViewModel(SessionManager sessionManager, EmployeeRepository employeeRepository, AttendanceRepository attendanceRepository)
    {
        _sessionManager = sessionManager;
        _employeeRepository = employeeRepository;
        _attendanceRepository = attendanceRepository;

        Departments = new ObservableCollection<Department>();
        Employees = new ObservableCollection<ManageEmployeeRowViewModel>();

        LoadDepartmentsCommand = new RelayCommand(_ => LoadDepartments());
        LoadEmployeesCommand = new RelayCommand(_ => LoadEmployees());
        SelectOtCommand = new RelayCommand(p => SelectOt(p));

        // initial load
        LoadDepartments();
        LoadEmployees();
    }

    public override string Title => "Manage Attendances";

    public ObservableCollection<Department> Departments { get; }
    public ObservableCollection<ManageEmployeeRowViewModel> Employees { get; }

    private Department? _selectedDepartment;
    public Department? SelectedDepartment { get => _selectedDepartment; set { SetProperty(ref _selectedDepartment, value); LoadEmployees(); } }

    public ICommand LoadDepartmentsCommand { get; }
    public ICommand LoadEmployeesCommand { get; }
    public ICommand SelectOtCommand { get; }

    private void LoadDepartments()
    {
        Departments.Clear();
        Departments.Add(new Department { DepartmentId = 0, DepartmentName = "All Departments" });

        // If admin, load department names from database; otherwise derive from employee list
        if (IsAdmin)
        {
            var depts = _departmentRepository.GetAll();
            foreach (var d in depts)
            {
                Departments.Add(d);
            }
        }
        else
        {
            var emps = _employeeRepository.GetAll();
            var deptIds = emps.Select(e => e.DepartmentId).Distinct().OrderBy(id => id);
            foreach (var d in deptIds)
            {
                Departments.Add(new Department { DepartmentId = d, DepartmentName = $"Dept {d}" });
            }
        }

        // if session manager has manager role, pre-select manager's department
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
            // manager sees only their department
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
            var vm = new ManageEmployeeRowViewModel
            {
                EmployeeId = e.EmployeeId,
                EmployeeName = e.FullName
            };

            // compute summary using same logic as employee Attendance summary (for current month view)
            var summary = ComputeSummaryForEmployeeMonth(e, DateTime.Now.Year, DateTime.Now.Month);
            vm.WorkingDays = summary.TotalWorkingDays;
            vm.OnTimeDays = summary.OnTimeDays;
            vm.LateDays = summary.LateDays;
            vm.AbsentDays = summary.AbsentDays;
            vm.TotalOtCount = summary.TotalOtCount;

            Employees.Add(vm);
        }
    }

    private (int TotalWorkingDays, int OnTimeDays, int LateDays, int AbsentDays, int TotalOtCount) ComputeSummaryForEmployeeMonth(Employee e, int year, int month)
    {
        // Mirror the AttendanceViewModel.UpdateSummary behavior but scoped to the requested month
        var hire = e.HireDate.Date;
        var empId = e.EmployeeId;

        // gather attendance records from hire month up to now
        var allAtts = new System.Collections.Generic.List<Attendance>();
        var m = new DateTime(hire.Year, hire.Month, 1);
        var searchEnd = DateTime.Now.Date;
        while (m <= searchEnd)
        {
            allAtts.AddRange(_attendanceRepository.GetAttendancesForEmployeeMonth(empId, m.Year, m.Month));
            m = m.AddMonths(1);
        }

        DateTime lastDataDate = allAtts.Select(a => (a.CheckIn ?? a.CheckOut ?? DateTime.Now).Date).DefaultIfEmpty(hire).Max();
        var lastDataMonthStart = new DateTime(lastDataDate.Year, lastDataDate.Month, 1);

        var viewMonthStart = new DateTime(year, month, 1);
        DateTime accumulateUpToMonthStart = viewMonthStart <= lastDataMonthStart ? viewMonthStart : lastDataMonthStart;
        var accumulateEndDate = accumulateUpToMonthStart.AddMonths(1).AddDays(-1);
        if (accumulateEndDate > DateTime.Now.Date) accumulateEndDate = DateTime.Now.Date;

        var attUpTo = allAtts.Where(a => ((a.CheckIn ?? a.CheckOut ?? DateTime.Now).Date) <= accumulateEndDate).ToList();

        var grouped = attUpTo.GroupBy(a => (a.CheckIn ?? a.CheckOut ?? DateTime.Now).Date)
                             .Select(g => new {
                                 Date = g.Key,
                                 EarliestCheckIn = g.Where(x => x.CheckIn.HasValue).Select(x => x.CheckIn!.Value).OrderBy(x => x).FirstOrDefault() as DateTime?,
                                 HasCheckIn = g.Any(x => x.CheckIn.HasValue),
                                 Status = g.Select(x => x.Status).FirstOrDefault(s => !string.IsNullOrEmpty(s)) ?? string.Empty,
                                 HasOt = g.Any(x => !string.IsNullOrEmpty(x.Status) && x.Status.Equals("OT", StringComparison.OrdinalIgnoreCase))
                             }).ToList();

        // total working days in the accumulated month period (exclude weekends and before hire)
        var totalWorkingDays = 0;
        for (var d = accumulateUpToMonthStart; d <= accumulateEndDate; d = d.AddDays(1))
        {
            if (d.DayOfWeek == System.DayOfWeek.Saturday || d.DayOfWeek == System.DayOfWeek.Sunday) continue;
            if (d.Date < hire) continue;
            totalWorkingDays++;
        }

        var daysWorked = grouped.Count(g => g.HasCheckIn && g.Date >= hire);
        var onTime = 0; var late = 0;
        var lateGrace = 5; // minutes
        var shiftStart = TimeSpan.FromHours(8);
        foreach (var g in grouped)
        {
            if (!g.HasCheckIn) continue;
            var ss = g.Date.Add(shiftStart);
            if (g.EarliestCheckIn.HasValue)
            {
                var minutesLate = (int)Math.Round((g.EarliestCheckIn.Value - ss).TotalMinutes);
                if (minutesLate <= lateGrace) onTime++;
                else late++;
            }
        }

        var absent = Math.Max(0, totalWorkingDays - daysWorked);

        var otCount = grouped.Count(g => g.HasOt);

        return (totalWorkingDays, onTime, late, absent, otCount);
    }

    private (int TotalWorkingDays, int OnTimeDays, int LateDays, int AbsentDays, double TotalOtHours) BuildDaySummaries(Employee e, List<Attendance> atts)
    {
        // simple approximation: count distinct dates with any attendance as worked
        var start = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var end = start.AddMonths(1);

        var totalWorkingDays = 0;
        var onTime = 0;
        var late = 0;
        var absent = 0;
        double otHours = 0;

        for (var d = start; d < end; d = d.AddDays(1))
        {
            if (d.DayOfWeek == System.DayOfWeek.Saturday || d.DayOfWeek == System.DayOfWeek.Sunday) continue; // skip weekends
            if (e.HireDate.Date > d.Date) continue; // before hire
            totalWorkingDays++;

            var att = atts.FirstOrDefault(a => ((a.CheckIn ?? a.CheckOut ?? DateTime.Now).Date) == d.Date);
            if (att == null)
            {
                absent++; continue;
            }

            if (att.CheckIn.HasValue)
            {
                var shiftStart = d.AddHours(8);
                var minutesLate = (int)Math.Round((att.CheckIn.Value - shiftStart).TotalMinutes);
                if (minutesLate <= 0) onTime++;
                else late++;
            }
            else
            {
                absent++;
            }

            // count OT occurrences by status containing "OT"
            if (!string.IsNullOrEmpty(att.Status) && att.Status.IndexOf("OT", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                otHours += 1;
            }
        }

        return (totalWorkingDays, onTime, late, absent, Math.Round(otHours, 2));
    }

    private void SelectOt(object? param)
    {
        if (param is not ManageEmployeeRowViewModel row) return;

        var dlg = new OtDialog();
        dlg.Owner = Application.Current.MainWindow;
        var res = dlg.ShowDialog();
        if (res != true) return;
        if (!dlg.StartTime.HasValue || !dlg.EndTime.HasValue) return;

        // construct attendance record for OT
        var date = dlg.SelectedDate.Date;
        var checkIn = date + dlg.StartTime.Value;
        var checkOut = date + dlg.EndTime.Value;

        var att = new Attendance
        {
            EmployeeId = row.EmployeeId,
            CheckIn = checkIn,
            CheckOut = checkOut,
            Status = "OT"
        };

        _attendanceRepository.UpsertAttendance(att);

        // refresh this employee's summary
        var atts = _attendanceRepository.GetAttendancesForEmployeeMonth(row.EmployeeId, DateTime.Now.Year, DateTime.Now.Month).ToList();
        var days = BuildDaySummaries(_employeeRepository.GetById(row.EmployeeId)!, atts);
        row.WorkingDays = days.TotalWorkingDays;
        row.OnTimeDays = days.OnTimeDays;
        row.LateDays = days.LateDays;
        row.AbsentDays = days.AbsentDays;
        row.TotalOtHours = days.TotalOtHours;

        // If Attendance page currently displayed for this employee, refresh it directly
        var nav = App.Services.GetService(typeof(HRManagement.Services.Interfaces.INavigationService)) as HRManagement.Services.Interfaces.INavigationService;
        if (nav?.CurrentView is HRManagement.ViewModels.AttendanceViewModel av)
        {
            av.RefreshIfEmployee(row.EmployeeId, date);
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

    private double _totalOtHours;
    public double TotalOtHours { get => _totalOtHours; set => SetProperty(ref _totalOtHours, value); }

    private int _totalOtCount;
    public int TotalOtCount { get => _totalOtCount; set => SetProperty(ref _totalOtCount, value); }
}
