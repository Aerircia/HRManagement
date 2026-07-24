using System;

namespace HRManagement.Models;

public class AttendanceChangedEventArgs : EventArgs
{
    public int EmployeeId { get; init; }
    public DateTime Date { get; init; }
}
