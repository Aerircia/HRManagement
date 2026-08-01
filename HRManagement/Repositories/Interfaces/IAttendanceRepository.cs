using HRManagement.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace HRManagement.Repositories.Interfaces
{
    public interface IAttendanceRepository
    {
        event EventHandler<AttendanceChangedEventArgs>? OnAttendanceChanged;

        public IEnumerable<Attendance> GetAttendancesForEmployeeMonth(int employeeId, int year, int month);

        void UpsertAttendance(Attendance attendance);
        public int GetTodayAttendanceCount(int departmentId);
        double GetDepartmentHoursThisMonth(int departmentId, int year, int month);

        /// <summary>
        /// Org-wide variant of GetTodayAttendanceCount: counts today's
        /// check-ins across every department (Admin scope).
        /// </summary>
        int GetTodayAttendanceCountOrgWide();

        /// <summary>
        /// Org-wide variant of GetDepartmentHoursThisMonth: sums worked
        /// hours across every department (Admin scope).
        /// </summary>
        double GetOrgHoursThisMonth(int year, int month);
    }
}
