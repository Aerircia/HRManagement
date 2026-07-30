using HRManagement.Models;
using System.Collections.Generic;

namespace HRManagement.Services.Interfaces;

public interface ILogService
{
    void WriteLog(int accountId, string action);
    List<SystemLog> GetLogs();
    void EnsureSeeded();
}