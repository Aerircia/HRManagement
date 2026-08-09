using HRManagement.Models;
using HRManagement.Repositories;
using HRManagement.Repositories.Interfaces;
using HRManagement.Services.Interfaces;

namespace HRManagement.Services;

public class AttendanceService : IAttendanceService
{
    private const int LateGraceMinutes = 5;
    private static readonly TimeSpan ShiftStart = TimeSpan.FromHours(8);
    private static readonly TimeSpan ShiftEnd = TimeSpan.FromHours(17);

    // Employees can no longer check in more than 2 hours after the shift
    // start (i.e. after 10:00 AM). CheckIn is still marked "Late" as soon as
    // they're past the LateGraceMinutes window (8:05 AM) - this cutoff is a
    // separate, harder stop that blocks check-in entirely.
    private static readonly TimeSpan CheckInCutoff = ShiftStart.Add(TimeSpan.FromHours(2));

    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ILogService _logService;
    private readonly SessionManager _sessionManager;

    public AttendanceService(
        IAttendanceRepository attendanceRepository,
        IEmployeeRepository employeeRepository,
        ILogService logService,
        SessionManager sessionManager)
    {
        _attendanceRepository = attendanceRepository;

        _employeeRepository = employeeRepository
            ?? throw new ArgumentNullException(nameof(employeeRepository));

        _logService = logService
            ?? throw new ArgumentNullException(nameof(logService));

        _sessionManager = sessionManager
            ?? throw new ArgumentNullException(nameof(sessionManager));

        _attendanceRepository.OnAttendanceChanged += (sender, e) => AttendanceChanged?.Invoke(sender, e);
    }

    // Account_ID of whoever is currently logged in, or 0 when there's no
    // active session (mirrors the fallback already used by LogService's
    // other callers, e.g. AuthenticationService's failed-login log).
    private int CurrentAccountId =>
        _sessionManager.CurrentUser?.Account.AccountId ?? 0;

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

    public AttendanceMonthSummary GetMonthSummary(
        int employeeId,
        DateTime hireDate,
        DateTime viewMonth)
    {
        var monthStart =
            new DateTime(
                viewMonth.Year,
                viewMonth.Month,
                1);

        var monthEnd =
            monthStart.AddMonths(1);

        /*
         * CalendarWorkingDays:
         * all Monday-Friday days in the selected calendar month.
         *
         * SalaryCalculator will use this value as the denominator
         * for the monthly daily-rate calculation.
         */
        var calendarWorkingDays =
            CountWeekdays(
                monthStart,
                monthEnd.AddDays(-1));

        /*
         * Effective attendance range:
         * - never before the employee's HireDate;
         * - never after the selected month;
         * - for the current month, never after today;
         * - for a future month, there are no effective working days yet.
         */
        var effectiveStart =
            hireDate.Date > monthStart
                ? hireDate.Date
                : monthStart;

        var effectiveEnd =
            monthEnd.AddDays(-1);

        if (monthStart > DateTime.Today)
        {
            effectiveEnd =
                effectiveStart.AddDays(-1);
        }
        else if (effectiveEnd > DateTime.Today)
        {
            effectiveEnd =
                DateTime.Today;
        }

        var effectiveWorkingDays =
            effectiveEnd >= effectiveStart
                ? CountWeekdays(
                    effectiveStart,
                    effectiveEnd)
                : 0;

        var records =
            _attendanceRepository
                .GetAttendancesForEmployeeMonth(
                    employeeId,
                    viewMonth.Year,
                    viewMonth.Month)
                .Where(
                    attendance =>
                    {
                        var attendanceDate =
                            GetAttendanceDate(
                                attendance);

                        return attendanceDate.HasValue
                               && attendanceDate.Value
                                   >= effectiveStart
                               && attendanceDate.Value
                                   <= effectiveEnd;
                    })
                .GroupBy(
                    attendance =>
                        GetAttendanceDate(
                            attendance)!.Value)
                .Select(
                    group =>
                        new
                        {
                            Date =
                                group.Key,

                            Status =
                                group
                                    .Select(
                                        attendance =>
                                            attendance.Status)
                                    .FirstOrDefault(
                                        status =>
                                            !string.IsNullOrWhiteSpace(
                                                status))
                                ?? string.Empty,

                            EarliestCheckIn =
                                group
                                    .Where(
                                        attendance =>
                                            attendance.CheckIn.HasValue)
                                    .Select(
                                        attendance =>
                                            attendance.CheckIn!.Value)
                                    .OrderBy(
                                        checkIn =>
                                            checkIn)
                                    .Cast<DateTime?>()
                                    .FirstOrDefault(),

                            LatestCheckOut =
                                group
                                    .Where(
                                        attendance =>
                                            attendance.CheckOut.HasValue)
                                    .Select(
                                        attendance =>
                                            attendance.CheckOut!.Value)
                                    .OrderByDescending(
                                        checkOut =>
                                            checkOut)
                                    .Cast<DateTime?>()
                                    .FirstOrDefault()
                        })
                .ToList();

        var totalPresent = 0;

        var totalLate = 0;

        var totalLateMinutes = 0;

        var totalDayOffDays = 0;

        var totalWeekdayOtDays = 0;

        var totalWeekendOtDays = 0;

        var totalWeekdayOtMinutes = 0;

        var totalWeekendOtMinutes = 0;

        var totalWorkedMinutes = 0;

        foreach (var record in records)
        {
            var isWeekend =
                record.Date.DayOfWeek
                is DayOfWeek.Saturday
                or DayOfWeek.Sunday;

            var workedMinutes =
                CalculateWorkedMinutes(
                    record.EarliestCheckIn,
                    record.LatestCheckOut);

            if (string.Equals(
                    record.Status,
                    "OT",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (isWeekend)
                {
                    totalWeekendOtDays++;

                    totalWeekendOtMinutes +=
                        workedMinutes;
                }
                else
                {
                    /*
                     * Weekday OT replaces the normal weekday attendance
                     * status in the current project, so it covers one
                     * effective working day.
                     *
                     * Salary coefficient is applied later:
                     * Weekday OT = 1.5.
                     */
                    totalWeekdayOtDays++;

                    totalWeekdayOtMinutes +=
                        workedMinutes;
                }

                totalWorkedMinutes +=
                    workedMinutes;

                continue;
            }

            if (string.Equals(
                    record.Status,
                    "Day Off",
                    StringComparison.OrdinalIgnoreCase))
            {
                /*
                 * Day Off only covers a normal weekday.
                 * Paid/Unpaid allocation is handled by PTO service.
                 */
                if (!isWeekend)
                {
                    totalDayOffDays++;
                }

                continue;
            }

            if (string.Equals(
                    record.Status,
                    "Late",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (!isWeekend)
                {
                    totalLate++;

                    if (record.EarliestCheckIn.HasValue)
                    {
                        var shiftStart =
                            record.Date.Add(
                                ShiftStart);

                        var minutesLate =
                            (int)Math.Round(
                                (
                                    record.EarliestCheckIn.Value
                                    - shiftStart
                                ).TotalMinutes);

                        totalLateMinutes +=
                            Math.Max(
                                0,
                                minutesLate);
                    }
                }

                totalWorkedMinutes +=
                    workedMinutes;

                continue;
            }

            if (string.Equals(
                    record.Status,
                    "Present",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (!isWeekend)
                {
                    totalPresent++;
                }

                totalWorkedMinutes +=
                    workedMinutes;
            }
        }

        var coveredEffectiveWorkingDays =
            totalPresent
            + totalLate
            + totalWeekdayOtDays
            + totalDayOffDays;

        /*
         * Absent is derived only from days the employee was actually
         * expected to work.
         *
         * Weekend OT is intentionally excluded because it lies outside
         * the Monday-Friday working calendar.
         */
        var totalAbsent =
            Math.Max(
                0,
                effectiveWorkingDays
                - coveredEffectiveWorkingDays);

        return new AttendanceMonthSummary
        {
            CalendarWorkingDays =
                calendarWorkingDays,

            EffectiveWorkingDays =
                effectiveWorkingDays,

            TotalDaysWorked =
                totalPresent
                + totalLate
                + totalWeekdayOtDays
                + totalWeekendOtDays,

            TotalOnTime =
                totalPresent,

            TotalLate =
                totalLate,

            TotalLateMinutes =
                totalLateMinutes,

            TotalAbsent =
                totalAbsent,

            TotalWeekdayOtDays =
                totalWeekdayOtDays,

            TotalWeekendOtDays =
                totalWeekendOtDays,

            TotalWeekdayOtMinutes =
                totalWeekdayOtMinutes,

            TotalWeekendOtMinutes =
                totalWeekendOtMinutes,

            TotalDayOffDays =
                totalDayOffDays,

            TotalWorkedMinutes =
                totalWorkedMinutes
        };
    }

    private static int CountWeekdays(
        DateTime startDate,
        DateTime endDate)
    {
        if (endDate.Date < startDate.Date)
        {
            return 0;
        }

        var count = 0;

        for (
            var date = startDate.Date;
            date <= endDate.Date;
            date = date.AddDays(1))
        {
            if (date.DayOfWeek
                is DayOfWeek.Saturday
                or DayOfWeek.Sunday)
            {
                continue;
            }

            count++;
        }

        return count;
    }

    private static DateTime? GetAttendanceDate(
        Attendance attendance)
    {
        if (attendance.CheckIn.HasValue)
        {
            return attendance.CheckIn.Value.Date;
        }

        if (attendance.CheckOut.HasValue)
        {
            return attendance.CheckOut.Value.Date;
        }

        return null;
    }

    private static int CalculateWorkedMinutes(
        DateTime? checkIn,
        DateTime? checkOut)
    {
        if (!checkIn.HasValue
            || !checkOut.HasValue
            || checkOut.Value <= checkIn.Value)
        {
            return 0;
        }

        return Math.Max(
            0,
            (int)Math.Round(
                (
                    checkOut.Value
                    - checkIn.Value
                ).TotalMinutes));
    }

    public bool CanCheckIn(int employeeId, DateTime hireDate)
    {
        var now = DateTime.Now;
        var today = now.Date;
        if (today < hireDate.Date)
            return false;

        var existing = FindToday(employeeId);
        if (existing?.CheckIn != null)
            return false;

        if (today.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return string.Equals(existing?.Status, "OT", StringComparison.OrdinalIgnoreCase);

        // Weekday check-in is no longer allowed more than 2 hours after
        // shift start (i.e. after 10:00 AM).
        var cutoff = today.Add(CheckInCutoff);
        if (now > cutoff)
            return false;

        return true;
    }

    public bool CanCheckOut(int employeeId, DateTime hireDate)
    {
        var now = DateTime.Now;
        var today = now.Date;
        if (today < hireDate.Date)
            return false;

        var existing = FindToday(employeeId);
        if (existing?.CheckIn == null || existing.CheckOut != null)
            return false;

        if (today.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return string.Equals(existing.Status, "OT", StringComparison.OrdinalIgnoreCase);

        // Weekday check-out is no longer allowed before the 5:00 PM shift end.
        var shiftEnd = today.Add(ShiftEnd);
        if (now < shiftEnd)
            return false;

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

        _logService.WriteLog(CurrentAccountId, $"Employee {employeeId} checked in ({status})");
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

        _logService.WriteLog(CurrentAccountId, $"Employee {employeeId} checked out");
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

        _logService.WriteLog(CurrentAccountId, $"OT scheduled for employee {employeeId} on {date:yyyy-MM-dd} ({start:hh\\:mm}-{end:hh\\:mm})");
    }
    public void ScheduleDayOff(int employeeId, DateTime startDate, DateTime endDate)
    {
        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            // Skip weekends
            if (date.DayOfWeek == DayOfWeek.Saturday ||
                date.DayOfWeek == DayOfWeek.Sunday)
            {
                continue;
            }

            _attendanceRepository.UpsertAttendance(new Attendance
            {
                EmployeeId = employeeId,
                CheckIn = date, // Used to correctly bucket the date in the repository
                CheckOut = date,
                Status = "Day Off"
            });
        }

        _logService.WriteLog(CurrentAccountId, $"Day off scheduled for employee {employeeId} from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
    }

    // Public "today's attendance record" lookup, used by ManageAttendancesViewModel
    // to populate per-row Check-in/Check-out-today columns without each caller
    // re-implementing the "find today's record" query (see FindToday below,
    // which this simply exposes through the interface).
    public Attendance? GetTodayAttendance(int employeeId) => FindToday(employeeId);

    public List<OpenCheckInRow> GetOpenCheckIns()
    {
        var openRecords = _attendanceRepository.GetOpenCheckIns().ToList();

        if (openRecords.Count == 0)
            return [];

        // Batch-resolve employee names via GetAll() rather than one
        // GetById() call per row, matching the "load once, join in memory"
        // approach ManageAttendancesService already uses elsewhere.
        var employeeNames = _employeeRepository
            .GetAll()
            .ToDictionary(e => e.EmployeeId, e => e.FullName);

        return [.. openRecords
            .Where(a => a.CheckIn.HasValue)
            .Select(a => new OpenCheckInRow
            {
                AttendanceId = a.AttendanceId,
                EmployeeId = a.EmployeeId,
                EmployeeName = employeeNames.TryGetValue(a.EmployeeId, out var name)
                    ? name
                    : $"Employee {a.EmployeeId}",
                CheckIn = a.CheckIn!.Value,
                Status = a.Status
            })];
    }

    public void ApproveOpenCheckIn(int attendanceId)
    {
        var record = _attendanceRepository
            .GetOpenCheckIns()
            .FirstOrDefault(a => a.AttendanceId == attendanceId);

        if (record == null || !record.CheckIn.HasValue)
            return;

        var defaultCheckOut = record.CheckIn.Value.Date.Add(ShiftEnd);

        _attendanceRepository.UpsertAttendance(new Attendance
        {
            EmployeeId = record.EmployeeId,
            CheckIn = record.CheckIn,
            CheckOut = defaultCheckOut,
            Status = record.Status
        });

        _logService.WriteLog(
            CurrentAccountId,
            $"Approved open check-in {attendanceId} for employee {record.EmployeeId} " +
            $"(check-out defaulted to {defaultCheckOut:yyyy-MM-dd HH:mm})");
    }

    public void DenyOpenCheckIn(int attendanceId)
    {
        var record = _attendanceRepository
            .GetOpenCheckIns()
            .FirstOrDefault(a => a.AttendanceId == attendanceId);

        _attendanceRepository.DeleteAttendance(attendanceId);

        _logService.WriteLog(
            CurrentAccountId,
            record != null
                ? $"Denied and deleted open check-in {attendanceId} for employee {record.EmployeeId}"
                : $"Denied and deleted open check-in {attendanceId}");
    }

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
