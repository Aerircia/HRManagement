using HRManagement.Data;
using HRManagement.Models;
using HRManagement.Repositories.Interfaces;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;

namespace HRManagement.Repositories;

public class SystemLogRepository : RepositoryBase, ISystemLogRepository
{
    public List<SystemLog> GetLogs()
    {
        var results = new List<SystemLog>();

        // Database connection logic kept entirely within the repository[cite: 3]
        using var conn = Db.CreateConnection();
        conn.Open();

        using var cmd = conn.CreateCommand();
        // Use column names matching the database schema[cite: 3]
        cmd.CommandText = "SELECT [Log_ID], [Account_ID], [Action], [TimeStamp] FROM [dbo].[SystemLog] ORDER BY [TimeStamp] DESC";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var log = new SystemLog
            {
                LogId = reader.IsDBNull(reader.GetOrdinal("Log_ID")) ? 0 : reader.GetInt32(reader.GetOrdinal("Log_ID")),
                AccountId = reader.IsDBNull(reader.GetOrdinal("Account_ID")) ? 0 : reader.GetInt32(reader.GetOrdinal("Account_ID")),
                Action = reader.IsDBNull(reader.GetOrdinal("Action")) ? string.Empty : reader.GetString(reader.GetOrdinal("Action")),
                TimeStamp = reader.IsDBNull(reader.GetOrdinal("TimeStamp")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("TimeStamp"))
            };

            results.Add(log);
        }

        return results;
    }

    public List<SystemLogItemModel> GetLogItems()
    {
        var results = new List<SystemLogItemModel>();

        using var conn = Db.CreateConnection();
        conn.Open();

        using var cmd = conn.CreateCommand();

        // Left join so a log whose account/employee was since removed still
        // shows up (falls back to "Unknown user" instead of disappearing).
        cmd.CommandText = """
            SELECT
                l.[Log_ID],
                l.[Account_ID],
                l.[Action],
                l.[TimeStamp],
                e.[FullName],
                e.[Avatar]
            FROM [dbo].[SystemLog] l
            LEFT JOIN [dbo].[Account] acc ON acc.[Account_ID] = l.[Account_ID]
            LEFT JOIN [dbo].[Employee] e ON e.[EmployeeID] = acc.[Employee_ID]
            ORDER BY l.[TimeStamp] DESC
            """;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var fullNameOrdinal = reader.GetOrdinal("FullName");
            var avatarOrdinal = reader.GetOrdinal("Avatar");

            results.Add(new SystemLogItemModel
            {
                LogId = reader.IsDBNull(reader.GetOrdinal("Log_ID")) ? 0 : reader.GetInt32(reader.GetOrdinal("Log_ID")),
                AccountId = reader.IsDBNull(reader.GetOrdinal("Account_ID")) ? 0 : reader.GetInt32(reader.GetOrdinal("Account_ID")),
                Action = reader.IsDBNull(reader.GetOrdinal("Action")) ? string.Empty : reader.GetString(reader.GetOrdinal("Action")),
                TimeStamp = reader.IsDBNull(reader.GetOrdinal("TimeStamp")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("TimeStamp")),
                EmployeeName = reader.IsDBNull(fullNameOrdinal) ? "Unknown user" : reader.GetString(fullNameOrdinal),
                Avatar = reader.IsDBNull(avatarOrdinal) ? null : reader.GetString(avatarOrdinal)
            });
        }

        return results;
    }

    public void AddLog(int accountId, string action, DateTime timeStamp)
    {
        // Moved the INSERT query from the service layer to ensure DB calls are only in the repository
        using var conn = Db.CreateConnection();
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO [dbo].[SystemLog] ([Account_ID], [Action], [TimeStamp]) VALUES (@accountId, @action, @ts)";
        cmd.Parameters.AddWithValue("@accountId", accountId);
        cmd.Parameters.AddWithValue("@action", action ?? string.Empty);
        cmd.Parameters.AddWithValue("@ts", timeStamp);

        cmd.ExecuteNonQuery();
    }

    public void DeleteLog(int logId)
    {
        using var conn = Db.CreateConnection();
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM [dbo].[SystemLog] WHERE [Log_ID] = @logId";
        cmd.Parameters.AddWithValue("@logId", logId);

        cmd.ExecuteNonQuery();
    }
}
