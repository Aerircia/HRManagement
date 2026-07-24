using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using HRManagement.Data;
using HRManagement.Models;
using Microsoft.Data.SqlClient;

namespace HRManagement.Repositories;

public class AttendanceRepository : RepositoryBase
{
    public static event EventHandler<HRManagement.Models.AttendanceChangedEventArgs>? OnAttendanceChanged;

    public IEnumerable<Attendance> GetAttendancesForEmployeeMonth(int employeeId, int year, int month)
    {
        var list = new List<Attendance>();

        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);

        using var conn = Db.CreateConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT Attendance_ID, Employee_ID, Check_in, Check_out, Status
FROM Attendance
WHERE Employee_ID = @emp AND (
      (Check_in IS NOT NULL AND Check_in >= @start AND Check_in < @end)
   OR (Check_out IS NOT NULL AND Check_out >= @start AND Check_out < @end)
)
";
        cmd.Parameters.Add(new SqlParameter("@emp", SqlDbType.Int) { Value = employeeId });
        cmd.Parameters.Add(new SqlParameter("@start", SqlDbType.DateTime) { Value = start });
        cmd.Parameters.Add(new SqlParameter("@end", SqlDbType.DateTime) { Value = end });

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Attendance
            {
                AttendanceId = reader.GetInt32(0),
                EmployeeId = reader.GetInt32(1),
                CheckIn = reader.IsDBNull(2) ? null : reader.GetDateTime(2),
                CheckOut = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                Status = reader.IsDBNull(4) ? string.Empty : reader.GetString(4)
            });
        }

        return list;
    }

    public void UpsertAttendance(Attendance attendance)
    {
        if (attendance == null) return;

        // determine date bucket (use date of checkin or checkout or today)
        var date = (attendance.CheckIn ?? attendance.CheckOut ?? DateTime.Now).Date;

        using var conn = Db.CreateConnection();
        conn.Open();

        using var sel = conn.CreateCommand();
        sel.CommandText = @"SELECT Attendance_ID FROM Attendance WHERE Employee_ID = @emp AND CAST(ISNULL(Check_in, Check_out) AS date) = @date";
        sel.Parameters.Add(new SqlParameter("@emp", SqlDbType.Int) { Value = attendance.EmployeeId });
        sel.Parameters.Add(new SqlParameter("@date", SqlDbType.Date) { Value = date });

        var existing = sel.ExecuteScalar();
        if (existing != null && existing != DBNull.Value)
        {
            var id = Convert.ToInt32(existing);
            using var upd = conn.CreateCommand();
            upd.CommandText = @"UPDATE Attendance SET Check_in = @ci, Check_out = @co, Status = @st WHERE Attendance_ID = @id";
            upd.Parameters.Add(new SqlParameter("@ci", SqlDbType.DateTime) { Value = (object?)attendance.CheckIn ?? DBNull.Value });
            upd.Parameters.Add(new SqlParameter("@co", SqlDbType.DateTime) { Value = (object?)attendance.CheckOut ?? DBNull.Value });
            upd.Parameters.Add(new SqlParameter("@st", SqlDbType.NVarChar, 100) { Value = attendance.Status ?? string.Empty });
            upd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = id });
            upd.ExecuteNonQuery();
            attendance.AttendanceId = id;
            OnAttendanceChanged?.Invoke(this, new HRManagement.Models.AttendanceChangedEventArgs { EmployeeId = attendance.EmployeeId, Date = date });
        }
        else
        {
            using var ins = conn.CreateCommand();
            ins.CommandText = @"INSERT INTO Attendance (Employee_ID, Check_in, Check_out, Status) VALUES (@emp, @ci, @co, @st); SELECT SCOPE_IDENTITY();";
            ins.Parameters.Add(new SqlParameter("@emp", SqlDbType.Int) { Value = attendance.EmployeeId });
            ins.Parameters.Add(new SqlParameter("@ci", SqlDbType.DateTime) { Value = (object?)attendance.CheckIn ?? DBNull.Value });
            ins.Parameters.Add(new SqlParameter("@co", SqlDbType.DateTime) { Value = (object?)attendance.CheckOut ?? DBNull.Value });
            ins.Parameters.Add(new SqlParameter("@st", SqlDbType.NVarChar, 100) { Value = attendance.Status ?? string.Empty });

            var idObj = ins.ExecuteScalar();
            if (idObj != null && idObj != DBNull.Value)
            {
                attendance.AttendanceId = Convert.ToInt32(idObj);
                OnAttendanceChanged?.Invoke(this, new HRManagement.Models.AttendanceChangedEventArgs { EmployeeId = attendance.EmployeeId, Date = date });
            }
        }
    }

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