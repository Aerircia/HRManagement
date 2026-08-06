namespace HRManagement.Models;

public class SalaryDetailModel
{
    public int EmployeeId { get; set; }

    public string FullName { get; set; }
        = string.Empty;

    public string DepartmentName { get; set; }
        = string.Empty;

    public string PositionName { get; set; }
        = string.Empty;

    public decimal BaseSalary { get; set; }

    public decimal PayRate { get; set; }

    public int CalendarWorkingDays { get; set; }

    public int EffectiveWorkingDays { get; set; }

    // Attendance breakdown
    public int PresentDays { get; set; }

    public int LateDays { get; set; }

    public int LateMinutes { get; set; }

    public int WeekdayOtDays { get; set; }

    public int WeekendOtDays { get; set; }

    public decimal WeekdayOtHours { get; set; }

    public decimal WeekendOtHours { get; set; }

    public int PaidDayOffDays { get; set; }

    public int UnpaidDayOffDays { get; set; }

    /*
     * Compatibility property used by the existing salary views.
     *
     * Paid-covered standard days:
     * Present + Late + Weekday OT + Paid Day Off.
     */
    public int WorkingDays { get; set; }

    public int AbsentDays { get; set; }

    // Salary rates
    public decimal RoleSalary { get; set; }

    public decimal DailySalary { get; set; }

    public decimal HourlySalary { get; set; }

    // Additions
    public decimal WeekdayOtSalary { get; set; }

    public decimal WeekendOtSalary { get; set; }

    public decimal OvertimeSalary =>
        WeekdayOtSalary
        + WeekendOtSalary;

    public decimal Reward { get; set; }

    // Deductions
    public decimal Penalty { get; set; }

    public decimal LatePenalty { get; set; }

    public decimal AbsentDeduction { get; set; }

    public decimal UnpaidDayOffDeduction { get; set; }

    public decimal TotalDeductions =>
        Penalty
        + LatePenalty
        + AbsentDeduction
        + UnpaidDayOffDeduction;

    public decimal GrossSalary =>
        RoleSalary
        + OvertimeSalary
        + Reward;

    public decimal TotalSalary { get; set; }

    public int Month { get; set; }

    public int Year { get; set; }

    /*
     * Kept for ManageSalariesView compatibility.
     * The ViewModel can overwrite this based on PayrollExists().
     */
    public string PayrollStatus { get; set; }
        = "Pending";
}
