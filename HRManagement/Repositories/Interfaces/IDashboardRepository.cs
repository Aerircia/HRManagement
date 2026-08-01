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

    double GetDepartmentHoursThisMonth(int departmentId,int year,int month);

    int GetTodayAttendanceCount(int departmentId);
}