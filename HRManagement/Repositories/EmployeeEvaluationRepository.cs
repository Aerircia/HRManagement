using HRManagement.Models;
using HRManagement.Repositories.Interfaces;
using Microsoft.Data.SqlClient;

namespace HRManagement.Repositories;

public class EmployeeEvaluationRepository : RepositoryBase, IEmployeeEvaluationRepository
{
    public EmployeeEvaluation? GetLatestByEmployeeId(int employeeId)
    {
        using var connection = Db.CreateConnection();

        connection.Open();

        const string sql = """
            SELECT TOP 1 *
            FROM EmployeeEvaluation
            WHERE Employee_ID = @EmployeeId
            ORDER BY Bonus_Date DESC
            """;

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@EmployeeId", employeeId);

        using var reader = command.ExecuteReader();

        if (!reader.Read())
            return null;

        return Map(reader);
    }

    public EmployeeEvaluation? GetById(int evaluationId)
    {
        using var connection = Db.CreateConnection();

        connection.Open();

        const string sql = """
            SELECT *
            FROM EmployeeEvaluation
            WHERE Evaluation_ID = @Id
            """;

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", evaluationId);

        using var reader = command.ExecuteReader();

        if (!reader.Read())
            return null;

        return Map(reader);
    }

    public decimal GetTotalBonusForYear(int employeeId, int year)
    {
        using var connection = Db.CreateConnection();

        connection.Open();

        const string sql = """
            SELECT ISNULL(SUM(Amount), 0)
            FROM EmployeeEvaluation
            WHERE Employee_ID = @EmployeeId
              AND YEAR(Bonus_Date) = @Year
            """;

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@EmployeeId", employeeId);
        command.Parameters.AddWithValue("@Year", year);

        var result = command.ExecuteScalar();
        return result == null ? 0m : (decimal)result;
    }

    public EmployeeEvaluation Map(SqlDataReader reader)
    {
        return new EmployeeEvaluation
        {
            EvaluationId = (int)reader["Evaluation_ID"],
            EmployeeId = (int)reader["Employee_ID"],
            EvaluationType = reader["EvaluationType"] as string,
            BonusType = reader["BonusType"] as string,
            Amount = (decimal)reader["Amount"],
            BonusDate = (System.DateTime)reader["Bonus_Date"]
        };
    }
}
