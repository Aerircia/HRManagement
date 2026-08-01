using HRManagement.Models;

namespace HRManagement.Repositories.Interfaces;

public interface IDashboardRepository
{
    IEnumerable<Attendance> GetEmployeeAttendanceMonth(
        int employeeId,
        int year,
        int month);

    IReadOnlyList<Announcement> GetAnnouncements();

    EmployeeEvaluation? GetLatestEvaluation(
        int employeeId);

    decimal GetTotalBonusThisMonth(
        int employeeId,
        int year,
        int month);

    IEnumerable<Employee> GetDepartmentEmployees(
        int departmentId);

    double GetDepartmentHoursThisMonth(int departmentId, int year, int month);

    int GetTodayAttendanceCount(int departmentId);

    /// <summary>
    /// Org-wide equivalents of the above, used for the Admin scope of the
    /// dashboard's second analytics card (Manager sees their department,
    /// Admin sees the whole organization).
    /// </summary>
    IEnumerable<Employee> GetAllEmployees();

    double GetOrgHoursThisMonth(int year, int month);

    int GetTodayAttendanceCountOrgWide();
}