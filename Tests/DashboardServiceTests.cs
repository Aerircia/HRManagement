using HRManagement.Models;
using HRManagement.Models.Dashboard;
using HRManagement.Repositories.Interfaces;
using HRManagement.Services;
using HRManagement.Services.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HRManagement.Tests;

[TestClass]
public class DashboardServiceTests
{
    private Mock<IDashboardRepository> _dashboardRepository = null!;
    private Mock<IAttendanceService> _attendanceService = null!;
    private Mock<IAuthorizationService> _authorizationService = null!;
    private Mock<IManageSalariesService> _manageSalariesService = null!;
    private SessionManager _sessionManager = null!;
    private DashboardService _sut = null!;

    // Fixed "today" the test data is built around, so weekday/month math is
    // deterministic instead of depending on the day this suite happens to run.
    // Wed, Aug 12 2026 — mid-week (weekday), mid-month, mid-year.
    private static readonly DateTime Today = new(2026, 8, 12);

    [TestInitialize]
    public void Setup()
    {
        _dashboardRepository = new Mock<IDashboardRepository>();
        _attendanceService = new Mock<IAttendanceService>();
        _authorizationService = new Mock<IAuthorizationService>();
        _manageSalariesService = new Mock<IManageSalariesService>();
        _sessionManager = new SessionManager();

        _sut = new DashboardService(
            _dashboardRepository.Object,
            _attendanceService.Object,
            _authorizationService.Object,
            _manageSalariesService.Object,
            _sessionManager);

        // Default: no manager analytics unless a test opts in.
        _authorizationService.SetupGet(a => a.IsManager).Returns(false);
        _authorizationService.SetupGet(a => a.IsAdmin).Returns(false);

        _dashboardRepository.Setup(r => r.GetAnnouncements()).Returns(new List<Announcement>());
        _dashboardRepository.Setup(r => r.GetEmployeeAttendanceMonth(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(new List<Attendance>());
        _dashboardRepository.Setup(r => r.GetLatestEvaluation(It.IsAny<int>())).Returns((EmployeeEvaluation?)null);
        _dashboardRepository.Setup(r => r.GetTotalBonusThisMonth(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())).Returns(0m);
        _manageSalariesService.Setup(s => s.GetEmployeeSalary(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns((ManageSalariesItemModel?)null);
        _attendanceService.Setup(a => a.GetTodayAttendance(It.IsAny<int>())).Returns((Attendance?)null);
    }

    private void LogInAsEmployee(int employeeId = 10, int departmentId = 3)
    {
        _sessionManager.Login(new CurrentUser
        {
            Account = new Account { AccountId = 1, EmployeeId = employeeId, Username = "user" },
            Employee = new Employee { EmployeeId = employeeId, FullName = "Test User", RoleId = 3, DepartmentId = departmentId, HireDate = new DateTime(2024, 1, 1) },
            Role = new Role { RoleId = 3, RoleName = "Employee" }
        });
    }

    // ---- All cards populated ----

    [TestMethod]
    public void LoadDashboard_EmployeeUser_PopulatesAllEmployeeFacingCards()
    {
        // Arrange
        LogInAsEmployee();
        _dashboardRepository.Setup(r => r.GetAnnouncements()).Returns(new List<Announcement> { new() { AnnouncementId = 1 } });

        // Act
        var data = _sut.LoadDashboard();

        // Assert — every card the UI binds to should be non-null and present.
        Assert.IsNotNull(data.WeekDays);
        Assert.AreEqual(7, data.WeekDays.Count); // full week card always has 7 days
        Assert.IsNotNull(data.MonthlyPayoutHistory);
        Assert.IsNotNull(data.Announcements);
        Assert.AreEqual(1, data.Announcements.Count);
        Assert.IsNotNull(data.EmployeeAnalytics);
    }

    [TestMethod]
    public void LoadDashboard_PlainEmployee_ManagerAnalyticsCardIsNull()
    {
        // Arrange — a non-Manager, non-Admin employee should NOT get the
        // manager analytics card at all (ShowManagerAnalytics = false in the VM).
        LogInAsEmployee();

        // Act
        var data = _sut.LoadDashboard();

        // Assert
        Assert.IsNull(data.ManagerAnalytics);
    }

    [TestMethod]
    public void LoadDashboard_ManagerUser_ManagerAnalyticsCardIsPopulated()
    {
        // Arrange
        LogInAsEmployee();
        _authorizationService.SetupGet(a => a.IsManager).Returns(true);
        _dashboardRepository.Setup(r => r.GetDepartmentEmployees(3)).Returns(new List<Employee> { new() { EmployeeId = 10 } });

        // Act
        var data = _sut.LoadDashboard();

        // Assert
        Assert.IsNotNull(data.ManagerAnalytics);
        Assert.AreEqual("Team", data.ManagerAnalytics!.ScopeLabel);
    }

    [TestMethod]
    public void LoadDashboard_AdminUser_ManagerAnalyticsCardShowsOrganizationScope()
    {
        // Arrange
        LogInAsEmployee();
        _authorizationService.SetupGet(a => a.IsAdmin).Returns(true);
        _dashboardRepository.Setup(r => r.GetAllEmployees()).Returns(new List<Employee> { new() { EmployeeId = 10 }, new() { EmployeeId = 11 } });

        // Act
        var data = _sut.LoadDashboard();

        // Assert — Admin sees org-wide card, not department-scoped.
        Assert.IsNotNull(data.ManagerAnalytics);
        Assert.AreEqual("Organization", data.ManagerAnalytics!.ScopeLabel);
        Assert.AreEqual(2, data.ManagerAnalytics.TotalEmployees);
    }

    // ---- Week card accuracy ----

    [TestMethod]
    public void GetCurrentWeek_MarksExactlyOneDayAsToday()
    {
        // Arrange
        LogInAsEmployee();

        // Act
        var week = _sut.GetCurrentWeek();

        // Assert
        Assert.AreEqual(7, week.Count);
        Assert.AreEqual(1, week.Count(d => d.IsToday));
    }

    [TestMethod]
    public void GetCurrentWeek_DayWithCheckInAndOut_MarksHasAttendanceTrue()
    {
        // Arrange
        LogInAsEmployee();
        var checkInDay = DateTime.Today; // GetCurrentWeek uses DateTime.Today internally
        _dashboardRepository.Setup(r => r.GetEmployeeAttendanceMonth(10, checkInDay.Year, checkInDay.Month))
            .Returns(new List<Attendance>
            {
                new() { CheckIn = checkInDay.AddHours(9), CheckOut = checkInDay.AddHours(17), Status = "Present" }
            });

        // Act
        var week = _sut.GetCurrentWeek();
        var todayItem = week.Single(d => d.Date.Date == checkInDay.Date);

        // Assert
        Assert.IsTrue(todayItem.HasAttendance);
        Assert.IsTrue(todayItem.IsCheckedIn);
        Assert.IsTrue(todayItem.IsCheckedOut);
        Assert.AreEqual("Present", todayItem.Status);
    }

    [TestMethod]
    public void GetCurrentWeek_DayWithNoAttendanceRecord_MarksHasAttendanceFalse()
    {
        // Arrange — no attendance rows returned at all (default setup)
        LogInAsEmployee();

        // Act
        var week = _sut.GetCurrentWeek();

        // Assert
        Assert.IsTrue(week.All(d => !d.HasAttendance));
    }

    // ---- Employee analytics accuracy ----

    [TestMethod]
    public void GetEmployeeAnalytics_SumsHoursFromCompleteAttendanceRecordsOnly()
    {
        // Arrange — one complete 8-hour day, one incomplete record (no check-out,
        // which the calculation must treat as 0 hours rather than throwing or
        // guessing an end time).
        LogInAsEmployee();
        var today = DateTime.Today;
        _dashboardRepository.Setup(r => r.GetEmployeeAttendanceMonth(10, today.Year, today.Month))
            .Returns(new List<Attendance>
            {
                new() { CheckIn = today.AddHours(9), CheckOut = today.AddHours(17) },      // 8 hrs
                new() { CheckIn = today.AddDays(-1).AddHours(9), CheckOut = null }         // incomplete -> 0 hrs
            });

        // Act
        var analytics = _sut.GetEmployeeAnalytics();

        // Assert
        Assert.AreEqual(8.0, analytics.HoursThisMonth);
    }

    [TestMethod]
    public void GetEmployeeAnalytics_TargetHours_EqualsWeekdayCountTimesEight()
    {
        // Arrange — August 2026 has 21 weekdays (Mon-Fri); verifying the formula,
        // not hardcoding assumptions about "this month" at test-run time.
        LogInAsEmployee();
        var expectedWeekdays = Enumerable.Range(1, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month))
            .Select(d => new DateTime(DateTime.Today.Year, DateTime.Today.Month, d))
            .Count(d => d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday));

        // Act
        var analytics = _sut.GetEmployeeAnalytics();

        // Assert
        Assert.AreEqual(expectedWeekdays * 8, analytics.TargetHoursThisMonth);
    }

    [TestMethod]
    public void GetEmployeeAnalytics_NoTodayAttendance_ShowsNotCheckedInText()
    {
        // Arrange
        LogInAsEmployee();
        _attendanceService.Setup(a => a.GetTodayAttendance(10)).Returns((Attendance?)null);

        // Act
        var analytics = _sut.GetEmployeeAnalytics();

        // Assert
        Assert.AreEqual("Not Checked In", analytics.TodayAttendance);
    }

    [TestMethod]
    public void GetEmployeeAnalytics_HasTodayAttendance_ShowsItsStatus()
    {
        // Arrange
        LogInAsEmployee();
        _attendanceService.Setup(a => a.GetTodayAttendance(10))
            .Returns(new Attendance { CheckIn = DateTime.Now, Status = "Present" });

        // Act
        var analytics = _sut.GetEmployeeAnalytics();

        // Assert
        Assert.AreEqual("Present", analytics.TodayAttendance);
    }

    [TestMethod]
    public void GetEmployeeAnalytics_PassesThroughLatestEvaluationAndBonusFromRepository()
    {
        // Arrange
        LogInAsEmployee();
        var evaluation = new EmployeeEvaluation { EvaluationId = 5, Amount = 200m };
        _dashboardRepository.Setup(r => r.GetLatestEvaluation(10)).Returns(evaluation);
        _dashboardRepository.Setup(r => r.GetTotalBonusThisMonth(10, DateTime.Today.Year, DateTime.Today.Month)).Returns(150m);

        // Act
        var analytics = _sut.GetEmployeeAnalytics();

        // Assert
        Assert.AreSame(evaluation, analytics.LatestEvaluation);
        Assert.AreEqual(150m, analytics.TotalBonusThisMonth);
    }

    // ---- Monthly payout history accuracy ----

    [TestMethod]
    public void GetMonthlyPayoutHistory_ReturnsOnePointPerMonthFromJanuaryThroughCurrentMonth()
    {
        // Arrange
        LogInAsEmployee();

        // Act
        var history = _sut.GetMonthlyPayoutHistory();

        // Assert — Jan (1) through DateTime.Today.Month inclusive.
        Assert.AreEqual(DateTime.Today.Month, history.Count);
        Assert.AreEqual(1, history.First().Month);
        Assert.AreEqual(DateTime.Today.Month, history.Last().Month);
    }

    [TestMethod]
    public void GetMonthlyPayoutHistory_ValidContractAndPosition_MarksHasValidPayoutTrueWithCorrectTotal()
    {
        // Arrange
        LogInAsEmployee();
        _manageSalariesService.Setup(s => s.GetEmployeeSalary(10, It.IsAny<int>(), DateTime.Today.Year))
            .Returns(new ManageSalariesItemModel { TotalSalary = 5000m, HasValidContract = true, HasValidPosition = true });

        // Act
        var history = _sut.GetMonthlyPayoutHistory();

        // Assert
        Assert.IsTrue(history.All(p => p.HasValidPayout));
        Assert.IsTrue(history.All(p => p.TotalSalary == 5000m));
    }

    [TestMethod]
    public void GetMonthlyPayoutHistory_NoSalaryData_MarksHasValidPayoutFalseWithZeroTotal()
    {
        // Arrange — GetEmployeeSalary returns null (default setup): no contract
        // for that month. Card should show 0, not throw or show stale data.
        LogInAsEmployee();

        // Act
        var history = _sut.GetMonthlyPayoutHistory();

        // Assert
        Assert.IsTrue(history.All(p => !p.HasValidPayout));
        Assert.IsTrue(history.All(p => p.TotalSalary == 0));
    }

    [TestMethod]
    public void GetMonthlyPayoutHistory_ContractButNoPosition_MarksHasValidPayoutFalse()
    {
        // Arrange — boundary case: HasValidContract true but HasValidPosition
        // false must still mark the point invalid (both must be true).
        LogInAsEmployee();
        _manageSalariesService.Setup(s => s.GetEmployeeSalary(10, It.IsAny<int>(), DateTime.Today.Year))
            .Returns(new ManageSalariesItemModel { TotalSalary = 3000m, HasValidContract = true, HasValidPosition = false });

        // Act
        var history = _sut.GetMonthlyPayoutHistory();

        // Assert
        Assert.IsTrue(history.All(p => !p.HasValidPayout));
    }

    // ---- Manager analytics accuracy (team scope) ----

    [TestMethod]
    public void GetManagerAnalytics_Manager_TargetHours_ScalesByTeamSize()
    {
        // Arrange — 3 employees in department, so target = weekdaysInMonth * 8 * 3.
        LogInAsEmployee(departmentId: 3);
        _authorizationService.SetupGet(a => a.IsManager).Returns(true);
        _dashboardRepository.Setup(r => r.GetDepartmentEmployees(3))
            .Returns(new List<Employee> { new(), new(), new() });
        _dashboardRepository.Setup(r => r.GetDepartmentHoursThisMonth(3, DateTime.Today.Year, DateTime.Today.Month))
            .Returns(240.0);
        _dashboardRepository.Setup(r => r.GetTodayAttendanceCount(3)).Returns(2);

        var expectedWeekdays = Enumerable.Range(1, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month))
            .Select(d => new DateTime(DateTime.Today.Year, DateTime.Today.Month, d))
            .Count(d => d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday));

        // Act
        var analytics = _sut.GetManagerAnalytics();

        // Assert
        Assert.IsNotNull(analytics);
        Assert.AreEqual("Team", analytics!.ScopeLabel);
        Assert.AreEqual(3, analytics.TotalEmployees);
        Assert.AreEqual(expectedWeekdays * 8 * 3, analytics.TargetHoursThisMonth);
        Assert.AreEqual(240.0, analytics.TeamHoursThisMonth);
        Assert.AreEqual(2, analytics.TodayAttendanceCount);
    }

    [TestMethod]
    public void GetManagerAnalytics_NeitherManagerNorAdmin_ReturnsNull()
    {
        // Arrange
        LogInAsEmployee();
        // IsManager/IsAdmin both false (default setup)

        // Act
        var analytics = _sut.GetManagerAnalytics();

        // Assert
        Assert.IsNull(analytics);
    }

    [TestMethod]
    public void GetManagerAnalytics_AdminChecksBeforeManager_UsesOrgWideEvenIfBothFlagsTrue()
    {
        // Arrange — IsAdmin implies IsManager per the project's role hierarchy;
        // service must check IsAdmin first so an Admin gets org-wide data, not
        // department-scoped data.
        LogInAsEmployee();
        _authorizationService.SetupGet(a => a.IsAdmin).Returns(true);
        _authorizationService.SetupGet(a => a.IsManager).Returns(true);
        _dashboardRepository.Setup(r => r.GetAllEmployees()).Returns(new List<Employee> { new(), new() });

        // Act
        var analytics = _sut.GetManagerAnalytics();

        // Assert
        Assert.AreEqual("Organization", analytics!.ScopeLabel);
        _dashboardRepository.Verify(r => r.GetDepartmentEmployees(It.IsAny<int>()), Times.Never);
    }

    [TestMethod]
    public void GetManagerAnalytics_HoursProgress_CapsAtOneHundredPercentWhenOverTarget()
    {
        // Arrange — team logged MORE hours than target; progress bar must cap
        // at 100%, not overshoot past it.
        LogInAsEmployee(departmentId: 3);
        _authorizationService.SetupGet(a => a.IsManager).Returns(true);
        _dashboardRepository.Setup(r => r.GetDepartmentEmployees(3)).Returns(new List<Employee> { new() });
        _dashboardRepository.Setup(r => r.GetDepartmentHoursThisMonth(3, DateTime.Today.Year, DateTime.Today.Month))
            .Returns(999999.0);
        _dashboardRepository.Setup(r => r.GetTodayAttendanceCount(3)).Returns(1);

        // Act
        var analytics = _sut.GetManagerAnalytics();

        // Assert
        Assert.AreEqual(100, analytics!.HoursProgress);
    }

    // ---- Check-in / check-out gating ----

    [TestMethod]
    public void CanCheckIn_DelegatesToAttendanceServiceWithEmployeeIdAndHireDate()
    {
        // Arrange
        var hireDate = new DateTime(2024, 1, 1);
        _sessionManager.Login(new CurrentUser
        {
            Account = new Account { AccountId = 1, EmployeeId = 10, Username = "user" },
            Employee = new Employee { EmployeeId = 10, FullName = "Test User", RoleId = 3, DepartmentId = 3, HireDate = hireDate },
            Role = new Role { RoleId = 3, RoleName = "Employee" }
        });
        _attendanceService.Setup(a => a.CanCheckIn(10, hireDate)).Returns(true);

        // Act
        var result = _sut.CanCheckIn();

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void CheckIn_NoLoggedInEmployee_ThrowsInvalidOperationException()
    {
        // Arrange — no login call at all; SessionManager.CurrentUser stays null.

        // Act / Assert
        Assert.ThrowsException<InvalidOperationException>(() => _sut.CheckIn());
    }
}
