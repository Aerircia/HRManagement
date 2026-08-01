using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using HRManagement.Models;

namespace HRManagement.Controls;

/// <summary>
/// Displays active company announcements. Bind Announcements to
/// DashboardData.Announcements / DashboardService.GetAnnouncements().
/// </summary>
public partial class AnnouncementCard : UserControl
{
    public static readonly DependencyProperty AnnouncementsProperty =
        DependencyProperty.Register(
            nameof(Announcements),
            typeof(ObservableCollection<Announcement>),
            typeof(AnnouncementCard),
            new PropertyMetadata(new ObservableCollection<Announcement>()));

    public ObservableCollection<Announcement> Announcements
    {
        get => (ObservableCollection<Announcement>)GetValue(AnnouncementsProperty);
        set => SetValue(AnnouncementsProperty, value);
    }

    public AnnouncementCard()
    {
        InitializeComponent();
    }
}