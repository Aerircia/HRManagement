using HRManagement.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace HRManagement.Repositories.Interfaces
{
    public interface IEmployeeEvaluationRepository
    {
        EmployeeEvaluation? GetLatestByEmployeeId(int employeeId);
        EmployeeEvaluation? GetById(int evaluationId);
        decimal GetTotalBonusForYear(int employeeId, int year);
    }
}
