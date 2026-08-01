using HRManagement.Models;
using HRManagement.Repositories.Interfaces;

namespace HRManagement.Repositories;

public class DashboardRepository(
    IAttendanceRepository attendanceRepository,
    IAnnouncementRepository announcementRepository,
    IEmployeeRepository employeeRepository,
    IEmployeeEvaluationRepository evaluationRepository) : IDashboardRepository
{
    private readonly IAttendanceRepository _attendanceRepository = attendanceRepository;
    private readonly IAnnouncementRepository _announcementRepository = announcementRepository;
    private readonly IEmployeeRepository _employeeRepository = employeeRepository;
    private readonly IEmployeeEvaluationRepository _evaluationRepository = evaluationRepository;

    public IEnumerable<Attendance> GetEmployeeAttendanceMonth(
        int employeeId,
        int year,
        int month)
    {
        return _attendanceRepository
            .GetAttendancesForEmployeeMonth(employeeId, year, month);
    }

    public IReadOnlyList<Announcement> GetAnnouncements()
    {
        return _announcementRepository.GetActive();
    }

    public EmployeeEvaluation? GetLatestEvaluation(int employeeId)
    {
        return _evaluationRepository
            .GetLatestByEmployeeId(employeeId);
    }

    public decimal GetTotalBonusThisMonth(int employeeId, int year, int month)
    {
        return _evaluationRepository.GetTotalBonusForMonth(
            employeeId,
            year,
            month);
    }

    public double GetDepartmentHoursThisMonth(int departmentId, int year, int month)
    {
        return _attendanceRepository.GetDepartmentHoursThisMonth(
            departmentId,
            year,
            month);
    }

    public IEnumerable<Employee> GetDepartmentEmployees(
        int departmentId)
    {
        return _employeeRepository
            .GetByDepartment(departmentId);
    }

    public int GetTodayAttendanceCount(
        int departmentId)
    {
        return _attendanceRepository
            .GetTodayAttendanceCount(departmentId);
    }
}