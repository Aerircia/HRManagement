using HRManagement.Models;

namespace HRManagement.Services.Interfaces;

/// <summary>
/// Input for creating or updating an announcement from the Manage
/// Announcements Add/Edit form. AnnouncementId == 0 means Add.
/// </summary>
public class AnnouncementInput
{
    public int AnnouncementId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public interface IManageAnnouncementsService
{
    /// <summary>
    /// True if the current logged-in user (Admin only) may access Manage
    /// Announcements.
    /// </summary>
    bool CurrentUserHasAccess();

    List<Announcement> GetAnnouncements();

    /// <summary>
    /// Creates or updates an announcement depending on
    /// <see cref="AnnouncementInput.AnnouncementId"/>, logging the change
    /// under the current user's account. Returns the saved announcement
    /// (with PostedByName populated for a new announcement).
    /// </summary>
    Announcement SaveAnnouncement(AnnouncementInput input);

    void DeleteAnnouncement(int announcementId, string announcementTitle);
}
