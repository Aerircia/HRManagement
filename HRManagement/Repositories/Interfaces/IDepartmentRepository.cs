using HRManagement.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace HRManagement.Repositories.Interfaces
{
    public interface IDepartmentRepository
    {
        Department? GetById(int id);
        List<Department> GetAll();
    }
}
