using System;
using System.Collections.Generic;
using System.Text;

namespace HRManagement.Models
{
    public class EmployeeEvaluationItemModel
    {
        public int EvaluationId { get; set; }

        public int EmployeeId { get; set; }

        public string EvaluationType { get; set; } = string.Empty;

        public string BonusType { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public DateTime BonusDate { get; set; }

        public string Comment { get; set; } = string.Empty;

        public bool IsReward =>
            BonusType.Equals("Reward", StringComparison.OrdinalIgnoreCase);

        public bool IsPenalty =>
            BonusType.Equals("Penalty", StringComparison.OrdinalIgnoreCase);

        public string DisplayDate =>
            BonusDate.ToString("dd/MM/yyyy");

        public string DisplayAmount =>
            $"{Amount:N0} $";
    }
}
