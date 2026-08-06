using HRManagement.Models;
using System.Collections.Generic;

namespace HRManagement.Repositories.Interfaces
{
    /// <summary>
    /// Master KPI definitions (KPI table).
    /// </summary>
    public interface IKpiRepository
    {
        List<Kpi> GetAll();
        Kpi? GetById(int kpiId);
        int Add(Kpi kpi);
        void Update(Kpi kpi);
        void Delete(int kpiId);
    }

    /// <summary>
    /// KPI packs (KPI_Set table) and their line items (KPI_Set_Detail).
    /// Kept as one repository since KpiSetDetail has no independent
    /// lifecycle outside of its owning KpiSet.
    /// </summary>
    public interface IKpiSetRepository
    {
        List<KpiSet> GetAll();
        List<KpiSet> GetByDepartment(int? departmentId);
        KpiSet? GetById(int kpiSetId);
        int Add(KpiSet kpiSet);
        void Update(KpiSet kpiSet);
        void Delete(int kpiSetId);

        List<KpiSetDetail> GetDetails(int kpiSetId);
        int AddDetail(KpiSetDetail detail);
        void UpdateDetail(KpiSetDetail detail);
        void DeleteDetail(int kpiSetDetailId);
    }

    /// <summary>
    /// Per-employee KPI assignments (Employee_KPI_Assignment table).
    /// </summary>
    public interface IEmployeeKpiAssignmentRepository
    {
        List<EmployeeKpiAssignment> GetByEmployee(int employeeId);
        List<EmployeeKpiAssignment> GetByEmployeeAndPeriod(int employeeId, System.DateTime startDate, System.DateTime endDate);
        List<EmployeeKpiAssignment> GetByDepartment(int? departmentId, System.DateTime startDate, System.DateTime endDate);
        EmployeeKpiAssignment? GetById(int assignmentId);

        int Add(EmployeeKpiAssignment assignment);
        void Update(EmployeeKpiAssignment assignment);
        void Delete(int assignmentId);
    }
}
