using HRManagement.Models;
using HRManagement.Services.Interfaces;
using HRManagement.Utilities;
using System;
using System.Collections.ObjectModel;

namespace HRManagement.ViewModels;

public class LogsViewModel : PageViewModel
{
    public override string Title => "Logs"; //[cite: 4]

    private readonly ILogService _logService;

    // View model initializes the observable collection[cite: 4]
    public ObservableCollection<SystemLog> Logs { get; } = new();

    public LogsViewModel(ILogService logService)
    {
        // Removed the SystemLogRepository injection[cite: 4]
        _logService = logService ?? throw new ArgumentNullException(nameof(logService));

        // Offload back-end seeding logic to the service layer
        _logService.EnsureSeeded();

        LoadLogs();
    }

    public void LoadLogs()
    {
        var logs = _logService.GetLogs();
        Logs.Clear();

        // Populate the logs collection for the view[cite: 4]
        foreach (var l in logs)
        {
            Logs.Add(l);
        }
    }
}