using HRManagement.Models;
using HRManagement.Repositories.Interfaces;
using HRManagement.Services.Interfaces;
using HRManagement.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HRManagement.Services
{
    public class ManageProfilesService : IManageProfilesService
    {
        // Admin, Manager
        private static readonly HashSet<int> AllowedRoleIds = new() { 1, 2 };
        private const int AdminRoleId = 1;

        private static readonly List<IdNamePair> Roles = new()
        {
            new IdNamePair(1, "Admin"),
            new IdNamePair(2, "Manager"),
            new IdNamePair(3, "Employee")
        };

        private readonly IEmployeeRepository _employeeRepository;
        private readonly IDepartmentRepository _departmentRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly SessionManager _sessionManager;
        private readonly ILogService _logService;

        public ManageProfilesService(
            IEmployeeRepository employeeRepository,
            IDepartmentRepository departmentRepository,
            IAccountRepository accountRepository,
            SessionManager sessionManager,
            ILogService logService)
        {
            _employeeRepository = employeeRepository;
            _departmentRepository = departmentRepository;
            _accountRepository = accountRepository;
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

        public List<IdNamePair> GetRoleOptions() => Roles;

        public List<IdNamePair> GetDepartments()
        {
            var departments = _departmentRepository.GetAll();

            // Managers only ever act within their own department, so they
            // don't get a department picker at all - just their one.
            if (!CurrentUserIsAdmin())
            {
                var ownDepartmentId = CurrentUserDepartmentId();
                departments = departments.Where(d => d.DepartmentId == ownDepartmentId).ToList();
            }

            return departments
                .Select(d => new IdNamePair(d.DepartmentId, d.DepartmentName))
                .ToList();
        }

        public List<EmployeeProfileItemModel> GetEmployees()
        {
            var departments = _departmentRepository.GetAll();
            var employees = _employeeRepository.GetAll();

            // Managers only see employees in their own department.
            if (!CurrentUserIsAdmin())
            {
                var ownDepartmentId = CurrentUserDepartmentId();
                employees = employees.Where(e => e.DepartmentId == ownDepartmentId).ToList();
            }

            return employees
                .Select(e => ToItem(e, departments))
                .ToList();
        }

        public EmployeeProfileItemModel AddEmployee(EmployeeProfileInput input)
        {
            ValidateInput(input);
            EnforceDepartmentScope(input);

            var employee = new Employee
            {
                FullName = input.FullName.Trim(),
                Email = input.Email.Trim(),
                Phone = string.IsNullOrWhiteSpace(input.Phone) ? null : input.Phone.Trim(),
                DateOfBirth = input.DateOfBirth ?? DateTime.Today.AddYears(-25),
                HireDate = input.HireDate ?? DateTime.Today,
                Status = input.Status,
                RoleId = input.RoleId,
                DepartmentId = input.DepartmentId
            };

            var newId = _employeeRepository.Insert(employee);
            employee.EmployeeId = newId;

            _logService.WriteLog(CurrentAccountId(), $"Added employee: {employee.FullName}");

            if (input.CreateAccount)
            {
                CreateAccountForEmployee(employee, input);
            }

            var departments = _departmentRepository.GetAll();
            return ToItem(employee, departments);
        }

        public EmployeeProfileItemModel UpdateEmployee(EmployeeProfileInput input)
        {
            ValidateInput(input);
            EnforceDepartmentScope(input);

            if (input.EmployeeId <= 0)
                throw new ArgumentOutOfRangeException(nameof(input.EmployeeId), "Employee ID must be greater than 0.");

            var oldEmployee = _employeeRepository.GetById(input.EmployeeId);

            if (oldEmployee == null)
                throw new InvalidOperationException($"Employee {input.EmployeeId} was not found.");

            var employee = new Employee
            {
                EmployeeId = input.EmployeeId,
                FullName = input.FullName.Trim(),
                Email = input.Email.Trim(),
                Phone = string.IsNullOrWhiteSpace(input.Phone) ? null : input.Phone.Trim(),
                DateOfBirth = input.DateOfBirth ?? DateTime.Today.AddYears(-25),
                HireDate = input.HireDate ?? DateTime.Today,
                Status = input.Status,
                RoleId = input.RoleId,
                DepartmentId = input.DepartmentId
            };

            _employeeRepository.Update(employee);

            LogChanges(oldEmployee, employee);

            var existingAccount = _accountRepository.GetByEmployeeId(employee.EmployeeId);

            // Allow provisioning an account later for an employee who didn't
            // get one when they were first added.
            if (input.CreateAccount && existingAccount == null)
            {
                CreateAccountForEmployee(employee, input);
            }
            // Admin/manager-initiated password reset for an employee who
            // already has an account. Independent of the CreateAccount
            // branch above, which only provisions brand new accounts.
            else if (input.ResetPassword && existingAccount != null)
            {
                ResetEmployeePassword(employee, existingAccount, input.NewPassword);
            }

            var departments = _departmentRepository.GetAll();
            return ToItem(employee, departments);
        }

        public void DeleteEmployee(int employeeId)
        {
            if (employeeId <= 0)
                throw new ArgumentOutOfRangeException(nameof(employeeId), "Employee ID must be greater than 0.");

            var employee = _employeeRepository.GetById(employeeId);

            if (employee == null)
                throw new InvalidOperationException($"Employee {employeeId} was not found.");

            if (!CurrentUserIsAdmin() && employee.DepartmentId != CurrentUserDepartmentId())
                throw new InvalidOperationException("You can only manage employees in your own department.");

            _employeeRepository.DeleteWithRelatedData(employeeId);

            _logService.WriteLog(CurrentAccountId(), $"Deleted employee: {employee.FullName}");
        }

        // =========================================================
        // Helpers
        // =========================================================

        private void CreateAccountForEmployee(Employee employee, EmployeeProfileInput input)
        {
            if (string.IsNullOrWhiteSpace(input.AccountUsername))
                throw new ArgumentException("Username is required to create an account.", nameof(input.AccountUsername));

            if (string.IsNullOrWhiteSpace(input.AccountPassword))
                throw new ArgumentException("Password is required to create an account.", nameof(input.AccountPassword));

            if (!ValidationRules.IsValidPassword(input.AccountPassword))
                throw new ArgumentException(ValidationRules.PasswordErrorMessage, nameof(input.AccountPassword));

            var username = input.AccountUsername.Trim();

            if (_accountRepository.UsernameExists(username))
                throw new InvalidOperationException($"Username '{username}' is already taken.");

            var account = new Account
            {
                Username = username,
                RoleId = employee.RoleId,
                EmployeeId = employee.EmployeeId
            };

            _accountRepository.Insert(account, input.AccountPassword);

            _logService.WriteLog(CurrentAccountId(), $"Created login account for employee: {employee.FullName}");
        }

        private void ResetEmployeePassword(Employee employee, Account account, string? newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword))
                throw new ArgumentException("New password is required to reset the password.", nameof(newPassword));

            if (!ValidationRules.IsValidPassword(newPassword))
                throw new ArgumentException(ValidationRules.PasswordErrorMessage, nameof(newPassword));

            var success = _accountRepository.SetPassword(account.AccountId, newPassword);

            if (!success)
                throw new InvalidOperationException("Failed to reset the employee's password.");

            _logService.WriteLog(CurrentAccountId(), $"Reset password for employee: {employee.FullName}");
        }

        private void LogChanges(Employee oldEmployee, Employee employee)
        {
            var changes = new List<string>();

            if (oldEmployee.FullName != employee.FullName) changes.Add("Full Name");
            if (oldEmployee.Email != employee.Email) changes.Add("Email");
            if ((oldEmployee.Phone ?? "") != (employee.Phone ?? "")) changes.Add("Phone");
            if (oldEmployee.DepartmentId != employee.DepartmentId) changes.Add("Department");
            if (oldEmployee.RoleId != employee.RoleId) changes.Add("Role");
            if (oldEmployee.Status != employee.Status) changes.Add("Status");
            if (oldEmployee.HireDate != employee.HireDate) changes.Add("Hire Date");
            if (oldEmployee.DateOfBirth != employee.DateOfBirth) changes.Add("Date Of Birth");

            var message = changes.Count > 0
                ? $"Updated employee {employee.FullName}: {string.Join(", ", changes)}"
                : $"Updated employee {employee.FullName}";

            _logService.WriteLog(CurrentAccountId(), message);
        }

        private int CurrentAccountId() =>
            _sessionManager.CurrentUser!.Employee.EmployeeId;

        private EmployeeProfileItemModel ToItem(Employee employee, List<Department> departments)
        {
            var departmentName = departments.FirstOrDefault(d => d.DepartmentId == employee.DepartmentId)?.DepartmentName
                ?? $"Department #{employee.DepartmentId}";
            var roleName = Roles.FirstOrDefault(r => r.Id == employee.RoleId)?.Name ?? "Employee";

            return new EmployeeProfileItemModel
            {
                Employee = employee,
                FullName = employee.FullName,
                Email = employee.Email,
                Phone = string.IsNullOrWhiteSpace(employee.Phone) ? "—" : employee.Phone,
                DepartmentName = departmentName,
                RoleName = roleName,
                Status = employee.Status,
                HireDateDisplay = employee.HireDate.ToString("MMM dd, yyyy"),
                HasAccount = _accountRepository.GetByEmployeeId(employee.EmployeeId) != null
            };
        }

        /// <summary>
        /// Managers may only add/edit employees within their own department.
        /// For an Add, the department is forced to the manager's own
        /// department regardless of what was submitted. For an Edit, moving
        /// an employee to a different department (or editing someone
        /// outside it) is rejected.
        /// </summary>
        private void EnforceDepartmentScope(EmployeeProfileInput input)
        {
            if (CurrentUserIsAdmin())
                return;

            var ownDepartmentId = CurrentUserDepartmentId();

            if (input.EmployeeId == 0)
            {
                input.DepartmentId = ownDepartmentId ?? 0;
                return;
            }

            var existing = _employeeRepository.GetById(input.EmployeeId);

            if (existing != null && existing.DepartmentId != ownDepartmentId)
                throw new InvalidOperationException("You can only manage employees in your own department.");

            if (input.DepartmentId != ownDepartmentId)
                throw new InvalidOperationException("You can only assign employees to your own department.");
        }

        private static void ValidateInput(EmployeeProfileInput input)
        {
            if (string.IsNullOrWhiteSpace(input.FullName) || string.IsNullOrWhiteSpace(input.Email))
                throw new ArgumentException("Full name and email are required.");

            if (input.RoleId <= 0 || input.DepartmentId <= 0)
                throw new ArgumentException("Role and department are required.");

            if (!ValidationRules.IsValidEmail(input.Email))
                throw new ArgumentException(ValidationRules.EmailErrorMessage, nameof(input.Email));

            if (!ValidationRules.IsValidPhone(input.Phone))
                throw new ArgumentException(ValidationRules.PhoneErrorMessage, nameof(input.Phone));

            if (!ValidationRules.IsValidHireDate(input.HireDate))
                throw new ArgumentException(ValidationRules.HireDateErrorMessage, nameof(input.HireDate));

            if (!ValidationRules.IsValidBirthDate(input.DateOfBirth))
                throw new ArgumentException(ValidationRules.BirthDateErrorMessage, nameof(input.DateOfBirth));
        }
    }
}
