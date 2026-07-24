using HRManagement.Models;
using HRManagement.Repositories;
using HRManagement.Repositories.Interfaces;
using HRManagement.Services;
using HRManagement.Utilities;
using System.Collections.Generic;

namespace HRManagement.ViewModels
{
    /// <summary>
    /// "Contract" page - read-only view of the signed-in user's own work contract.
    /// Employee and Manager only (per the app's role model, Admin manages
    /// everyone's contracts through the separate "Manage Contracts" page instead).
    /// </summary>
    public class ContractViewModel : PageViewModel
    {
        // Mirrors the Role table (Role_ID 1=Admin, 2=Manager, 3=Employee).
        private static readonly Dictionary<int, string> RoleNames = new()
        {
            [1] = "Admin",
            [2] = "Manager",
            [3] = "Employee"
        };

        private static readonly HashSet<int> AllowedRoleIds = [2, 3]; // Manager, Employee

        private readonly IContractRepository _contractRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly DepartmentRepository _departmentRepository;
        private readonly SessionManager _sessionManager;

        public override string Title => "Contract";

        public ContractViewModel(
            IContractRepository contractRepository,
            IEmployeeRepository employeeRepository,
            DepartmentRepository departmentRepository,
            SessionManager sessionManager)
        {
            _contractRepository = contractRepository;
            _employeeRepository = employeeRepository;
            _departmentRepository = departmentRepository;
            _sessionManager = sessionManager;

            var currentRoleId = _sessionManager.CurrentUser?.Employee?.RoleId ?? 0;
            HasAccess = AllowedRoleIds.Contains(currentRoleId);
            HasNoAccess = !HasAccess;

            if (HasAccess)
                LoadContract();
        }

        // ===== Permissions =====

        public bool HasAccess { get; }
        public bool HasNoAccess { get; }

        // ===== Contract details =====

        private bool _hasContract;
        public bool HasContract
        {
            get => _hasContract;
            set
            {
                if (SetProperty(ref _hasContract, value))
                    OnPropertyChanged(nameof(HasNoContract));
            }
        }

        public bool HasNoContract => !HasContract;

        private string _contractIdDisplay = string.Empty;
        public string ContractIdDisplay
        {
            get => _contractIdDisplay;
            set => SetProperty(ref _contractIdDisplay, value);
        }

        private string _contractType = string.Empty;
        public string ContractType
        {
            get => _contractType;
            set => SetProperty(ref _contractType, value);
        }

        private string _status = string.Empty;
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private string _roleDisplay = string.Empty;
        public string RoleDisplay
        {
            get => _roleDisplay;
            set => SetProperty(ref _roleDisplay, value);
        }

        private string _departmentDisplay = string.Empty;
        public string DepartmentDisplay
        {
            get => _departmentDisplay;
            set => SetProperty(ref _departmentDisplay, value);
        }

        private string _baseSalaryDisplay = string.Empty;
        public string BaseSalaryDisplay
        {
            get => _baseSalaryDisplay;
            set => SetProperty(ref _baseSalaryDisplay, value);
        }

        private string _startDateDisplay = string.Empty;
        public string StartDateDisplay
        {
            get => _startDateDisplay;
            set => SetProperty(ref _startDateDisplay, value);
        }

        private string _endDateDisplay = string.Empty;
        public string EndDateDisplay
        {
            get => _endDateDisplay;
            set => SetProperty(ref _endDateDisplay, value);
        }

        private string _employeeNameDisplay = string.Empty;
        public string EmployeeNameDisplay
        {
            get => _employeeNameDisplay;
            set => SetProperty(ref _employeeNameDisplay, value);
        }

        // ===== Loading =====

        private void LoadContract()
        {
            var employeeId = _sessionManager.CurrentUser?.Employee?.EmployeeId;
            if (employeeId == null)
                return;

            var employee = _employeeRepository.GetById(employeeId.Value);
            if (employee == null)
                return;

            EmployeeNameDisplay = employee.FullName;

            var department = _departmentRepository.GetById(employee.DepartmentId);
            DepartmentDisplay = department?.DepartmentName ?? $"Department #{employee.DepartmentId}";

            var contract = _contractRepository.GetCurrentByEmployeeId(employeeId.Value);
            if (contract == null)
            {
                HasContract = false;
                return;
            }

            HasContract = true;
            ContractIdDisplay = $"CTR-{contract.ContractId:0000}";
            ContractType = contract.ContractType;
            Status = contract.Status;
            RoleDisplay = RoleNames.TryGetValue(contract.RoleId, out var roleName) ? roleName : "—";
            BaseSalaryDisplay = contract.BaseSalary.ToString("C0");
            StartDateDisplay = contract.StartDate.ToString("MMM dd, yyyy");
            EndDateDisplay = contract.EndDate.HasValue ? contract.EndDate.Value.ToString("MMM dd, yyyy") : "No end date";
        }
    }
}
