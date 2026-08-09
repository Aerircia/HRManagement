using HRManagement.Models;

namespace HRManagement.Services.Interfaces;

/// <summary>
/// Read model for one employee row on the Manage Attendances page. Combines
/// Employee with the same monthly attendance summary/today's attendance
/// used by the detail calendar, so the row list and the calendar can never
/// disagree (both derive from IAttendanceService.GetMonthSummary).
/// </summary>
public class ManageAttendanceRow
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime HireDate { get; set; }

    public int WorkingDays { get; set; }
    public int OnTimeDays { get; set; }
    public int LateDays { get; set; }
    public int AbsentDays { get; set; }

    public DateTime? CheckInToday { get; set; }
    public DateTime? CheckOutToday { get; set; }
}

/// <summary>
/// Aggregated stat-card values for the Manage Attendances page, computed
/// across whatever employee scope (department/manager/filter) was passed
/// to <see cref="IManageAttendancesService.GetEmployeeAttendanceOverview"/>.
/// </summary>
public class ManageAttendanceOverview
{
    public List<ManageAttendanceRow> Rows { get; set; } = [];

    public double PresentRateToday { get; set; }
    public double LateRateToday { get; set; }
    public double AbsentRateToday { get; set; }

    public int DayOffThisMonthCount { get; set; }
    public int OtThisMonthCount { get; set; }
    public int TeamMinutesThisMonth { get; set; }
}

public interface IManageAttendancesService
{
    /// <summary>
    /// Department options for the filter dropdown, scoped by the current
    /// user's role: Admins see every department (plus "All Departments"),
    /// Managers see only their own department, everyone else sees a single
    /// entry for their own department.
    /// </summary>
    List<Department> GetDepartmentOptions();

    /// <summary>
    /// The department the filter dropdown should default to for the
    /// current user: the manager's own department for Managers, otherwise
    /// the first entry in <see cref="GetDepartmentOptions"/> ("All
    /// Departments" for Admins).
    /// </summary>
    Department? GetDefaultDepartment(List<Department> departmentOptions);

    /// <summary>
    /// Builds the employee row list and today/this-month stat aggregates
    /// for Manage Attendances, scoped by the current user's role
    /// (Managers are always scoped to their own department regardless of
    /// selectedDepartmentId), the selected department filter (0 or null =
    /// no department filter), and an optional employee-ID text filter.
    /// </summary>
    ManageAttendanceOverview GetEmployeeAttendanceOverview(
        int? selectedDepartmentId,
        string? employeeIdFilter);

    /// <summary>
    /// Hire date lookup used when an employee row is selected, so the
    /// attendance calendar knows the employee's effective start date.
    /// </summary>
    DateTime? GetEmployeeHireDate(int employeeId);

    /// <summary>
    /// Open check-ins (checked in, never checked out) for the "Open
    /// Check-Ins" grid, scoped by the current user's role: Managers always
    /// see only their own department's employees regardless of
    /// selectedDepartmentId (mirroring GetEmployeeAttendanceOverview's
    /// scoping), Admins see the selected department or all departments
    /// when selectedDepartmentId is 0/null.
    /// </summary>
    List<OpenCheckInRow> GetOpenCheckIns(int? selectedDepartmentId);

    /// <summary>
    /// Approves an open check-in: defaults its Check_out to the standard
    /// 17:00 shift end for that day. Delegates to IAttendanceService.
    /// </summary>
    void ApproveOpenCheckIn(int attendanceId);

    /// <summary>
    /// Denies an open check-in: hard-deletes the attendance row.
    /// Delegates to IAttendanceService.
    /// </summary>
    void DenyOpenCheckIn(int attendanceId);
}
