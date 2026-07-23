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

    public IEnumerable<Employee> GetAll()
    {
        var list = new List<Employee>();

        using var conn = Db.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT EmployeeID, FullName, Date_of_birth, Phone, Email, Role_ID, Department_ID, HireDate, Status, Avatar FROM Employee";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Employee
            {
                EmployeeId = (int)reader[0],
                FullName = reader[1].ToString()!,
                DateOfBirth = (DateTime)reader[2],
                Phone = reader[3] as string,
                Email = reader[4].ToString()!,
                RoleId = (int)reader[5],
                DepartmentId = (int)reader[6],
                HireDate = (DateTime)reader[7],
                Status = reader[8].ToString()!,
                Avatar = reader[9] as string
            });
        }

        return list;
    }

    public IEnumerable<Employee> GetByDepartment(int departmentId)
    {
        var list = new List<Employee>();
        using var conn = Db.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT EmployeeID, FullName, Date_of_birth, Phone, Email, Role_ID, Department_ID, HireDate, Status, Avatar FROM Employee WHERE Department_ID = @dept";
        var p = cmd.CreateParameter(); p.ParameterName = "@dept"; p.Value = departmentId; cmd.Parameters.Add(p);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Employee
            {
                EmployeeId = (int)reader[0],
                FullName = reader[1].ToString()!,
                DateOfBirth = (DateTime)reader[2],
                Phone = reader[3] as string,
                Email = reader[4].ToString()!,
                RoleId = (int)reader[5],
                DepartmentId = (int)reader[6],
                HireDate = (DateTime)reader[7],
                Status = reader[8].ToString()!,
                Avatar = reader[9] as string
            });
        }

        return list;
    }
}