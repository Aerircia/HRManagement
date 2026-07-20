namespace HRManagement.Models;

public class SystemLog
{
    public int LogId { get; set; }

    public int AccountId { get; set; }

    public string Action { get; set; } = string.Empty;

    public DateTime TimeStamp { get; set; }
}