using HRManagement.Models;
using HRManagement.Repositories;
using HRManagement.Services.Interfaces;
using HRManagement.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace HRManagement.ViewModels;

public class LogsViewModel : PageViewModel
{
    public override string Title => "Logs";

    private readonly SystemLogRepository _repo;
    private readonly ILogService _logService;

    public ObservableCollection<SystemLog> Logs { get; } = new();

    public LogsViewModel(SystemLogRepository repo, ILogService logService)
    {
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _logService = logService ?? throw new ArgumentNullException(nameof(logService));

        LoadLogs();

        // seed if empty (optional)
        if (!Logs.Any())
        {
            try
            {
                _logService.WriteLog(0, "Application initialized - sample log");
                LoadLogs();
            }
            catch
            {
                // ignore
            }
        }
    }

    public void LoadLogs()
    {
        try
        {
            var logs = _repo.GetLogs() ?? new List<SystemLog>();
            Logs.Clear();
            foreach (var l in logs)
                Logs.Add(l);
        }
        catch
        {
            // ignore
        }
    }
}
