using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using HRManagement.Models.Dashboard;

namespace HRManagement.Controls;

/// <summary>
/// Display-only weekly calendar strip (Mon–Sun) used on the dashboard.
/// Bind WeekDays to an ObservableCollection&lt;WeekDayItem&gt; sourced from
/// DashboardService.GetCurrentWeek() / DashboardData.WeekDays.
/// </summary>
public partial class WeeklyCalendar : UserControl
{
    public static readonly DependencyProperty WeekDaysProperty =
        DependencyProperty.Register(
            nameof(WeekDays),
            typeof(ObservableCollection<WeekDayItem>),
            typeof(WeeklyCalendar),
            new PropertyMetadata(new ObservableCollection<WeekDayItem>()));

    public ObservableCollection<WeekDayItem> WeekDays
    {
        get => (ObservableCollection<WeekDayItem>)GetValue(WeekDaysProperty);
        set => SetValue(WeekDaysProperty, value);
    }

    public WeeklyCalendar()
    {
        InitializeComponent();
    }
}