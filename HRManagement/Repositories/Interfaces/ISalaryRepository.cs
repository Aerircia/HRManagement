using HRManagement.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace HRManagement.Repositories.Interfaces
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

        // ===== Dashboard support =====
        // Employee-ID scopes used by the dashboard's monthly payout chart so
        // it can batch ISalaryCalculator (the single source of truth for the
        // payout formula) over "all employees in scope" instead of the SQL
        // layer re-deriving payout independently.

        List<int> GetAllEmployeeIds();

        List<int> GetEmployeeIdsByDepartment(int departmentId);
    }
}
