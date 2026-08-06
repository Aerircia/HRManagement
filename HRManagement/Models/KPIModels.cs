using System;

namespace HRManagement.Models
{
    // ============================================================
    // Domain models mirroring the KPI database schema:
    //   KPI.sql, KPI_Set.sql, KPI_Set_Detail.sql,
    //   Employee_KPI_Assignment.sql
    // ============================================================
    // These are plain entity-shaped models (repository/service layer),
    // distinct from the UI-facing row ViewModels in
    // ViewModels/KpiEmployeeRowViewModel.cs. ViewModels map to/from
    // these via IKpiService, they never bind directly to these types.

    /// <summary>
    /// Master KPI definition (KPI table). One KPI can appear in many
    /// KPI_Set_Detail rows (many packs), each with its own
    /// Target_Value/Weight override for that pack.
    /// </summary>
    public class Kpi
    {
        public int KpiId { get; set; }
        public string KpiName { get; set; } = string.Empty;
        public string? Description { get; set; }

        /// <summary>Financial | Quality | Productivity</summary>
        public string KpiType { get; set; } = string.Empty;

        /// <summary>USD | Percent | Review | Candidate | Ticket | Task</summary>
        public string MeasurementUnit { get; set; } = string.Empty;

        public decimal DefaultTarget { get; set; }

        /// <summary>LowerIsBetter | HigherIsBetter</summary>
        public string CalculationMethod { get; set; } = "HigherIsBetter";

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// A named, reusable "KPI pack" (KPI_Set table), optionally scoped to
    /// a department. Referred to as "Monthly KPI list" in the UI.
    /// </summary>
    public class KpiSet
    {
        public int KpiSetId { get; set; }
        public string KpiSetName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? DepartmentId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// One KPI line item within a KpiSet (KPI_Set_Detail table): the
    /// pack's default Target_Value/Weight for that KPI. Unique per
    /// (KpiSetId, KpiId).
    /// </summary>
    public class KpiSetDetail
    {
        public int KpiSetDetailId { get; set; }
        public int KpiSetId { get; set; }
        public int KpiId { get; set; }

        public decimal TargetValue { get; set; }
        public decimal Weight { get; set; }

        // Convenience navigation data (populated by the service layer
        // via joins), not FK-backed columns themselves.
        public string? KpiName { get; set; }
        public string? MeasurementUnit { get; set; }
    }

    /// <summary>
    /// An individual employee's assignment of one KpiSetDetail
    /// (Employee_KPI_Assignment table) for a given period. Assigned_Target
    /// /Assigned_Weight start as a copy of the pack's KpiSetDetail values
    /// but can be overridden per employee before/at assignment time.
    /// </summary>
    public class EmployeeKpiAssignment
    {
        public int AssignmentId { get; set; }
        public int EmployeeId { get; set; }
        public int KpiSetDetailId { get; set; }

        public decimal AssignedTarget { get; set; }
        public decimal AssignedWeight { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal? PendingValue { get; set; }

        /// <summary>Not Started | In Progress | Pending Approval | Completed | Rejected | Cancelled</summary>
        public string Status { get; set; } = "Not Started";

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsLocked { get; set; }

        // Convenience navigation data (populated by the service layer),
        // not columns on the table itself.
        public string? KpiName { get; set; }
        public string? MeasurementUnit { get; set; }
    }
}
