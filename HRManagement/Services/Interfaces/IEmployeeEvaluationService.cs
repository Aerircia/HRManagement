using HRManagement.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace HRManagement.Services.Interfaces
{
    public interface IEmployeeEvaluationService
    {
        /// <summary>
        /// Lấy danh sách nhân viên cùng tổng hợp đánh giá
        /// trong tháng và năm được chọn.
        /// </summary>
        IReadOnlyList<EvaluationEmployeeItemModel> GetEmployees(
            int month,
            int year,
            string? searchText = null,
            int? departmentId = null);

        /// <summary>
        /// Lấy danh sách đánh giá của một nhân viên
        /// trong tháng và năm được chọn.
        /// </summary>
        IReadOnlyList<EmployeeEvaluationItemModel>
            GetEmployeeEvaluations(
                int employeeId,
                int month,
                int year);

        /// <summary>
        /// Lấy chi tiết một đánh giá theo ID.
        /// </summary>
        EmployeeEvaluation? GetEvaluationById(
            int evaluationId);

        /// <summary>
        /// Tạo đánh giá mới cho nhân viên.
        /// </summary>
        int CreateEvaluation(
            int employeeId,
            string bonusType,
            string evaluationType,
            decimal amount,
            DateTime bonusDate,
            string? comment);

        /// <summary>
        /// Cập nhật một đánh giá hiện có.
        /// </summary>
        void UpdateEvaluation(
            int evaluationId,
            string bonusType,
            string evaluationType,
            decimal amount,
            DateTime bonusDate,
            string? comment);

        /// <summary>
        /// Xóa một đánh giá.
        /// </summary>
        void DeleteEvaluation(
            int evaluationId);
    }
}
