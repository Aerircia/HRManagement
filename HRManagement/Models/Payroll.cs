namespace HRManagement.Models;

public class Payroll
{
    public int PayrollId { get; set; }

    public int EmployeeId { get; set; }

    public int? EvaluationId { get; set; }

    public int Month { get; set; }

    public int Year { get; set; }
}