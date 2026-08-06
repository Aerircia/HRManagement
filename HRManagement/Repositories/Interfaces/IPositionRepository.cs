using HRManagement.Models;
using System.Collections.Generic;

namespace HRManagement.Repositories.Interfaces
{
    public interface IPositionRepository
    {
        Position? GetById(int id);

        List<Position> GetAll();
    }
}
