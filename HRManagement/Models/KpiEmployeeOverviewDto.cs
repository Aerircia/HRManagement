using System;
using System.Collections.Generic;

namespace HRManagement.Models
{
    // ============================================================
    // DTOs for IKpiService <-> ViewModel data transfer.
    // ============================================================
    // Per HRManagement_PROJECT_GUIDE.md section 2, DTOs move data
    // between layers instead of leaking entity/repository types into
    // ViewModels. KpiAssignmentViewModel / PersonalKpiViewModel should
    // consume these (and the UI-facing row ViewModels built from them),
    // never the raw Models/Kpi*.cs entities directly.

    /// <summary>
    /// One row in the KPI Assignment page's left-hand employee list.
    /// </summary>
    public class KpiEmployeeOverviewDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public int AssignedKpiCount { get; set; }
        public double KpiMtdPercent { get; set; }
    }

    /// <summary>
    /// Aggregate stat-card data for the KPI Assignment page.
    /// </summary>
    public class KpiAssignmentOverviewDto
    {
        public List<KpiEmployeeOverviewDto> Employees { get; set; } = new();
        public int TotalAssignedCount { get; set; }
        public int EmployeesWithoutKpiCount { get; set; }
        public double AverageKpiMtdPercent { get; set; }
        public int AtRiskCount { get; set; }
    }

    /// <summary>
    /// One assigned KPI line (Employee_KPI_Assignment joined with
    /// KPI_Set_Detail + KPI), used by both the KPI Assignment page's
    /// detail grid and the Personal KPI page.
    /// </summary>
    public class AssignedKpiDto
    {
        public int AssignmentId { get; set; }
        public int KpiSetDetailId { get; set; }
        public int KpiId { get; set; }
        public string KpiName { get; set; } = string.Empty;
        public string MeasurementUnit { get; set; } = string.Empty;

        public decimal AssignedTarget { get; set; }
        public decimal AssignedWeight { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal? PendingValue { get; set; }

        public string Status { get; set; } = "Not Started";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsLocked { get; set; }
    }

    /// <summary>
    /// Summary of a KpiSet ("Monthly KPI list" / "pack") for Step 1 of
    /// the Assign KPI wizard, including its full line-item breakdown so
    /// the wizard can render the pack's KPIs inline under the card
    /// without a second round trip.
    /// </summary>
    public class KpiSetSummaryDto
    {
        public int KpiSetId { get; set; }
        public string KpiSetName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Department { get; set; }
        public List<KpiSetDetailDto> Details { get; set; } = new();
    }

    /// <summary>
    /// One KPI line item within a KpiSetSummaryDto.
    /// </summary>
    public class KpiSetDetailDto
    {
        public int KpiSetDetailId { get; set; }
        public int KpiId { get; set; }
        public string KpiName { get; set; } = string.Empty;
        public string MeasurementUnit { get; set; } = string.Empty;
        public decimal TargetValue { get; set; }
        public decimal Weight { get; set; }
    }

    /// <summary>
    /// Request payload for assigning a chosen KpiSet's chosen line items
    /// to a set of employees for a given month (service layer maps the
    /// month to a Start/End date range internally).
    /// </summary>
    public class AssignKpiRequest
    {
        public int KpiSetId { get; set; }
        public IReadOnlyList<int> EmployeeIds { get; set; } = Array.Empty<int>();

        /// <summary>MM/yyyy, e.g. "06/2026" - mapped to Start/End Date by the service.</summary>
        public string Month { get; set; } = string.Empty;

        /// <summary>
        /// Per-employee, per-KpiSetDetail overrides (selected line items
        /// + optionally adjusted Target/Weight). Keyed by EmployeeId.
        /// </summary>
        public List<EmployeeKpiAssignmentInput> EmployeeAssignments { get; set; } = new();
    }

    public class EmployeeKpiAssignmentInput
    {
        public int EmployeeId { get; set; }
        public List<KpiSetDetailSelectionInput> SelectedDetails { get; set; } = new();
    }

    public class KpiSetDetailSelectionInput
    {
        public int KpiSetDetailId { get; set; }
        public decimal AssignedTarget { get; set; }
        public decimal AssignedWeight { get; set; }
    }

    /// <summary>
    /// Edit payload for a single existing assignment row (Edit KPI modal).
    /// </summary>
    public class UpdateKpiAssignmentRequest
    {
        public int AssignmentId { get; set; }
        public decimal AssignedTarget { get; set; }
        public decimal AssignedWeight { get; set; }
        public decimal CurrentValue { get; set; }
    }
}
