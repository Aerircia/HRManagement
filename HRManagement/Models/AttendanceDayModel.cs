namespace HRManagement.Models;

// Pure data for one calendar cell. Deliberately has no Brush/Background -
// that's a View concern and is resolved by AttendanceStatusToBrushConverter
// from the Status string instead.
public class AttendanceDayModel
{
    public DateTime Date { get; set; }

    public bool IsCurrentMonth { get; set; }

    public bool IsWeekend { get; set; }

    public bool IsBeforeHireDate { get; set; }

    public bool IsFuture { get; set; }

    public DateTime? CheckIn { get; set; }

    public DateTime? CheckOut { get; set; }

    // "Present" | "Late" | "Absent" | "OT" | "Weekend" | "Before Hire Date" | ""
    public string Status { get; set; } = string.Empty;

    public int? LatenessMinutes { get; set; }
}