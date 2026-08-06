using HRManagement.Models;
using System;
using System.Collections.Generic;

namespace HRManagement.Repositories.Interfaces;

public interface IEmployeeKpiAssignmentRepository
{
    List<EmployeeKpiAssignment> GetByEmployee(
        int employeeId);

    List<EmployeeKpiAssignment> GetByEmployeeAndPeriod(
        int employeeId,
        DateTime startDate,
        DateTime endDate);

    List<EmployeeKpiAssignment> GetByDepartment(
        int? departmentId,
        DateTime startDate,
        DateTime endDate);

    EmployeeKpiAssignment? GetById(
        int assignmentId);

    List<EmployeeKpiAssignment> GetExpiredUnlocked(
        DateTime today);

    int Add(
        EmployeeKpiAssignment assignment);

    void Update(
        EmployeeKpiAssignment assignment);

    /*
     * Inserts and updates all supplied assignments in one transaction.
     * AssignmentId <= 0 means INSERT; AssignmentId > 0 means UPDATE.
     */
    void SaveBatch(
        IReadOnlyList<EmployeeKpiAssignment> assignments);

    /*
     * Replaces the complete KPI assignment set for one employee/period
     * in a single transaction. Existing rows not present in assignments
     * are deleted. This operation is allowed only before progress starts.
     */
    void ReplacePeriodAssignments(
        int employeeId,
        DateTime startDate,
        DateTime endDate,
        IReadOnlyList<EmployeeKpiAssignment> assignments);

    void LockPeriod(
        int employeeId,
        DateTime startDate,
        DateTime endDate);

    void FinalizePeriod(
        int employeeId,
        DateTime startDate,
        DateTime endDate,
        EmployeeEvaluation? evaluation);

    void Delete(
        int assignmentId);
}
