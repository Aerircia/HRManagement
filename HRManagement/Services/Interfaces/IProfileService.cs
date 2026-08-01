using HRManagement.Models;
using HRManagement.ViewModels;

namespace HRManagement.Services.Interfaces;

/// <summary>
/// Aggregated read model for the "My Profile" page - the signed-in
/// employee's personal/employment info plus the dashboard stat tiles
/// (attendance rate, request approval rate, latest evaluation, latest
/// payslip) and the redesigned page's global stats / detail cards
/// (PTO balance, retirement savings, tenure, performance snapshot, job
/// history, compensation, benefits). Kept as one payload since
/// ProfileViewModel needs it all at once and it's all derived from a
/// single "current employee" load.
/// </summary>
public class ProfileData
{
    public Employee Employee { get; set; } = null!;
    public string RoleLabel { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public Contract? CurrentContract { get; set; }

    public List<KeyValueItem> EmploymentDetails { get; set; } = [];

    // Attendance
    public double AttendanceRate { get; set; }
    public string AttendanceSummary { get; set; } = string.Empty;

    // Requests
    public double RequestApprovalRate { get; set; }
    public int PendingRequestsCount { get; set; }
    public string RequestsSummary { get; set; } = string.Empty;

    // Latest evaluation / bonus
    public string LatestEvaluationAmountDisplay { get; set; } = string.Empty;
    public string LatestEvaluationTypeDisplay { get; set; } = string.Empty;
    public string TotalBonusThisYearDisplay { get; set; } = string.Empty;

    // Quick info
    public string LatestPayslipDate { get; set; } = string.Empty;
    public string LatestPayslipAmount { get; set; } = string.Empty;
    public string ContractTypeDisplay { get; set; } = string.Empty;
    public string ContractStatusDisplay { get; set; } = string.Empty;

    // ===== Personal Details =====
    public string? Address { get; set; }

    // ===== At-a-glance global stats =====

    // PTO: 12 days/year policy minus approved "Day Off" request days taken
    // this calendar year (RequestForm, Status = Approved). Real data only -
    // no attendance-table "PTO" status exists, so this is derived from
    // approved day-off requests, which is the actual system of record for
    // time-off decisions.
    public const int AnnualPtoAllowanceDays = 12;
    public int PtoDaysUsed { get; set; }
    public int PtoDaysRemaining { get; set; }

    // Retirement savings: 7% of current annualized salary (BaseSalary x 12)
    // per completed year of tenure. There is no historical per-year salary
    // record, so this uses the current contract's base salary for every
    // completed year - an approximation, clearly derived from real
    // Contract/Employee data rather than any hardcoded figure.
    public decimal RetirementSavings { get; set; }

    // Tenure
    public int TenureYears { get; set; }
    public int TenureMonths { get; set; }
    public string TenureDisplay { get; set; } = string.Empty;

    // Performance snapshot: reward vs. penalty ratio across all of the
    // employee's EmployeeEvaluation rows (all-time, not just this year -
    // this is a "snapshot", distinct from the year-scoped TotalBonusThisYear
    // above).
    public int TotalRewardCount { get; set; }
    public int TotalPenaltyCount { get; set; }
    public double PerformanceRewardRatio { get; set; }
    public string PerformanceSummaryDisplay { get; set; } = string.Empty;

    // ===== Job history (from Contract rows, most recent first) =====
    public List<JobHistoryEntry> JobHistory { get; set; } = [];

    // ===== Compensation & Benefits =====
    // Reuses ISalaryCalculator's own output for the *current* period so
    // this never drifts from what SalaryView/ManageSalariesView compute.
    public string CurrentBaseSalaryDisplay { get; set; } = string.Empty;
    public string CurrentPayRateDisplay { get; set; } = string.Empty;

    // Net salary for the current month via ISalaryCalculator (live
    // estimate, not gated on a finalized Payroll row - see ProfileService
    // for why this differs from LatestPayslipAmount above).
    public string CurrentNetSalaryDisplay { get; set; } = string.Empty;
    public string CurrentSalaryPeriodDisplay { get; set; } = string.Empty;

    // Benefits: no Benefits table exists, so instead of a hardcoded list
    // this is derived from the current contract's ContractType (a real,
    // stored field) via a fixed mapping - see ProfileService.ResolveBenefits.
    public List<string> Benefits { get; set; } = [];
}

/// <summary>
/// One entry in the employee's Job History timeline, built from a single
/// Contract row.
/// </summary>
public class JobHistoryEntry
{
    public string ContractType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string DateRangeDisplay { get; set; } = string.Empty;
    public bool IsCurrent { get; set; }
}

/// <summary>
/// Input for updating the signed-in employee's own editable profile fields.
/// </summary>
public class ProfileUpdateInput
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Address { get; set; }
}

public class ProfileUpdateResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Changes { get; set; } = [];
}

public interface IProfileService
{
    /// <summary>
    /// Loads the full profile + dashboard payload for the given employee.
    /// Returns null if the employee cannot be found.
    /// </summary>
    ProfileData? GetProfile(int employeeId);

    /// <summary>
    /// Validates and saves the editable profile fields for the given
    /// employee, then logs what changed.
    /// </summary>
    ProfileUpdateResult UpdateProfile(int employeeId, ProfileUpdateInput input);
}
