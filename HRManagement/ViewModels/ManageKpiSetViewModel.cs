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

            HasAccess = _manageKpiSetService.CurrentUserHasAccess();
            HasNoAccess = !HasAccess;

            if (HasAccess)
            {
                LoadKpiSets();
                LoadDepartments();
            }
        }

        // Access control

        public bool HasAccess { get; }
        public bool HasNoAccess { get; }

        // List

        public ObservableCollection<KpiSetRow> KpiSets { get; }
        public ICollectionView KpiSetsView { get; }

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
                    KpiSetsView.Refresh();
            }
        }

        public RelayCommand AddCommand { get; }
        public RelayCommand EditCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand CancelCommand { get; }

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

        public RelayCommand ConfirmDeleteCommand { get; }
        public RelayCommand CancelDeleteCommand { get; }

        // Loading

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
                    var row = ToRow(kpiSet, setWithDetails.Details.Count);

                    // Map department name from lookup
                    if (row.DepartmentName?.StartsWith("Dept") == true && kpiSet.DepartmentId.HasValue)
                    {
                        var deptName = deptLookup.TryGetValue(kpiSet.DepartmentId.Value, out var name) 
                            ? name 
                            : $"Dept {kpiSet.DepartmentId}";
                        row.DepartmentName = deptName;
                    }

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

            // Group by Department for the grouped view
            var grouped = KpiSets
                .GroupBy(k => k.DepartmentName ?? "Organization-wide")
                .ToList();

            DepartmentGroups = new ObservableCollection<IGrouping<string, KpiSetRow>>(grouped);
        }

        private static KpiSetRow ToRow(KpiSet kpiSet, int detailCount)
        {
            return new KpiSetRow
            {
                KpiSet = kpiSet,
                KpiSetId = kpiSet.KpiSetId,
                KpiSetName = kpiSet.KpiSetName,
                Description = kpiSet.Description,
                DepartmentName = kpiSet.DepartmentId.HasValue ? $"Dept {kpiSet.DepartmentId}" : "Organization-wide",
                Status = kpiSet.IsActive ? "Active" : "Inactive",
                CreatedAtDisplay = kpiSet.CreatedAt.ToString("MMM dd, yyyy"),
                CreatedDate = kpiSet.CreatedAt,
                DetailCount = detailCount,
                IsActive = kpiSet.IsActive,
                Details = new()
            };
        }

        private bool FilterKpiSet(object obj)
        {
            if (obj is not KpiSetRow row)
                return false;

            if (string.IsNullOrWhiteSpace(SearchText))
                return true;

            var term = SearchText.Trim();

            return row.KpiSetName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (row.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false);
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

            PendingDelete = row;
            IsDeleteConfirmOpen = true;
        }

        private void ConfirmDelete()
        {
            try
            {
                if (PendingDelete == null)
                    return;

                _manageKpiSetService.DeleteKpiSet(
                    PendingDelete.KpiSetId,
                    PendingDelete.KpiSetName);

                LoadKpiSets();
                CancelDelete();
            }
            catch (Exception ex)
            {
                // Could show error in UI here
                System.Diagnostics.Debug.WriteLine($"Delete failed: {ex.Message}");
            }
        }

        private void CancelDelete()
        {
            IsDeleteConfirmOpen = false;
            PendingDelete = null;
        }
    }
}
