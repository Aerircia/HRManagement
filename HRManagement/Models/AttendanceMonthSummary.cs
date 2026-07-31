namespace HRManagement.Models;

public class AttendanceMonthSummary
{
    /*
     * Total weekday count (Monday-Friday) in the selected calendar month.
     *
     * This is the denominator used later by SalaryCalculator
     * when calculating the daily salary for that month.
     */
    public int CalendarWorkingDays { get; set; }

    /*
     * Weekday count that the employee is actually eligible to work.
     *
     * For an employee hired before or on the first day of the month:
     * EffectiveWorkingDays == CalendarWorkingDays.
     *
     * For an employee hired during the month:
     * weekdays before HireDate are excluded.
     */
    public int EffectiveWorkingDays { get; set; }

    // Regular attendance

    public int TotalDaysWorked { get; set; }

    public int TotalOnTime { get; set; }

    public int TotalLate { get; set; }

    public int TotalLateMinutes { get; set; }

    /*
     * Absent is derived by AttendanceService.
     *
     * Formula:
     * EffectiveWorkingDays
     * - Present
     * - Late
     * - Weekday OT
     * - Day Off
     */
    public int TotalAbsent { get; set; }

    public int TotalDayOffDays { get; set; }

    // Overtime

    /*
     * Weekday OT participates in weekday attendance coverage.
     * Salary coefficient is applied later:
     * Weekday OT = 1.5.
     */
    public int TotalWeekdayOtDays { get; set; }

    /*
     * Weekend OT is outside the normal weekday calendar.
     * It must not reduce TotalAbsent.
     *
     * Salary coefficient is applied later:
     * Weekend OT = 2.0.
     */
    public int TotalWeekendOtDays { get; set; }

    /*
     * Raw OT duration is stored in minutes to stay consistent
     * with TotalWorkedMinutes and avoid floating-point rounding.
     */
    public int TotalWeekdayOtMinutes { get; set; }

    public int TotalWeekendOtMinutes { get; set; }

    public decimal TotalWeekdayOtHours =>
        TotalWeekdayOtMinutes / 60m;

    public decimal TotalWeekendOtHours =>
        TotalWeekendOtMinutes / 60m;

    public int TotalOtDays =>
        TotalWeekdayOtDays
        + TotalWeekendOtDays;

    public decimal TotalOtHours =>
        TotalWeekdayOtHours
        + TotalWeekendOtHours;

    /*
     * Sum of (CheckOut - CheckIn) for attendance records
     * that contain a valid time range.
     */
    public int TotalWorkedMinutes { get; set; }
}
