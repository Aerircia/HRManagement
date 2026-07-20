namespace HRManagement.Models;

public class EmployeeEvaluation
{
    public int EvaluationId { get; set; }

    public int EmployeeId { get; set; }

    public string? EvaluationType { get; set; }

    public string? BonusType { get; set; }

    public decimal Amount { get; set; }

    public DateTime BonusDate { get; set; }
}