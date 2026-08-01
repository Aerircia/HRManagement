namespace HRManagement.Models.Dashboard;

public class MonthlyPayoutPoint
{
    public int Month { get; set; }

    public int Year { get; set; }

    /// <summary>
    /// Three-letter month label (e.g. "Jan") for chart axis display.
    /// </summary>
    public string Label => new DateTime(Year, Month, 1).ToString("MMM");

    /// <summary>
    /// Full payout for the month (base salary + overtime + reward, minus
    /// penalties/deductions) as computed by ISalaryCalculator - the single
    /// source of truth for the payout formula (see ManageSalariesService).
    /// </summary>
    public decimal TotalSalary { get; set; }

    /// <summary>
    /// True when the employee had no valid contract/role for this month
    /// (e.g. hired later in the year). The chart should still plot 0 for
    /// these months, matching how ManageSalariesService.CreateInvalidSalaryItem
    /// reports an unresolved payout rather than throwing.
    /// </summary>
    public bool HasValidPayout { get; set; }
}