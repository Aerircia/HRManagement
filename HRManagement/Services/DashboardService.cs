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
    private readonly IManageSalariesService _manageSalariesService;
    private readonly SessionManager _sessionManager;

    public DashboardService(
        IDashboardRepository dashboardRepository,
        IAttendanceService attendanceService,
        IAuthorizationService authorizationService,
        IManageSalariesService manageSalariesService,
        SessionManager sessionManager)
    {
        _dashboardRepository = dashboardRepository;
        _attendanceService = attendanceService;
        _authorizationService = authorizationService;
        _manageSalariesService = manageSalariesService;
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

            MonthlyPayoutHistory = BuildMonthlyPayoutHistory(employee),

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

    public ObservableCollection<MonthlyPayoutPoint> GetMonthlyPayoutHistory()
    {
        return BuildMonthlyPayoutHistory(GetCurrentEmployee());
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

    /// <summary>
    /// Builds the "Monthly payout overview" line chart data: full payout
    /// (base salary + overtime + reward, minus deductions) from January of
    /// the current year through the current month. Reuses
    /// IManageSalariesService.GetEmployeeSalary, which already runs the
    /// employee's contract/role/attendance/evaluation data through
    /// ISalaryCalculator - the same pipeline Manage Salaries uses - so the
    /// dashboard never re-derives the payout formula on its own.
    /// </summary>
    private ObservableCollection<MonthlyPayoutPoint> BuildMonthlyPayoutHistory(Employee employee)
    {
        var result = new ObservableCollection<MonthlyPayoutPoint>();

        var today = DateTime.Today;

        for (int month = 1; month <= today.Month; month++)
        {
            var salary = _manageSalariesService.GetEmployeeSalary(
                employee.EmployeeId,
                month,
                today.Year);

            result.Add(new MonthlyPayoutPoint
            {
                Month = month,
                Year = today.Year,
                TotalSalary = salary?.TotalSalary ?? 0,
                HasValidPayout = salary is { HasValidContract: true, HasValidPosition: true }
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

        var weekdaysInMonth = CountWeekdaysInMonth(today.Year, today.Month);
        var targetHoursThisMonth = weekdaysInMonth * 8;

        var todayAttendance =
            _attendanceService.GetTodayAttendance(employee.EmployeeId);

        string attendanceText =
            todayAttendance?.Status ?? "Not Checked In";

        return new EmployeeAnalytics
        {
            HoursThisMonth = Math.Round(hoursThisMonth, 2),

            TargetHoursThisMonth = targetHoursThisMonth,

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

    /// <summary>
    /// Number of Monday-Friday days in the given calendar month (i.e. all
    /// days excluding Saturday/Sunday), used as the denominator for the
    /// "Hours this month" target (8 hrs * weekday count).
    /// </summary>
    private static int CountWeekdaysInMonth(int year, int month)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var count = 0;

        for (var day = 1; day <= daysInMonth; day++)
        {
            var date = new DateTime(year, month, day);
            if (date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
                count++;
        }

        return count;
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

    private ManagerAnalytics BuildManagerAnalytics(Employee employee)
    {
        var today = DateTime.Today;
        var weekdaysInMonth = CountWeekdaysInMonth(today.Year, today.Month);

        // Admin sees the whole organization; Manager sees only their own
        // department ("team"). IsAdmin implies IsManager (see
        // AuthorizationService), so Admin is checked first.
        if (_authorizationService.IsAdmin)
        {
            var totalEmployees = _dashboardRepository.GetAllEmployees().Count();

            return new ManagerAnalytics
            {
                ScopeLabel = "Organization",

                TeamHoursThisMonth =
                    Math.Round(
                        _dashboardRepository.GetOrgHoursThisMonth(
                            today.Year,
                            today.Month),
                        2),

                TargetHoursThisMonth = weekdaysInMonth * 8 * totalEmployees,

                TodayAttendanceCount =
                    _dashboardRepository.GetTodayAttendanceCountOrgWide(),

                TotalEmployees = totalEmployees
            };
        }

        var teamEmployees = _dashboardRepository
            .GetDepartmentEmployees(employee.DepartmentId)
            .Count();

        return new ManagerAnalytics
        {
            ScopeLabel = "Team",

            TeamHoursThisMonth =
                Math.Round(
                    _dashboardRepository.GetDepartmentHoursThisMonth(
                        employee.DepartmentId,
                        today.Year,
                        today.Month),
                    2),

            TargetHoursThisMonth = weekdaysInMonth * 8 * teamEmployees,

            TodayAttendanceCount =
                _dashboardRepository.GetTodayAttendanceCount(
                    employee.DepartmentId),

            TotalEmployees = teamEmployees
        };
    }
    #endregion
}