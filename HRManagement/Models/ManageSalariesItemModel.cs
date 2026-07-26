using System;
using System.Collections.Generic;
using System.Text;

namespace HRManagement.Models
{
    public class ManageSalariesItemModel
    {
        // Employee information

        public int EmployeeId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string DepartmentName { get; set; } = string.Empty;

        public int DepartmentId { get; set; }

        // Role information

        public int RoleId { get; set; }

        public string RoleName { get; set; } = string.Empty;

        public decimal PayRate { get; set; }

        // Contract information

        public int ContractId { get; set; }

        public decimal BaseSalary { get; set; }

        // Attendance information

        public int WorkingDays { get; set; }

        public int AbsentDays { get; set; }

        // Evaluation information

        public decimal Reward { get; set; }

        public decimal Penalty { get; set; }

        // Payroll information

        public int? PayrollId { get; set; }

        public int Month { get; set; }

        public int Year { get; set; }

        public decimal TotalSalary { get; set; }

        public bool IsPayrollCreated { get; set; }

        // Validation information

        public bool HasValidContract { get; set; }

        public bool HasValidRole { get; set; }

        public string ValidationMessage { get; set; } = string.Empty;

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
            !string.IsNullOrWhiteSpace(ValidationMessage);
    }
}
