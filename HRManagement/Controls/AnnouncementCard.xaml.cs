using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using HRManagement.Models;

namespace HRManagement.Controls;

/// <summary>
/// Displays active company announcements. Bind Announcements to
/// DashboardData.Announcements / DashboardService.GetAnnouncements().
///
/// Only the 3 most recent announcements are shown on the dashboard card
/// (Manage Announcements remains the place to see the full list). The
/// repository already orders by PostedDate DESC, so DisplayAnnouncements
/// is simply the first 3 of whatever's bound - kept in sync via both the
/// AnnouncementsProperty changed callback (new collection instance) and
/// CollectionChanged (same instance, items added/removed) so it never
/// goes stale either way.
/// </summary>
public partial class AnnouncementCard : UserControl
{
    private const int MaxDisplayCount = 3;

    public static readonly DependencyProperty AnnouncementsProperty =
        DependencyProperty.Register(
            nameof(Announcements),
            typeof(ObservableCollection<Announcement>),
            typeof(AnnouncementCard),
            new PropertyMetadata(new ObservableCollection<Announcement>(), OnAnnouncementsChanged));

    public ObservableCollection<Announcement> Announcements
    {
        get => (ObservableCollection<Announcement>)GetValue(AnnouncementsProperty);
        set => SetValue(AnnouncementsProperty, value);
    }

    public static readonly DependencyProperty DisplayAnnouncementsProperty =
        DependencyProperty.Register(
            nameof(DisplayAnnouncements),
            typeof(ObservableCollection<Announcement>),
            typeof(AnnouncementCard),
            new PropertyMetadata(new ObservableCollection<Announcement>()));

    /// <summary>The (at most) 3 most recent announcements - bind the list to this, not Announcements.</summary>
    public ObservableCollection<Announcement> DisplayAnnouncements
    {
        get => (ObservableCollection<Announcement>)GetValue(DisplayAnnouncementsProperty);
        private set => SetValue(DisplayAnnouncementsProperty, value);
    }

    public AnnouncementCard()
    {
        InitializeComponent();
        Announcements.CollectionChanged += (_, _) => RefreshDisplayAnnouncements();
        RefreshDisplayAnnouncements();
    }

    private static void OnAnnouncementsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (AnnouncementCard)d;

        if (e.OldValue is ObservableCollection<Announcement> oldCollection)
            oldCollection.CollectionChanged -= control.OnAnnouncementsCollectionChanged;

        if (e.NewValue is ObservableCollection<Announcement> newCollection)
            newCollection.CollectionChanged += control.OnAnnouncementsCollectionChanged;

        control.RefreshDisplayAnnouncements();
    }

    private void OnAnnouncementsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        RefreshDisplayAnnouncements();
    }

    private void RefreshDisplayAnnouncements()
    {
        DisplayAnnouncements = new ObservableCollection<Announcement>(
            Announcements.Take(MaxDisplayCount));
    }
}