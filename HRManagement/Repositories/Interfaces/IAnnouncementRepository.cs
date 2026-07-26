using HRManagement.Models;

namespace HRManagement.Repositories.Interfaces;

public interface IAnnouncementRepository
{
    List<Announcement> GetActive(int take = 10);

    List<Announcement> GetAll();

    int Insert(Announcement announcement);

    void Update(Announcement announcement);

    void Delete(int id);
}
