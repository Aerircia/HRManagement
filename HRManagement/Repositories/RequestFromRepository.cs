using System.Collections.Generic;
using HRManagement.Models;
using Microsoft.Data.SqlClient;

namespace HRManagement.Repositories;

public class RequestFormRepository : RepositoryBase
{
    public List<RequestForm> GetByEmployeeId(int employeeId)
    {
        using var connection = Db.CreateConnection();

        connection.Open();

        const string sql = """
            SELECT *
            FROM RequestForm
            WHERE Employee_ID = @EmployeeId
            ORDER BY SubmitDate DESC
            """;

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@EmployeeId", employeeId);

        using var reader = command.ExecuteReader();

        var results = new List<RequestForm>();

        while (reader.Read())
        {
            results.Add(new RequestForm
            {
                RequestId = (int)reader["Request_ID"],
                EmployeeId = (int)reader["Employee_ID"],
                RequestType = reader["RequestType"].ToString()!,
                Content = reader["Content"] as string,
                SubmitDate = (System.DateTime)reader["SubmitDate"],
                Status = reader["Status"].ToString()!
            });
        }

        return results;
    }

    public int CountPendingByEmployeeId(int employeeId)
    {
        using var connection = Db.CreateConnection();

        connection.Open();

        const string sql = """
            SELECT COUNT(*)
            FROM RequestForm
            WHERE Employee_ID = @EmployeeId
              AND Status = 'Pending'
            """;

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@EmployeeId", employeeId);

        return (int)command.ExecuteScalar();
    }
}
