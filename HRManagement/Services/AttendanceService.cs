using HRManagement.Models;
using HRManagement.Repositories;
using HRManagement.Repositories.Interfaces;
using HRManagement.Services.Interfaces;

namespace HRManagement.Services;

public class AttendanceService : IAttendanceService
{
    private const int LateGraceMinutes = 5;
    private static readonly TimeSpan ShiftStart = TimeSpan.FromHours(8);

    private readonly IAttendanceRepository _attendanceRepository;

    public AttendanceService(IAttendanceRepository attendanceRepository)
    {
        _attendanceRepository = attendanceRepository;
        _attendanceRepository.OnAttendanceChanged += (sender, e) => AttendanceChanged?.Invoke(sender, e);
    }

    public event EventHandler<AttendanceChangedEventArgs>? AttendanceChanged;

    public List<AttendanceDayModel> BuildMonth(int employeeId, DateTime hireDate, DateTime monthStart)
    {
        var first = new DateTime(monthStart.Year, monthStart.Month, 1);
        var daysInMonth = DateTime.DaysInMonth(monthStart.Year, monthStart.Month);

        var gridStart = first;
        while (gridStart.DayOfWeek != DayOfWeek.Monday)
            gridStart = gridStart.AddDays(-1);

        var totalCells = (int)Math.Ceiling((first.AddDays(daysInMonth) - gridStart).TotalDays);
        while (totalCells % 7 != 0)
            totalCells++;

        var records = _attendanceRepository
            .GetAttendancesForEmployeeMonth(employeeId, monthStart.Year, monthStart.Month)
            .GroupBy(a => (a.CheckIn ?? a.CheckOut ?? DateTime.Now).Date)
            .ToDictionary(g => g.Key, g => g.First());

        var days = new List<AttendanceDayModel>(totalCells);

        for (var i = 0; i < totalCells; i++)
        {
            var date = gridStart.AddDays(i);

            var day = new AttendanceDayModel
            {
                Date = date,
                IsCurrentMonth = date.Month == monthStart.Month,
                IsWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday,
                IsBeforeHireDate = date.Date < hireDate.Date,
                IsFuture = date.Date > DateTime.Now.Date
            };

            records.TryGetValue(date.Date, out var record);

            if (record != null)
            {
                day.CheckIn = record.CheckIn;
                day.CheckOut = record.CheckOut;
            }

            day.Status = ResolveStatus(day, record?.Status);
            day.LatenessMinutes = day.Status == "Late"
                ? (int)Math.Round((day.CheckIn!.Value - day.Date.Add(ShiftStart)).TotalMinutes)
                : null;

            days.Add(day);
        }

        return days;
    }

    public AttendanceMonthSummary GetMonthSummary(int employeeId, DateTime hireDate, DateTime viewMonth)
    {
        var hire = hireDate.Date;

        var allRecords = new List<Attendance>();
        var cursor = new DateTime(hire.Year, hire.Month, 1);
        var searchEnd = DateTime.Now.Date;

        while (cursor <= searchEnd)
        {
            allRecords.AddRange(_attendanceRepository.GetAttendancesForEmployeeMonth(employeeId, cursor.Year, cursor.Month));
            cursor = cursor.AddMonths(1);
        }

        var lastDataDate = allRecords
            .Select(a => (a.CheckIn ?? a.CheckOut ?? DateTime.Now).Date)
            .DefaultIfEmpty(hire)
            .Max();
        var lastDataMonthStart = new DateTime(lastDataDate.Year, lastDataDate.Month, 1);

        var viewMonthStart = new DateTime(viewMonth.Year, viewMonth.Month, 1);
        var accumulateMonthStart = viewMonthStart <= lastDataMonthStart ? viewMonthStart : lastDataMonthStart;

        var accumulateEnd = accumulateMonthStart.AddMonths(1).AddDays(-1);
        if (accumulateEnd > DateTime.Now.Date)
            accumulateEnd = DateTime.Now.Date;

        var grouped = allRecords
            .Where(a => (a.CheckIn ?? a.CheckOut ?? DateTime.Now).Date <= accumulateEnd)
            .GroupBy(a => (a.CheckIn ?? a.CheckOut ?? DateTime.Now).Date)
            .Select(g => new
            {
                Date = g.Key,
                EarliestCheckIn = g.Where(x => x.CheckIn.HasValue)
                    .Select(x => x.CheckIn!.Value)
                    .OrderBy(x => x)
                    .Cast<DateTime?>()
                    .FirstOrDefault(),
                HasCheckIn = g.Any(x => x.CheckIn.HasValue),
                Status = g.First().Status,
                CheckIn = g.First().CheckIn,
                CheckOut = g.First().CheckOut
            })
            .ToList();

        var totalWorkingDays = 0;
        for (var d = hire; d <= accumulateEnd; d = d.AddDays(1))
        {
            if (d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                continue;

            totalWorkingDays++;
        }

        var daysWorked = grouped.Count(g => g.HasCheckIn && g.Date >= hire);

        var onTime = 0;
        var late = 0;
        var lateMinutes = 0;

        foreach (var g in grouped.Where(g => g.HasCheckIn && g.EarliestCheckIn.HasValue))
        {
            var minutesLate = (int)Math.Round((g.EarliestCheckIn!.Value - g.Date.Add(ShiftStart)).TotalMinutes);

            if (minutesLate <= LateGraceMinutes)
                onTime++;
            else
            {
                late++;
                lateMinutes += Math.Max(0, minutesLate);
            }
        }

        var otDays = grouped.Count(g => string.Equals(g.Status, "OT", StringComparison.OrdinalIgnoreCase));
        var dayOffDays = grouped.Count(g => string.Equals(g.Status, "Day Off", StringComparison.OrdinalIgnoreCase));

        var totalWorkedMinutes = grouped
            .Where(g => g.CheckIn.HasValue && g.CheckOut.HasValue && g.CheckOut.Value > g.CheckIn.Value)
            .Sum(g => (int)Math.Round((g.CheckOut!.Value - g.CheckIn!.Value).TotalMinutes));

        return new AttendanceMonthSummary
        {
            TotalDaysWorked = daysWorked,
            TotalOnTime = onTime,
            TotalLate = late,
            TotalLateMinutes = lateMinutes,
            TotalAbsent = Math.Max(0, totalWorkingDays - daysWorked),
            TotalOtDays = otDays,
            TotalDayOffDays = dayOffDays,
            TotalWorkedMinutes = totalWorkedMinutes
        };
    }

    public bool CanCheckIn(int employeeId, DateTime hireDate)
    {
        var today = DateTime.Now.Date;
        if (today < hireDate.Date)
            return false;

        var existing = FindToday(employeeId);
        if (existing?.CheckIn != null)
            return false;

        if (today.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return string.Equals(existing?.Status, "OT", StringComparison.OrdinalIgnoreCase);

        return true;
    }

    public bool CanCheckOut(int employeeId, DateTime hireDate)
    {
        var today = DateTime.Now.Date;
        if (today < hireDate.Date)
            return false;

        var existing = FindToday(employeeId);
        if (existing?.CheckIn == null || existing.CheckOut != null)
            return false;

        if (today.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return string.Equals(existing.Status, "OT", StringComparison.OrdinalIgnoreCase);

        return true;
    }

    public void CheckIn(int employeeId)
    {
        var now = DateTime.Now;
        var threshold = DateTime.Today.Add(ShiftStart).AddMinutes(LateGraceMinutes);
        var status = now <= threshold ? "Present" : "Late";

        _attendanceRepository.UpsertAttendance(new Attendance
        {
            EmployeeId = employeeId,
            CheckIn = now,
            Status = status
        });
    }

    public void CheckOut(int employeeId)
    {
        var today = FindToday(employeeId);

        _attendanceRepository.UpsertAttendance(new Attendance
        {
            EmployeeId = employeeId,
            CheckIn = today?.CheckIn,
            CheckOut = DateTime.Now,
            Status = string.IsNullOrEmpty(today?.Status) ? "Present" : today.Status
        });
    }

    public void ScheduleOt(int employeeId, DateTime date, TimeSpan start, TimeSpan end)
    {
        _attendanceRepository.UpsertAttendance(new Attendance
        {
            EmployeeId = employeeId,
            CheckIn = date.Date + start,
            CheckOut = date.Date + end,
            Status = "OT"
        });
    }
    public void ScheduleDayOff(int employeeId, DateTime startDate, DateTime endDate)
    {
        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            _attendanceRepository.UpsertAttendance(new Attendance
            {
                EmployeeId = employeeId,
                CheckIn = date, // Used to correctly bucket the date in the repository
                CheckOut = date,
                Status = "Day Off"
            });
        }
    }

    // Public "today's attendance record" lookup, used by ManageAttendancesViewModel
    // to populate per-row Check-in/Check-out-today columns without each caller
    // re-implementing the "find today's record" query (see FindToday below,
    // which this simply exposes through the interface).
    public Attendance? GetTodayAttendance(int employeeId) => FindToday(employeeId);

    private static string ResolveStatus(AttendanceDayModel day, string? dbStatus)
    {
        if (!day.IsCurrentMonth)
            return string.Empty;

        var isOt = string.Equals(dbStatus, "OT", StringComparison.OrdinalIgnoreCase);
        var isDayOff = string.Equals(dbStatus, "Day Off", StringComparison.OrdinalIgnoreCase);
        var hasRecord = day.CheckIn.HasValue || day.CheckOut.HasValue;

        if (isOt)
            return "OT";

        if (isDayOff)
            return "Day Off";

        if (day.IsWeekend)
            return hasRecord ? "OT" : "Weekend";

        if (day.IsFuture && !hasRecord)
            return string.Empty;

        if (day.IsBeforeHireDate)
            return "Before Hire Date";

        if (!day.CheckIn.HasValue)
            return day.IsFuture ? string.Empty : "Absent";

        var minutesLate = (int)Math.Round((day.CheckIn.Value - day.Date.Add(ShiftStart)).TotalMinutes);
        return minutesLate <= LateGraceMinutes ? "Present" : "Late";
    }

    private Attendance? FindToday(int employeeId) =>
        _attendanceRepository
            .GetAttendancesForEmployeeMonth(employeeId, DateTime.Now.Year, DateTime.Now.Month)
            .FirstOrDefault(a => (a.CheckIn ?? a.CheckOut ?? DateTime.MinValue).Date == DateTime.Now.Date);
}
