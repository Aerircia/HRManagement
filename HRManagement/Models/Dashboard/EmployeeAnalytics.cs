using HRManagement.Models;

namespace HRManagement.Models.Dashboard;

public class EmployeeAnalytics
{
    public double HoursThisMonth { get; set; }

    public string TodayAttendance { get; set; } = "Not checked in";

    public EmployeeEvaluation? LatestEvaluation { get; set; }

    public decimal TotalBonusThisMonth { get; set; }
}