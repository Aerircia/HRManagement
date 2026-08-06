namespace HRManagement.Models;

/// <summary>
/// Input model for creating or updating a KPI.
/// Used by UI forms to collect KPI data before persistence.
/// </summary>
public class KpiInput
{
    /// <summary>
    /// KPI ID (0 for new KPIs, existing ID for updates).
    /// </summary>
    public int KpiId { get; set; }

    /// <summary>
    /// The name of the KPI.
    /// </summary>
    public string KpiName { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the KPI.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The KPI type: Financial | Quality | Productivity
    /// </summary>
    public string KpiType { get; set; } = string.Empty;

    /// <summary>
    /// The measurement unit: USD | Percent | Review | Candidate | Ticket | Task
    /// </summary>
    public string MeasurementUnit { get; set; } = string.Empty;

    /// <summary>
    /// The default target value for this KPI.
    /// </summary>
    public decimal DefaultTarget { get; set; }

    /// <summary>
    /// The calculation method: HigherIsBetter | LowerIsBetter
    /// </summary>
    public string CalculationMethod { get; set; } = string.Empty;

    /// <summary>
    /// Whether this KPI is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
