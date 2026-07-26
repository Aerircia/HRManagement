using HRManagement.Models;
using HRManagement.Repositories;
using HRManagement.Services.Interfaces;
using HRManagement.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace HRManagement.Services
{
    public class EmployeeEvaluationService
        : IEmployeeEvaluationService
    {
        private const string RewardType = "Reward";
        private const string PenaltyType = "Penalty";

        private const int MaximumCommentLength = 500;

        private readonly IEmployeeEvaluationRepository
            _evaluationRepository;

        private readonly ISalaryRepository
            _salaryRepository;

        public EmployeeEvaluationService(
            IEmployeeEvaluationRepository evaluationRepository,
            ISalaryRepository salaryRepository)
        {
            _evaluationRepository =
                evaluationRepository
                ?? throw new ArgumentNullException(
                    nameof(evaluationRepository));

            _salaryRepository =
                salaryRepository
                ?? throw new ArgumentNullException(
                    nameof(salaryRepository));
        }

        public IReadOnlyList<EvaluationEmployeeItemModel>
            GetEmployees(
                int month,
                int year,
                string? searchText = null,
                int? departmentId = null)
        {
            
            ValidateEvaluationPeriod(month, year);

            if (departmentId.HasValue &&
                departmentId.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(departmentId),
                    "Department ID must be greater than 0.");
            }

            var normalizedSearchText =
                NormalizeOptionalText(searchText);

            return _evaluationRepository.GetEmployees(
                month,
                year,
                normalizedSearchText,
                departmentId);
        }

        public IReadOnlyList<EmployeeEvaluationItemModel>
            GetEmployeeEvaluations(
                int employeeId,
                int month,
                int year)
        {
            ValidateEmployeeId(employeeId);
            ValidateEvaluationPeriod(month, year);

            EnsureEmployeeExists(employeeId);

            return _evaluationRepository
                .GetEvaluationsByEmployee(
                    employeeId,
                    month,
                    year);
        }

        public EmployeeEvaluation? GetEvaluationById(
            int evaluationId)
        {
            
            ValidateEvaluationId(evaluationId);

            return _evaluationRepository
                .GetEvaluationById(evaluationId);
        }

        public int CreateEvaluation(
            int employeeId,
            string bonusType,
            string evaluationType,
            decimal amount,
            DateTime bonusDate,
            string? comment)
        {
            

            ValidateEmployeeId(employeeId);
            EnsureEmployeeExists(employeeId);

            var normalizedBonusType =
                NormalizeBonusType(bonusType);

            var normalizedEvaluationType =
                NormalizeRequiredText(
                    evaluationType,
                    nameof(evaluationType),
                    "Evaluation type is required.");

            ValidateAmount(amount);
            ValidateBonusDate(bonusDate);
            ValidateComment(comment);

            EnsurePayrollNotCreated(
                employeeId,
                bonusDate.Month,
                bonusDate.Year);

            var evaluation =
                new EmployeeEvaluation
                {
                    EmployeeId = employeeId,

                    BonusType =
                        normalizedBonusType,

                    EvaluationType =
                        normalizedEvaluationType,

                    Amount = amount,

                    BonusDate =
                        bonusDate.Date,

                    Comment =
                        NormalizeOptionalText(comment)
                };

            return _evaluationRepository
                .AddEvaluation(evaluation);
        }

        public void UpdateEvaluation(
            int evaluationId,
            string bonusType,
            string evaluationType,
            decimal amount,
            DateTime bonusDate,
            string? comment)
        {
            ValidateEvaluationId(evaluationId);

            var existingEvaluation =
                _evaluationRepository
                    .GetEvaluationById(evaluationId);

            if (existingEvaluation == null)
            {
                throw new InvalidOperationException(
                    $"Employee evaluation {evaluationId} was not found.");
            }

            var normalizedBonusType =
                NormalizeBonusType(bonusType);

            var normalizedEvaluationType =
                NormalizeRequiredText(
                    evaluationType,
                    nameof(evaluationType),
                    "Evaluation type is required.");

            ValidateAmount(amount);
            ValidateBonusDate(bonusDate);
            ValidateComment(comment);

            /*
             * Khóa kỳ lương cũ.
             *
             * Điều này ngăn Admin thay đổi một bản ghi đã thuộc
             * Payroll được tạo trước đó.
             */
            EnsurePayrollNotCreated(
                existingEvaluation.EmployeeId,
                existingEvaluation.BonusDate.Month,
                existingEvaluation.BonusDate.Year);

            /*
             * Nếu Admin đổi ngày đánh giá sang tháng khác,
             * kỳ lương mới cũng phải chưa được tạo.
             */
            var periodChanged =
                existingEvaluation.BonusDate.Month
                    != bonusDate.Month
                ||
                existingEvaluation.BonusDate.Year
                    != bonusDate.Year;

            if (periodChanged)
            {
                EnsurePayrollNotCreated(
                    existingEvaluation.EmployeeId,
                    bonusDate.Month,
                    bonusDate.Year);
            }

            existingEvaluation.BonusType =
                normalizedBonusType;

            existingEvaluation.EvaluationType =
                normalizedEvaluationType;

            existingEvaluation.Amount =
                amount;

            existingEvaluation.BonusDate =
                bonusDate.Date;

            existingEvaluation.Comment =
                NormalizeOptionalText(comment);

            _evaluationRepository.UpdateEvaluation(
                existingEvaluation);
        }

        public void DeleteEvaluation(
            int evaluationId)
        {
            ValidateEvaluationId(evaluationId);

            var existingEvaluation =
                _evaluationRepository
                    .GetEvaluationById(evaluationId);

            if (existingEvaluation == null)
            {
                throw new InvalidOperationException(
                    $"Employee evaluation {evaluationId} was not found.");
            }

            EnsurePayrollNotCreated(
                existingEvaluation.EmployeeId,
                existingEvaluation.BonusDate.Month,
                existingEvaluation.BonusDate.Year);

            _evaluationRepository.DeleteEvaluation(
                evaluationId);
        }

        

        private void EnsureEmployeeExists(
            int employeeId)
        {
            if (_evaluationRepository.EmployeeExists(
                    employeeId))
            {
                return;
            }

            throw new InvalidOperationException(
                $"Employee {employeeId} was not found.");
        }

        private void EnsurePayrollNotCreated(
            int employeeId,
            int month,
            int year)
        {
            var payrollExists =
                _salaryRepository.PayrollExists(
                    employeeId,
                    month,
                    year);

            if (!payrollExists)
                return;

            throw new InvalidOperationException(
                $"The evaluation cannot be changed because payroll " +
                $"for {month:00}/{year} has already been created.");
        }

        private static string NormalizeBonusType(
            string bonusType)
        {
            var normalizedValue =
                NormalizeRequiredText(
                    bonusType,
                    nameof(bonusType),
                    "Reward or penalty type is required.");

            if (string.Equals(
                    normalizedValue,
                    RewardType,
                    StringComparison.OrdinalIgnoreCase))
            {
                return RewardType;
            }

            if (string.Equals(
                    normalizedValue,
                    PenaltyType,
                    StringComparison.OrdinalIgnoreCase))
            {
                return PenaltyType;
            }

            throw new ArgumentException(
                "Bonus type must be Reward or Penalty.",
                nameof(bonusType));
        }

        private static string NormalizeRequiredText(
            string? value,
            string parameterName,
            string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    errorMessage,
                    parameterName);
            }

            return value.Trim();
        }

        private static string? NormalizeOptionalText(
            string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        private static void ValidateEmployeeId(
            int employeeId)
        {
            if (employeeId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(employeeId),
                    "Employee ID must be greater than 0.");
            }
        }

        private static void ValidateEvaluationId(
            int evaluationId)
        {
            if (evaluationId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(evaluationId),
                    "Evaluation ID must be greater than 0.");
            }
        }

        private static void ValidateEvaluationPeriod(
            int month,
            int year)
        {
            if (month is < 1 or > 12)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(month),
                    "Month must be between 1 and 12.");
            }

            if (year < 2000)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(year),
                    "Year must be greater than or equal to 2000.");
            }
        }

        private static void ValidateAmount(
            decimal amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    "Evaluation amount must be greater than 0.");
            }
        }

        private static void ValidateBonusDate(
            DateTime bonusDate)
        {
            if (bonusDate == default)
            {
                throw new ArgumentException(
                    "Evaluation date is required.",
                    nameof(bonusDate));
            }

            if (bonusDate.Year < 2000)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(bonusDate),
                    "Evaluation date is invalid.");
            }

            if (bonusDate.Date > DateTime.Today)
            {
                throw new ArgumentException(
                    "Evaluation date cannot be in the future.",
                    nameof(bonusDate));
            }
        }

        private static void ValidateComment(
            string? comment)
        {
            if (comment?.Trim().Length
                > MaximumCommentLength)
            {
                throw new ArgumentException(
                    $"Comment cannot exceed " +
                    $"{MaximumCommentLength} characters.",
                    nameof(comment));
            }
        }
    }
}
