using HRManagement.Utilities;
using System;
using System.Collections.ObjectModel;

namespace HRManagement.ViewModels
{
    // ============================================================
    // UI-facing row ViewModels for the KPI Assignment (Admin/Manager)
    // and Personal KPI (Employee) views.
    // ============================================================
    // These wrap IKpiService DTOs (Models/KpiDtos.cs) for binding.
    // ViewModels populate these from service calls; they hold no mock
    // data of their own.

    /// <summary>
    /// One row in the left-hand employee list (KPI Assignment page).
    /// Mirrors ManageEmployeeRowViewModel's shape/avatar convention.
    /// </summary>
    public class KpiEmployeeRowViewModel : ViewModelBase
    {
        private int _employeeId;
        public int EmployeeId { get => _employeeId; set => SetProperty(ref _employeeId, value); }

        private string _employeeName = string.Empty;
        public string EmployeeName { get => _employeeName; set => SetProperty(ref _employeeName, value); }

        private string _employeeCode = string.Empty;
        public string EmployeeCode { get => _employeeCode; set => SetProperty(ref _employeeCode, value); }

        private string _department = string.Empty;
        public string Department { get => _department; set => SetProperty(ref _department, value); }

        private int _assignedKpiCount;
        public int AssignedKpiCount { get => _assignedKpiCount; set => SetProperty(ref _assignedKpiCount, value); }

        private double _kpiMtdPercent;
        public double KpiMtdPercent { get => _kpiMtdPercent; set => SetProperty(ref _kpiMtdPercent, value); }

        public string Initials
        {
            get
            {
                if (string.IsNullOrWhiteSpace(EmployeeName)) return "?";
                var parts = EmployeeName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                return parts.Length switch
                {
                    0 => "?",
                    1 => parts[0][..1].ToUpperInvariant(),
                    _ => (parts[0][..1] + parts[^1][..1]).ToUpperInvariant()
                };
            }
        }
    }

    /// <summary>
    /// One assigned KPI line for the selected employee (wraps
    /// AssignedKpiDto), shown in the right-hand grid on the KPI
    /// Assignment page, and (progress-editable) on the Personal KPI page.
    /// Field names follow KPI_Set_Detail / Employee_KPI_Assignment:
    /// Target (was "Planned"), CurrentValue (was "Actual"), AssignedWeight
    /// (was "WeightPercent").
    /// </summary>
    public class AssignedKpiRowViewModel : ViewModelBase
    {
        private int _assignmentId;
        public int AssignmentId { get => _assignmentId; set => SetProperty(ref _assignmentId, value); }

        private int _kpiSetDetailId;
        public int KpiSetDetailId { get => _kpiSetDetailId; set => SetProperty(ref _kpiSetDetailId, value); }

        private string _kpiName = string.Empty;
        public string KpiName { get => _kpiName; set => SetProperty(ref _kpiName, value); }

        private string _measurementUnit = string.Empty;
        public string MeasurementUnit { get => _measurementUnit; set => SetProperty(ref _measurementUnit, value); }

        private decimal _assignedTarget;
        public decimal AssignedTarget
        {
            get => _assignedTarget;
            set { if (SetProperty(ref _assignedTarget, value)) RecalculateProgress(); }
        }

        private decimal _currentValue;
        public decimal CurrentValue
        {
            get => _currentValue;
            set
            {
                if (SetProperty(ref _currentValue, value))
                {
                    RecalculateProgress();
                    NotifyActionState();
                }
            }
        }

        /*
         * Employee enters only this value.
         * It represents an increment waiting for Manager/Admin approval.
         * CurrentValue remains read-only on Personal KPI.
         */
        private decimal? _pendingValue;
        public decimal? PendingValue
        {
            get => _pendingValue;
            set
            {
                if (SetProperty(ref _pendingValue, value))
                {
                    OnPropertyChanged(nameof(PendingValueDisplay));
                    NotifyActionState();
                }
            }
        }

        public string PendingValueDisplay =>
            PendingValue.HasValue
                ? PendingValue.Value.ToString("N2")
                : "-";

        private decimal _assignedWeight;
        public decimal AssignedWeight { get => _assignedWeight; set => SetProperty(ref _assignedWeight, value); }

        private double _progressPercent;
        public double ProgressPercent { get => _progressPercent; private set => SetProperty(ref _progressPercent, value); }

        private string _status = "Not Started";
        public string Status
        {
            get => _status;
            set
            {
                if (SetProperty(ref _status, value))
                {
                    NotifyActionState();
                }
            }
        }

        private bool _isLocked;
        public bool IsLocked
        {
            get => _isLocked;
            set
            {
                if (SetProperty(ref _isLocked, value))
                {
                    NotifyActionState();
                }
            }
        }

        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        public bool HasPendingApproval =>
            PendingValue.HasValue
            || string.Equals(
                Status,
                "Pending Approval",
                StringComparison.OrdinalIgnoreCase);

        public bool CanUpdateProgress =>
            !IsLocked
            && !HasPendingApproval
            && !string.Equals(
                Status,
                "Completed",
                StringComparison.OrdinalIgnoreCase)
            && !string.Equals(
                Status,
                "Cancelled",
                StringComparison.OrdinalIgnoreCase);

        public bool CanApprovePending =>
            !IsLocked
            && PendingValue.HasValue
            && string.Equals(
                Status,
                "Pending Approval",
                StringComparison.OrdinalIgnoreCase);

        public bool CanRejectPending =>
            CanApprovePending;

        public bool CanModify =>
            !IsLocked;

        private void NotifyActionState()
        {
            OnPropertyChanged(nameof(HasPendingApproval));
            OnPropertyChanged(nameof(CanUpdateProgress));
            OnPropertyChanged(nameof(CanApprovePending));
            OnPropertyChanged(nameof(CanRejectPending));
            OnPropertyChanged(nameof(CanModify));
        }

        private void RecalculateProgress()
        {
            ProgressPercent = AssignedTarget <= 0 ? 0 : Math.Min(100, (double)(CurrentValue / AssignedTarget) * 100);
        }
    }

    /// <summary>
    /// A selectable KPI pack (KpiSet) summary, used by the "Assign"
    /// dialog's Step 1. Carries its full line-item breakdown
    /// (Details) so the card can list every KPI in the pack inline.
    /// </summary>
    public class MonthlyKpiListOption : ViewModelBase
    {
        public int KpiSetId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Month { get; set; } = string.Empty;

        public ObservableCollection<KpiSetDetailItem> Details { get; } = new();

        public int KpiCount => Details.Count;

        private bool _isSelected;
        public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
    }

    /// <summary>
    /// One KPI line item within a MonthlyKpiListOption - shown listed
    /// under the pack card in Step 1, and as a checkbox row in Step 3.
    /// </summary>
    public class KpiSetDetailItem : ViewModelBase
    {
        public int KpiSetDetailId { get; set; }
        public int KpiId { get; set; }
        public string KpiName { get; set; } = string.Empty;
        public string MeasurementUnit { get; set; } = string.Empty;
        public decimal TargetValue { get; set; }
        public decimal Weight { get; set; }
    }

    /// <summary>
    /// Step 3 of the Assign KPI wizard: one card per chosen employee.
    /// Holds the FULL set of the chosen pack's KPI line items
    /// (KpiItems) plus which ones are currently selected for this
    /// employee (via KpiSelectionItem.IsSelected) - selection is done
    /// with checkboxes rather than separate add/remove buttons.
    /// </summary>
    public class EmployeeAssignmentCard : ViewModelBase
    {
        private const decimal RequiredTotalWeight = 100m;
        private const decimal WeightTolerance = 0.01m;

        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;

        public string Initials
        {
            get
            {
                if (string.IsNullOrWhiteSpace(EmployeeName))
                    return "?";

                var parts = EmployeeName.Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries);

                return parts.Length switch
                {
                    0 => "?",
                    1 => parts[0][..1].ToUpperInvariant(),
                    _ => (parts[0][..1] + parts[^1][..1])
                        .ToUpperInvariant()
                };
            }
        }

        public ObservableCollection<KpiSelectionItem> KpiItems { get; } = new();

        public int SelectedCount =>
            KpiItems.Count(item => item.IsSelected);

        public decimal TotalSelectedWeight =>
            KpiItems
                .Where(item => item.IsSelected)
                .Sum(item => item.AssignedWeight);

        public decimal RemainingWeight =>
            RequiredTotalWeight - TotalSelectedWeight;

        public bool HasValidWeight =>
            Math.Abs(TotalSelectedWeight - RequiredTotalWeight)
            <= WeightTolerance;

        public string WeightSummary =>
            HasValidWeight
                ? $"Total Weight: {TotalSelectedWeight:N2}%"
                : RemainingWeight > 0
                    ? $"Total Weight: {TotalSelectedWeight:N2}% | Remaining: {RemainingWeight:N2}%"
                    : $"Total Weight: {TotalSelectedWeight:N2}% | Exceeded: {Math.Abs(RemainingWeight):N2}%";

        private bool _isExpanded = true;
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        public void RefreshWeightState()
        {
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(TotalSelectedWeight));
            OnPropertyChanged(nameof(RemainingWeight));
            OnPropertyChanged(nameof(HasValidWeight));
            OnPropertyChanged(nameof(WeightSummary));
        }
    }

    /// <summary>
    /// One checkbox-selectable KPI line item inside an
    /// EmployeeAssignmentCard (Step 3). Assigned Target/Weight default
    /// to the pack's KpiSetDetail values but remain independently
    /// editable per employee while selected.
    /// </summary>
    public class KpiSelectionItem : ViewModelBase
    {
        public int KpiSetDetailId { get; set; }
        public int KpiId { get; set; }
        public string KpiName { get; set; } = string.Empty;
        public string MeasurementUnit { get; set; } = string.Empty;

        public EmployeeAssignmentCard? OwnerCard { get; set; }

        private bool _isSelected = true;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (SetProperty(ref _isSelected, value))
                    OwnerCard?.RefreshWeightState();
            }
        }

        private decimal _assignedTarget;
        public decimal AssignedTarget
        {
            get => _assignedTarget;
            set => SetProperty(ref _assignedTarget, value);
        }

        private decimal _assignedWeight;
        public decimal AssignedWeight
        {
            get => _assignedWeight;
            set
            {
                if (SetProperty(ref _assignedWeight, value))
                    OwnerCard?.RefreshWeightState();
            }
        }
    }

    /// <summary>
    /// A selectable employee entry in the "Assign" dialog's Step 2
    /// multi-employee picker.
    /// </summary>
    public class SelectableEmployeeItem : ViewModelBase
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;

        private bool _isSelected;
        public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
    }

    /// <summary>
    /// Backing model for the "Edit KPI" modal form (Target / Current
    /// value / Weight, per the updated field names). Holds a working
    /// copy so Cancel can discard changes without mutating the
    /// underlying AssignedKpiRowViewModel until Save.
    /// </summary>
    public class KpiEditDialogViewModel : ViewModelBase
    {
        private const decimal RequiredTotalWeight = 100m;
        private const decimal WeightTolerance = 0.01m;

        public string KpiName { get; set; } = string.Empty;
        public string MeasurementUnit { get; set; } = string.Empty;

        private decimal _assignedTarget;
        public decimal AssignedTarget
        {
            get => _assignedTarget;
            set
            {
                if (SetProperty(ref _assignedTarget, value))
                    OnPropertyChanged(nameof(IsValid));
            }
        }

        private decimal _currentValue;
        public decimal CurrentValue
        {
            get => _currentValue;
            set => SetProperty(ref _currentValue, value);
        }

        private decimal _assignedWeight;
        public decimal AssignedWeight
        {
            get => _assignedWeight;
            set
            {
                if (SetProperty(ref _assignedWeight, value))
                    RefreshValidation();
            }
        }

        private decimal _otherKpiWeight;
        public decimal OtherKpiWeight
        {
            get => _otherKpiWeight;
            set
            {
                if (SetProperty(ref _otherKpiWeight, value))
                    RefreshValidation();
            }
        }

        public decimal TotalWeightAfterEdit =>
            OtherKpiWeight + AssignedWeight;

        public bool IsWeightValid =>
            Math.Abs(TotalWeightAfterEdit - RequiredTotalWeight)
            <= WeightTolerance;

        public bool IsValid =>
            AssignedTarget > 0
            && AssignedWeight > 0
            && AssignedWeight <= RequiredTotalWeight
            && IsWeightValid;

        public string WeightValidationMessage =>
            IsWeightValid
                ? $"Total Weight: {TotalWeightAfterEdit:N2}%"
                : TotalWeightAfterEdit < RequiredTotalWeight
                    ? $"Total Weight: {TotalWeightAfterEdit:N2}% | Remaining: {RequiredTotalWeight - TotalWeightAfterEdit:N2}%"
                    : $"Total Weight: {TotalWeightAfterEdit:N2}% | Exceeded: {TotalWeightAfterEdit - RequiredTotalWeight:N2}%";

        private void RefreshValidation()
        {
            OnPropertyChanged(nameof(TotalWeightAfterEdit));
            OnPropertyChanged(nameof(IsWeightValid));
            OnPropertyChanged(nameof(WeightValidationMessage));
            OnPropertyChanged(nameof(IsValid));
        }
    }

}