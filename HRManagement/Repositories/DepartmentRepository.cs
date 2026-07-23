using System;
using System.Collections.Generic;
using HRManagement.Models;
using Microsoft.Data.SqlClient;

namespace HRManagement.Repositories;

public class DepartmentRepository : RepositoryBase
{
    public IEnumerable<Department> GetAll()
    {
        var list = new List<Department>();
        using var conn = Db.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT Department_ID, DepartmentName FROM Department ORDER BY DepartmentName";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Department
            {
                DepartmentId = reader.GetInt32(0),
                DepartmentName = reader.GetString(1)
            });
        }
        return list;
    }
}
