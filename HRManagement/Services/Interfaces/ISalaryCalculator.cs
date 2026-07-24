using HRManagement.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace HRManagement.Services.Interfaces
{
    public interface ISalaryCalculator
    {
        SalaryDetailModel CalculateSalary(
            Employee employee,
            Contract contract,
            Role role,
            IReadOnlyList<Attendance> attendances,
            IReadOnlyList<EmployeeEvaluation> evaluations,
            string departmentName,
            int month,
            int year);
    }
}
