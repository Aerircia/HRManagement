using HRManagement.Models;
using System.Collections.Generic;

namespace HRManagement.Services.Interfaces
{
    /// <summary>
    /// Business-logic facade over IKpiRepository / IKpiSetRepository /
    /// IEmployeeKpiAssignmentRepository. ViewModels (KpiAssignmentViewModel,
    /// PersonalKpiViewModel) must only depend on this interface, never on
    /// the repositories directly (per HRManagement_PROJECT_GUIDE.md �2).
    ///
    /// Responsibilities that belong here once implemented:
    ///   - Manager department scoping (enforced here, not just UI hiding).
    ///   - Mapping the UI's simple "MM/yyyy" month selection to a
    ///     Start_Date/End_Date range for Employee_KPI_Assignment rows.
    ///   - KPI MTD / progress percent calculations.
    ///   - Calling ILogService for mutating actions (Assign/Edit/Delete/
    ///     UpdateProgress).
    /// </summary>
    public interface IKpiService
    {
        // ===== KPI Assignment page (Admin / Manager) =====

        /// <summary>
        /// Department options for the filter dropdown, scoped by the
        /// current user's role - mirrors
        /// IManageAttendancesService.GetDepartmentOptions(): Admins see
        /// every department (plus "All Departments"), Managers see only
        /// their own department.
        /// </summary>
        List<Department> GetDepartmentOptions();

        /// <summary>
        /// Left-hand employee list + top stat cards, optionally scoped to
        /// a department and filtered by name/code.
        /// </summary>
        KpiAssignmentOverviewDto GetAssignmentOverview(int? departmentId, string? employeeFilter, string month);

        /// <summary>
        /// The selected employee's assigned KPIs for the given month.
        /// </summary>
        List<AssignedKpiDto> GetAssignedKpis(int employeeId, string month);

        /// <summary>
        /// Available months to assign/view (drives the month picker).
        /// </summary>
        List<string> GetAvailableMonths();

        /// <summary>
        /// Available KPI packs (KpiSet + line items) for a given month,
        /// optionally scoped by department.
        /// </summary>
        List<KpiSetSummaryDto> GetKpiSetsForMonth(string month, int? departmentId);

        /// <summary>
        /// Assigns a chosen KPI pack's selected line items to one or more
        /// employees for the given month. Logs via ILogService.
        /// </summary>
        void AssignKpis(AssignKpiRequest request);

        /// <summary>
        /// Updates an existing assignment's Target/Weight/Current value
        /// (Edit KPI modal). Logs via ILogService.
        /// </summary>
        void UpdateAssignment(UpdateKpiAssignmentRequest request);

        /// <summary>
        /// Removes a single assignment row. Logs via ILogService.
        /// </summary>
        void DeleteAssignment(int assignmentId);

        // ===== Personal KPI page (Employee self-service) =====

        /// <summary>
        /// The logged-in employee's own assigned KPIs for the given month.
        /// </summary>
        List<AssignedKpiDto> GetMyAssignedKpis(int employeeId, string month);

        /// <summary>
        /// Employee self-service progress update - only Current_Value may
        /// change here; enforcement that other fields are untouched
        /// belongs in this method, not just UI disabling. Logs via
        /// ILogService.
        /// </summary>
        void UpdateMyProgress(int assignmentId, int employeeId, decimal currentValue);
    }
}
