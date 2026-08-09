using HRManagement.Models;
using HRManagement.Repositories.Interfaces;
using HRManagement.Services.Interfaces;

namespace HRManagement.Services;

public class ManageAttendancesService(
    IEmployeeRepository employeeRepository,
    IDepartmentRepository departmentRepository,
    IAttendanceService attendanceService,
    SessionManager sessionManager) : IManageAttendancesService
{
    private readonly IEmployeeRepository _employeeRepository = employeeRepository
            ?? throw new ArgumentNullException(nameof(employeeRepository));
    private readonly IDepartmentRepository _departmentRepository = departmentRepository
            ?? throw new ArgumentNullException(nameof(departmentRepository));
    private readonly IAttendanceService _attendanceService = attendanceService
            ?? throw new ArgumentNullException(nameof(attendanceService));
    private readonly SessionManager _sessionManager = sessionManager
            ?? throw new ArgumentNullException(nameof(sessionManager));

    public List<Department> GetDepartmentOptions()
    {
        var departments = new List<Department>
        {
            new() { DepartmentId = 0, DepartmentName = "All Departments" }
        };

        if (IsAdmin())
        {
            departments.AddRange(_departmentRepository.GetAll());
        }
        else if (_sessionManager.CurrentUser != null)
        {
            // Non-admin (manager): only include the manager's own department
            // so filtering still works without exposing other departments.
            var deptId = _sessionManager.CurrentUser.Employee.DepartmentId;
            var dept = _departmentRepository.GetAll()
                .FirstOrDefault(d => d.DepartmentId == deptId);

            departments.Add(
                dept ?? new Department { DepartmentId = deptId, DepartmentName = $"Department {deptId}" });
        }

        return departments;
    }

    public Department? GetDefaultDepartment(List<Department> departmentOptions)
    {
        if (departmentOptions == null || departmentOptions.Count == 0)
            return null;

        if (_sessionManager.CurrentUser != null && IsManager())
        {
            var deptId = _sessionManager.CurrentUser.Employee.DepartmentId;
            return departmentOptions.FirstOrDefault(x => x.DepartmentId == deptId)
                ?? departmentOptions.First();
        }

        return departmentOptions.First();
    }

    public ManageAttendanceOverview GetEmployeeAttendanceOverview(
        int? selectedDepartmentId,
        string? employeeFilter)
    {
        IEnumerable<Employee> list;

        if (_sessionManager.CurrentUser != null && IsManager())
        {
            list = _employeeRepository.GetByDepartment(
                _sessionManager.CurrentUser.Employee.DepartmentId);
        }
        else if (selectedDepartmentId is > 0)
        {
            list = _employeeRepository.GetByDepartment(selectedDepartmentId.Value);
        }
        else
        {
            list = _employeeRepository.GetAll();
        }

        if (!string.IsNullOrWhiteSpace(employeeFilter))
        {
            list = list.Where(e => e.FullName != null &&
                           e.FullName.Contains(employeeFilter, StringComparison.OrdinalIgnoreCase));
        }

        var scopedEmployees = list.ToList();

        var now = DateTime.Now;
        var isWeekendToday = now.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

        var rows = new List<ManageAttendanceRow>();

        var presentToday = 0;
        var lateToday = 0;
        var absentToday = 0;
        var expectedTodayCount = 0;

        var dayOffCount = 0;
        var otCount = 0;
        var teamMinutes = 0;

        foreach (var employee in scopedEmployees)
        {
            // Single source of truth for the summary math - same service
            // method used by the calendar itself, so the row list and the
            // detail calendar can never disagree.
            var summary = _attendanceService.GetMonthSummary(
                employee.EmployeeId,
                employee.HireDate.Date,
                now);

            var today = _attendanceService.GetTodayAttendance(employee.EmployeeId);

            rows.Add(new ManageAttendanceRow
            {
                EmployeeId = employee.EmployeeId,
                EmployeeName = employee.FullName,
                HireDate = employee.HireDate,
                WorkingDays = summary.TotalDaysWorked,
                OnTimeDays = summary.TotalOnTime,
                LateDays = summary.TotalLate,
                AbsentDays = summary.TotalAbsent,
                CheckInToday = today?.CheckIn,
                CheckOutToday = today?.CheckOut
            });

            dayOffCount += summary.TotalDayOffDays;
            otCount += summary.TotalOtDays;
            teamMinutes += summary.TotalWorkedMinutes;

            // "Expected today" excludes weekends and employees not yet hired,
            // mirroring the exclusion logic already used for absence
            // counting in AttendanceService.BuildMonth/GetMonthSummary.
            var expectedToday = !isWeekendToday && employee.HireDate.Date <= now.Date;
            if (!expectedToday)
                continue;

            expectedTodayCount++;

            if (string.Equals(today?.Status, "Late", StringComparison.OrdinalIgnoreCase))
                lateToday++;
            else if (today?.CheckIn.HasValue == true)
                presentToday++;
            else
                absentToday++;
        }

        return new ManageAttendanceOverview
        {
            Rows = rows,

            PresentRateToday = expectedTodayCount > 0 ? presentToday * 100.0 / expectedTodayCount : 0,
            LateRateToday = expectedTodayCount > 0 ? lateToday * 100.0 / expectedTodayCount : 0,
            AbsentRateToday = expectedTodayCount > 0 ? absentToday * 100.0 / expectedTodayCount : 0,

            DayOffThisMonthCount = dayOffCount,
            OtThisMonthCount = otCount,
            TeamMinutesThisMonth = teamMinutes
        };
    }

    public DateTime? GetEmployeeHireDate(int employeeId)
    {
        return _employeeRepository.GetById(employeeId)?.HireDate.Date;
    }

    public List<OpenCheckInRow> GetOpenCheckIns(int? selectedDepartmentId)
    {
        var allOpenCheckIns = _attendanceService.GetOpenCheckIns();

        if (allOpenCheckIns.Count == 0)
            return [];

        // Same scoping shape as GetEmployeeAttendanceOverview: Managers are
        // always locked to their own department regardless of the selected
        // filter; Admins get the selected department or everyone when
        // selectedDepartmentId is 0/null.
        HashSet<int>? scopedEmployeeIds = null;

        if (_sessionManager.CurrentUser != null && IsManager())
        {
            scopedEmployeeIds = [.. _employeeRepository
                .GetByDepartment(_sessionManager.CurrentUser.Employee.DepartmentId)
                .Select(e => e.EmployeeId)];
        }
        else if (selectedDepartmentId is > 0)
        {
            scopedEmployeeIds = [.. _employeeRepository
                .GetByDepartment(selectedDepartmentId.Value)
                .Select(e => e.EmployeeId)];
        }

        return scopedEmployeeIds == null
            ? allOpenCheckIns
            : [.. allOpenCheckIns.Where(row => scopedEmployeeIds.Contains(row.EmployeeId))];
    }

    public void ApproveOpenCheckIn(int attendanceId)
    {
        _attendanceService.ApproveOpenCheckIn(attendanceId);
    }

    public void DenyOpenCheckIn(int attendanceId)
    {
        _attendanceService.DenyOpenCheckIn(attendanceId);
    }

    private bool IsAdmin() =>
        _sessionManager.CurrentUser != null &&
        _sessionManager.CurrentUser.Role.RoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase);

    private bool IsManager() =>
        _sessionManager.CurrentUser != null &&
        _sessionManager.CurrentUser.Role.RoleName.Equals("Manager", StringComparison.OrdinalIgnoreCase);
}
