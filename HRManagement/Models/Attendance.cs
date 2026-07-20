namespace HRManagement.Models;

public class Attendance
{
    public int AttendanceId { get; set; }

    public int EmployeeId { get; set; }

    public DateTime? CheckIn { get; set; }

    public DateTime? CheckOut { get; set; }

    public string Status { get; set; } = string.Empty;
}