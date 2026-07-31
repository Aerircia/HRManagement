using HRManagement.Models;
using HRManagement.Services.Interfaces;
using HRManagement.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace HRManagement.ViewModels;

public class LogsViewModel : PageViewModel
{
    public override string Title => "Logs";

    private readonly ILogService _logService;

    private List<SystemLogItemModel> _allLogs = new();

    public LogsViewModel(ILogService logService)
    {
        _logService = logService ?? throw new ArgumentNullException(nameof(logService));

        DeleteLogCommand = new RelayCommand(param => DeleteLog(param as LogEntryViewModel));

        // Offload back-end seeding logic to the service layer
        _logService.EnsureSeeded();

        LoadLogs();
    }

    public ObservableCollection<LogDayGroupViewModel> DayGroups { get; } = new();

    public ICommand DeleteLogCommand { get; }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                ApplyFilter();
        }
    }

    private bool _isEmpty;
    public bool IsEmpty
    {
        get => _isEmpty;
        set => SetProperty(ref _isEmpty, value);
    }

    public void LoadLogs()
    {
        _allLogs = _logService.GetLogItems();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        IEnumerable<SystemLogItemModel> filtered = _allLogs;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();

            filtered = filtered.Where(l =>
                l.EmployeeName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                l.Action.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        DayGroups.Clear();

        var groups = filtered
            .OrderByDescending(l => l.TimeStamp)
            .GroupBy(l => l.TimeStamp.Date)
            .OrderByDescending(g => g.Key);

        foreach (var group in groups)
        {
            var dayGroup = new LogDayGroupViewModel
            {
                DayLabel = FormatDayLabel(group.Key),
                Count = group.Count()
            };

            foreach (var log in group)
                dayGroup.Entries.Add(new LogEntryViewModel(log));

            DayGroups.Add(dayGroup);
        }

        IsEmpty = DayGroups.Count == 0;
    }

    private void DeleteLog(LogEntryViewModel? entry)
    {
        if (entry == null)
            return;

        if (!_logService.DeleteLog(entry.LogId))
            return;

        _allLogs.RemoveAll(l => l.LogId == entry.LogId);
        ApplyFilter();
    }

    private static string FormatDayLabel(DateTime date)
    {
        if (date == DateTime.Today)
            return "Today";

        if (date == DateTime.Today.AddDays(-1))
            return "Yesterday";

        return date.ToString("ddd, dd MMM yyyy");
    }
}

// One collapsible day section in the timeline (e.g. "Today", "Mon, 01 Dec 2021").
public class LogDayGroupViewModel : ViewModelBase
{
    private string _dayLabel = string.Empty;
    public string DayLabel { get => _dayLabel; set => SetProperty(ref _dayLabel, value); }

    private int _count;
    public int Count { get => _count; set => SetProperty(ref _count, value); }

    public ObservableCollection<LogEntryViewModel> Entries { get; } = new();
}

// Thin, bindable wrapper around SystemLogItemModel for one timeline row.
public class LogEntryViewModel : ViewModelBase
{
    public LogEntryViewModel(SystemLogItemModel model)
    {
        LogId = model.LogId;
        AccountId = model.AccountId;
        EmployeeName = model.EmployeeName;
        Avatar = model.Avatar;
        Action = model.Action;
        TimeStamp = model.TimeStamp;
    }

    public int LogId { get; }

    public int AccountId { get; }

    public string EmployeeName { get; }

    public string? Avatar { get; }

    public string Action { get; }

    public DateTime TimeStamp { get; }

    public string TimeDisplay => TimeStamp.ToString("h:mm tt").ToLowerInvariant();

    // First letter of each of the first two name parts, e.g. "Nicholas Sharp" -> "NS".
    public string Initials
    {
        get
        {
            var parts = EmployeeName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
                return "?";

            if (parts.Length == 1)
                return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpperInvariant();

            return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
        }
    }
}
