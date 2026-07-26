using HRManagement.Repositories.Interfaces;
using HRManagement.Services;
using HRManagement.Utilities;
using System.Collections.Generic;

namespace HRManagement.ViewModels
{
    public class ContractViewModel : PageViewModel
    {
        private static readonly Dictionary<int, string> RoleNames = new()
        {
            [1] = "Admin",
            [2] = "Manager",
            [3] = "Employee"
        };

        private readonly IContractRepository _contractRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IDepartmentRepository _departmentRepository;
        private readonly SessionManager _sessionManager;

        public override string Title => "Contract";

        public ContractViewModel(
            IContractRepository contractRepository,
            IEmployeeRepository employeeRepository,
            IDepartmentRepository departmentRepository,
            SessionManager sessionManager)
        {
            _contractRepository = contractRepository;
            _employeeRepository = employeeRepository;
            _departmentRepository = departmentRepository;
            _sessionManager = sessionManager;

            // Unconditionally load the contract for the logged-in user
            LoadContract();
        }

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

        // ===== Existing Properties =====
        private string _contractIdDisplay = string.Empty;
        public string ContractIdDisplay { get => _contractIdDisplay; set => SetProperty(ref _contractIdDisplay, value); }

        private string _contractType = string.Empty;
        public string ContractType { get => _contractType; set => SetProperty(ref _contractType, value); }

        private string _status = string.Empty;
        public string Status { get => _status; set => SetProperty(ref _status, value); }

        private string _roleDisplay = string.Empty;
        public string RoleDisplay { get => _roleDisplay; set => SetProperty(ref _roleDisplay, value); }

        private string _departmentDisplay = string.Empty;
        public string DepartmentDisplay { get => _departmentDisplay; set => SetProperty(ref _departmentDisplay, value); }

        private string _baseSalaryDisplay = string.Empty;
        public string BaseSalaryDisplay { get => _baseSalaryDisplay; set => SetProperty(ref _baseSalaryDisplay, value); }

        private string _startDateDisplay = string.Empty;
        public string StartDateDisplay { get => _startDateDisplay; set => SetProperty(ref _startDateDisplay, value); }

        private string _endDateDisplay = string.Empty;
        public string EndDateDisplay { get => _endDateDisplay; set => SetProperty(ref _endDateDisplay, value); }

        private string _employeeNameDisplay = string.Empty;
        public string EmployeeNameDisplay { get => _employeeNameDisplay; set => SetProperty(ref _employeeNameDisplay, value); }

        // ===== NEW PROPERTIES =====

        private string _employerNameDisplay = string.Empty;
        public string EmployerNameDisplay { get => _employerNameDisplay; set => SetProperty(ref _employerNameDisplay, value); }

        private string _employerAddressDisplay = string.Empty;
        public string EmployerAddressDisplay { get => _employerAddressDisplay; set => SetProperty(ref _employerAddressDisplay, value); }

        private string _employeeAddressDisplay = string.Empty;
        public string EmployeeAddressDisplay { get => _employeeAddressDisplay; set => SetProperty(ref _employeeAddressDisplay, value); }

        private string _noticePeriodDaysDisplay = string.Empty;
        public string NoticePeriodDaysDisplay { get => _noticePeriodDaysDisplay; set => SetProperty(ref _noticePeriodDaysDisplay, value); }

        // ===== Loading =====

        private void LoadContract()
        {
            var employeeId = _sessionManager.CurrentUser?.Employee?.EmployeeId;
            if (employeeId == null) return;

            var employee = _employeeRepository.GetById(employeeId.Value);
            if (employee == null) return;

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

            EmployerNameDisplay = "My Company Inc.";
            EmployerAddressDisplay = "123 Business Rd, Suite 400, Tech City, ST 12345";

            EmployeeAddressDisplay = "987 Residential Ave, Apt 2B, Home City, ST 54321";
            NoticePeriodDaysDisplay = "14";
        }
    }
}