using HRManagement.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace HRManagement.Repositories.Interfaces
{
    public interface IEmployeeRepository
    {
        Employee? GetById(int id);
        List<Employee> GetAll();
        int Insert(Employee employee);
        void Update(Employee employee);
        void Delete(int id);
        IEnumerable<Employee> GetByDepartment(int departmentId);
    }
}
