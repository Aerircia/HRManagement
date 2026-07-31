namespace HRManagement.Models;

public class SalaryAttendanceSummary
{
    /*
     * Actual Monday-Friday count of the selected calendar month.
     *
     * SalaryCalculator uses this value as the denominator when
     * calculating the daily salary for the month.
     */
    public int CalendarWorkingDays { get; set; }

    /*
     * Actual weekday count the employee is eligible to work.
     *
     * This excludes weekdays before HireDate and, for the current
     * month, excludes future weekdays.
     */
    public int EffectiveWorkingDays { get; set; }

    // Attendance breakdown

    public int PresentDays { get; set; }

    public int LateDays { get; set; }

    public int LateMinutes { get; set; }

    public int WeekdayOtDays { get; set; }

    public int WeekendOtDays { get; set; }

    public int WeekdayOtMinutes { get; set; }

    public int WeekendOtMinutes { get; set; }

    public int PaidDayOffDays { get; set; }

    public int UnpaidDayOffDays { get; set; }

    public int AbsentDays { get; set; }

    // Calculated OT values

    public decimal WeekdayOtHours =>
        WeekdayOtMinutes / 60m;

    public decimal WeekendOtHours =>
        WeekendOtMinutes / 60m;

    public decimal TotalOtHours =>
        WeekdayOtHours
        + WeekendOtHours;

    public int TotalDayOffDays =>
        PaidDayOffDays
        + UnpaidDayOffDays;

    /*
     * Weekday attendance coverage.
     *
     * Weekend OT is intentionally excluded because it lies outside
     * the normal Monday-Friday working calendar.
     */
    public int CoveredEffectiveWorkingDays =>
        PresentDays
        + LateDays
        + WeekdayOtDays
        + TotalDayOffDays;

    public int TotalWorkingDays =>
        PresentDays
        + LateDays
        + WeekdayOtDays
        + WeekendOtDays;

    public static SalaryAttendanceSummary Create(
        AttendanceMonthSummary attendance,
        PaidTimeOffSummary paidTimeOff)
    {
        if (attendance == null)
        {
            throw new ArgumentNullException(
                nameof(attendance));
        }

        if (paidTimeOff == null)
        {
            throw new ArgumentNullException(
                nameof(paidTimeOff));
        }

        /*
         * Attendance is the source of truth for how many Day Off
         * records actually exist in the selected month.
         *
         * PTO only decides how many of those days are paid.
         */
        var paidDayOffDays =
            Math.Min(
                attendance.TotalDayOffDays,
                paidTimeOff.PaidDayOffDays);

        var unpaidDayOffDays =
            Math.Max(
                0,
                attendance.TotalDayOffDays
                - paidDayOffDays);

        return new SalaryAttendanceSummary
        {
            CalendarWorkingDays =
                attendance.CalendarWorkingDays,

            EffectiveWorkingDays =
                attendance.EffectiveWorkingDays,

            PresentDays =
                attendance.TotalOnTime,

            LateDays =
                attendance.TotalLate,

            LateMinutes =
                attendance.TotalLateMinutes,

            WeekdayOtDays =
                attendance.TotalWeekdayOtDays,

            WeekendOtDays =
                attendance.TotalWeekendOtDays,

            WeekdayOtMinutes =
                attendance.TotalWeekdayOtMinutes,

            WeekendOtMinutes =
                attendance.TotalWeekendOtMinutes,

            PaidDayOffDays =
                paidDayOffDays,

            UnpaidDayOffDays =
                unpaidDayOffDays,

            AbsentDays =
                attendance.TotalAbsent
        };
    }
}
