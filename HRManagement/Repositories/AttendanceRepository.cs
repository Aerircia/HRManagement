using System.Data;
using HRManagement.Models;
using HRManagement.Repositories.Interfaces;
using Microsoft.Data.SqlClient;

namespace HRManagement.Repositories;

// NOTE: replaces the old AttendanceRepository, which had two overlapping
// "get attendance for month" queries (GetAttendancesForEmployeeMonth /
// GetByEmployeeForMonth) and a static event. Consolidated to a single
// query and an instance event so this is DI-friendly and testable.
public class AttendanceRepository : RepositoryBase, IAttendanceRepository
{
    public event EventHandler<AttendanceChangedEventArgs>? OnAttendanceChanged;

    public List<Attendance> GetByEmployeeForMonth(int employeeId, int year, int month)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);

        using var connection = Db.CreateConnection();
        connection.Open();

        const string sql = """
            SELECT Attendance_ID, Employee_ID, Check_in, Check_out, Status
            FROM Attendance
            WHERE Employee_ID = @EmployeeId AND (
                  (Check_in IS NOT NULL AND Check_in >= @Start AND Check_in < @End)
               OR (Check_out IS NOT NULL AND Check_out >= @Start AND Check_out < @End)
            )
            """;

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@EmployeeId", employeeId);
        command.Parameters.AddWithValue("@Start", start);
        command.Parameters.AddWithValue("@End", end);

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
                Status = reader["Status"].ToString() ?? string.Empty
            });
        }

        return results;
    }

    public void UpsertAttendance(Attendance attendance)
    {
        if (attendance == null)
            return;

        var date = (attendance.CheckIn ?? attendance.CheckOut ?? DateTime.Now).Date;

        using var connection = Db.CreateConnection();
        connection.Open();

        using var select = connection.CreateCommand();
        select.CommandText = """
            SELECT Attendance_ID
            FROM Attendance
            WHERE Employee_ID = @Emp AND CAST(ISNULL(Check_in, Check_out) AS date) = @Date
            """;
        select.Parameters.Add(new SqlParameter("@Emp", SqlDbType.Int) { Value = attendance.EmployeeId });
        select.Parameters.Add(new SqlParameter("@Date", SqlDbType.Date) { Value = date });

        var existing = select.ExecuteScalar();

        if (existing is int existingId)
        {
            using var update = connection.CreateCommand();
            update.CommandText = """
                UPDATE Attendance
                SET Check_in = @CheckIn, Check_out = @CheckOut, Status = @Status
                WHERE Attendance_ID = @Id
                """;
            update.Parameters.Add(new SqlParameter("@CheckIn", SqlDbType.DateTime) { Value = (object?)attendance.CheckIn ?? DBNull.Value });
            update.Parameters.Add(new SqlParameter("@CheckOut", SqlDbType.DateTime) { Value = (object?)attendance.CheckOut ?? DBNull.Value });
            update.Parameters.Add(new SqlParameter("@Status", SqlDbType.NVarChar, 50) { Value = attendance.Status ?? string.Empty });
            update.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int) { Value = existingId });
            update.ExecuteNonQuery();

            attendance.AttendanceId = existingId;
        }
        else
        {
            using var insert = connection.CreateCommand();
            insert.CommandText = """
                INSERT INTO Attendance (Employee_ID, Check_in, Check_out, Status)
                VALUES (@Emp, @CheckIn, @CheckOut, @Status);
                SELECT CAST(SCOPE_IDENTITY() AS int);
                """;
            insert.Parameters.Add(new SqlParameter("@Emp", SqlDbType.Int) { Value = attendance.EmployeeId });
            insert.Parameters.Add(new SqlParameter("@CheckIn", SqlDbType.DateTime) { Value = (object?)attendance.CheckIn ?? DBNull.Value });
            insert.Parameters.Add(new SqlParameter("@CheckOut", SqlDbType.DateTime) { Value = (object?)attendance.CheckOut ?? DBNull.Value });
            insert.Parameters.Add(new SqlParameter("@Status", SqlDbType.NVarChar, 50) { Value = attendance.Status ?? string.Empty });

            var newId = insert.ExecuteScalar();

            if (newId is int id)
                attendance.AttendanceId = id;
        }

        OnAttendanceChanged?.Invoke(this, new AttendanceChangedEventArgs { EmployeeId = attendance.EmployeeId, Date = date });
    }
}