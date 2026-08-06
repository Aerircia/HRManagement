using HRManagement.Services;
using HRManagement.Services.Interfaces;
using HRManagement.Utilities;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace HRManagement.ViewModels
{
    // ============================================================
    // KPI Assignment / Management (Admin + Manager).
    // ============================================================
    // Data now comes from IKpiService (see Services/Interfaces/
    // IKpiService.cs) - no mock/in-memory seeding. IKpiService is not
    // implemented yet (interface only, per project scope); this
    // ViewModel is wired against the interface so it compiles and is
    // ready the moment a concrete KpiService is registered in
    // App.xaml.cs.
    //
    // TODO (backend phase, per WIRING_NOTES.md):
    //   - Manager-role department scoping is enforced in KpiService,
    //     not here - the IsAdmin-based filtering below is UI
    //     convenience only, matching ManageAttendancesViewModel.
    //   - ILogService calls for Assign/Edit/Delete live in the service
    //     layer, not here.

    public class KpiAssignmentViewModel : PageViewModel
    {
        private readonly SessionManager _sessionManager;
        private readonly IKpiService _kpiService;

        // Maps the display name shown/selected in the Departments dropdown
        // back to the real Department_ID the service needs. "All
        // Departments" intentionally has no entry (== no filter / null).
        private readonly System.Collections.Generic.Dictionary<string, int> _departmentIdsByName =
            new(StringComparer.OrdinalIgnoreCase);

        public KpiAssignmentViewModel(
            SessionManager sessionManager,
            IKpiService kpiService)
        {
            _sessionManager = sessionManager;
            _kpiService = kpiService;

            Departments = new ObservableCollection<string>();
            Employees = new ObservableCollection<KpiEmployeeRowViewModel>();
            AssignedKpis = new ObservableCollection<AssignedKpiRowViewModel>();
            AvailableMonths = new ObservableCollection<string>();

            LoadEmployeesCommand = new RelayCommand(_ => LoadEmployees());
            SelectEmployeeCommand = new RelayCommand(p => SelectEmployee(p));
            DeleteKpiRowCommand = new RelayCommand(p => DeleteKpiRow(p));

            // ===== Wizard =====
            OpenAssignDialogCommand = new RelayCommand(_ => OpenAssignWizard());
            CloseAssignDialogCommand = new RelayCommand(_ => CloseAssignWizard());
            SelectMonthlyListCommand = new RelayCommand(p => SelectMonthlyList(p));
            ToggleWizardEmployeeCommand = new RelayCommand(p => ToggleWizardEmployee(p));
            ToggleKpiSelectionCommand = new RelayCommand(p => ToggleKpiSelection(p));
            WizardNextCommand = new RelayCommand(_ => WizardNext(), _ => CanGoNext());
            WizardBackCommand = new RelayCommand(_ => WizardBack(), _ => WizardStep > 1);
            ConfirmAssignCommand = new RelayCommand(_ => ConfirmAssign());

            // ===== Edit KPI modal =====
            EditKpiRowCommand = new RelayCommand(p => OpenEditDialog(p));
            CloseEditDialogCommand = new RelayCommand(_ => IsEditDialogOpen = false);
            SaveEditDialogCommand = new RelayCommand(_ => SaveEditDialog());

            LoadDepartments();
            LoadAvailableMonths();
            LoadEmployees();
        }

        public override string Title => "KPI Assignment";

        public bool IsAdmin => _sessionManager.CurrentUser != null &&
                                _sessionManager.CurrentUser.Role.RoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase);

        public ObservableCollection<string> Departments { get; }
        public ObservableCollection<KpiEmployeeRowViewModel> Employees { get; }
        public ObservableCollection<AssignedKpiRowViewModel> AssignedKpis { get; }
        public ObservableCollection<string> AvailableMonths { get; }

        private string? _selectedDepartment;
        public string? SelectedDepartment
        {
            get => _selectedDepartment;
            set { SetProperty(ref _selectedDepartment, value); LoadEmployees(); }
        }

        private string? _selectedMonth;
        public string? SelectedMonth
        {
            get => _selectedMonth;
            set { if (SetProperty(ref _selectedMonth, value)) LoadEmployees(); }
        }

        private string _employeeFilter = string.Empty;
        public string EmployeeFilter
        {
            get => _employeeFilter;
            set { if (SetProperty(ref _employeeFilter, value)) LoadEmployees(); }
        }

        private KpiEmployeeRowViewModel? _selectedEmployee;
        public KpiEmployeeRowViewModel? SelectedEmployee
        {
            get => _selectedEmployee;
            set
            {
                SetProperty(ref _selectedEmployee, value);
                OnPropertyChanged(nameof(IsEmployeeSelected));
                LoadAssignedKpis();
            }
        }

        public bool IsEmployeeSelected => SelectedEmployee != null;

        // ===== Top stat cards =====

        private int _totalAssignedCount;
        public int TotalAssignedCount { get => _totalAssignedCount; private set => SetProperty(ref _totalAssignedCount, value); }

        private int _employeesWithoutKpiCount;
        public int EmployeesWithoutKpiCount { get => _employeesWithoutKpiCount; private set => SetProperty(ref _employeesWithoutKpiCount, value); }

        private double _averageKpiMtdPercent;
        public double AverageKpiMtdPercent { get => _averageKpiMtdPercent; private set => SetProperty(ref _averageKpiMtdPercent, value); }

        private int _atRiskCount;
        public int AtRiskCount { get => _atRiskCount; private set => SetProperty(ref _atRiskCount, value); }

        // ===== Assign KPI wizard state =====
        // 4 steps:
        //   1. Choose month -> shows that month's KPI packs (KpiSet) as
        //      full-width vertical cards, each listing every KPI it
        //      contains underneath.
        //   2. Choose employees (multi-select).
        //   3. Per-employee cards: every KPI in the chosen pack is
        //      listed with a checkbox (checked by default) - unchecking
        //      removes it from that employee's assignment, no separate
        //      add/remove buttons.
        //   4. Confirmation summary.

        private bool _isAssignDialogOpen;
        public bool IsAssignDialogOpen { get => _isAssignDialogOpen; set => SetProperty(ref _isAssignDialogOpen, value); }

        private int _wizardStep = 1;
        public int WizardStep
        {
            get => _wizardStep;
            private set
            {
                if (SetProperty(ref _wizardStep, value))
                {
                    OnPropertyChanged(nameof(IsStep1));
                    OnPropertyChanged(nameof(IsStep2));
                    OnPropertyChanged(nameof(IsStep3));
                    OnPropertyChanged(nameof(IsStep4));
                    WizardNextCommand?.RaiseCanExecuteChanged();
                    WizardBackCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        public bool IsStep1 => WizardStep == 1;
        public bool IsStep2 => WizardStep == 2;
        public bool IsStep3 => WizardStep == 3;
        public bool IsStep4 => WizardStep == 4;

        // Step 1
        private string? _dialogSelectedMonth;
        public string? DialogSelectedMonth
        {
            get => _dialogSelectedMonth;
            set { if (SetProperty(ref _dialogSelectedMonth, value)) LoadMonthlyListsForMonth(); }
        }

        public ObservableCollection<MonthlyKpiListOption> DialogMonthlyLists { get; } = new();

        private MonthlyKpiListOption? _dialogSelectedList;
        public MonthlyKpiListOption? DialogSelectedList
        {
            get => _dialogSelectedList;
            private set => SetProperty(ref _dialogSelectedList, value);
        }

        // Step 2
        public ObservableCollection<SelectableEmployeeItem> DialogEmployees { get; } = new();
        public int DialogSelectedEmployeeCount => DialogEmployees.Count(e => e.IsSelected);

        // Step 3
        public ObservableCollection<EmployeeAssignmentCard> WizardEmployeeCards { get; } = new();

        public ICommand LoadEmployeesCommand { get; }
        public ICommand SelectEmployeeCommand { get; }
        public ICommand OpenAssignDialogCommand { get; }
        public ICommand CloseAssignDialogCommand { get; }
        public ICommand SelectMonthlyListCommand { get; }
        public ICommand ToggleWizardEmployeeCommand { get; }
        public ICommand ToggleKpiSelectionCommand { get; }

        // Typed as RelayCommand (not ICommand) so RaiseCanExecuteChanged
        // can be called explicitly after selection/step changes - relying
        // solely on CommandManager.RequerySuggested is not reliable for
        // programmatic state changes triggered by card-click / checkbox
        // commands rather than direct focus/keyboard input.
        public RelayCommand WizardNextCommand { get; }
        public RelayCommand WizardBackCommand { get; }
        public ICommand ConfirmAssignCommand { get; }
        public ICommand DeleteKpiRowCommand { get; }

        // ===== Edit KPI modal state =====

        private bool _isEditDialogOpen;
        public bool IsEditDialogOpen { get => _isEditDialogOpen; set => SetProperty(ref _isEditDialogOpen, value); }

        private KpiEditDialogViewModel? _editDialog;
        public KpiEditDialogViewModel? EditDialog { get => _editDialog; private set => SetProperty(ref _editDialog, value); }

        private AssignedKpiRowViewModel? _editingTargetRow;

        public ICommand EditKpiRowCommand { get; }
        public ICommand CloseEditDialogCommand { get; }
        public ICommand SaveEditDialogCommand { get; }

        // ============================================================
        // Loading (via IKpiService)
        // ============================================================

        private void LoadAvailableMonths()
        {
            AvailableMonths.Clear();
            foreach (var m in _kpiService.GetAvailableMonths())
                AvailableMonths.Add(m);

            SelectedMonth = AvailableMonths.FirstOrDefault();
        }

        private void LoadDepartments()
        {
            Departments.Clear();
            _departmentIdsByName.Clear();

            foreach (var dept in _kpiService.GetDepartmentOptions())
            {
                Departments.Add(dept.DepartmentName);

                // "All Departments" (Department_ID == 0, the sentinel used
                // by GetDepartmentOptions) intentionally has no entry here,
                // so ResolveSelectedDepartmentId() falls back to null/no
                // filter for it.
                if (dept.DepartmentId != 0)
                    _departmentIdsByName[dept.DepartmentName] = dept.DepartmentId;
            }

            SelectedDepartment = Departments.FirstOrDefault();
        }

        private int? ResolveSelectedDepartmentId()
        {
            if (SelectedDepartment != null && _departmentIdsByName.TryGetValue(SelectedDepartment, out var id))
                return id;

            return null;
        }

        private void LoadEmployees()
        {
            Employees.Clear();

            var departmentId = ResolveSelectedDepartmentId();

            var overview = _kpiService.GetAssignmentOverview(departmentId, EmployeeFilter, SelectedMonth ?? string.Empty);

            foreach (var row in overview.Employees)
            {
                Employees.Add(new KpiEmployeeRowViewModel
                {
                    EmployeeId = row.EmployeeId,
                    EmployeeName = row.EmployeeName,
                    EmployeeCode = row.EmployeeCode,
                    Department = row.Department,
                    AssignedKpiCount = row.AssignedKpiCount,
                    KpiMtdPercent = row.KpiMtdPercent
                });
            }

            TotalAssignedCount = overview.TotalAssignedCount;
            EmployeesWithoutKpiCount = overview.EmployeesWithoutKpiCount;
            AverageKpiMtdPercent = overview.AverageKpiMtdPercent;
            AtRiskCount = overview.AtRiskCount;

            if (SelectedEmployee == null || !Employees.Contains(SelectedEmployee))
                SelectedEmployee = Employees.FirstOrDefault();
        }

        private void LoadAssignedKpis()
        {
            AssignedKpis.Clear();

            if (SelectedEmployee == null)
                return;

            var kpis = _kpiService.GetAssignedKpis(SelectedEmployee.EmployeeId, SelectedMonth ?? string.Empty);

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
        }

        private void SelectEmployee(object? param)
        {
            if (param is KpiEmployeeRowViewModel row)
                SelectedEmployee = row;
        }

        private void DeleteKpiRow(object? param)
        {
            if (param is not AssignedKpiRowViewModel row)
                return;

            _kpiService.DeleteAssignment(row.AssignmentId);
            LoadAssignedKpis();
            LoadEmployees();
        }

        // ============================================================
        // Assign KPI wizard (Step 1 -> 4)
        // ============================================================

        private void OpenAssignWizard()
        {
            WizardStep = 1;
            DialogSelectedMonth = SelectedMonth ?? AvailableMonths.FirstOrDefault();
            LoadMonthlyListsForMonth();

            DialogEmployees.Clear();
            foreach (var e in Employees)
            {
                DialogEmployees.Add(new SelectableEmployeeItem
                {
                    EmployeeId = e.EmployeeId,
                    EmployeeName = e.EmployeeName,
                    Department = e.Department,
                    IsSelected = SelectedEmployee != null && e.EmployeeId == SelectedEmployee.EmployeeId
                });
            }
            OnPropertyChanged(nameof(DialogSelectedEmployeeCount));

            WizardEmployeeCards.Clear();

            IsAssignDialogOpen = true;
        }

        private void CloseAssignWizard()
        {
            IsAssignDialogOpen = false;
        }

        // ---- Step 1: month -> KPI pack cards (full detail, no mock data) ----

        private void LoadMonthlyListsForMonth()
        {
            DialogMonthlyLists.Clear();
            DialogSelectedList = null;

            if (string.IsNullOrEmpty(DialogSelectedMonth))
                return;

            var departmentId = ResolveSelectedDepartmentId();

            foreach (var set in _kpiService.GetKpiSetsForMonth(DialogSelectedMonth, departmentId))
            {
                var option = new MonthlyKpiListOption
                {
                    KpiSetId = set.KpiSetId,
                    Name = set.KpiSetName,
                    Department = set.Department ?? string.Empty,
                    Month = DialogSelectedMonth,
                    IsSelected = false
                };

                foreach (var d in set.Details)
                {
                    option.Details.Add(new KpiSetDetailItem
                    {
                        KpiSetDetailId = d.KpiSetDetailId,
                        KpiId = d.KpiId,
                        KpiName = d.KpiName,
                        MeasurementUnit = d.MeasurementUnit,
                        TargetValue = d.TargetValue,
                        Weight = d.Weight
                    });
                }

                DialogMonthlyLists.Add(option);
            }
        }

        private void SelectMonthlyList(object? param)
        {
            if (param is not MonthlyKpiListOption chosen)
                return;

            foreach (var l in DialogMonthlyLists)
                l.IsSelected = l == chosen;

            DialogSelectedList = chosen;
            WizardNextCommand.RaiseCanExecuteChanged();
        }

        // ---- Step 2: employees ----

        private void ToggleWizardEmployee(object? param)
        {
            if (param is SelectableEmployeeItem item)
            {
                item.IsSelected = !item.IsSelected;
                OnPropertyChanged(nameof(DialogSelectedEmployeeCount));
                WizardNextCommand.RaiseCanExecuteChanged();
            }
        }

        // ---- Step 3: per-employee KPI checkbox cards ----

        private void BuildWizardEmployeeCards()
        {
            WizardEmployeeCards.Clear();

            if (DialogSelectedList == null)
                return;

            var chosenEmployees = DialogEmployees.Where(e => e.IsSelected).ToList();

            foreach (var emp in chosenEmployees)
            {
                var card = new EmployeeAssignmentCard
                {
                    EmployeeId = emp.EmployeeId,
                    EmployeeName = emp.EmployeeName,
                    Department = emp.Department,
                    EmployeeCode = Employees.FirstOrDefault(x => x.EmployeeId == emp.EmployeeId)?.EmployeeCode ?? string.Empty
                };

                // Every KPI in the chosen pack is listed, pre-checked.
                foreach (var detail in DialogSelectedList.Details)
                {
                    card.KpiItems.Add(new KpiSelectionItem
                    {
                        KpiSetDetailId = detail.KpiSetDetailId,
                        KpiId = detail.KpiId,
                        KpiName = detail.KpiName,
                        MeasurementUnit = detail.MeasurementUnit,
                        AssignedTarget = detail.TargetValue,
                        AssignedWeight = detail.Weight,
                        IsSelected = true
                    });
                }

                WizardEmployeeCards.Add(card);
            }
        }

        private void ToggleKpiSelection(object? param)
        {
            if (param is KpiSelectionItem item)
            {
                item.IsSelected = !item.IsSelected;
                WizardNextCommand.RaiseCanExecuteChanged();
            }
        }

        // ---- Step navigation ----

        private bool CanGoNext()
        {
            return WizardStep switch
            {
                1 => DialogSelectedList != null,
                2 => DialogEmployees.Any(e => e.IsSelected),
                3 => WizardEmployeeCards.Any(c => c.KpiItems.Any(k => k.IsSelected)),
                _ => true
            };
        }

        private void WizardNext()
        {
            if (!CanGoNext())
                return;

            if (WizardStep == 2)
                BuildWizardEmployeeCards();

            if (WizardStep < 4)
                WizardStep++;
        }

        private void WizardBack()
        {
            if (WizardStep > 1)
                WizardStep--;
        }

        // ---- Step 4: confirm ----

        private void ConfirmAssign()
        {
            if (DialogSelectedList == null)
            {
                IsAssignDialogOpen = false;
                return;
            }

            var request = new Models.AssignKpiRequest
            {
                KpiSetId = DialogSelectedList.KpiSetId,
                Month = DialogSelectedMonth ?? string.Empty,
                EmployeeIds = WizardEmployeeCards.Select(c => c.EmployeeId).ToList(),
                EmployeeAssignments = WizardEmployeeCards.Select(card => new Models.EmployeeKpiAssignmentInput
                {
                    EmployeeId = card.EmployeeId,
                    SelectedDetails = card.KpiItems
                        .Where(k => k.IsSelected)
                        .Select(k => new Models.KpiSetDetailSelectionInput
                        {
                            KpiSetDetailId = k.KpiSetDetailId,
                            AssignedTarget = k.AssignedTarget,
                            AssignedWeight = k.AssignedWeight
                        })
                        .ToList()
                }).ToList()
            };

            _kpiService.AssignKpis(request);

            IsAssignDialogOpen = false;
            LoadEmployees();
            LoadAssignedKpis();
        }

        // ============================================================
        // Edit KPI modal (right-hand grid's Edit button)
        // ============================================================

        private void OpenEditDialog(object? param)
        {
            if (param is not AssignedKpiRowViewModel row)
                return;

            _editingTargetRow = row;

            EditDialog = new KpiEditDialogViewModel
            {
                KpiName = row.KpiName,
                MeasurementUnit = row.MeasurementUnit,
                AssignedTarget = row.AssignedTarget,
                CurrentValue = row.CurrentValue,
                AssignedWeight = row.AssignedWeight
            };

            IsEditDialogOpen = true;
        }

        private void SaveEditDialog()
        {
            if (_editingTargetRow == null || EditDialog == null)
            {
                IsEditDialogOpen = false;
                return;
            }

            _kpiService.UpdateAssignment(new Models.UpdateKpiAssignmentRequest
            {
                AssignmentId = _editingTargetRow.AssignmentId,
                AssignedTarget = EditDialog.AssignedTarget,
                AssignedWeight = EditDialog.AssignedWeight,
                CurrentValue = EditDialog.CurrentValue
            });

            IsEditDialogOpen = false;
            _editingTargetRow = null;
            EditDialog = null;

            LoadAssignedKpis();
            LoadEmployees();
        }
    }
}
