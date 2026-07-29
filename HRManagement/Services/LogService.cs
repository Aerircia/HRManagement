using HRManagement.Data;
using HRManagement.Services.Interfaces;
using Microsoft.Data.SqlClient;
using System;

namespace HRManagement.Services;

public class LogService : ILogService
{
    private readonly DatabaseContext _db = new();

    public void WriteLog(int accountId, string action)
    {
        try
        {
            using var conn = _db.CreateConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            // use the actual column names in the database
            cmd.CommandText = "INSERT INTO [dbo].[SystemLog] ([Account_ID], [Action], [TimeStamp]) VALUES (@accountId, @action, @ts)";
            cmd.Parameters.AddWithValue("@accountId", accountId);
            cmd.Parameters.AddWithValue("@action", action ?? string.Empty);
            cmd.Parameters.AddWithValue("@ts", DateTime.UtcNow);
            cmd.ExecuteNonQuery();
        }
        catch
        {
            // fail silently for now - preserve existing app stability
        }
    }
}
