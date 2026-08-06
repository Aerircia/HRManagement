using HRManagement.Models;

namespace HRManagement.Services.Interfaces;

/// <summary>
/// Service for managing KPI (Key Performance Indicator) master data.
/// Handles CRUD operations on KPIs with authorization checks and logging.
/// </summary>
public interface IManageKpisService
{
    /// <summary>
    /// Checks if the current user has access to manage KPIs (Admin role).
    /// </summary>
    bool CurrentUserHasAccess();

    /// <summary>
    /// Gets all KPIs as display rows.
    /// </summary>
    List<KpiRow> GetKpis();

    /// <summary>
    /// Gets available KPI type options.
    /// </summary>
    List<string> GetKpiTypeOptions();

    /// <summary>
    /// Gets available measurement unit options.
    /// </summary>
    List<string> GetMeasurementUnitOptions();

    /// <summary>
    /// Gets available calculation method options.
    /// </summary>
    List<string> GetCalculationMethodOptions();

    /// <summary>
    /// Saves a new KPI or updates an existing one.
    /// </summary>
    /// <param name="input">The KPI input data</param>
    /// <returns>The saved KPI row</returns>
    KpiRow SaveKpi(KpiInput input);

    /// <summary>
    /// Deletes a KPI if it's not used in any KPI set.
    /// </summary>
    /// <param name="kpiId">The KPI ID to delete</param>
    /// <param name="kpiName">The KPI name (for logging)</param>
    void DeleteKpi(int kpiId, string kpiName);
}
