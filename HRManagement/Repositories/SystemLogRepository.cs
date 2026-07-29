using HRManagement.Data;
using HRManagement.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;

namespace HRManagement.Repositories;

public class SystemLogRepository : RepositoryBase
{
    public List<SystemLog> GetLogs()
    {
        var results = new List<SystemLog>();

        using var conn = Db.CreateConnection();
        conn.Open();

        using var cmd = conn.CreateCommand();
        // use column names matching the database schema
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
}
