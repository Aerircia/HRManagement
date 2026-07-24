using HRManagement.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace HRManagement.Repositories
{
    public interface ISalaryRepository
    {
        Employee? GetEmployee(int employeeId);

        Contract? GetContractForPeriod(
            int employeeId,
            int month,
            int year);

        Role? GetRole(int roleId);

        string GetDepartmentName(int departmentId);

        IReadOnlyList<Attendance> GetAttendances(
            int employeeId,
            int month,
            int year);

        IReadOnlyList<EmployeeEvaluation> GetEvaluations(
            int employeeId,
            int month,
            int year);

        int? GetPayrollId(
            int employeeId,
            int month,
            int year);

        bool PayrollExists(
            int employeeId,
            int month,
            int year);
    }
}
