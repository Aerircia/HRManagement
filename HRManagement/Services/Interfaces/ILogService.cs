using HRManagement.Models;
using System.Collections.Generic;

namespace HRManagement.Services.Interfaces;

public interface ILogService
{
    void WriteLog(int accountId, string action);
    List<SystemLog> GetLogs();

    /// <summary>
    /// Same log data as GetLogs, but with each entry's employee name/avatar
    /// resolved for display.
    /// </summary>
    List<SystemLogItemModel> GetLogItems();

    /// <summary>
    /// Deletes a single log entry. Returns true if the delete call
    /// completed without error.
    /// </summary>
    bool DeleteLog(int logId);

    void EnsureSeeded();
}
