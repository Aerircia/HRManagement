namespace HRManagement.Models;

public class PaidTimeOffSummary
{
    public const int DefaultAnnualAllowance = 12;

    public int Year { get; set; }

    public int AnnualAllowance { get; set; }
        = DefaultAnnualAllowance;

    public int TotalApprovedDayOffDays { get; set; }

    public int UsedPaidDays { get; set; }

    public int RemainingPaidDays =>
        Math.Max(
            0,
            AnnualAllowance - UsedPaidDays);

    public int PaidDayOffDays { get; set; }

    public int UnpaidDayOffDays { get; set; }
}
