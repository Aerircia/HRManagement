using HRManagement.Services;
using HRManagement.Services.Interfaces;
using HRManagement.Utilities;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace HRManagement.ViewModels
{
    // ============================================================
    // Personal KPI (Employee self-view).
    // ============================================================
    // Data comes from IKpiService, scoped to SessionManager.CurrentUser's
    // Employee_ID - no mock/in-memory seeding. IKpiService is interface
    // -only for now (see WIRING_NOTES.md); this ViewModel is wired
    // against it so it compiles and is ready once a concrete
    // implementation is registered in App.xaml.cs.
    //
    // Only "Actual"/Current_Value (progress) is editable here -
    // Assigned Target/Weight are read-only, since target-setting is a
    // Manager/Admin action on the KPI Assignment page. UI disabling of
    // the other fields is cosmetic only; real enforcement belongs in
    // IKpiService.UpdateMyProgress.

    public class PersonalKpiViewModel : PageViewModel
    {
        private readonly SessionManager _sessionManager;
        private readonly IKpiService _kpiService;

        public PersonalKpiViewModel(
            SessionManager sessionManager,
            IKpiService kpiService)
        {
            _sessionManager = sessionManager;
            _kpiService = kpiService;

            AssignedKpis = new ObservableCollection<AssignedKpiRowViewModel>();
            AvailableMonths = new ObservableCollection<string>();

            EditProgressCommand = new RelayCommand(p => EditProgress(p));
            SaveProgressCommand = new RelayCommand(p => SaveProgress(p));
            CancelProgressCommand = new RelayCommand(p => CancelProgress(p));

            LoadAvailableMonths();
            LoadKpisForMonth();
        }

        public override string Title => "My KPI";

        public string EmployeeDisplayName => _sessionManager.CurrentUser?.Employee?.FullName ?? string.Empty;

        public ObservableCollection<string> AvailableMonths { get; }

        private string? _selectedMonth;
        public string? SelectedMonth
        {
            get => _selectedMonth;
            set { if (SetProperty(ref _selectedMonth, value)) LoadKpisForMonth(); }
        }

        public ObservableCollection<AssignedKpiRowViewModel> AssignedKpis { get; }

        // ===== Top stat cards =====

        private int _totalKpiCount;
        public int TotalKpiCount { get => _totalKpiCount; private set => SetProperty(ref _totalKpiCount, value); }

        private int _completedKpiCount;
        public int CompletedKpiCount { get => _completedKpiCount; private set => SetProperty(ref _completedKpiCount, value); }

        private int _inProgressKpiCount;
        public int InProgressKpiCount { get => _inProgressKpiCount; private set => SetProperty(ref _inProgressKpiCount, value); }

        private double _overallKpiMtdPercent;
        public double OverallKpiMtdPercent { get => _overallKpiMtdPercent; private set => SetProperty(ref _overallKpiMtdPercent, value); }

        public ICommand EditProgressCommand { get; }
        public ICommand SaveProgressCommand { get; }
        public ICommand CancelProgressCommand { get; }

        private readonly Dictionary<int, decimal> _preEditActualBackup = new();

        private int CurrentEmployeeId => _sessionManager.CurrentUser?.Employee?.EmployeeId ?? 0;

        private void LoadAvailableMonths()
        {
            AvailableMonths.Clear();
            foreach (var m in _kpiService.GetAvailableMonths())
                AvailableMonths.Add(m);

            SelectedMonth = AvailableMonths.FirstOrDefault();
        }

        private void LoadKpisForMonth()
        {
            AssignedKpis.Clear();

            if (string.IsNullOrEmpty(SelectedMonth))
                return;

            var kpis = _kpiService.GetMyAssignedKpis(CurrentEmployeeId, SelectedMonth);

            foreach (var dto in kpis)
            {
                AssignedKpis.Add(new AssignedKpiRowViewModel
                {
                    AssignmentId = dto.AssignmentId,
                    KpiSetDetailId = dto.KpiSetDetailId,
                    KpiName = dto.KpiName,
                    MeasurementUnit = dto.MeasurementUnit,
                    AssignedTarget = dto.AssignedTarget,
                    CurrentValue = dto.CurrentValue,
                    AssignedWeight = dto.AssignedWeight,
                    Status = dto.Status
                });
            }

            RecalculateStats();
        }

        private void RecalculateStats()
        {
            TotalKpiCount = AssignedKpis.Count;
            CompletedKpiCount = AssignedKpis.Count(k => k.ProgressPercent >= 100);
            InProgressKpiCount = AssignedKpis.Count(k => k.ProgressPercent is > 0 and < 100);
            OverallKpiMtdPercent = AssignedKpis.Count == 0 ? 0 : Math.Round(AssignedKpis.Average(k => k.ProgressPercent), 1);
        }

        // ===== Progress-only update (employee self-service) =====

        private void EditProgress(object? param)
        {
            if (param is AssignedKpiRowViewModel row)
            {
                _preEditActualBackup[row.AssignmentId] = row.CurrentValue;
                row.IsEditing = true;
            }
        }

        private void SaveProgress(object? param)
        {
            if (param is not AssignedKpiRowViewModel row)
                return;

            _kpiService.UpdateMyProgress(row.AssignmentId, CurrentEmployeeId, row.CurrentValue);

            row.IsEditing = false;
            _preEditActualBackup.Remove(row.AssignmentId);
            RecalculateStats();
        }

        private void CancelProgress(object? param)
        {
            if (param is AssignedKpiRowViewModel row)
            {
                if (_preEditActualBackup.TryGetValue(row.AssignmentId, out var original))
                {
                    row.CurrentValue = original;
                    _preEditActualBackup.Remove(row.AssignmentId);
                }
                row.IsEditing = false;
            }
        }
    }
}
