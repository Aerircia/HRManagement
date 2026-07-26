namespace HRManagement.Models;

public class AttendanceMonthSummary
{
    public int TotalDaysWorked { get; set; }

    public int TotalOnTime { get; set; }

    public int TotalLate { get; set; }

    public int TotalLateMinutes { get; set; }

    public int TotalAbsent { get; set; }
}