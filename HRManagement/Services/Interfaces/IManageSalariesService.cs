using System;
using System.Collections.Generic;
using System.Text;
using AttendanceModel = HRManagement.Models.Attendance;
using EvaluationModel = HRManagement.Models.EmployeeEvaluation;
using ManageSalaryModel = HRManagement.Models.ManageSalariesItemModel;

namespace HRManagement.Services.Interfaces
{
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

        void UpdateBaseSalary(
            int contractId,
            decimal baseSalary);

        void UpdatePayRate(
            int roleId,
            decimal payRate);

        void UpdateSalaryComponents(
            int contractId,
            decimal baseSalary,
            int roleId,
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
