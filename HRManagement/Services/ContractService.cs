using HRManagement.Models;
using HRManagement.Repositories.Interfaces;
using HRManagement.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HRManagement.Services
{
    public class ContractService : IContractService
    {
        // Admin, Manager
        private static readonly HashSet<int> AllowedRoleIds = new() { 1, 2 };
        private const int AdminRoleId = 1;

        // TODO: replace with your real employer/company-profile and
        // employee-address sources if ones exist (e.g. an
        // ICompanyProfileRepository, or an Address field on Employee) —
        // filled in here as constants so GetContract has somewhere to
        // read them from.
        private const string EmployerName = "Your Company Name";
        private const string EmployerAddress = "123 Business Ave, Suite 100";
        private const int NoticePeriodDays = 30;

        private readonly IContractRepository _contractRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IDepartmentRepository _departmentRepository;
        private readonly IPositionRepository _positionRepository;
        private readonly SessionManager _sessionManager;
        private readonly ILogService _logService;

        public ContractService(
            IContractRepository contractRepository,
            IEmployeeRepository employeeRepository,
            IDepartmentRepository departmentRepository,
            IPositionRepository positionRepository,
            SessionManager sessionManager,
            ILogService logService)
        {
            _contractRepository = contractRepository;
            _employeeRepository = employeeRepository;
            _departmentRepository = departmentRepository;
            _positionRepository = positionRepository;
            _sessionManager = sessionManager;
            _logService = logService;
        }

        public bool CurrentUserHasAccess()
        {
            var currentRoleId = _sessionManager.CurrentUser?.Employee?.RoleId;
            return currentRoleId.HasValue && AllowedRoleIds.Contains(currentRoleId.Value);
        }

        public bool CurrentUserIsAdmin()
        {
            return _sessionManager.CurrentUser?.Employee?.RoleId == AdminRoleId;
        }

        private int? CurrentUserDepartmentId()
        {
            return _sessionManager.CurrentUser?.Employee?.DepartmentId;
        }

        public List<IdNamePair> GetPositionOptions() =>
            _positionRepository.GetAll()
                .Select(p => new IdNamePair(p.PositionId, p.PositionName))
                .ToList();

        public List<IdNamePair> GetEmployees()
        {
            var employees = _employeeRepository.GetAll();

            // Managers can only create/assign contracts for employees in
            // their own department.
            if (!CurrentUserIsAdmin())
            {
                var ownDepartmentId = CurrentUserDepartmentId();
                employees = employees.Where(e => e.DepartmentId == ownDepartmentId).ToList();
            }

            return employees
                .Select(e => new IdNamePair(e.EmployeeId, e.FullName))
                .ToList();
        }

        public List<ContractItemModel> GetContracts()
        {
            var employees = _employeeRepository.GetAll();
            var contracts = _contractRepository.GetAll();
            var positions = _positionRepository.GetAll();

            // Managers only see contracts belonging to employees in their
            // own department.
            if (!CurrentUserIsAdmin())
            {
                var ownDepartmentId = CurrentUserDepartmentId();
                var ownDepartmentEmployeeIds = employees
                    .Where(e => e.DepartmentId == ownDepartmentId)
                    .Select(e => e.EmployeeId)
                    .ToHashSet();

                contracts = contracts.Where(c => ownDepartmentEmployeeIds.Contains(c.EmployeeId)).ToList();
            }

            return contracts
                .Select(c => ToItem(c, employees, positions))
                .ToList();
        }

        public ContractItemModel AddContract(ContractInput input)
        {
            ValidateInput(input);
            EnforceDepartmentScope(input);

            var contract = new Contract
            {
                EmployeeId = input.EmployeeId,
                PositionId = input.PositionId,
                ContractType = input.ContractType.Trim(),
                StartDate = input.StartDate,
                EndDate = input.EndDate,
                Status = input.Status,
                BaseSalary = input.BaseSalary
            };

            var newId = _contractRepository.Insert(contract);
            contract.ContractId = newId;

            var employees = _employeeRepository.GetAll();
            var positions = _positionRepository.GetAll();
            var employeeName = employees.FirstOrDefault(e => e.EmployeeId == contract.EmployeeId)?.FullName ?? "Unknown";

            _logService.WriteLog(CurrentAccountId(), $"Added contract for {employeeName}");

            return ToItem(contract, employees, positions);
        }

        public ContractItemModel UpdateContract(ContractInput input)
        {
            ValidateInput(input);
            EnforceDepartmentScope(input);

            if (input.ContractId <= 0)
                throw new ArgumentOutOfRangeException(nameof(input.ContractId), "Contract ID must be greater than 0.");

            var oldContract = _contractRepository.GetCurrentByEmployeeId(input.ContractId);

            var contract = new Contract
            {
                ContractId = input.ContractId,
                EmployeeId = input.EmployeeId,
                PositionId = input.PositionId,
                ContractType = input.ContractType.Trim(),
                StartDate = input.StartDate,
                EndDate = input.EndDate,
                Status = input.Status,
                BaseSalary = input.BaseSalary
            };

            _contractRepository.Update(contract);

            var employees = _employeeRepository.GetAll();
            var positions = _positionRepository.GetAll();
            var employeeName = employees.FirstOrDefault(e => e.EmployeeId == contract.EmployeeId)?.FullName ?? "Unknown";

            if (oldContract != null)
                LogChanges(oldContract, contract, employeeName);
            else
                _logService.WriteLog(CurrentAccountId(), $"Updated contract of {employeeName}");

            return ToItem(contract, employees, positions);
        }

        public ContractDetailModel? GetContract(int employeeId)
        {
            var employee = _employeeRepository.GetById(employeeId);
            if (employee == null)
                return null;

            var departments = _departmentRepository.GetAll();
            var departmentName = departments.FirstOrDefault(d => d.DepartmentId == employee.DepartmentId)?.DepartmentName
                ?? $"Department #{employee.DepartmentId}";

            var contract = _contractRepository.GetCurrentByEmployeeId(employeeId);
            var positions = _positionRepository.GetAll();

            var detail = new ContractDetailModel
            {
                Employee = employee,
                DepartmentName = departmentName,
                HasContract = contract != null,
                Contract = contract,
                PositionName = contract != null
                    ? positions.FirstOrDefault(p => p.PositionId == contract.PositionId)?.PositionName ?? "—"
                    : string.Empty,
                EmployerName = EmployerName,
                EmployerAddress = EmployerAddress,
                EmployeeAddress = employee.Address!,
                NoticePeriodDays = $"{NoticePeriodDays} days"
            };

            return detail;
        }

        public void DeleteContract(int contractId)
        {
            if (contractId <= 0)
                throw new ArgumentOutOfRangeException(nameof(contractId), "Contract ID must be greater than 0.");

            if (!CurrentUserIsAdmin())
            {
                var contract = _contractRepository.GetAll().FirstOrDefault(c => c.ContractId == contractId);
                var employee = contract != null ? _employeeRepository.GetById(contract.EmployeeId) : null;

                if (employee == null || employee.DepartmentId != CurrentUserDepartmentId())
                    throw new InvalidOperationException("You can only manage contracts for employees in your own department.");
            }

            _contractRepository.Delete(contractId);

            _logService.WriteLog(CurrentAccountId(), $"Deleted contract (ID: {contractId})");
        }

        // =========================================================
        // Helpers
        // =========================================================

        private void LogChanges(Contract oldContract, Contract contract, string employeeName)
        {
            var changes = new List<string>();

            if (oldContract.EmployeeId != contract.EmployeeId) changes.Add("Employee");
            if (oldContract.PositionId != contract.PositionId) changes.Add("Position");
            if (oldContract.ContractType != contract.ContractType) changes.Add("Contract Type");
            if (oldContract.StartDate != contract.StartDate) changes.Add("Start Date");
            if (oldContract.EndDate != contract.EndDate) changes.Add("End Date");
            if (oldContract.Status != contract.Status) changes.Add("Status");
            if (oldContract.BaseSalary != contract.BaseSalary) changes.Add("Base Salary");

            var message = changes.Count > 0
                ? $"Updated contract of {employeeName}: {string.Join(", ", changes)}"
                : $"Updated contract of {employeeName}";

            _logService.WriteLog(CurrentAccountId(), message);
        }

        private int CurrentAccountId() =>
            _sessionManager.CurrentUser!.Account.AccountId;

        private static ContractItemModel ToItem(Contract contract, List<Employee> employees, List<Position> positions)
        {
            var employeeName = employees.FirstOrDefault(e => e.EmployeeId == contract.EmployeeId)?.FullName
                ?? $"Employee #{contract.EmployeeId}";
            var positionName = positions.FirstOrDefault(p => p.PositionId == contract.PositionId)?.PositionName ?? "—";

            return new ContractItemModel
            {
                Contract = contract,
                EmployeeName = employeeName,
                PositionName = positionName,
                ContractType = contract.ContractType,
                StartDateDisplay = contract.StartDate.ToString("MMM dd, yyyy"),
                EndDateDisplay = contract.EndDate.HasValue ? contract.EndDate.Value.ToString("MMM dd, yyyy") : "No end date",
                Status = contract.Status,
                BaseSalaryDisplay = contract.BaseSalary.ToString("C0")
            };
        }

        /// <summary>
        /// Managers may only create/edit contracts for employees in their
        /// own department, and cannot move a contract to an employee
        /// outside it.
        /// </summary>
        private void EnforceDepartmentScope(ContractInput input)
        {
            if (CurrentUserIsAdmin())
                return;

            var ownDepartmentId = CurrentUserDepartmentId();
            var employee = _employeeRepository.GetById(input.EmployeeId);

            if (employee == null || employee.DepartmentId != ownDepartmentId)
                throw new InvalidOperationException("You can only manage contracts for employees in your own department.");
        }

        private static void ValidateInput(ContractInput input)
        {
            if (input.EmployeeId <= 0 || input.PositionId <= 0)
                throw new ArgumentException("Employee and position are required.");

            if (string.IsNullOrWhiteSpace(input.ContractType))
                throw new ArgumentException("Contract type is required.");

            if (input.EndDate.HasValue && input.EndDate.Value < input.StartDate)
                throw new ArgumentException("End date cannot be before the start date.");

            if (input.BaseSalary < 0)
                throw new ArgumentException("Base salary must be a valid non-negative number.");
        }
    }
}