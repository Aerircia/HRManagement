using System;
using System.Collections.Generic;
using System.Text;
using HRManagement.Models;
using System.Collections.Generic;

namespace HRManagement.Repositories
{
    public interface IEmployeeEvaluationRepository
    {
        /// <summary>
        /// Lấy danh sách nhân viên kèm thống kê đánh giá
        /// trong tháng và năm được chọn.
        /// </summary>
        IReadOnlyList<EvaluationEmployeeItemModel> GetEmployees(
            int month,
            int year,
            string? searchText = null,
            int? departmentId = null);

        /// <summary>
        /// Lấy toàn bộ đánh giá của một nhân viên trong tháng.
        /// </summary>
        IReadOnlyList<EmployeeEvaluationItemModel>
            GetEvaluationsByEmployee(
                int employeeId,
                int month,
                int year);

        /// <summary>
        /// Lấy một bản ghi đánh giá theo ID.
        /// </summary>
        EmployeeEvaluation? GetEvaluationById(
            int evaluationId);

        /// <summary>
        /// Kiểm tra nhân viên có tồn tại hay không.
        /// </summary>
        bool EmployeeExists(
            int employeeId);

        /// <summary>
        /// Kiểm tra bản ghi đánh giá có tồn tại hay không.
        /// </summary>
        bool EvaluationExists(
            int evaluationId);

        /// <summary>
        /// Thêm một đánh giá mới và trả về ID vừa tạo.
        /// </summary>
        int AddEvaluation(
            EmployeeEvaluation evaluation);

        /// <summary>
        /// Cập nhật một đánh giá hiện có.
        /// </summary>
        void UpdateEvaluation(
            EmployeeEvaluation evaluation);

        /// <summary>
        /// Xóa một đánh giá.
        /// </summary>
        void DeleteEvaluation(
            int evaluationId);
    }
}
