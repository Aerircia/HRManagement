using HRManagement.Models;
using HRManagement.Repositories.Interfaces;
using HRManagement.Services.Interfaces;

namespace HRManagement.Services;

public class ManageAnnouncementsService : IManageAnnouncementsService
{
    // Admin only
    private const int AdminRoleId = 1;

    private readonly IAnnouncementRepository _announcementRepository;
    private readonly SessionManager _sessionManager;
    private readonly ILogService _logService;

    public ManageAnnouncementsService(
        IAnnouncementRepository announcementRepository,
        SessionManager sessionManager,
        ILogService logService)
    {
        _announcementRepository = announcementRepository
            ?? throw new ArgumentNullException(nameof(announcementRepository));

        _sessionManager = sessionManager
            ?? throw new ArgumentNullException(nameof(sessionManager));

        _logService = logService
            ?? throw new ArgumentNullException(nameof(logService));
    }

    public bool CurrentUserHasAccess()
    {
        var currentRoleId = _sessionManager.CurrentUser?.Employee?.RoleId;
        return currentRoleId.HasValue && currentRoleId.Value == AdminRoleId;
    }

    public List<Announcement> GetAnnouncements()
    {
        return _announcementRepository.GetAll();
    }

    public Announcement SaveAnnouncement(AnnouncementInput input)
    {
        ValidateInput(input);

        if (input.AnnouncementId == 0)
        {
            var currentUser = _sessionManager.CurrentUser
                ?? throw new InvalidOperationException(
                    "Your session could not be found. Please log in again.");

            var announcement = new Announcement
            {
                Title = input.Title.Trim(),
                Content = input.Content.Trim(),
                PostedBy = currentUser.Account.AccountId,
                PostedDate = DateTime.Now,
                IsActive = input.IsActive
            };

            var newId = _announcementRepository.Insert(announcement);
            announcement.AnnouncementId = newId;
            announcement.PostedByName = currentUser.Employee.FullName;

            _logService.WriteLog(
                currentUser.Account.AccountId,
                $"Created announcement: {announcement.Title}");

            return announcement;
        }
        else
        {
            var existing = _announcementRepository.GetAll()
                .FirstOrDefault(a => a.AnnouncementId == input.AnnouncementId)
                ?? throw new InvalidOperationException("Announcement could not be found.");

            existing.Title = input.Title.Trim();
            existing.Content = input.Content.Trim();
            existing.IsActive = input.IsActive;

            _announcementRepository.Update(existing);

            _logService.WriteLog(
                CurrentAccountId(),
                $"Updated announcement: {existing.Title}");

            return existing;
        }
    }

    public void DeleteAnnouncement(int announcementId, string announcementTitle)
    {
        _announcementRepository.Delete(announcementId);

        _logService.WriteLog(
            CurrentAccountId(),
            $"Deleted announcement: {announcementTitle}");
    }

    private int CurrentAccountId() =>
        _sessionManager.CurrentUser!.Account.AccountId;

    private static void ValidateInput(AnnouncementInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Title) || string.IsNullOrWhiteSpace(input.Content))
        {
            throw new ArgumentException("Title and content are required.");
        }
    }
}
