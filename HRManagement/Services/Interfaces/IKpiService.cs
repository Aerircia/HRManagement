using HRManagement.Models;
using System.Collections.Generic;

namespace HRManagement.Services.Interfaces;

public interface IKpiService
{
    List<Department> GetDepartmentOptions();

    KpiAssignmentOverviewDto GetAssignmentOverview(
        int? departmentId,
        string? employeeFilter,
        string month);

    List<AssignedKpiDto> GetAssignedKpis(
        int employeeId,
        string month);

    List<string> GetAvailableMonths();

    List<KpiSetSummaryDto> GetKpiSetsForMonth(
        string month,
        int? departmentId);

    void AssignKpis(
        AssignKpiRequest request);

    void UpdateAssignment(
        UpdateKpiAssignmentRequest request);

    void DeleteAssignment(
        int assignmentId);

    List<AssignedKpiDto> GetMyAssignedKpis(
        int employeeId,
        string month);

    /*
     * Writes PendingValue only.
     * CurrentValue changes only after approval.
     */
    void UpdateMyProgress(
        int assignmentId,
        int employeeId,
        decimal pendingValue);

    void ApprovePendingProgress(
        int assignmentId);

    void RejectPendingProgress(
        int assignmentId);

    void SynchronizeExpiredAssignments();
}
