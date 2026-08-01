namespace HRManagement.Models.Dashboard;

public class ManagerAnalytics
{
    /// <summary>
    /// "Team" for a Manager (department-scoped) or "Organization" for an
    /// Admin (org-wide). Drives the card's section labels so the same
    /// bindings read correctly for either role.
    /// </summary>
    public string ScopeLabel { get; set; } = "Team";

    public double TeamHoursThisMonth { get; set; }

    /// <summary>
    /// 8 * (number of Mon-Fri weekdays in the current calendar month) *
    /// (number of people in scope) - the expected hours target for the
    /// progress bar, mirroring EmployeeAnalytics.TargetHoursThisMonth but
    /// scaled to however many employees are being aggregated.
    /// </summary>
    public double TargetHoursThisMonth { get; set; }

    public double HoursProgress =>
        TargetHoursThisMonth > 0
            ? Math.Min(100, TeamHoursThisMonth / TargetHoursThisMonth * 100)
            : 0;

    public string HoursThisMonthDisplay =>
        $"{TeamHoursThisMonth:0.##} / {TargetHoursThisMonth:0.##} hrs";

    public int TodayAttendanceCount { get; set; }

    public int TotalEmployees { get; set; }
}