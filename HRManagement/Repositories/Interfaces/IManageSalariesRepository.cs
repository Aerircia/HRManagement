using HRManagement.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace HRManagement.Repositories.Interfaces
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

        // Position

        void UpdatePositionPayRate(
            int positionId,
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
