namespace HRManagement.Models;

public class Position
{
    public int PositionId { get; set; }

    public string PositionName { get; set; } = string.Empty;

    public decimal PayRate { get; set; }
}
