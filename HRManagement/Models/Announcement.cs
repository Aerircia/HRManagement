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
}
