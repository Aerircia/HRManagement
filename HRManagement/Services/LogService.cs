using HRManagement.Models;
using HRManagement.Repositories.Interfaces;
using HRManagement.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HRManagement.Services;

public class LogService : ILogService
{
    private readonly ISystemLogRepository _repo;

    // Use Dependency Injection instead of creating new instances[cite: 1]
    public LogService(ISystemLogRepository repo)
    {
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
    }

    public void WriteLog(int accountId, string action)
    {
        try
        {
            _repo.AddLog(accountId, action, DateTime.UtcNow);
        }
        catch
        {
            // fail silently for now - preserve existing app stability[cite: 1]
        }
    }

    public List<SystemLog> GetLogs()
    {
        try
        {
            return _repo.GetLogs() ?? new List<SystemLog>();
        }
        catch
        {
            return new List<SystemLog>();
        }
    }

    public void EnsureSeeded()
    {
        // Moved the seeding logic out of the ViewModel
        try
        {
            var logs = GetLogs();
            if (!logs.Any())
            {
                WriteLog(0, "Application initialized - sample log");
            }
        }
        catch
        {
            // ignore[cite: 4]
        }
    }
}