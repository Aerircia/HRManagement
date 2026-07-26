using HRManagement.Models;

namespace HRManagement.Services.Interfaces;

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
}