namespace HRManagement.Models;

// Read-only projection joining RequestForm with the employee's name, used
// for display in Manage Requests. Unlike other Models, this does not map
// 1:1 to a single table.
public class RequestFormSummary
{
    public int RequestId { get; set; }

    public int EmployeeId { get; set; }

    public string EmployeeName { get; set; } = string.Empty;

    public string RequestType { get; set; } = string.Empty;

    public string? Content { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public DateTime SubmitDate { get; set; }

    public string Status { get; set; } = string.Empty;
}
