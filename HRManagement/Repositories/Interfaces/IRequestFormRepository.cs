using HRManagement.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace HRManagement.Repositories.Interfaces
{
    public interface IRequestFormRepository
    {
        bool Insert(RequestForm form);
        List<RequestFormSummary> GetAll();
        List<RequestFormSummary> GetByEmployee(int employeeId);
        List<RequestFormSummary> GetByDepartment(int departmentId);
        bool UpdateStatus(int requestId, string status);
        List<RequestForm> GetByEmployeeId(int employeeId);
        int CountPendingByEmployeeId(int employeeId);
    }
}
