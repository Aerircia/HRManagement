using HRManagement.Models;
using System;
using System.Collections.Generic;

namespace HRManagement.Repositories.Interfaces;

public interface ISystemLogRepository
{
    List<SystemLog> GetLogs();

    /// <summary>
    /// Same log rows as GetLogs, but joined with Account/Employee so each
    /// entry carries the acting employee's display name and avatar.
    /// </summary>
    List<SystemLogItemModel> GetLogItems();

    void AddLog(int accountId, string action, DateTime timeStamp);

    /// <summary>
    /// Deletes a single log entry by ID. No-op if the log doesn't exist.
    /// </summary>
    void DeleteLog(int logId);
}
