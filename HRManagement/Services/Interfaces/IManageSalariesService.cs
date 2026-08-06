using System;
using System.Collections.Generic;
using System.Text;
using AttendanceModel = HRManagement.Models.Attendance;
using EvaluationModel = HRManagement.Models.EmployeeEvaluation;
using ManageSalaryModel = HRManagement.Models.ManageSalariesItemModel;
using SalaryDetailModel = HRManagement.Models.SalaryDetailModel;

namespace HRManagement.Services.Interfaces
{
    public class SalaryDetailResult
    {
        public SalaryDetailModel? Salary { get; init; }

        public string StatusMessage { get; init; } = string.Empty;

        public static SalaryDetailResult Found(SalaryDetailModel salary, string statusMessage) =>
            new() { Salary = salary, StatusMessage = statusMessage };

        public static SalaryDetailResult NotFound(string statusMessage) =>
            new() { Salary = null, StatusMessage = statusMessage };
    }

    public interface IManageSalariesService
    {
        IReadOnlyList<ManageSalaryModel>
            GetMonthlySalaries(
                int month,
                int year);

        ManageSalaryModel?
            GetEmployeeSalary(
                int employeeId,
                int month,
                int year);

        /// <summary>
        /// Employee-facing "My Salary" lookup used by SalaryViewModel.
        /// Requires payroll for the period to already be created (an
        /// employee should only see a finalized payslip here, unlike the
        /// live estimate on the Profile page) and returns a friendly
        /// StatusMessage explaining why Salary is null when it can't be
        /// computed (no payroll yet, no contract/position on file, etc.).
        /// </summary>
        SalaryDetailResult GetMySalaryDetail(
            int employeeId,
            int month,
            int year);

        void UpdateBaseSalary(
            int contractId,
            decimal baseSalary);

        void UpdatePayRate(
            int positionId,
            decimal payRate);

        void UpdateSalaryComponents(
            int contractId,
            decimal baseSalary,
            int positionId,
            decimal payRate);

        int AddAttendance(
            AttendanceModel attendance);

        void UpdateAttendance(
            AttendanceModel attendance);

        void DeleteAttendance(
            int attendanceId);

        int AddEvaluation(
            EvaluationModel evaluation);

        void UpdateEvaluation(
            EvaluationModel evaluation);

        void DeleteEvaluation(
            int evaluationId);

        int CreateEmployeePayroll(
            int employeeId,
            int month,
            int year);

        int CreateMonthlyPayroll(
            int month,
            int year);

        void DeleteEmployeePayroll(
            int employeeId,
            int month,
            int year);
    }
}
