namespace HRManagement.Models;

// Read-only projection joining SystemLog with the employee's name/avatar
// (via Account), used for display on the Logs page. Unlike SystemLog, this
// does not map 1:1 to a single table.
public class SystemLogItemModel
{
    public int LogId { get; set; }

    public int AccountId { get; set; }

    public string EmployeeName { get; set; } = string.Empty;

    public string? Avatar { get; set; }

    public string Action { get; set; } = string.Empty;

    public DateTime TimeStamp { get; set; }
}
