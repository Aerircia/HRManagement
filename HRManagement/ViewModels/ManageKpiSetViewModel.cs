using HRManagement.Models;
using HRManagement.Services;
using HRManagement.Services.Interfaces;
using HRManagement.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

namespace HRManagement.ViewModels
{
    /// <summary>
    /// "Manage KPI Sets" page - Admin-only CRUD over KPI Set definitions (packs).
    /// Follows the same Add/Edit + Delete confirmation overlay pattern as
    /// ManageAnnouncementsViewModel and ManageProfilesViewModel.
    /// All persistence/authorization/logging lives in IManageKpiSetService;
    /// this ViewModel only owns presentation state (form fields, row mapping, filtering).
    /// </summary>
    public class ManageKpiSetViewModel : PageViewModel
    {
        private readonly IManageKpiSetService _manageKpiSetService;

        public override string Title => "Manage KPI Sets";

        public ManageKpiSetViewModel(IManageKpiSetService manageKpiSetService)
        {
            _manageKpiSetService = manageKpiSetService
                ?? throw new ArgumentNullException(nameof(manageKpiSetService));

            KpiSets = [];
            KpiSetsView = CollectionViewSource.GetDefaultView(KpiSets);
            KpiSetsView.Filter = FilterKpiSet;

            AvailableKpis = [];
            Departments = [];
            SelectedDetails = [];

            AddCommand = new RelayCommand(_ => OpenAddForm());
            EditCommand = new RelayCommand(param => OpenEditForm(param as KpiSetRow));
            DeleteCommand = new RelayCommand(param => RequestDelete(param as KpiSetRow));
            SaveCommand = new RelayCommand(_ => SaveForm());
            CancelCommand = new RelayCommand(_ => CloseForm());
            ConfirmDeleteCommand = new RelayCommand(_ => ConfirmDelete());
            CancelDeleteCommand = new RelayCommand(_ => CancelDelete());

            AddDetailCommand = new RelayCommand(_ => AddDetailRow());
            RemoveDetailCommand = new RelayCommand(param => RemoveDetailRow(param as KpiSetDetailRow));

            RefreshCommand = new RelayCommand(_ => Refresh());

            HasAccess = _manageKpiSetService.CurrentUserHasAccess();
            HasNoAccess = !HasAccess;

            if (HasAccess)
            {
                // Departments must be loaded first: LoadKpiSets() builds a
                // department-name lookup from this collection while mapping
                // rows, so loading it after would leave every row showing
                // the "Dept {id}" fallback instead of the real name.
                LoadDepartments();
                LoadKpiSets();
            }
        }

        // Access control

        public bool HasAccess { get; }
        public bool HasNoAccess { get; }

        // List

        public ObservableCollection<KpiSetRow> KpiSets { get; }
        public ICollectionView KpiSetsView { get; }

        // Stat cards - derived from KpiSets, refreshed alongside it in
        // LoadKpiSets() rather than bound to any fake/static numbers.

        private int _totalSetsCount;
        public int TotalSetsCount
        {
            get => _totalSetsCount;
            set => SetProperty(ref _totalSetsCount, value);
        }

        private int _totalKpiCount;
        public int TotalKpiCount
        {
            get => _totalKpiCount;
            set => SetProperty(ref _totalKpiCount, value);
        }

        private int _activeSetsCount;
        public int ActiveSetsCount
        {
            get => _activeSetsCount;
            set => SetProperty(ref _activeSetsCount, value);
        }

        private int _inactiveSetsCount;
        public int InactiveSetsCount
        {
            get => _inactiveSetsCount;
            set => SetProperty(ref _inactiveSetsCount, value);
        }

        // Bound directly by ManageKpiSetView.xaml's ItemsControl - this is
        // the collection actually rendered on screen, so it must always be
        // rebuilt from the *filtered* view (KpiSetsView), not from the raw
        // KpiSets collection. Both LoadKpiSets() and SearchText funnel
        // through RebuildDepartmentGroups() so the grouped list and the
        // DepartmentGroups.Count-based empty-state binding stay correct
        // whether the list was just reloaded or just filtered.
        private ObservableCollection<IGrouping<string, KpiSetRow>>? _departmentGroups;
        public ObservableCollection<IGrouping<string, KpiSetRow>>? DepartmentGroups
        {
            get => _departmentGroups;
            set => SetProperty(ref _departmentGroups, value);
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    KpiSetsView.Refresh();
                    RebuildDepartmentGroups();
                }
            }
        }

        public RelayCommand AddCommand { get; }
        public RelayCommand EditCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand CancelCommand { get; }
        public RelayCommand RefreshCommand { get; }

        // Add/Edit form overlay

        private bool _isFormOpen;
        public bool IsFormOpen
        {
            get => _isFormOpen;
            set => SetProperty(ref _isFormOpen, value);
        }

        private string _formTitleText = "Add KPI Set";
        public string FormTitleText
        {
            get => _formTitleText;
            set => SetProperty(ref _formTitleText, value);
        }

        private int _formKpiSetId;

        private string _formKpiSetName = string.Empty;
        public string FormKpiSetName
        {
            get => _formKpiSetName;
            set => SetProperty(ref _formKpiSetName, value);
        }

        private string _formDescription = string.Empty;
        public string FormDescription
        {
            get => _formDescription;
            set => SetProperty(ref _formDescription, value);
        }

        private int? _formDepartmentId;
        public int? FormDepartmentId
        {
            get => _formDepartmentId;
            set => SetProperty(ref _formDepartmentId, value);
        }

        private bool _formIsActive = true;
        public bool FormIsActive
        {
            get => _formIsActive;
            set => SetProperty(ref _formIsActive, value);
        }

        private string? _formErrorMessage;
        public string? FormErrorMessage
        {
            get => _formErrorMessage;
            set
            {
                if (SetProperty(ref _formErrorMessage, value))
                    OnPropertyChanged(nameof(HasFormError));
            }
        }

        public bool HasFormError => !string.IsNullOrWhiteSpace(FormErrorMessage);

        // KPI Details sub-form

        public ObservableCollection<KpiSetDetailRow> SelectedDetails { get; }
        public List<Kpi> AvailableKpis { get; }
        public List<Department> Departments { get; }

        public RelayCommand AddDetailCommand { get; }
        public RelayCommand RemoveDetailCommand { get; }

        // Delete confirmation overlay

        private bool _isDeleteConfirmOpen;
        public bool IsDeleteConfirmOpen
        {
            get => _isDeleteConfirmOpen;
            set => SetProperty(ref _isDeleteConfirmOpen, value);
        }

        private KpiSetRow? _pendingDelete;
        public KpiSetRow? PendingDelete
        {
            get => _pendingDelete;
            set => SetProperty(ref _pendingDelete, value);
        }

        private string? _deleteErrorMessage;
        public string? DeleteErrorMessage
        {
            get => _deleteErrorMessage;
            set => SetProperty(ref _deleteErrorMessage, value);
        }

        public RelayCommand ConfirmDeleteCommand { get; }
        public RelayCommand CancelDeleteCommand { get; }

        // Loading

        private void Refresh()
        {
            LoadDepartments();
            LoadKpiSets();
        }

        private void LoadKpiSets()
        {
            KpiSets.Clear();
            var sets = _manageKpiSetService.GetKpiSets();

            // Create a lookup for department names
            var deptLookup = Departments.ToDictionary(d => d.DepartmentId, d => d.DepartmentName);

            foreach (var kpiSet in sets)
            {
                var setWithDetails = _manageKpiSetService.GetKpiSetById(kpiSet.KpiSetId);
                if (setWithDetails != null)
                {
                    var row = ToRow(kpiSet, setWithDetails.Details.Count, deptLookup);

                    // Populate detail rows
                    foreach (var detail in setWithDetails.Details)
                    {
                        row.Details.Add(new KpiSetDetailRow
                        {
                            Detail = detail,
                            KpiSetDetailId = detail.KpiSetDetailId,
                            KpiId = detail.KpiId,
                            KpiName = detail.KpiName ?? string.Empty,
                            MeasurementUnit = detail.MeasurementUnit ?? string.Empty,
                            TargetValue = detail.TargetValue,
                            Weight = detail.Weight
                        });
                    }

                    KpiSets.Add(row);
                }
            }

            TotalSetsCount = KpiSets.Count;
            TotalKpiCount = KpiSets.Sum(k => k.DetailCount);
            ActiveSetsCount = KpiSets.Count(k => k.IsActive);
            InactiveSetsCount = KpiSets.Count(k => !k.IsActive);

            // Group by Department for the grouped view - built from the
            // filtered view so an active search term is respected on load
            // too (e.g. after Refresh while a search is still typed in).
            RebuildDepartmentGroups();
        }

        /// <summary>
        /// Rebuilds DepartmentGroups (the collection actually bound in
        /// ManageKpiSetView.xaml) from the current filtered KpiSetsView.
        /// Must be called both after reloading KpiSets and whenever the
        /// search filter changes, otherwise the grouped list on screen and
        /// the DepartmentGroups.Count empty-state binding go stale.
        /// </summary>
        private void RebuildDepartmentGroups()
        {
            var grouped = KpiSetsView.Cast<KpiSetRow>()
                .GroupBy(k => k.DepartmentName ?? "Organization-wide")
                .ToList();

            DepartmentGroups = new ObservableCollection<IGrouping<string, KpiSetRow>>(grouped);
        }

        private static KpiSetRow ToRow(KpiSet kpiSet, int detailCount, Dictionary<int, string> deptLookup)
        {
            string departmentName;
            if (kpiSet.DepartmentId.HasValue)
            {
                departmentName = deptLookup.TryGetValue(kpiSet.DepartmentId.Value, out var name)
                    ? name
                    : $"Department {kpiSet.DepartmentId}";
            }
            else
            {
                departmentName = "Organization-wide";
            }

            return new KpiSetRow
            {
                KpiSet = kpiSet,
                KpiSetId = kpiSet.KpiSetId,
                KpiSetName = kpiSet.KpiSetName,
                Description = kpiSet.Description,
                DepartmentName = departmentName,
                Status = kpiSet.IsActive ? "Active" : "Inactive",
                CreatedAtDisplay = kpiSet.CreatedAt.ToString("MMM dd, yyyy"),
                CreatedDate = kpiSet.CreatedAt,
                DetailCount = detailCount,
                IsActive = kpiSet.IsActive,
                Details = new()
            };
        }

        /// <summary>
        /// Matches SearchText against the KPI Set name, description,
        /// department name, and the name of any KPI included in the set -
        /// so searching "Sales" surfaces sets named after it, sets
        /// belonging to that department, and sets that merely contain a
        /// "Sales ..." KPI line item.
        /// </summary>
        private bool FilterKpiSet(object obj)
        {
            if (obj is not KpiSetRow row)
                return false;

            if (string.IsNullOrWhiteSpace(SearchText))
                return true;

            var term = SearchText.Trim();

            return row.KpiSetName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (row.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                || (row.DepartmentName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                || row.Details.Any(d => d.KpiName.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        // Add / Edit

        private void OpenAddForm()
        {
            _formKpiSetId = 0;
            FormTitleText = "Add KPI Set";
            FormKpiSetName = string.Empty;
            FormDescription = string.Empty;
            FormDepartmentId = null;
            FormIsActive = true;
            FormErrorMessage = null;

            SelectedDetails.Clear();
            LoadAvailableKpis();

            IsFormOpen = true;
        }

        private void OpenEditForm(KpiSetRow? row)
        {
            if (row == null)
                return;

            var setWithDetails = _manageKpiSetService.GetKpiSetById(row.KpiSetId);
            if (setWithDetails == null)
            {
                FormErrorMessage = "KPI Set could not be loaded.";
                return;
            }

            _formKpiSetId = setWithDetails.KpiSet.KpiSetId;
            FormTitleText = "Edit KPI Set";
            FormKpiSetName = setWithDetails.KpiSet.KpiSetName;
            FormDescription = setWithDetails.KpiSet.Description ?? string.Empty;
            FormDepartmentId = setWithDetails.KpiSet.DepartmentId;
            FormIsActive = setWithDetails.KpiSet.IsActive;
            FormErrorMessage = null;

            SelectedDetails.Clear();
            foreach (var detail in setWithDetails.Details)
            {
                SelectedDetails.Add(new KpiSetDetailRow
                {
                    Detail = detail,
                    KpiSetDetailId = detail.KpiSetDetailId,
                    KpiId = detail.KpiId,
                    KpiName = detail.KpiName ?? string.Empty,
                    MeasurementUnit = detail.MeasurementUnit ?? string.Empty,
                    TargetValue = detail.TargetValue,
                    Weight = detail.Weight
                });
            }

            LoadAvailableKpis();
            IsFormOpen = true;
        }

        private void LoadAvailableKpis()
        {
            AvailableKpis.Clear();
            foreach (var kpi in _manageKpiSetService.GetAvailableKpis())
            {
                AvailableKpis.Add(kpi);
            }
        }

        private void LoadDepartments()
        {
            Departments.Clear();
            foreach (var dept in _manageKpiSetService.GetDepartments())
            {
                Departments.Add(dept);
            }
        }

        private void SaveForm()
        {
            try
            {
                FormErrorMessage = null;

                if (SelectedDetails.Count == 0)
                {
                    FormErrorMessage = "At least one KPI must be added to the set.";
                    return;
                }

                // Validate that all details have a valid KPI selected
                var invalidDetails = SelectedDetails.Where(d => d.KpiId <= 0).ToList();
                if (invalidDetails.Any())
                {
                    FormErrorMessage = "All KPI details must have a KPI selected.";
                    return;
                }

                var input = new KpiSetInput
                {
                    KpiSetId = _formKpiSetId,
                    KpiSetName = FormKpiSetName,
                    Description = FormDescription,
                    DepartmentId = FormDepartmentId,
                    IsActive = FormIsActive,
                    Details = SelectedDetails.Select(d => new KpiSetDetailInput
                    {
                        KpiSetDetailId = d.KpiSetDetailId,
                        KpiId = d.KpiId,
                        TargetValue = d.TargetValue,
                        Weight = d.Weight
                    }).ToList()
                };

                var saved = _manageKpiSetService.SaveKpiSet(input);

                // Reload the list
                LoadKpiSets();
                CloseForm();
            }
            catch (ArgumentException ex)
            {
                FormErrorMessage = ex.Message;
            }
            catch (Exception ex)
            {
                FormErrorMessage = $"An error occurred: {ex.Message}";
            }
        }

        private void CloseForm()
        {
            IsFormOpen = false;
            _formKpiSetId = 0;
            FormKpiSetName = string.Empty;
            FormDescription = string.Empty;
            FormDepartmentId = null;
            FormIsActive = true;
            FormErrorMessage = null;
            SelectedDetails.Clear();
        }

        // Add / Remove detail rows

        private void AddDetailRow()
        {
            var newRow = new KpiSetDetailRow
            {
                KpiSetDetailId = 0,
                KpiId = 0,
                KpiName = string.Empty,
                MeasurementUnit = string.Empty,
                TargetValue = 0m,
                Weight = 1m
            };

            SelectedDetails.Add(newRow);
        }

        private void RemoveDetailRow(KpiSetDetailRow? row)
        {
            if (row != null)
                SelectedDetails.Remove(row);
        }

        // Delete

        private void RequestDelete(KpiSetRow? row)
        {
            if (row == null)
                return;

            DeleteErrorMessage = null;
            PendingDelete = row;
            IsDeleteConfirmOpen = true;
        }

        private void ConfirmDelete()
        {
            try
            {
                if (PendingDelete == null)
                    return;

                DeleteErrorMessage = null;

                _manageKpiSetService.DeleteKpiSet(
                    PendingDelete.KpiSetId,
                    PendingDelete.KpiSetName);

                LoadKpiSets();
                CancelDelete();
            }
            catch (Exception ex)
            {
                DeleteErrorMessage = $"Could not delete: {ex.Message}";
            }
        }

        private void CancelDelete()
        {
            IsDeleteConfirmOpen = false;
            PendingDelete = null;
            DeleteErrorMessage = null;
        }
    }
}