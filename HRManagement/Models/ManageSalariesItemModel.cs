namespace HRManagement.Models
{
    public class ManageSalariesItemModel
    {
        // Employee information

        public int EmployeeId { get; set; }

        public string FullName { get; set; }
            = string.Empty;

        public string DepartmentName { get; set; }
            = string.Empty;

        public int DepartmentId { get; set; }

        // Role information

        public int RoleId { get; set; }

        public string RoleName { get; set; }
            = string.Empty;

        public decimal PayRate { get; set; }

        // Contract information

        public int ContractId { get; set; }

        public decimal BaseSalary { get; set; }

        // Attendance information

        public int CalendarWorkingDays { get; set; }

        public int EffectiveWorkingDays { get; set; }

        public int PresentDays { get; set; }

        public int LateDays { get; set; }

        public int LateMinutes { get; set; }

        public int WeekdayOtDays { get; set; }

        public int WeekendOtDays { get; set; }

        public decimal WeekdayOtHours { get; set; }

        public decimal WeekendOtHours { get; set; }

        public int PaidDayOffDays { get; set; }

        public int UnpaidDayOffDays { get; set; }

        // Compatibility fields used by the existing view.

        public int WorkingDays { get; set; }

        public int AbsentDays { get; set; }

        // Salary breakdown

        public decimal RoleSalary { get; set; }

        public decimal DailySalary { get; set; }

        public decimal HourlySalary { get; set; }

        public decimal WeekdayOtSalary { get; set; }

        public decimal WeekendOtSalary { get; set; }

        public decimal OvertimeSalary =>
            WeekdayOtSalary
            + WeekendOtSalary;

        // Evaluation information

        public decimal Reward { get; set; }

        public decimal Penalty { get; set; }

        // Deductions

        public decimal LatePenalty { get; set; }

        public decimal AbsentDeduction { get; set; }

        public decimal UnpaidDayOffDeduction { get; set; }

        public decimal TotalDeductions =>
            Penalty
            + LatePenalty
            + AbsentDeduction
            + UnpaidDayOffDeduction;

        public decimal GrossSalary =>
            RoleSalary
            + OvertimeSalary
            + Reward;

        // Payroll information

        public int? PayrollId { get; set; }

        public int Month { get; set; }

        public int Year { get; set; }

        public decimal TotalSalary { get; set; }

        public bool IsPayrollCreated { get; set; }

        // Validation information

        public bool HasValidContract { get; set; }

        public bool HasValidRole { get; set; }

        public string ValidationMessage { get; set; }
            = string.Empty;

        // Display properties

        public string PayrollStatus =>
            IsPayrollCreated
                ? "Created"
                : "Not Created";

        public string SalaryPeriod =>
            $"{Month:00}/{Year}";

        public bool CanCreatePayroll =>
            !IsPayrollCreated
            && HasValidContract
            && HasValidRole;

        public bool HasValidationError =>
            !string.IsNullOrWhiteSpace(
                ValidationMessage);
    }
}
