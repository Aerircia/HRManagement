using HRManagement.Models;
using Microsoft.Data.SqlClient;

namespace HRManagement.Repositories;

public class EmployeeRepository : RepositoryBase
{
    public Employee? GetById(int id)
    {
        using var connection = Db.CreateConnection();

        connection.Open();

        const string sql = """
            SELECT *
            FROM Employee
            WHERE EmployeeID = @Id
            """;

        using var command = new SqlCommand(sql, connection);

        command.Parameters.AddWithValue("@Id", id);

        using var reader = command.ExecuteReader();

        if (!reader.Read())
            return null;

        return new Employee
        {
            EmployeeId = (int)reader["EmployeeID"],
            FullName = reader["FullName"].ToString()!,
            DateOfBirth = (DateTime)reader["Date_of_birth"],
            Phone = reader["Phone"] as string,
            Email = reader["Email"].ToString()!,
            RoleId = (int)reader["Role_ID"],
            DepartmentId = (int)reader["Department_ID"],
            HireDate = (DateTime)reader["HireDate"],
            Status = reader["Status"].ToString()!,
            Avatar = reader["Avatar"] as string
        };
    }
}