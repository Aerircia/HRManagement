using HRManagement.Models;
using System.Collections.Generic;

namespace HRManagement.Services.Interfaces;

public interface ISalaryCalculator
{
    /*
     * Legacy overload.
     *
     * Keep this temporarily so existing SalaryViewModel and
     * ManageSalariesViewModel continue to compile while the
     * salary flow is migrated to SalaryAttendanceSummary.
     */
    SalaryDetailModel CalculateSalary(
        Employee employee,
        Contract contract,
        Role role,
        IReadOnlyList<Attendance> attendances,
        IReadOnlyList<EmployeeEvaluation> evaluations,
        string departmentName,
        int month,
        int year);

    /*
     * New payroll calculation path.
     *
     * Attendance and PTO have already been normalized into
     * SalaryAttendanceSummary before reaching the calculator.
     *
     * Late penalty is calculated internally from LateMinutes
     * and the calculated HourlySalary.
     */
    SalaryDetailModel CalculateSalary(
        Employee employee,
        Contract contract,
        Role role,
        SalaryAttendanceSummary attendanceSummary,
        IReadOnlyList<EmployeeEvaluation> evaluations,
        string departmentName,
        int month,
        int year);
}
