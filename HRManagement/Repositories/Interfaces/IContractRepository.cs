using HRManagement.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace HRManagement.Repositories.Interfaces
{
    public interface IContractRepository
    {
        Contract? GetCurrentByEmployeeId(int employeeId);
        List<Contract> GetAllByEmployeeId(int employeeId);
        List<Contract> GetAll();
        int Insert(Contract contract);
        void Update(Contract contract);
        void Delete(int id);
    }
}
