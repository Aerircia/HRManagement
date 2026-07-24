using HRManagement.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace HRManagement.Repositories.Interfaces
{
    public interface IRoleRepository
    {
        Role? GetById(int id);
    }
}
