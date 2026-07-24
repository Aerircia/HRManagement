using HRManagement.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace HRManagement.Repositories.Interfaces
{
    public interface IAttendanceRepository
    {
        event EventHandler<AttendanceChangedEventArgs>? OnAttendanceChanged;

        List<Attendance> GetByEmployeeForMonth(int employeeId, int year, int month);

        void UpsertAttendance(Attendance attendance);
    }
}
