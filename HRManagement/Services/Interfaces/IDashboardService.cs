using System.Collections.ObjectModel;
using HRManagement.Models;
using HRManagement.Models.Dashboard;

namespace HRManagement.Services.Interfaces;

public interface IDashboardService
{
    /// <summary>
    /// Builds the complete dashboard data for the currently logged-in user.
    /// </summary>
    DashboardData LoadDashboard();

    /// <summary>
    /// Reloads all dashboard data after an action (check in/out, etc.).
    /// </summary>

    #region Attendance

    bool CanCheckIn();

    bool CanCheckOut();

    void CheckIn();

    void CheckOut();

    Attendance? GetTodayAttendance();

    #endregion

    #region Calendar

    ObservableCollection<WeekDayItem> GetCurrentWeek();

    #endregion

    #region Charts

    /// <summary>
    /// Monthly payout history for the current employee, from January of the
    /// current year through the current month. Backed by
    /// IManageSalariesService.GetEmployeeSalary, which is the same salary
    /// pipeline (SalaryCalculator) used by Manage Salaries - not a
    /// dashboard-only reimplementation of the payout formula.
    /// </summary>
    ObservableCollection<MonthlyPayoutPoint> GetMonthlyPayoutHistory();

    #endregion

    #region Analytics

    EmployeeAnalytics GetEmployeeAnalytics();

    ManagerAnalytics? GetManagerAnalytics();

    #endregion

    #region Announcement

    ObservableCollection<Announcement> GetAnnouncements();

    #endregion
}