namespace HRManagement.Models;

public class Announcement
{
    public int AnnouncementId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public int PostedBy { get; set; }

    public string PostedByName { get; set; } = string.Empty;

    public DateTime PostedDate { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Friendly relative/short date for list displays (e.g. "5h ago", "Yesterday",
    /// or "Oct 3"). AnnouncementCard.xaml binds this directly on Announcement items.
    /// </summary>
    public string PostedDateDisplay
    {
        get
        {
            var span = DateTime.Now - PostedDate;

            if (span < TimeSpan.Zero)
                return PostedDate.ToString("MMM d");

            if (span.TotalMinutes < 1)
                return "Just now";
            if (span.TotalMinutes < 60)
                return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalHours < 24)
                return $"{(int)span.TotalHours}h ago";
            if (span.TotalDays < 2)
                return "Yesterday";
            if (span.TotalDays < 7)
                return $"{(int)span.TotalDays}d ago";

            return PostedDate.ToString("MMM d");
        }
    }
}

public class AnnouncementRow
{
    public Announcement Announcement { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string PostedByName { get; set; } = string.Empty;
    public string PostedDateDisplay { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}