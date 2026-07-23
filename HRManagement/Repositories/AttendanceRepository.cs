using System.Collections.Generic;
using HRManagement.Models;
using Microsoft.Data.SqlClient;

namespace HRManagement.Repositories;

public class AttendanceRepository : RepositoryBase
{
    public List<Attendance> GetByEmployeeForMonth(int employeeId, int year, int month)
    {
        using var connection = Db.CreateConnection();

        connection.Open();

        const string sql = """
            SELECT *
            FROM Attendance
            WHERE Employee_ID = @EmployeeId
              AND YEAR(Check_in) = @Year
              AND MONTH(Check_in) = @Month
            ORDER BY Check_in
            """;

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@EmployeeId", employeeId);
        command.Parameters.AddWithValue("@Year", year);
        command.Parameters.AddWithValue("@Month", month);

        using var reader = command.ExecuteReader();

        var results = new List<Attendance>();

        while (reader.Read())
        {
            results.Add(new Attendance
            {
                AttendanceId = (int)reader["Attendance_ID"],
                EmployeeId = (int)reader["Employee_ID"],
                CheckIn = reader["Check_in"] as DateTime?,
                CheckOut = reader["Check_out"] as DateTime?,
                Status = reader["Status"].ToString()!
            });
        }

        return results;
    }
}
