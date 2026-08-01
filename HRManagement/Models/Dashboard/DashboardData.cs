using System.Collections.ObjectModel;

namespace HRManagement.Models.Dashboard;

public class DashboardData
{
    public ObservableCollection<WeekDayItem> WeekDays { get; set; } = [];

    public EmployeeAnalytics EmployeeAnalytics { get; set; } = new();

    public ManagerAnalytics? ManagerAnalytics { get; set; }

    public ObservableCollection<MonthlyPayoutPoint> MonthlyPayoutHistory { get; set; } = [];

    public ObservableCollection<Announcement> Announcements { get; set; } = [];

    public Attendance? TodayAttendance { get; set; }
}