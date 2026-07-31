namespace HRManagement.Models;

public class AttendanceMonthSummary
{
    public int TotalDaysWorked { get; set; }

    public int TotalOnTime { get; set; }

    public int TotalLate { get; set; }

    public int TotalLateMinutes { get; set; }

    public int TotalAbsent { get; set; }

    public int TotalOtDays { get; set; }

    public int TotalDayOffDays { get; set; }

    // Sum of (CheckOut - CheckIn) across days with both values present,
    // within the accumulation range. Stored as raw minutes so consumers can
    // format however they like (e.g. "142h 30m").
    public int TotalWorkedMinutes { get; set; }
}
