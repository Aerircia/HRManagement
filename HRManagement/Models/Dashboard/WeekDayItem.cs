namespace HRManagement.Models.Dashboard;

public class WeekDayItem
{
    public DateTime Date { get; set; }

    public string DayName => Date.ToString("ddd");

    public bool IsToday { get; set; }

    public bool HasAttendance { get; set; }

    public bool IsCheckedIn { get; set; }

    public bool IsCheckedOut { get; set; }

    public string Status { get; set; } = string.Empty;
}