namespace HRManagement.Models.Dashboard;

public class WeeklyHourPoint
{
    public DateTime Date { get; set; }

    public string Label => Date.ToString("ddd");

    public double Hours { get; set; }
}