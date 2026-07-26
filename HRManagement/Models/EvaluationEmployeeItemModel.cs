using System;
using System.Collections.Generic;
using System.Text;

namespace HRManagement.Models
{
    public class EvaluationEmployeeItemModel
    {
        public int EmployeeId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public int DepartmentId { get; set; }

        public string DepartmentName { get; set; } = string.Empty;

        public int RoleId { get; set; }

        public string RoleName { get; set; } = string.Empty;

        public int EvaluationCount { get; set; }

        public decimal TotalReward { get; set; }

        public decimal TotalPenalty { get; set; }

        public decimal NetAdjustment => TotalReward - TotalPenalty;
    }
}
