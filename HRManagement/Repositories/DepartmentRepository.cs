using HRManagement.Models;
using HRManagement.Repositories.Interfaces;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;

namespace HRManagement.Repositories;

public class DepartmentRepository : RepositoryBase, IDepartmentRepository
{
    public Department? GetById(int id)
    {
        using var connection = Db.CreateConnection();

        connection.Open();

        const string sql = """
            SELECT *
            FROM Department
            WHERE Department_ID = @Id
            """;

        using var command = new SqlCommand(sql, connection);

        command.Parameters.AddWithValue("@Id", id);

        using var reader = command.ExecuteReader();

        if (!reader.Read())
            return null;

        return Map(reader);
    }

    public List<Department> GetAll()
    {
        using var connection = Db.CreateConnection();

        connection.Open();

        const string sql = """
            SELECT *
            FROM Department
            ORDER BY DepartmentName
            """;

        using var command = new SqlCommand(sql, connection);

        using var reader = command.ExecuteReader();

        var departments = new List<Department>();

        while (reader.Read())
            departments.Add(Map(reader));

        return departments;
    }

    public int Insert(Department department)
    {
        using var connection = Db.CreateConnection();

        connection.Open();

        const string sql = """
            INSERT INTO Department (DepartmentName)
            OUTPUT INSERTED.Department_ID
            VALUES (@DepartmentName)
            """;

        using var command = new SqlCommand(sql, connection);

        command.Parameters.AddWithValue("@DepartmentName", department.DepartmentName);

        return (int)command.ExecuteScalar();
    }

    public void Update(Department department)
    {
        using var connection = Db.CreateConnection();

        connection.Open();

        const string sql = """
            UPDATE Department
            SET DepartmentName = @DepartmentName
            WHERE Department_ID = @Id
            """;

        using var command = new SqlCommand(sql, connection);

        command.Parameters.AddWithValue("@DepartmentName", department.DepartmentName);
        command.Parameters.AddWithValue("@Id", department.DepartmentId);

        command.ExecuteNonQuery();
    }

    public void Delete(int id)
    {
        using var connection = Db.CreateConnection();

        connection.Open();

        const string sql = """
            DELETE FROM Department
            WHERE Department_ID = @Id
            """;

        using var command = new SqlCommand(sql, connection);

        command.Parameters.AddWithValue("@Id", id);

        command.ExecuteNonQuery();
    }

    private static Department Map(SqlDataReader reader)
    {
        return new Department
        {
            DepartmentId = (int)reader["Department_ID"],
            DepartmentName = reader["DepartmentName"].ToString()!
        };
    }
}
