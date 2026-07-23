using HRManagement.Models;
using Microsoft.Data.SqlClient;

namespace HRManagement.Repositories;

public class RequestFormRepository : RepositoryBase
{
    public bool Insert(RequestForm form)
    {
        using var connection = Db.CreateConnection();

        connection.Open();

        const string sql = """
            INSERT INTO RequestForm (Employee_ID, RequestType, Content, StartDate, EndDate, SubmitDate, Status)
            VALUES (@EmployeeId, @RequestType, @Content, @StartDate, @EndDate, @SubmitDate, @Status)
            """;

        using var command = new SqlCommand(sql, connection);

        command.Parameters.AddWithValue("@EmployeeId", form.EmployeeId);
        command.Parameters.AddWithValue("@RequestType", form.RequestType);
        command.Parameters.AddWithValue("@Content", (object?)form.Content ?? DBNull.Value);
        command.Parameters.AddWithValue("@StartDate", (object?)form.StartDate ?? DBNull.Value);
        command.Parameters.AddWithValue("@EndDate", (object?)form.EndDate ?? DBNull.Value);
        command.Parameters.AddWithValue("@SubmitDate", form.SubmitDate);
        command.Parameters.AddWithValue("@Status", form.Status);

        return command.ExecuteNonQuery() > 0;
    }

    public List<RequestFormSummary> GetAll()
    {
        var requests = new List<RequestFormSummary>();

        using var connection = Db.CreateConnection();

        connection.Open();

        const string sql = """
            SELECT r.Request_ID, r.Employee_ID, e.FullName, r.RequestType, r.Content,
                   r.StartDate, r.EndDate, r.SubmitDate, r.Status
            FROM RequestForm r
            JOIN Employee e ON e.EmployeeID = r.Employee_ID
            ORDER BY r.SubmitDate DESC
            """;

        using var command = new SqlCommand(sql, connection);

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            requests.Add(ReadSummary(reader));
        }

        return requests;
    }

    public List<RequestFormSummary> GetByEmployee(int employeeId)
    {
        var requests = new List<RequestFormSummary>();

        using var connection = Db.CreateConnection();

        connection.Open();

        const string sql = """
            SELECT r.Request_ID, r.Employee_ID, e.FullName, r.RequestType, r.Content,
                   r.StartDate, r.EndDate, r.SubmitDate, r.Status
            FROM RequestForm r
            JOIN Employee e ON e.EmployeeID = r.Employee_ID
            WHERE r.Employee_ID = @EmployeeId
            ORDER BY r.SubmitDate DESC
            """;

        using var command = new SqlCommand(sql, connection);

        command.Parameters.AddWithValue("@EmployeeId", employeeId);

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            requests.Add(ReadSummary(reader));
        }

        return requests;
    }

    private static RequestFormSummary ReadSummary(SqlDataReader reader)
    {
        return new RequestFormSummary
        {
            RequestId = (int)reader["Request_ID"],
            EmployeeId = (int)reader["Employee_ID"],
            EmployeeName = reader["FullName"].ToString()!,
            RequestType = reader["RequestType"].ToString()!,
            Content = reader["Content"] as string,
            StartDate = reader["StartDate"] as DateTime?,
            EndDate = reader["EndDate"] as DateTime?,
            SubmitDate = (DateTime)reader["SubmitDate"],
            Status = reader["Status"].ToString()!
        };
    }

    public List<RequestFormSummary> GetByDepartment(int departmentId)
    {
        var requests = new List<RequestFormSummary>();

        using var connection = Db.CreateConnection();

        connection.Open();

        const string sql = """
            SELECT r.Request_ID, r.Employee_ID, e.FullName, r.RequestType, r.Content,
                   r.StartDate, r.EndDate, r.SubmitDate, r.Status
            FROM RequestForm r
            JOIN Employee e ON e.EmployeeID = r.Employee_ID
            WHERE e.Department_ID = @DepartmentId
            ORDER BY r.SubmitDate DESC
            """;

        using var command = new SqlCommand(sql, connection);

        command.Parameters.AddWithValue("@DepartmentId", departmentId);

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            requests.Add(ReadSummary(reader));
        }

        return requests;
    }

    public bool UpdateStatus(int requestId, string status)
    {
        using var connection = Db.CreateConnection();

        connection.Open();

        const string sql = """
            UPDATE RequestForm
            SET Status = @Status
            WHERE Request_ID = @RequestId
            """;

        using var command = new SqlCommand(sql, connection);

        command.Parameters.AddWithValue("@Status", status);
        command.Parameters.AddWithValue("@RequestId", requestId);

        return command.ExecuteNonQuery() > 0;
    }
}
