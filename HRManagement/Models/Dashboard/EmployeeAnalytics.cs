using HRManagement.Models;

namespace HRManagement.Models.Dashboard;

public class EmployeeAnalytics
{
    public double HoursThisMonth { get; set; }

    /// <summary>
    /// 8 * (number of Mon-Fri weekdays in the current calendar month),
    /// i.e. the "expected" hours target for the month. Used as the
    /// denominator for HoursThisMonthDisplay (e.g. "85.5 / 168 hrs").
    /// </summary>
    public double TargetHoursThisMonth { get; set; }

    public double HoursProgress =>
        TargetHoursThisMonth > 0
            ? Math.Min(100, HoursThisMonth / TargetHoursThisMonth * 100)
            : 0;

    public string HoursThisMonthDisplay =>
        $"{HoursThisMonth:0.##} / {TargetHoursThisMonth:0.##} hrs";

    public string TodayAttendance { get; set; } = "Not checked in";

    public EmployeeEvaluation? LatestEvaluation { get; set; }

    public decimal TotalBonusThisMonth { get; set; }
}