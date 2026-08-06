using HRManagement.Models;
using HRManagement.Services.Interfaces;

namespace HRManagement.Services;

public class SalaryCalculator
    : ISalaryCalculator
{
    private const int StandardHoursPerDay = 8;

    private const decimal WeekdayOtCoefficient = 1.5m;

    private const decimal WeekendOtCoefficient = 2.0m;

    /*
     * Legacy calculation.
     *
     * This overload is kept temporarily to avoid breaking existing
     * ViewModels during migration. New salary code should use the
     * SalaryAttendanceSummary overload below.
     */
    public SalaryDetailModel CalculateSalary(
        Employee employee,
        Contract contract,
        Position position,
        IReadOnlyList<Attendance> attendances,
        IReadOnlyList<EmployeeEvaluation> evaluations,
        string departmentName,
        int month,
        int year)
    {
        ValidateCommonInputs(
            employee,
            contract,
            position,
            evaluations,
            departmentName,
            month,
            year);

        if (attendances == null)
        {
            throw new ArgumentNullException(
                nameof(attendances));
        }

        var workingDays =
            attendances.Count(
                attendance =>
                    string.Equals(
                        attendance.Status,
                        "Present",
                        StringComparison.OrdinalIgnoreCase));

        var calendarWorkingDays =
            CountWeekdaysInMonth(
                month,
                year);

        var absentDays =
            Math.Max(
                0,
                calendarWorkingDays
                - workingDays);

        var reward =
            CalculateReward(evaluations);

        var penalty =
            CalculatePenalty(evaluations);

        var roleSalary =
            contract.BaseSalary
            * position.PayRate;

        var dailySalary =
            calendarWorkingDays > 0
                ? roleSalary
                  / calendarWorkingDays
                : 0;

        var attendanceSalary =
            dailySalary
            * workingDays;

        var totalSalary =
            attendanceSalary
            + reward
            - penalty;

        return CreateSalaryDetail(
            employee,
            contract,
            position,
            departmentName,
            workingDays,
            absentDays,
            reward,
            penalty,
            totalSalary,
            month,
            year);
    }

    /*
     * New salary calculation.
     *
     * Base calculation:
     *
     * RoleSalary
     * + Weekday OT Salary
     * + Weekend OT Salary
     * + Reward
     * - Evaluation Penalty
     * - Late Penalty
     * - Absent Deduction
     * - Unpaid Day Off Deduction
     *
     * Paid Day Off receives 100% salary, therefore it does not
     * generate a deduction.
     */
    public SalaryDetailModel CalculateSalary(
        Employee employee,
        Contract contract,
        Position position,
        SalaryAttendanceSummary attendanceSummary,
        IReadOnlyList<EmployeeEvaluation> evaluations,
        string departmentName,
        int month,
        int year)
    {
        ValidateCommonInputs(
            employee,
            contract,
            position,
            evaluations,
            departmentName,
            month,
            year);

        if (attendanceSummary == null)
        {
            throw new ArgumentNullException(
                nameof(attendanceSummary));
        }

        ValidateAttendanceSummary(
            attendanceSummary);

        var reward =
            CalculateReward(evaluations);

        var evaluationPenalty =
            CalculatePenalty(evaluations);

        var fullMonthRoleSalary =
            contract.BaseSalary
            * position.PayRate;

        /*
         * Daily salary uses the actual weekday count
         * of the selected calendar month.
         */
        var dailySalary =
            attendanceSummary.CalendarWorkingDays > 0
                ? fullMonthRoleSalary
                  / attendanceSummary.CalendarWorkingDays
                : 0;

        /*
         * Employees hired during the month are prorated using
         * EffectiveWorkingDays. Weekdays before HireDate are
         * neither paid nor counted as absent.
         */
        var roleSalary =
            dailySalary
            * attendanceSummary.EffectiveWorkingDays;

        var hourlySalary =
            dailySalary
            / StandardHoursPerDay;

        /*
         * Late employees still receive the normal salary for the day.
         * The penalty only deducts the equivalent salary for the
         * accumulated late duration:
         *
         * LatePenalty = HourlySalary * (LateMinutes / 60)
         */
        var lateHours =
            attendanceSummary.LateMinutes
            / 60m;

        var latePenalty =
            hourlySalary
            * lateHours;

        var weekdayOtSalary =
            hourlySalary
            * attendanceSummary.WeekdayOtHours
            * WeekdayOtCoefficient;

        var weekendOtSalary =
            hourlySalary
            * attendanceSummary.WeekendOtHours
            * WeekendOtCoefficient;

        var overtimeSalary =
            weekdayOtSalary
            + weekendOtSalary;

        var absentDeduction =
            dailySalary
            * attendanceSummary.AbsentDays;

        var unpaidDayOffDeduction =
            dailySalary
            * attendanceSummary.UnpaidDayOffDays;

        var totalSalary =
            roleSalary
            + overtimeSalary
            + reward
            - evaluationPenalty
            - latePenalty
            - absentDeduction
            - unpaidDayOffDeduction;

        /*
         * Salary cannot be negative.
         *
         * This protects the payroll total when deductions are larger
         * than the employee's gross payable amount.
         */
        totalSalary =
            Math.Max(
                0,
                totalSalary);

        /*
         * WorkingDays is kept compatible with the existing UI.
         *
         * It represents standard paid-covered days:
         * Present + Late + Weekday OT + Paid Day Off.
         *
         * Weekend OT is additional work outside the 22 standard days.
         * Unpaid Day Off is approved leave but is not a paid day.
         */
        var workingDays =
            attendanceSummary.PresentDays
            + attendanceSummary.LateDays
            + attendanceSummary.WeekdayOtDays
            + attendanceSummary.PaidDayOffDays;

        return new SalaryDetailModel
        {
            EmployeeId =
                employee.EmployeeId,

            FullName =
                employee.FullName,

            DepartmentName =
                departmentName,

            PositionName =
                position.PositionName,

            BaseSalary =
                contract.BaseSalary,

            PayRate =
                position.PayRate,

            CalendarWorkingDays =
                attendanceSummary.CalendarWorkingDays,

            EffectiveWorkingDays =
                attendanceSummary.EffectiveWorkingDays,

            PresentDays =
                attendanceSummary.PresentDays,

            LateDays =
                attendanceSummary.LateDays,

            LateMinutes =
                attendanceSummary.LateMinutes,

            WeekdayOtDays =
                attendanceSummary.WeekdayOtDays,

            WeekendOtDays =
                attendanceSummary.WeekendOtDays,

            WeekdayOtHours =
                attendanceSummary.WeekdayOtHours,

            WeekendOtHours =
                attendanceSummary.WeekendOtHours,

            PaidDayOffDays =
                attendanceSummary.PaidDayOffDays,

            UnpaidDayOffDays =
                attendanceSummary.UnpaidDayOffDays,

            WorkingDays =
                workingDays,

            AbsentDays =
                attendanceSummary.AbsentDays,

            RoleSalary =
                decimal.Round(
                    roleSalary,
                    2,
                    MidpointRounding.AwayFromZero),

            DailySalary =
                decimal.Round(
                    dailySalary,
                    2,
                    MidpointRounding.AwayFromZero),

            HourlySalary =
                decimal.Round(
                    hourlySalary,
                    2,
                    MidpointRounding.AwayFromZero),

            WeekdayOtSalary =
                decimal.Round(
                    weekdayOtSalary,
                    2,
                    MidpointRounding.AwayFromZero),

            WeekendOtSalary =
                decimal.Round(
                    weekendOtSalary,
                    2,
                    MidpointRounding.AwayFromZero),

            Reward =
                decimal.Round(
                    reward,
                    2,
                    MidpointRounding.AwayFromZero),

            Penalty =
                decimal.Round(
                    evaluationPenalty,
                    2,
                    MidpointRounding.AwayFromZero),

            LatePenalty =
                decimal.Round(
                    latePenalty,
                    2,
                    MidpointRounding.AwayFromZero),

            AbsentDeduction =
                decimal.Round(
                    absentDeduction,
                    2,
                    MidpointRounding.AwayFromZero),

            UnpaidDayOffDeduction =
                decimal.Round(
                    unpaidDayOffDeduction,
                    2,
                    MidpointRounding.AwayFromZero),

            TotalSalary =
                decimal.Round(
                    totalSalary,
                    2,
                    MidpointRounding.AwayFromZero),

            Month =
                month,

            Year =
                year
        };
    }

    private static SalaryDetailModel CreateSalaryDetail(
        Employee employee,
        Contract contract,
        Position position,
        string departmentName,
        int workingDays,
        int absentDays,
        decimal reward,
        decimal penalty,
        decimal totalSalary,
        int month,
        int year)
    {
        return new SalaryDetailModel
        {
            EmployeeId =
                employee.EmployeeId,

            FullName =
                employee.FullName,

            DepartmentName =
                departmentName,

            PositionName =
                position.PositionName,

            BaseSalary =
                contract.BaseSalary,

            PayRate =
                position.PayRate,

            WorkingDays =
                workingDays,

            AbsentDays =
                absentDays,

            Reward =
                reward,

            Penalty =
                penalty,

            TotalSalary =
                decimal.Round(
                    totalSalary,
                    2,
                    MidpointRounding.AwayFromZero),

            Month =
                month,

            Year =
                year
        };
    }

    private static decimal CalculateReward(
        IReadOnlyList<EmployeeEvaluation> evaluations)
    {
        decimal reward = 0;

        foreach (var evaluation in evaluations)
        {
            if (string.Equals(
                    evaluation.BonusType,
                    "Reward",
                    StringComparison.OrdinalIgnoreCase))
            {
                reward +=
                    evaluation.Amount;
            }
        }

        return reward;
    }

    private static decimal CalculatePenalty(
        IReadOnlyList<EmployeeEvaluation> evaluations)
    {
        decimal penalty = 0;

        foreach (var evaluation in evaluations)
        {
            if (string.Equals(
                    evaluation.BonusType,
                    "Penalty",
                    StringComparison.OrdinalIgnoreCase))
            {
                penalty +=
                    evaluation.Amount;
            }
        }

        return penalty;
    }

    private static int CountWeekdaysInMonth(
        int month,
        int year)
    {
        var monthStart =
            new DateTime(
                year,
                month,
                1);

        var monthEnd =
            monthStart
                .AddMonths(1)
                .AddDays(-1);

        var count = 0;

        for (
            var date = monthStart;
            date <= monthEnd;
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

    private static void ValidateCommonInputs(
        Employee employee,
        Contract contract,
        Position position,
        IReadOnlyList<EmployeeEvaluation> evaluations,
        string departmentName,
        int month,
        int year)
    {
        if (employee == null)
        {
            throw new ArgumentNullException(
                nameof(employee));
        }

        if (contract == null)
        {
            throw new ArgumentNullException(
                nameof(contract));
        }

        if (position == null)
        {
            throw new ArgumentNullException(
                nameof(position));
        }

        if (evaluations == null)
        {
            throw new ArgumentNullException(
                nameof(evaluations));
        }

        if (departmentName == null)
        {
            throw new ArgumentNullException(
                nameof(departmentName));
        }

        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(
                nameof(month),
                "Month must be between 1 and 12.");
        }

        if (year < 2000)
        {
            throw new ArgumentOutOfRangeException(
                nameof(year),
                "Year must be greater than or equal to 2000.");
        }

        if (contract.BaseSalary < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(contract.BaseSalary),
                "Base salary cannot be negative.");
        }

        if (position.PayRate < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(position.PayRate),
                "Pay rate cannot be negative.");
        }
    }

    private static void ValidateAttendanceSummary(
        SalaryAttendanceSummary attendanceSummary)
    {
        if (attendanceSummary.CalendarWorkingDays < 0)
        {
            throw new InvalidOperationException(
                "Calendar working days cannot be negative.");
        }

        if (attendanceSummary.EffectiveWorkingDays < 0
            || attendanceSummary.EffectiveWorkingDays
                > attendanceSummary.CalendarWorkingDays)
        {
            throw new InvalidOperationException(
                "Effective working days must be between 0 and " +
                "the calendar working days of the selected month.");
        }

        if (attendanceSummary.PresentDays < 0
            || attendanceSummary.LateDays < 0
            || attendanceSummary.WeekdayOtDays < 0
            || attendanceSummary.WeekendOtDays < 0
            || attendanceSummary.PaidDayOffDays < 0
            || attendanceSummary.UnpaidDayOffDays < 0
            || attendanceSummary.AbsentDays < 0
            || attendanceSummary.WeekdayOtMinutes < 0
            || attendanceSummary.WeekendOtMinutes < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(attendanceSummary),
                "Attendance summary values cannot be negative.");
        }

        if (attendanceSummary.CoveredEffectiveWorkingDays
            + attendanceSummary.AbsentDays
            > attendanceSummary.EffectiveWorkingDays)
        {
            throw new InvalidOperationException(
                "Attendance summary exceeds the employee's effective " +
                "working days for the selected month.");
        }
    }
}
