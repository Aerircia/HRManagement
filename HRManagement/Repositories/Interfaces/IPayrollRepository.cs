using HRManagement.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace HRManagement.Repositories.Interfaces
{
    public interface IPayrollRepository
    {
        Payroll? GetLatestByEmployeeId(int employeeId);
    }
}
