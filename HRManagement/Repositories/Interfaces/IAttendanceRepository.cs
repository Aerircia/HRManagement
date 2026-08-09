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

        /// <summary>
        /// Attendance rows with a non-null Check_in and a null Check_out -
        /// i.e. employees who checked in (manually, on a normal weekday)
        /// and never checked out. Scheduled OT/day-off rows always have
        /// both Check_in and Check_out set at creation time (see
        /// AttendanceService.ScheduleOt/ScheduleDayOff), so this can only
        /// ever surface manual weekday check-ins. No department filter is
        /// applied here - callers that need department scoping (Manager
        /// role) filter the returned set by Employee_ID membership
        /// themselves, mirroring the existing pattern in
        /// ManageAttendancesService.GetEmployeeAttendanceOverview.
        /// </summary>
        IEnumerable<Attendance> GetOpenCheckIns();

        /// <summary>
        /// Hard-deletes a single attendance row by ID. Used by the "Deny"
        /// action on the open check-ins grid to remove a bad/incomplete
        /// manual check-in entirely.
        /// </summary>
        void DeleteAttendance(int attendanceId);
    }
}
