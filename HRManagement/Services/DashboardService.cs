using HRManagement.Models;
using HRManagement.Models.Dashboard;
using HRManagement.Repositories.Interfaces;
using HRManagement.Services.Interfaces;
using System.Collections.ObjectModel;

namespace HRManagement.Services;

public class DashboardService : IDashboardService
{
    private readonly IDashboardRepository _dashboardRepository;
    private readonly IAttendanceService _attendanceService;
    private readonly IAuthorizationService _authorizationService;
    private readonly SessionManager _sessionManager;

    public DashboardService(
        IDashboardRepository dashboardRepository,
        IAttendanceService attendanceService,
        IAuthorizationService authorizationService,
        SessionManager sessionManager)
    {
        _dashboardRepository = dashboardRepository;
        _attendanceService = attendanceService;
        _authorizationService = authorizationService;
        _sessionManager = sessionManager;
    }

    #region Public API
    public DashboardData LoadDashboard()
    {
        var employee = GetCurrentEmployee();

        var dashboard = new DashboardData
        {
            TodayAttendance = _attendanceService.GetTodayAttendance(employee.EmployeeId),

            WeekDays = BuildCurrentWeek(employee),

            WeeklyHours = BuildWeeklyHours(employee),

            EmployeeAnalytics = BuildEmployeeAnalytics(employee),

            Announcements = GetAnnouncements()
        };

        if (_authorizationService.IsManager || _authorizationService.IsAdmin)
        {
            dashboard.ManagerAnalytics =
                BuildManagerAnalytics(employee);
        }

        return dashboard;
    }

    public bool CanCheckIn()
    {
        var employee = GetCurrentEmployee();

        return _attendanceService.CanCheckIn(
            employee.EmployeeId,
            employee.HireDate);
    }

    public bool CanCheckOut()
    {
        var employee = GetCurrentEmployee();

        return _attendanceService.CanCheckOut(
            employee.EmployeeId,
            employee.HireDate);
    }

    public void CheckIn()
    {
        var employee = GetCurrentEmployee();

        _attendanceService.CheckIn(employee.EmployeeId);
    }

    public void CheckOut()
    {
        var employee = GetCurrentEmployee();

        _attendanceService.CheckOut(employee.EmployeeId);
    }

    public Attendance? GetTodayAttendance()
    {
        return _attendanceService.GetTodayAttendance(
            GetCurrentEmployee().EmployeeId);
    }

    public ObservableCollection<WeekDayItem> GetCurrentWeek()
    {
        return BuildCurrentWeek(GetCurrentEmployee());
    }

    public ObservableCollection<WeeklyHourPoint> GetWeeklyHours()
    {
        return BuildWeeklyHours(GetCurrentEmployee());
    }

    public EmployeeAnalytics GetEmployeeAnalytics()
    {
        return BuildEmployeeAnalytics(GetCurrentEmployee());
    }

    public ManagerAnalytics? GetManagerAnalytics()
    {
        if (!_authorizationService.IsManager &&
            !_authorizationService.IsAdmin)
        {
            return null;
        }

        return BuildManagerAnalytics(GetCurrentEmployee());
    }

    public ObservableCollection<Announcement> GetAnnouncements()
    {
        return new ObservableCollection<Announcement>(
            _dashboardRepository.GetAnnouncements());
    }

    #endregion

    #region Helpers

    private Employee GetCurrentEmployee()
    {
        var employee = _sessionManager.CurrentUser?.Employee;

        if (employee == null)
            throw new InvalidOperationException(
                "No logged in employee.");

        return employee;
    }

    private static DateTime GetWeekStart(DateTime date)
    {
        int offset = ((int)date.DayOfWeek + 6) % 7;
        return date.Date.AddDays(-offset);
    }

    private static double CalculateHours(Attendance attendance)
    {
        if (attendance.CheckIn == null ||
            attendance.CheckOut == null)
        {
            return 0;
        }

        return Math.Round(
            (attendance.CheckOut.Value -
             attendance.CheckIn.Value).TotalHours,
            2);
    }

    #endregion

    #region Builder Methods

    private ObservableCollection<WeekDayItem> BuildCurrentWeek(Employee employee)
    {
        var result = new ObservableCollection<WeekDayItem>();

        var today = DateTime.Today;
        var weekStart = GetWeekStart(today);

        var attendances = _dashboardRepository
            .GetEmployeeAttendanceMonth(
                employee.EmployeeId,
                today.Year,
                today.Month)
            .ToList();

        var attendanceLookup = attendances
            .Where(a => a.CheckIn != null || a.CheckOut != null)
            .GroupBy(a => (a.CheckIn ?? a.CheckOut)!.Value.Date)
            .ToDictionary(g => g.Key, g => g.First());

        for (int i = 0; i < 7; i++)
        {
            var date = weekStart.AddDays(i);

            attendanceLookup.TryGetValue(
                date.Date,
                out var attendance);

            result.Add(new WeekDayItem
            {
                Date = date,
                IsToday = date.Date == today,
                HasAttendance = attendance != null,
                IsCheckedIn = attendance?.CheckIn != null,
                IsCheckedOut = attendance?.CheckOut != null,
                Status = attendance?.Status ?? string.Empty
            });
        }

        return result;
    }

    private ObservableCollection<WeeklyHourPoint> BuildWeeklyHours(Employee employee)
    {
        var result = new ObservableCollection<WeeklyHourPoint>();

        var today = DateTime.Today;
        var weekStart = GetWeekStart(today);

        var attendances = _dashboardRepository
            .GetEmployeeAttendanceMonth(
                employee.EmployeeId,
                today.Year,
                today.Month)
            .ToList();

        var attendanceLookup = attendances
            .Where(a => a.CheckIn != null || a.CheckOut != null)
            .GroupBy(a => (a.CheckIn ?? a.CheckOut)!.Value.Date)
            .ToDictionary(g => g.Key, g => g.First());

        for (int i = 0; i < 7; i++)
        {
            var date = weekStart.AddDays(i);

            attendanceLookup.TryGetValue(
                date.Date,
                out var attendance);

            result.Add(new WeeklyHourPoint
            {
                Date = date,
                Hours = attendance == null
                    ? 0
                    : CalculateHours(attendance)
            });
        }

        return result;
    }

    private EmployeeAnalytics BuildEmployeeAnalytics(Employee employee)
    {
        var today = DateTime.Today;

        var attendances = _dashboardRepository
            .GetEmployeeAttendanceMonth(
                employee.EmployeeId,
                today.Year,
                today.Month);

        double hoursThisMonth = attendances.Sum(CalculateHours);

        var todayAttendance =
            _attendanceService.GetTodayAttendance(employee.EmployeeId);

        string attendanceText =
            todayAttendance?.Status ?? "Not Checked In";

        return new EmployeeAnalytics
        {
            HoursThisMonth = Math.Round(hoursThisMonth, 2),

            TodayAttendance = attendanceText,

            LatestEvaluation =
                _dashboardRepository.GetLatestEvaluation(
                    employee.EmployeeId),

            TotalBonusThisMonth =
                _dashboardRepository.GetTotalBonusThisMonth(
                    employee.EmployeeId,
                    today.Year,
                    today.Month)
        };
    }

    private ManagerAnalytics BuildManagerAnalytics(Employee employee)
    {
        var today = DateTime.Today;

        return new ManagerAnalytics
        {
            TeamHoursThisMonth =
                Math.Round(
                    _dashboardRepository.GetDepartmentHoursThisMonth(
                        employee.DepartmentId,
                        today.Year,
                        today.Month),
                    2),

            TodayAttendanceCount =
                _dashboardRepository.GetTodayAttendanceCount(
                    employee.DepartmentId),

            TotalEmployees =
                _dashboardRepository
                    .GetDepartmentEmployees(
                        employee.DepartmentId)
                    .Count()
        };
    }
    #endregion
}