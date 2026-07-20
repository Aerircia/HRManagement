namespace HRManagement.Models;

public class RequestForm
{
    public int RequestId { get; set; }

    public int EmployeeId { get; set; }

    public string RequestType { get; set; } = string.Empty;

    public string? Content { get; set; }

    public DateTime SubmitDate { get; set; }

    public string Status { get; set; } = string.Empty;
}