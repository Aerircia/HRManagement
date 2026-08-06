using HRManagement.Models;
using HRManagement.Services.Interfaces;
using HRManagement.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Xml.Linq;

namespace HRManagement.ViewModels
{
    /// <summary>
    /// "Manage Contracts" page - full CRUD table over every employee's
    /// contract. Admin/Manager only. All persistence, access-control, and
    /// logging logic lives in IContractService; this ViewModel only talks
    /// to that interface and has no knowledge of repositories.
    /// </summary>
    public class ManageContractsViewModel : PageViewModel
    {
        public static readonly List<string> ContractTypeOptions = ["Full-time", "Part-time", "Internship", "Seasonal"];
        public static readonly List<string> StatusOptions = ["Active", "Expired", "Terminated", "Pending"];

        private readonly IContractService _contractService;

        public override string Title => "Manage Contracts";

        public ManageContractsViewModel(IContractService contractService)
        {
            _contractService = contractService;

            Contracts = [];
            Employees = [];
            PositionOptions = _contractService.GetPositionOptions();
            ContractTypeOptionsList = ContractTypeOptions;
            StatusOptionsList = StatusOptions;
            SortOptions = ["Employee (A-Z)", "Employee (Z-A)", "Start Date (Newest)", "Start Date (Oldest)"];
            _selectedSort = SortOptions[0];

            ContractsView = CollectionViewSource.GetDefaultView(Contracts);
            ContractsView.Filter = FilterContract;
            ApplySort();

            AddCommand = new RelayCommand(_ => OpenAddForm());
            EditCommand = new RelayCommand(param => OpenEditForm(param as ContractItemModel));
            DeleteCommand = new RelayCommand(param => RequestDelete(param as ContractItemModel));
            SaveCommand = new RelayCommand(_ => SaveForm());
            CancelCommand = new RelayCommand(_ => CloseForm());
            ConfirmDeleteCommand = new RelayCommand(_ => ConfirmDelete());
            CancelDeleteCommand = new RelayCommand(_ => CancelDelete());

            HasAccess = _contractService.CurrentUserHasAccess();
            HasNoAccess = !HasAccess;

            if (HasAccess)
            {
                LoadEmployees();
                LoadContracts();
            }
        }

        // Access control

        public bool HasAccess { get; }
        public bool HasNoAccess { get; }

        // List

        public ObservableCollection<ContractItemModel> Contracts { get; }
        public ICollectionView ContractsView { get; }
        public ObservableCollection<IdNamePair> Employees { get; }
        public List<IdNamePair> PositionOptions { get; }
        public List<string> ContractTypeOptionsList { get; }
        public List<string> StatusOptionsList { get; }
        public List<string> SortOptions { get; }

        // Summary stat cards (Total / Active / Expired / Terminated)

        private int _totalContractsCount;
        public int TotalContractsCount
        {
            get => _totalContractsCount;
            private set => SetProperty(ref _totalContractsCount, value);
        }

        private int _activeContractsCount;
        public int ActiveContractsCount
        {
            get => _activeContractsCount;
            private set => SetProperty(ref _activeContractsCount, value);
        }

        private int _expiredContractsCount;
        public int ExpiredContractsCount
        {
            get => _expiredContractsCount;
            private set => SetProperty(ref _expiredContractsCount, value);
        }

        private int _terminatedContractsCount;
        public int TerminatedContractsCount
        {
            get => _terminatedContractsCount;
            private set => SetProperty(ref _terminatedContractsCount, value);
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                    ContractsView.Refresh();
            }
        }

        private string _selectedSort;
        public string SelectedSort
        {
            get => _selectedSort;
            set
            {
                if (SetProperty(ref _selectedSort, value))
                    ApplySort();
            }
        }

        public RelayCommand AddCommand { get; }
        public RelayCommand EditCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand CancelCommand { get; }

        //Add/Edit form overlay 

        private bool _isFormOpen;
        public bool IsFormOpen
        {
            get => _isFormOpen;
            set => SetProperty(ref _isFormOpen, value);
        }

        private string _formTitle = "Add Contract";
        public string FormTitle
        {
            get => _formTitle;
            set => SetProperty(ref _formTitle, value);
        }

        private int _formContractId;

        private IdNamePair? _formSelectedEmployee;
        public IdNamePair? FormSelectedEmployee
        {
            get => _formSelectedEmployee;
            set => SetProperty(ref _formSelectedEmployee, value);
        }

        private IdNamePair? _formSelectedPosition;
        public IdNamePair? FormSelectedPosition
        {
            get => _formSelectedPosition;
            set => SetProperty(ref _formSelectedPosition, value);
        }

        private string _formContractType = ContractTypeOptions[0];
        public string FormContractType
        {
            get => _formContractType;
            set => SetProperty(ref _formContractType, value);
        }

        private DateTime? _formStartDate = DateTime.Today;
        public DateTime? FormStartDate
        {
            get => _formStartDate;
            set => SetProperty(ref _formStartDate, value);
        }

        private DateTime? _formEndDate;
        public DateTime? FormEndDate
        {
            get => _formEndDate;
            set => SetProperty(ref _formEndDate, value);
        }

        private string _formStatus = "Active";
        public string FormStatus
        {
            get => _formStatus;
            set => SetProperty(ref _formStatus, value);
        }

        private string _formBaseSalary = string.Empty;
        public string FormBaseSalary
        {
            get => _formBaseSalary;
            set => SetProperty(ref _formBaseSalary, value);
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

        private ContractItemModel? _pendingDelete;
        public ContractItemModel? PendingDelete
        {
            get => _pendingDelete;
            set => SetProperty(ref _pendingDelete, value);
        }

        public RelayCommand ConfirmDeleteCommand { get; }
        public RelayCommand CancelDeleteCommand { get; }

        // Loading

        private void LoadEmployees()
        {
            Employees.Clear();
            foreach (var employee in _contractService.GetEmployees())
                Employees.Add(employee);
        }

        private void LoadContracts()
        {
            Contracts.Clear();
            foreach (var contract in _contractService.GetContracts())
                Contracts.Add(contract);

            UpdateStats();
        }

        private void UpdateStats()
        {
            TotalContractsCount = Contracts.Count;
            ActiveContractsCount = Contracts.Count(c => string.Equals(c.Status, "Active", StringComparison.OrdinalIgnoreCase));
            ExpiredContractsCount = Contracts.Count(c => string.Equals(c.Status, "Expired", StringComparison.OrdinalIgnoreCase));
            TerminatedContractsCount = Contracts.Count(c => string.Equals(c.Status, "Terminated", StringComparison.OrdinalIgnoreCase));
        }

        private bool FilterContract(object obj)
        {
            if (obj is not ContractItemModel row)
                return false;

            if (string.IsNullOrWhiteSpace(SearchText))
                return true;

            var term = SearchText.Trim();

            return row.EmployeeName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || row.ContractType.Contains(term, StringComparison.OrdinalIgnoreCase);
        }

        private void ApplySort()
        {
            ContractsView.SortDescriptions.Clear();

            switch (SelectedSort)
            {
                case "Employee (Z-A)":
                    ContractsView.SortDescriptions.Add(new SortDescription(nameof(ContractItemModel.EmployeeName), ListSortDirection.Descending));
                    break;

                case "Start Date (Newest)":
                    ContractsView.SortDescriptions.Add(new SortDescription($"{nameof(ContractItemModel.Contract)}.{nameof(Models.Contract.StartDate)}", ListSortDirection.Descending));
                    break;

                case "Start Date (Oldest)":
                    ContractsView.SortDescriptions.Add(new SortDescription($"{nameof(ContractItemModel.Contract)}.{nameof(Models.Contract.StartDate)}", ListSortDirection.Ascending));
                    break;

                default: // "Employee (A-Z)"
                    ContractsView.SortDescriptions.Add(new SortDescription(nameof(ContractItemModel.EmployeeName), ListSortDirection.Ascending));
                    break;
            }
        }

        // Add / Edit

        private void OpenAddForm()
        {
            _formContractId = 0;
            FormTitle = "Add Contract";
            FormSelectedEmployee = Employees.FirstOrDefault();
            FormSelectedPosition = PositionOptions.FirstOrDefault();
            FormContractType = ContractTypeOptions[0];
            FormStartDate = DateTime.Today;
            FormEndDate = null;
            FormStatus = "Active";
            FormBaseSalary = string.Empty;
            FormErrorMessage = null;
            IsFormOpen = true;
        }

        private void OpenEditForm(ContractItemModel? row)
        {
            if (row?.Contract == null)
                return;

            var contract = row.Contract;

            _formContractId = contract.ContractId;
            FormTitle = "Edit Contract";
            FormSelectedEmployee = Employees.FirstOrDefault(e => e.Id == contract.EmployeeId);
            FormSelectedPosition = PositionOptions.FirstOrDefault(p => p.Id == contract.PositionId);
            FormContractType = contract.ContractType;
            FormStartDate = contract.StartDate;
            FormEndDate = contract.EndDate;
            FormStatus = contract.Status;
            FormBaseSalary = contract.BaseSalary.ToString("0.##");
            FormErrorMessage = null;
            IsFormOpen = true;
        }

        private void CloseForm()
        {
            IsFormOpen = false;
        }

        private void SaveForm()
        {
            if (FormSelectedEmployee == null || FormSelectedPosition == null)
            {
                FormErrorMessage = "Employee and position are required.";
                return;
            }

            if (string.IsNullOrWhiteSpace(FormContractType))
            {
                FormErrorMessage = "Contract type is required.";
                return;
            }

            if (FormStartDate == null)
            {
                FormErrorMessage = "Start date is required.";
                return;
            }

            if (FormEndDate.HasValue && FormEndDate.Value < FormStartDate.Value)
            {
                FormErrorMessage = "End date cannot be before the start date.";
                return;
            }

            if (!decimal.TryParse(FormBaseSalary, out var baseSalary) || baseSalary < 0)
            {
                FormErrorMessage = "Base salary must be a valid non-negative number.";
                return;
            }

            var input = new ContractInput
            {
                ContractId = _formContractId,
                EmployeeId = FormSelectedEmployee.Id,
                PositionId = FormSelectedPosition.Id,
                ContractType = FormContractType,
                StartDate = FormStartDate.Value,
                EndDate = FormEndDate,
                Status = FormStatus,
                BaseSalary = baseSalary
            };

            try
            {
                if (_formContractId == 0)
                {
                    var saved = _contractService.AddContract(input);
                    Contracts.Add(saved);
                }
                else
                {
                    var saved = _contractService.UpdateContract(input);
                    var existing = Contracts.FirstOrDefault(c => c.Contract.ContractId == _formContractId);
                    if (existing != null)
                    {
                        var index = Contracts.IndexOf(existing);
                        Contracts[index] = saved;
                    }
                }

                UpdateStats();

                IsFormOpen = false;
            }
            catch (Exception ex)
            {
                FormErrorMessage = ex.Message;
            }
        }

        //Delete 

        private void RequestDelete(ContractItemModel? row)
        {
            if (row == null)
                return;

            PendingDelete = row;
            IsDeleteConfirmOpen = true;
        }

        private void ConfirmDelete()
        {
            if (PendingDelete == null)
                return;

            try
            {
                _contractService.DeleteContract(PendingDelete.Contract.ContractId);
                Contracts.Remove(PendingDelete);
                UpdateStats();
            }
            finally
            {
                PendingDelete = null;
                IsDeleteConfirmOpen = false;
            }
        }

        private void CancelDelete()
        {
            PendingDelete = null;
            IsDeleteConfirmOpen = false;
        }
    }
}
