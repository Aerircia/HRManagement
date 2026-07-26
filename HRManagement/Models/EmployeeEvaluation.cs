namespace HRManagement.Models;

public class EmployeeEvaluation
{
    public int EvaluationId { get; set; }

    public int EmployeeId { get; set; }

    public string BonusType { get; set; } = string.Empty;

    public string EvaluationType { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateTime BonusDate { get; set; }

    public string? Comment { get; set; }
}