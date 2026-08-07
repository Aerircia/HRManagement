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

        private string _operationMessage = string.Empty;
        public string OperationMessage
        {
            get => _operationMessage;
            private set
            {
                if (SetProperty(ref _operationMessage, value))
                {
                    OnPropertyChanged(nameof(HasOperationMessage));
                }
            }
        }

        private bool _isOperationError;
        public bool IsOperationError
        {
            get => _isOperationError;
            private set => SetProperty(ref _isOperationError, value);
        }

        public bool HasOperationMessage =>
            !string.IsNullOrWhiteSpace(OperationMessage);

        private void SetSuccessMessage(string message)
        {
            IsOperationError = false;
            OperationMessage = message;
        }

        private void SetErrorMessage(Exception exception)
        {
            IsOperationError = true;
            OperationMessage = exception.Message;
        }

        private void ClearOperationMessage()
        {
            IsOperationError = false;
            OperationMessage = string.Empty;
        }

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

        private readonly Dictionary<int, decimal?> _preEditPendingBackup = new();

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

            try
            {
                /*
                 * Desktop app is not continuously running. Synchronize
                 * expired KPI periods whenever Personal KPI is loaded.
                 */
                _kpiService.SynchronizeExpiredAssignments();
            }
            catch (Exception exception)
            {
                SetErrorMessage(exception);
            }

            var kpis =
                _kpiService.GetMyAssignedKpis(
                    CurrentEmployeeId,
                    SelectedMonth);

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
                    PendingValue = dto.PendingValue,
                    AssignedWeight = dto.AssignedWeight,
                    Status = dto.Status,
                    IsLocked = dto.IsLocked
                });
            }

            RecalculateStats();
        }

        private void RecalculateStats()
        {
            TotalKpiCount =
                AssignedKpis.Count;

            /*
             * Status must drive completion because an expired KPI is
             * Completed even when CurrentValue is below Target.
             */
            CompletedKpiCount =
                AssignedKpis.Count(
                    item =>
                        string.Equals(
                            item.Status,
                            "Completed",
                            StringComparison.OrdinalIgnoreCase));

            InProgressKpiCount =
                AssignedKpis.Count(
                    item =>
                        string.Equals(
                            item.Status,
                            "In Progress",
                            StringComparison.OrdinalIgnoreCase)
                        || string.Equals(
                            item.Status,
                            "Pending Approval",
                            StringComparison.OrdinalIgnoreCase));

            var totalWeight =
                AssignedKpis.Sum(
                    item =>
                        item.AssignedWeight);

            if (totalWeight <= 0)
            {
                OverallKpiMtdPercent = 0;
                return;
            }

            var weightedProgress =
                AssignedKpis.Sum(
                    item =>
                        (decimal)item.ProgressPercent
                        * item.AssignedWeight);

            OverallKpiMtdPercent =
                Math.Round(
                    (double)(
                        weightedProgress
                        / totalWeight),
                    1);
        }

        // ===== Progress-only update (employee self-service) =====

        private void EditProgress(object? param)
        {
            ClearOperationMessage();

            if (param is not AssignedKpiRowViewModel row
                || !row.CanUpdateProgress)
            {
                return;
            }

            _preEditPendingBackup[row.AssignmentId] =
                row.PendingValue;

            /*
             * Start from 0 because PendingValue is an increment,
             * not the replacement total CurrentValue.
             */
            row.PendingValue = 0;
            row.IsEditing = true;
        }

        private void SaveProgress(object? param)
        {
            if (param is not AssignedKpiRowViewModel row)
                return;

            var pendingIncrement =
                row.PendingValue ?? 0;

            if (pendingIncrement <= 0)
            {
                SetErrorMessage(
                    new InvalidOperationException(
                        "Pending progress must be greater than 0."));
                return;
            }

            try
            {
                _kpiService.UpdateMyProgress(
                    row.AssignmentId,
                    CurrentEmployeeId,
                    pendingIncrement);

                row.IsEditing = false;

                _preEditPendingBackup.Remove(
                    row.AssignmentId);

                SetSuccessMessage(
                    $"Progress update for '{row.KpiName}' was submitted " +
                    "and is waiting for Manager/Admin approval.");

                LoadKpisForMonth();
            }
            catch (Exception exception)
            {
                SetErrorMessage(exception);
            }
        }

        private void CancelProgress(object? param)
        {
            if (param is not AssignedKpiRowViewModel row)
                return;

            if (_preEditPendingBackup.TryGetValue(
                    row.AssignmentId,
                    out var original))
            {
                row.PendingValue = original;

                _preEditPendingBackup.Remove(
                    row.AssignmentId);
            }

            row.IsEditing = false;
        }
    }
}
