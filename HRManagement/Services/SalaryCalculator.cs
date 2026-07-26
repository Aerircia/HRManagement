using System;
using System.Collections.Generic;
using System.Text;
using HRManagement.Models;
using HRManagement.Services.Interfaces;

namespace HRManagement.Services
{
    public class SalaryCalculator : ISalaryCalculator
    {
        private const int StandardWorkingDays = 26;

        public SalaryDetailModel CalculateSalary(
            Employee employee,
            Contract contract,
            Role role,
            IReadOnlyList<Attendance> attendances,
            IReadOnlyList<EmployeeEvaluation> evaluations,
            string departmentName,
            int month,
            int year)
        {
            ArgumentNullException.ThrowIfNull(employee);

            ArgumentNullException.ThrowIfNull(contract);

            ArgumentNullException.ThrowIfNull(role);

            // Attendance

            int workingDays = CountWorkingDays(attendances);

            int absentDays = CountAbsentDays(attendances);

            // Evaluation

            decimal reward = CalculateReward(evaluations);

            decimal penalty = CalculatePenalty(evaluations);

            // Salary

            decimal roleSalary =
                contract.BaseSalary * role.PayRate;

            decimal dailySalary =
                roleSalary / StandardWorkingDays;

            decimal attendanceSalary =
                dailySalary * workingDays;

            decimal totalSalary =
                attendanceSalary
                + reward
                - penalty;

            //---------------------------------------------------

            return new SalaryDetailModel
            {
                EmployeeId = employee.EmployeeId,

                FullName = employee.FullName,

                DepartmentName = departmentName,

                RoleName = role.RoleName,

                BaseSalary = contract.BaseSalary,

                PayRate = role.PayRate,

                WorkingDays = workingDays,

                AbsentDays = absentDays,

                Reward = reward,

                Penalty = penalty,

                TotalSalary = decimal.Round(
                    totalSalary,
                    0,
                    MidpointRounding.AwayFromZero),

                Month = month,

                Year = year
            };
        }

        // Attendance

        private static int CountWorkingDays(
            IReadOnlyList<Attendance> attendances)
        {
            if (attendances.Count == 0)
                return 0;

            return attendances.Count(a =>
                a.Status.Equals(
                    "Present",
                    StringComparison.OrdinalIgnoreCase));
        }

        private static int CountAbsentDays(
            IReadOnlyList<Attendance> attendances)
        {
            if (attendances.Count == 0)
                return 0;

            return attendances.Count(a =>
                !a.Status.Equals(
                    "Present",
                    StringComparison.OrdinalIgnoreCase));
        }

        // Evaluation

        private static decimal CalculateReward(
            IReadOnlyList<EmployeeEvaluation> evaluations)
        {
            decimal reward = 0;

            foreach (var item in evaluations)
            {
                if (string.IsNullOrWhiteSpace(item.BonusType))
                    continue;

                if (item.BonusType.Equals(
                        "Reward",
                        StringComparison.OrdinalIgnoreCase))
                {
                    reward += item.Amount;
                }
            }

            return reward;
        }

        private static decimal CalculatePenalty(
            IReadOnlyList<EmployeeEvaluation> evaluations)
        {
            decimal penalty = 0;

            foreach (var item in evaluations)
            {
                if (string.IsNullOrWhiteSpace(item.BonusType))
                    continue;

                if (item.BonusType.Equals(
                        "Penalty",
                        StringComparison.OrdinalIgnoreCase))
                {
                    penalty += item.Amount;
                }
            }

            return penalty;
        }
    }
}
