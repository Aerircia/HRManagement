using HRManagement.Models;
using HRManagement.Repositories.Interfaces;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;

namespace HRManagement.Repositories;

public class EmployeeRepository : RepositoryBase, IEmployeeRepository
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

        return Map(reader);
    }

    public List<Employee> GetAll()
    {
        using var connection = Db.CreateConnection();

        connection.Open();

        const string sql = """
            SELECT *
            FROM Employee
            ORDER BY FullName
            """;

        using var command = new SqlCommand(sql, connection);

        using var reader = command.ExecuteReader();

        var employees = new List<Employee>();

        while (reader.Read())
            employees.Add(Map(reader));

        return employees;
    }

    public int Insert(Employee employee)
    {
        using var connection = Db.CreateConnection();

        connection.Open();

        const string sql = """
            INSERT INTO Employee (FullName, Date_of_birth, Phone, Email, Role_ID, Department_ID, HireDate, Status, Avatar)
            OUTPUT INSERTED.EmployeeID
            VALUES (@FullName, @DateOfBirth, @Phone, @Email, @RoleId, @DepartmentId, @HireDate, @Status, @Avatar)
            """;

        using var command = new SqlCommand(sql, connection);
        AddEmployeeParameters(command, employee);

        return (int)command.ExecuteScalar();
    }

    public void Update(Employee employee)
    {
        using var connection = Db.CreateConnection();

        connection.Open();

        const string sql = """
            UPDATE Employee
            SET FullName = @FullName,
                Date_of_birth = @DateOfBirth,
                Phone = @Phone,
                Email = @Email,
                Role_ID = @RoleId,
                Department_ID = @DepartmentId,
                HireDate = @HireDate,
                Status = @Status,
                Avatar = @Avatar
            WHERE EmployeeID = @Id
            """;

        using var command = new SqlCommand(sql, connection);
        AddEmployeeParameters(command, employee);
        command.Parameters.AddWithValue("@Id", employee.EmployeeId);

        command.ExecuteNonQuery();
    }

    public void Delete(int id)
    {
        using var connection = Db.CreateConnection();

        connection.Open();

        const string sql = """
            DELETE FROM Employee
            WHERE EmployeeID = @Id
            """;

        using var command = new SqlCommand(sql, connection);

        command.Parameters.AddWithValue("@Id", id);

        command.ExecuteNonQuery();
    }

    private static void AddEmployeeParameters(SqlCommand command, Employee employee)
    {
        command.Parameters.AddWithValue("@FullName", employee.FullName);
        command.Parameters.AddWithValue("@DateOfBirth", employee.DateOfBirth);
        command.Parameters.AddWithValue("@Phone", (object?)employee.Phone ?? System.DBNull.Value);
        command.Parameters.AddWithValue("@Email", employee.Email);
        command.Parameters.AddWithValue("@RoleId", employee.RoleId);
        command.Parameters.AddWithValue("@DepartmentId", employee.DepartmentId);
        command.Parameters.AddWithValue("@HireDate", employee.HireDate);
        command.Parameters.AddWithValue("@Status", employee.Status);
        command.Parameters.AddWithValue("@Avatar", (object?)employee.Avatar ?? System.DBNull.Value);
    }

    private static Employee Map(SqlDataReader reader)
    {
        return new Employee
        {
            EmployeeId = (int)reader["EmployeeID"],
            FullName = reader["FullName"].ToString()!,
            DateOfBirth = (System.DateTime)reader["Date_of_birth"],
            Phone = reader["Phone"] as string,
            Email = reader["Email"].ToString()!,
            RoleId = (int)reader["Role_ID"],
            DepartmentId = (int)reader["Department_ID"],
            HireDate = (System.DateTime)reader["HireDate"],
            Status = reader["Status"].ToString()!,
            Avatar = reader["Avatar"] as string
        };
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
