using HRManagement.Models;
using System;
using System.Collections.Generic;

namespace HRManagement.Repositories.Interfaces;

public interface ISystemLogRepository
{
    List<SystemLog> GetLogs();
    void AddLog(int accountId, string action, DateTime timeStamp);
}