using System;
using System.Collections.Generic;
using System.Text;

namespace HRManagement.Models
{
    public class SalaryDetailModel
    {
        public int EmployeeId { get; set; }

        public string FullName { get; set; } = "";

        public string DepartmentName { get; set; } = "";

        public string RoleName { get; set; } = "";

        public decimal BaseSalary { get; set; }

        public decimal PayRate { get; set; }

        public int WorkingDays { get; set; }

        public int AbsentDays { get; set; }

        public decimal Reward { get; set; }

        public decimal Penalty { get; set; }

        public int Month { get; set; }

        public int Year { get; set; }

        public decimal TotalSalary { get; set; }
    }
}