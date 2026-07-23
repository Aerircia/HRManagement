using System;

namespace HRManagement.ViewModels
{
    public class EmployeeAttendanceDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public int WorkingDays { get; set; }
        public int OnTimeDays { get; set; }
        public int LateDays { get; set; }
        public int AbsentDays { get; set; }
        // This represents the count of OT assignments (integer)
        public int TotalOtHours { get; set; }
    }
}
