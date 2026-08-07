using HRManagement.Models;

namespace HRManagement.Services.Interfaces;

/// <summary>
/// Input for creating or updating a KPI Set from the Manage KPI Set Add/Edit form.
/// KpiSetId == 0 means Add.
/// </summary>
public class KpiSetInput
{
    public int KpiSetId { get; set; }
    public string KpiSetName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? DepartmentId { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// List of KPI details to be added or updated with this KPI Set.
    /// Each item contains KPI_ID, Target_Value, and Weight.
    /// </summary>
    public List<KpiSetDetailInput> Details { get; set; } = [];
}

/// <summary>
/// Input for a KPI line item within a KPI Set.
/// KpiSetDetailId == 0 means new item.
/// </summary>
public class KpiSetDetailInput
{
    public int KpiSetDetailId { get; set; }
    public int KpiId { get; set; }
    public decimal TargetValue { get; set; }
    public decimal Weight { get; set; }
}

/// <summary>
/// Provides CRUD operations for KPI Sets, restricted to Admin role.
/// Orchestrates KpiSet, KpiSetDetail, and related data.
/// </summary>
public interface IManageKpiSetService
{
    /// <summary>
    /// True if the current logged-in user (Admin only) may access Manage KPI Set.
    /// </summary>
    bool CurrentUserHasAccess();

    /// <summary>
    /// Returns all KPI Sets, optionally filtered by department.
    /// </summary>
    List<KpiSet> GetKpiSets();

    /// <summary>
    /// Retrieves a KPI Set by ID along with all its KPI_Set_Detail rows.
    /// </summary>
    KpiSetWithDetails? GetKpiSetById(int kpiSetId);

    /// <summary>
    /// Returns all active master KPIs (for dropdown/selection in the Add/Edit form).
    /// </summary>
    List<Kpi> GetAvailableKpis();

    /// <summary>
    /// Returns all departments for the department dropdown in the Add/Edit form.
    /// </summary>
    List<Department> GetDepartments();

    /// <summary>
    KpiSet SaveKpiSet(KpiSetInput input);

    /// <summary>
    /// Deletes a KPI Set and all associated KPI_Set_Detail rows.
    /// </summary>
    void DeleteKpiSet(int kpiSetId, string kpiSetName);
}

/// <summary>
/// Data transfer object combining KpiSet header and detail rows,
/// used for read operations/forms.
/// </summary>
public class KpiSetWithDetails
{
    public KpiSet KpiSet { get; set; } = new();
    public List<KpiSetDetail> Details { get; set; } = [];
}
