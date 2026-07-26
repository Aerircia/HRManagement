namespace HRManagement.Services.Interfaces;

public class MonthlyPayoutPoint
{
    public int Month { get; set; }
    public decimal Total { get; set; }
}

public class TodayAttendanceStat
{
    public int CheckedIn { get; set; }
    public int TotalEmployees { get; set; }
}

public interface IDashboardService
{
    /// <summary>
    /// Net salary totals for each month (1-12) of <paramref name="year"/>,
    /// summed across every employee in <paramref name="employeeIds"/>.
    /// Reuses ISalaryCalculator so the payout figure never drifts from the
    /// Salary page's own math. Months with no payroll created yet return 0.
    /// </summary>
    List<MonthlyPayoutPoint> GetMonthlyPayoutTotals(IReadOnlyList<int> employeeIds, int year);

    /// <summary>
    /// How many of the given employees have checked in today.
    /// </summary>
    TodayAttendanceStat GetTodayAttendanceStat(IReadOnlyList<int> employeeIds);
}
