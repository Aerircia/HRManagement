using HRManagement.Models;
using HRManagement.ViewModels;

namespace HRManagement.Services.Interfaces;

/// <summary>
/// Aggregated read model for the "My Profile" page - the signed-in
/// employee's personal/employment info plus the dashboard stat tiles
/// (attendance rate, request approval rate, latest evaluation, latest
/// payslip). Kept as one payload since ProfileViewModel needs it all at
/// once and it's all derived from a single "current employee" load.
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
