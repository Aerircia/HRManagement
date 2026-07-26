using HRManagement.Models;
using HRManagement.Repositories.Interfaces;
using Microsoft.Data.SqlClient;

namespace HRManagement.Repositories;

public class PayrollRepository : RepositoryBase, IPayrollRepository
{
    public Payroll? GetLatestByEmployeeId(int employeeId)
    {
        using var connection = Db.CreateConnection();

        connection.Open();

        const string sql = """
            SELECT TOP 1 *
            FROM Payroll
            WHERE Employee_ID = @EmployeeId
            ORDER BY Year DESC, Month DESC
            """;

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@EmployeeId", employeeId);

        using var reader = command.ExecuteReader();

        if (!reader.Read())
            return null;

        return new Payroll
        {
            PayrollId = (int)reader["Payroll_ID"],
            EmployeeId = (int)reader["Employee_ID"],
            EvaluationId = reader["Evaluation_ID"] as int?,
            Month = (int)reader["Month"],
            Year = (int)reader["Year"]
        };
    }
}
