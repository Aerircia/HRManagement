using HRManagement.Models;
using HRManagement.Services;
using HRManagement.Services.Interfaces;
using HRManagement.Utilities;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

namespace HRManagement.ViewModels
{
    /// <summary>
    /// "Manage KPI" page - Admin-only CRUD over the master KPI catalog
    /// (KPI table): the company's reusable list of KPIs that KPI packs
    /// (KpiSet) are built from on the KPI Assignment page. Follows the
    /// same Add/Edit + Delete confirmation overlay pattern as
    /// ManageDepartmentsViewModel / ManageAnnouncementsViewModel. All
    /// persistence/authorization/logging lives in IManageKpisService; this
    /// ViewModel only owns presentation state (form fields, row mapping,
    /// filtering).
    /// </summary>
    public class ManageKpisViewModel : PageViewModel
    {
        private readonly IManageKpisService _manageKpisService;

        public override string Title => "Manage KPI";

        public ManageKpisViewModel(
            IManageKpisService manageKpisService)
        {
            _manageKpisService = manageKpisService;

            Kpis = [];

            KpisView = CollectionViewSource.GetDefaultView(Kpis);
            KpisView.Filter = FilterKpi;

            KpiTypeOptions = _manageKpisService.GetKpiTypeOptions();
            MeasurementUnitOptions = _manageKpisService.GetMeasurementUnitOptions();
            CalculationMethodOptions = _manageKpisService.GetCalculationMethodOptions();

            AddCommand = new RelayCommand(_ => OpenAddForm());
            EditCommand = new RelayCommand(param => OpenEditForm(param as KpiRow));
            DeleteCommand = new RelayCommand(param => RequestDelete(param as KpiRow));
            SaveCommand = new RelayCommand(_ => SaveForm());
            CancelCommand = new RelayCommand(_ => CloseForm());
            ConfirmDeleteCommand = new RelayCommand(_ => ConfirmDelete());
            CancelDeleteCommand = new RelayCommand(_ => CancelDelete());

            HasAccess = _manageKpisService.CurrentUserHasAccess();
            HasNoAccess = !HasAccess;

            if (HasAccess)
                LoadKpis();
        }

        // Access control

        public bool HasAccess { get; }
        public bool HasNoAccess { get; }

        // Dropdown options

        public List<string> KpiTypeOptions { get; }
        public List<string> MeasurementUnitOptions { get; }
        public List<string> CalculationMethodOptions { get; }

        // List

        public ObservableCollection<KpiRow> Kpis { get; }
        public ICollectionView KpisView { get; }

        public bool IsEmpty => Kpis.Count == 0;

        // Summary stat cards (Total / Active / Inactive / Distinct Types)

        private int _totalKpisCount;
        public int TotalKpisCount
        {
            get => _totalKpisCount;
            private set => SetProperty(ref _totalKpisCount, value);
        }

        private int _activeKpisCount;
        public int ActiveKpisCount
        {
            get => _activeKpisCount;
            private set => SetProperty(ref _activeKpisCount, value);
        }

        private int _inactiveKpisCount;
        public int InactiveKpisCount
        {
            get => _inactiveKpisCount;
            private set => SetProperty(ref _inactiveKpisCount, value);
        }

        private int _distinctKpiTypeCount;
        public int DistinctKpiTypeCount
        {
            get => _distinctKpiTypeCount;
            private set => SetProperty(ref _distinctKpiTypeCount, value);
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                    KpisView.Refresh();
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

        private string _formTitleText = "Add KPI";
        public string FormTitleText
        {
            get => _formTitleText;
            set => SetProperty(ref _formTitleText, value);
        }

        private int _formKpiId;

        private string _formKpiName = string.Empty;
        public string FormKpiName
        {
            get => _formKpiName;
            set => SetProperty(ref _formKpiName, value);
        }

        private string _formDescription = string.Empty;
        public string FormDescription
        {
            get => _formDescription;
            set => SetProperty(ref _formDescription, value);
        }

        private string _formKpiType = string.Empty;
        public string FormKpiType
        {
            get => _formKpiType;
            set => SetProperty(ref _formKpiType, value);
        }

        private string _formMeasurementUnit = string.Empty;
        public string FormMeasurementUnit
        {
            get => _formMeasurementUnit;
            set => SetProperty(ref _formMeasurementUnit, value);
        }

        private string _formDefaultTarget = "0";
        public string FormDefaultTarget
        {
            get => _formDefaultTarget;
            set => SetProperty(ref _formDefaultTarget, value);
        }

        private string _formCalculationMethod = "HigherIsBetter";
        public string FormCalculationMethod
        {
            get => _formCalculationMethod;
            set => SetProperty(ref _formCalculationMethod, value);
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

        // Delete confirmation overlay

        private bool _isDeleteConfirmOpen;
        public bool IsDeleteConfirmOpen
        {
            get => _isDeleteConfirmOpen;
            set => SetProperty(ref _isDeleteConfirmOpen, value);
        }

        private KpiRow? _pendingDelete;
        public KpiRow? PendingDelete
        {
            get => _pendingDelete;
            set => SetProperty(ref _pendingDelete, value);
        }

        private string? _deleteErrorMessage;
        public string? DeleteErrorMessage
        {
            get => _deleteErrorMessage;
            set
            {
                if (SetProperty(ref _deleteErrorMessage, value))
                    OnPropertyChanged(nameof(HasDeleteError));
            }
        }

        public bool HasDeleteError => !string.IsNullOrWhiteSpace(DeleteErrorMessage);

        public RelayCommand ConfirmDeleteCommand { get; }
        public RelayCommand CancelDeleteCommand { get; }

        // Loading

        private void LoadKpis()
        {
            Kpis.Clear();
            foreach (var kpi in _manageKpisService.GetKpis())
                Kpis.Add(kpi);

            OnPropertyChanged(nameof(IsEmpty));
            UpdateStats();
        }

        private void UpdateStats()
        {
            TotalKpisCount = Kpis.Count;
            ActiveKpisCount = Kpis.Count(k => k.Kpi.IsActive);
            InactiveKpisCount = Kpis.Count(k => !k.Kpi.IsActive);
            DistinctKpiTypeCount = Kpis
                .Select(k => k.KpiType)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
        }

        private bool FilterKpi(object obj)
        {
            if (obj is not KpiRow row)
                return false;

            if (string.IsNullOrWhiteSpace(SearchText))
                return true;

            var term = SearchText.Trim();

            return row.KpiName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || row.KpiType.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (row.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false);
        }

        // Add / Edit

        private void OpenAddForm()
        {
            _formKpiId = 0;
            FormTitleText = "Add KPI";
            FormKpiName = string.Empty;
            FormDescription = string.Empty;
            FormKpiType = KpiTypeOptions.FirstOrDefault() ?? string.Empty;
            FormMeasurementUnit = MeasurementUnitOptions.FirstOrDefault() ?? string.Empty;
            FormDefaultTarget = "0";
            FormCalculationMethod = CalculationMethodOptions.FirstOrDefault() ?? "HigherIsBetter";
            FormIsActive = true;
            FormErrorMessage = null;
            IsFormOpen = true;
        }

        private void OpenEditForm(KpiRow? row)
        {
            if (row?.Kpi == null)
                return;

            var kpi = row.Kpi;

            _formKpiId = kpi.KpiId;
            FormTitleText = "Edit KPI";
            FormKpiName = kpi.KpiName;
            FormDescription = kpi.Description ?? string.Empty;
            FormKpiType = kpi.KpiType;
            FormMeasurementUnit = kpi.MeasurementUnit;
            FormDefaultTarget = kpi.DefaultTarget.ToString();
            FormCalculationMethod = kpi.CalculationMethod;
            FormIsActive = kpi.IsActive;
            FormErrorMessage = null;
            IsFormOpen = true;
        }

        private void CloseForm()
        {
            IsFormOpen = false;
        }

        private void SaveForm()
        {
            if (!decimal.TryParse(FormDefaultTarget, out var defaultTarget) || defaultTarget < 0)
            {
                FormErrorMessage = "Default target must be a valid non-negative number.";
                return;
            }

            var input = new KpiInput
            {
                KpiId = _formKpiId,
                KpiName = FormKpiName,
                Description = FormDescription,
                KpiType = FormKpiType,
                MeasurementUnit = FormMeasurementUnit,
                DefaultTarget = defaultTarget,
                CalculationMethod = FormCalculationMethod,
                IsActive = FormIsActive
            };

            try
            {
                var savedKpi = _manageKpisService.SaveKpi(input);

                if (_formKpiId == 0)
                {
                    Kpis.Add(savedKpi);
                }
                else
                {
                    var existing = Kpis.FirstOrDefault(k => k.Kpi.KpiId == _formKpiId);

                    if (existing != null)
                    {
                        var index = Kpis.IndexOf(existing);
                        Kpis[index] = savedKpi;
                    }
                }

                OnPropertyChanged(nameof(IsEmpty));
                UpdateStats();
                IsFormOpen = false;
            }
            catch (ArgumentException exception)
            {
                FormErrorMessage = exception.Message;
            }
            catch (InvalidOperationException exception)
            {
                FormErrorMessage = exception.Message;
            }
        }

        // Delete

        private void RequestDelete(KpiRow? row)
        {
            if (row == null)
                return;

            PendingDelete = row;
            DeleteErrorMessage = null;
            IsDeleteConfirmOpen = true;
        }

        private void ConfirmDelete()
        {
            if (PendingDelete == null)
                return;

            try
            {
                _manageKpisService.DeleteKpi(
                    PendingDelete.Kpi.KpiId,
                    PendingDelete.KpiName);

                Kpis.Remove(PendingDelete);
                OnPropertyChanged(nameof(IsEmpty));
                UpdateStats();
                PendingDelete = null;
                IsDeleteConfirmOpen = false;
            }
            catch (InvalidOperationException exception)
            {
                DeleteErrorMessage = exception.Message;
            }
        }

        private void CancelDelete()
        {
            PendingDelete = null;
            DeleteErrorMessage = null;
            IsDeleteConfirmOpen = false;
        }
    }
}
