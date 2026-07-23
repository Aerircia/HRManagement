using HRManagement.Models;
using HRManagement.Repositories;
using HRManagement.Services;
using HRManagement.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

namespace HRManagement.ViewModels
{
   
    public class ContractViewModel : PageViewModel
    {
        
        private static readonly List<IdNamePair> Roles = new()
        {
            new IdNamePair(1, "Admin"),
            new IdNamePair(2, "Manager"),
            new IdNamePair(3, "Employee")
        };

        public static readonly List<string> ContractTypeOptions = new() { "Full-time", "Part-time", "Internship", "Seasonal" };
        public static readonly List<string> StatusOptions = new() { "Active", "Expired", "Terminated", "Pending" };

        private static readonly HashSet<int> AllowedRoleIds = new() { 1, 2 }; // Admin, Manager

        private readonly ContractRepository _contractRepository;
        private readonly EmployeeRepository _employeeRepository;
        private readonly SessionManager _sessionManager;

        public override string Title => "Contracts";

        public ContractViewModel(
            ContractRepository contractRepository,
            EmployeeRepository employeeRepository,
            SessionManager sessionManager)
        {
            _contractRepository = contractRepository;
            _employeeRepository = employeeRepository;
            _sessionManager = sessionManager;

            Contracts = new ObservableCollection<ContractRow>();
            Employees = new ObservableCollection<IdNamePair>();
            RoleOptions = Roles;
            ContractTypeOptionsList = ContractTypeOptions;
            StatusOptionsList = StatusOptions;

            ContractsView = CollectionViewSource.GetDefaultView(Contracts);
            ContractsView.Filter = FilterContract;

            AddCommand = new RelayCommand(_ => OpenAddForm());
            EditCommand = new RelayCommand(param => OpenEditForm(param as ContractRow));
            DeleteCommand = new RelayCommand(param => RequestDelete(param as ContractRow));
            SaveCommand = new RelayCommand(_ => SaveForm());
            CancelCommand = new RelayCommand(_ => CloseForm());
            ConfirmDeleteCommand = new RelayCommand(_ => ConfirmDelete());
            CancelDeleteCommand = new RelayCommand(_ => CancelDelete());

            var currentRoleId = _sessionManager.CurrentUser?.Employee?.RoleId;
            HasAccess = currentRoleId.HasValue && AllowedRoleIds.Contains(currentRoleId.Value);
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

        public ObservableCollection<ContractRow> Contracts { get; }
        public ICollectionView ContractsView { get; }
        public ObservableCollection<IdNamePair> Employees { get; }
        public List<IdNamePair> RoleOptions { get; }
        public List<string> ContractTypeOptionsList { get; }
        public List<string> StatusOptionsList { get; }

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

        private IdNamePair? _formSelectedRole;
        public IdNamePair? FormSelectedRole
        {
            get => _formSelectedRole;
            set => SetProperty(ref _formSelectedRole, value);
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

        private ContractRow? _pendingDelete;
        public ContractRow? PendingDelete
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
            foreach (var employee in _employeeRepository.GetAll())
                Employees.Add(new IdNamePair(employee.EmployeeId, employee.FullName));
        }

        private void LoadContracts()
        {
            Contracts.Clear();
            foreach (var contract in _contractRepository.GetAll())
                Contracts.Add(ToRow(contract));
        }

        private ContractRow ToRow(Contract contract)
        {
            var employeeName = Employees.FirstOrDefault(e => e.Id == contract.EmployeeId)?.Name
                ?? $"Employee #{contract.EmployeeId}";
            var roleName = Roles.FirstOrDefault(r => r.Id == contract.RoleId)?.Name ?? "—";

            return new ContractRow
            {
                Contract = contract,
                EmployeeName = employeeName,
                RoleName = roleName,
                ContractType = contract.ContractType,
                StartDateDisplay = contract.StartDate.ToString("MMM dd, yyyy"),
                EndDateDisplay = contract.EndDate.HasValue ? contract.EndDate.Value.ToString("MMM dd, yyyy") : "No end date",
                Status = contract.Status,
                BaseSalaryDisplay = contract.BaseSalary.ToString("C0")
            };
        }

        private bool FilterContract(object obj)
        {
            if (obj is not ContractRow row)
                return false;

            if (string.IsNullOrWhiteSpace(SearchText))
                return true;

            var term = SearchText.Trim();

            return row.EmployeeName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || row.ContractType.Contains(term, StringComparison.OrdinalIgnoreCase);
        }

        // Add / Edit

        private void OpenAddForm()
        {
            _formContractId = 0;
            FormTitle = "Add Contract";
            FormSelectedEmployee = Employees.FirstOrDefault();
            FormSelectedRole = RoleOptions.FirstOrDefault(r => r.Id == 3); // default: Employee
            FormContractType = ContractTypeOptions[0];
            FormStartDate = DateTime.Today;
            FormEndDate = null;
            FormStatus = "Active";
            FormBaseSalary = string.Empty;
            FormErrorMessage = null;
            IsFormOpen = true;
        }

        private void OpenEditForm(ContractRow? row)
        {
            if (row?.Contract == null)
                return;

            var contract = row.Contract;

            _formContractId = contract.ContractId;
            FormTitle = "Edit Contract";
            FormSelectedEmployee = Employees.FirstOrDefault(e => e.Id == contract.EmployeeId);
            FormSelectedRole = RoleOptions.FirstOrDefault(r => r.Id == contract.RoleId);
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
            if (FormSelectedEmployee == null || FormSelectedRole == null)
            {
                FormErrorMessage = "Employee and role are required.";
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

            var contract = new Contract
            {
                ContractId = _formContractId,
                EmployeeId = FormSelectedEmployee.Id,
                RoleId = FormSelectedRole.Id,
                ContractType = FormContractType.Trim(),
                StartDate = FormStartDate.Value,
                EndDate = FormEndDate,
                Status = FormStatus,
                BaseSalary = baseSalary
            };

            if (_formContractId == 0)
            {
                var newId = _contractRepository.Insert(contract);
                contract.ContractId = newId;
                Contracts.Add(ToRow(contract));
            }
            else
            {
                _contractRepository.Update(contract);
                var existing = Contracts.FirstOrDefault(c => c.Contract.ContractId == _formContractId);
                if (existing != null)
                {
                    var index = Contracts.IndexOf(existing);
                    Contracts[index] = ToRow(contract);
                }
            }

            IsFormOpen = false;
        }

        //Delete 

        private void RequestDelete(ContractRow? row)
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

            _contractRepository.Delete(PendingDelete.Contract.ContractId);
            Contracts.Remove(PendingDelete);

            PendingDelete = null;
            IsDeleteConfirmOpen = false;
        }

        private void CancelDelete()
        {
            PendingDelete = null;
            IsDeleteConfirmOpen = false;
        }
    }

    public class ContractRow
    {
        public Contract Contract { get; set; } = null!;
        public string EmployeeName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string ContractType { get; set; } = string.Empty;
        public string StartDateDisplay { get; set; } = string.Empty;
        public string EndDateDisplay { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string BaseSalaryDisplay { get; set; } = string.Empty;
    }
}
