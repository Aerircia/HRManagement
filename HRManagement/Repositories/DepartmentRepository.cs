using System;
using System.Collections.Generic;
using HRManagement.Models;
using Microsoft.Data.SqlClient;

namespace HRManagement.Repositories;

public class DepartmentRepository : RepositoryBase
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

    private static Department Map(SqlDataReader reader)
        {
        return new Department
            {
            DepartmentId = (int)reader["Department_ID"],
            DepartmentName = reader["DepartmentName"].ToString()!
        };
    }
}
