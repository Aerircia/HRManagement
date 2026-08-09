using HRManagement.Models;

namespace HRManagement.Services.Interfaces;

/// <summary>
/// Read model for one row on the "Open Check-Ins" grid (Manage Attendances):
/// a manual weekday check-in that was never followed by a check-out.
/// Scheduled OT/Day Off rows always have both Check_in and Check_out set
/// at creation time, so this can only ever represent Present/Late records.
/// </summary>
public class OpenCheckInRow
{
    public int AttendanceId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime CheckIn { get; set; }
    public string Status { get; set; } = string.Empty;
}

public interface IAttendanceService
{
    event EventHandler<AttendanceChangedEventArgs>? AttendanceChanged;
    List<AttendanceDayModel> BuildMonth(int employeeId, DateTime hireDate, DateTime monthStart);
    AttendanceMonthSummary GetMonthSummary(int employeeId, DateTime hireDate, DateTime viewMonth);
    bool CanCheckIn(int employeeId, DateTime hireDate);
    bool CanCheckOut(int employeeId, DateTime hireDate);
    void CheckIn(int employeeId);

    void CheckOut(int employeeId);
    void ScheduleOt(int employeeId, DateTime date, TimeSpan start, TimeSpan end);

    void ScheduleDayOff(int employeeId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Returns the employee's attendance record for today, if any. Used by
    /// row-level "check-in/out today" displays (e.g. Manage Attendances)
    /// without each caller re-implementing the "find today's record" query.
    /// </summary>
    Attendance? GetTodayAttendance(int employeeId);

    /// <summary>
    /// All attendance rows with a check-in and no check-out, across every
    /// employee, joined with employee name for display. Not department-
    /// scoped here - callers needing scoping (Manager role) filter by
    /// employee ID themselves, mirroring the pattern already used by
    /// ManageAttendancesService.GetEmployeeAttendanceOverview.
    /// </summary>
    List<OpenCheckInRow> GetOpenCheckIns();

    /// <summary>
    /// "Approve" action for an open check-in: sets Check_out to 17:00 on
    /// the check-in's own date (the standard shift end used elsewhere in
    /// this service), leaving Check_in/Status untouched. Logs the action.
    /// </summary>
    void ApproveOpenCheckIn(int attendanceId);

    /// <summary>
    /// "Deny" action for an open check-in: hard-deletes the attendance
    /// row. Logs the action.
    /// </summary>
    void DenyOpenCheckIn(int attendanceId);
}
