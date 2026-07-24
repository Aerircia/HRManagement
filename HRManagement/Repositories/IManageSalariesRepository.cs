using System;
using System.Collections.Generic;
using System.Text;
using HRManagement.Models;

namespace HRManagement.Repositories
{
    public interface IManageSalariesRepository
    {
        // Employee

        IReadOnlyList<Employee> GetEmployeesForPeriod(
            int month,
            int year);

        // Contract

        void UpdateContractBaseSalary(
            int contractId,
            decimal baseSalary);

        // Role

        void UpdateRolePayRate(
            int roleId,
            decimal payRate);

        // Attendance

        int AddAttendance(
            Attendance attendance);

        void UpdateAttendance(
            Attendance attendance);

        void DeleteAttendance(
            int attendanceId);

        // Evaluation

        int AddEvaluation(
            EmployeeEvaluation evaluation);

        void UpdateEvaluation(
            EmployeeEvaluation evaluation);

        void DeleteEvaluation(
            int evaluationId);

        // Payroll

        int CreatePayroll(
            Payroll payroll);

        int CreateMonthlyPayroll(
            IReadOnlyList<Payroll> payrolls);

        void DeletePayroll(
            int employeeId,
            int month,
            int year);
    }
}
