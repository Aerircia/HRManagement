using HRManagement.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace HRManagement.Repositories
{
    public interface ISalaryRepository
    {
        /// <summary>
        /// Lấy thông tin nhân viên theo mã nhân viên.
        /// </summary>
        Employee? GetEmployee(int employeeId);

        /// <summary>
        /// Lấy hợp đồng có hiệu lực của nhân viên trong tháng được chọn.
        /// </summary>
        Contract? GetContractForPeriod(
            int employeeId,
            int month,
            int year);

        /// <summary>
        /// Lấy thông tin chức vụ theo Role ID.
        /// </summary>
        Role? GetRole(int roleId);

        /// <summary>
        /// Lấy tên phòng ban theo Department ID.
        /// </summary>
        string GetDepartmentName(int departmentId);

        /// <summary>
        /// Lấy dữ liệu chấm công của nhân viên trong tháng.
        /// </summary>
        IReadOnlyList<Attendance> GetAttendances(
            int employeeId,
            int month,
            int year);

        /// <summary>
        /// Lấy các đánh giá, khoản thưởng và khoản phạt trong tháng.
        /// </summary>
        IReadOnlyList<EmployeeEvaluation> GetEvaluations(
            int employeeId,
            int month,
            int year);

        /// <summary>
        /// Lấy Payroll ID của bảng lương đã được lập trong tháng.
        /// Trả về null nếu chưa có bảng lương.
        /// </summary>
        int? GetPayrollId(
            int employeeId,
            int month,
            int year);

        /// <summary>
        /// Kiểm tra nhân viên đã có bảng lương trong tháng hay chưa.
        /// </summary>
        bool PayrollExists(
            int employeeId,
            int month,
            int year);
    }
}
