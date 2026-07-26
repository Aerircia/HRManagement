using HRManagement.Models;
using HRManagement.Repositories;
using HRManagement.Repositories.Interfaces;
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

    public class ManageProfilesViewModel : PageViewModel
    {
      
        private static readonly List<IdNamePair> Roles = new()
        {
            new IdNamePair(1, "Admin"),
            new IdNamePair(2, "Manager"),
            new IdNamePair(3, "Employee")
        };

        public static readonly List<string> StatusOptions = new() { "Active", "Inactive", "On Leave" };

        private readonly IEmployeeRepository _employeeRepository;
        private readonly IDepartmentRepository _departmentRepository;
        private readonly SessionManager _sessionManager;

        private static readonly HashSet<int> AllowedRoleIds = new() { 1, 2 }; // Admin, Manager

        public override string Title => "Manage Profiles";

        public ManageProfilesViewModel(
            IEmployeeRepository employeeRepository,
            IDepartmentRepository departmentRepository,
            SessionManager sessionManager)
        {
            _employeeRepository = employeeRepository;
            _departmentRepository = departmentRepository;
            _sessionManager = sessionManager;

            Employees = new ObservableCollection<EmployeeRow>();
            Departments = new ObservableCollection<IdNamePair>();
            RoleOptions = Roles;
            StatusOptionsList = StatusOptions;

            EmployeesView = CollectionViewSource.GetDefaultView(Employees);
            EmployeesView.Filter = FilterEmployee;

            AddCommand = new RelayCommand(_ => OpenAddForm());
            EditCommand = new RelayCommand(param => OpenEditForm(param as EmployeeRow));
            DeleteCommand = new RelayCommand(param => RequestDelete(param as EmployeeRow));
            SaveCommand = new RelayCommand(_ => SaveForm());
            CancelCommand = new RelayCommand(_ => CloseForm());
            ConfirmDeleteCommand = new RelayCommand(_ => ConfirmDelete());
            CancelDeleteCommand = new RelayCommand(_ => CancelDelete());

            var currentRoleId = _sessionManager.CurrentUser?.Employee?.RoleId;
            HasAccess = currentRoleId.HasValue && AllowedRoleIds.Contains(currentRoleId.Value);

            if (HasAccess)
            {
                LoadDepartments();
                LoadEmployees();
            }
        }

        //Access control

        public bool HasAccess { get; }
        public bool HasNoAccess => !HasAccess;

        //List

        public ObservableCollection<EmployeeRow> Employees { get; }
        public ICollectionView EmployeesView { get; }
        public ObservableCollection<IdNamePair> Departments { get; }
        public List<IdNamePair> RoleOptions { get; }
        public List<string> StatusOptionsList { get; }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                    EmployeesView.Refresh();
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

        private string _formTitle = "Add Employee";
        public string FormTitle
        {
            get => _formTitle;
            set => SetProperty(ref _formTitle, value);
        }

        private int _formEmployeeId;

        private string _formFullName = string.Empty;
        public string FormFullName
        {
            get => _formFullName;
            set => SetProperty(ref _formFullName, value);
        }

        private string _formEmail = string.Empty;
        public string FormEmail
        {
            get => _formEmail;
            set => SetProperty(ref _formEmail, value);
        }

        private string _formPhone = string.Empty;
        public string FormPhone
        {
            get => _formPhone;
            set => SetProperty(ref _formPhone, value);
        }

        private DateTime? _formDateOfBirth = DateTime.Today.AddYears(-25);
        public DateTime? FormDateOfBirth
        {
            get => _formDateOfBirth;
            set => SetProperty(ref _formDateOfBirth, value);
        }

        private DateTime? _formHireDate = DateTime.Today;
        public DateTime? FormHireDate
        {
            get => _formHireDate;
            set => SetProperty(ref _formHireDate, value);
        }

        private string _formStatus = "Active";
        public string FormStatus
        {
            get => _formStatus;
            set => SetProperty(ref _formStatus, value);
        }

        private IdNamePair? _formSelectedRole;
        public IdNamePair? FormSelectedRole
        {
            get => _formSelectedRole;
            set => SetProperty(ref _formSelectedRole, value);
        }

        private IdNamePair? _formSelectedDepartment;
        public IdNamePair? FormSelectedDepartment
        {
            get => _formSelectedDepartment;
            set => SetProperty(ref _formSelectedDepartment, value);
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

        //Delete confirmation overlay 

        private bool _isDeleteConfirmOpen;
        public bool IsDeleteConfirmOpen
        {
            get => _isDeleteConfirmOpen;
            set => SetProperty(ref _isDeleteConfirmOpen, value);
        }

        private EmployeeRow? _pendingDelete;
        public EmployeeRow? PendingDelete
        {
            get => _pendingDelete;
            set => SetProperty(ref _pendingDelete, value);
        }

        public RelayCommand ConfirmDeleteCommand { get; }
        public RelayCommand CancelDeleteCommand { get; }

        //Loading

        private void LoadDepartments()
        {
            Departments.Clear();
            foreach (var department in _departmentRepository.GetAll())
                Departments.Add(new IdNamePair(department.DepartmentId, department.DepartmentName));
        }

        private void LoadEmployees()
        {
            Employees.Clear();
            foreach (var employee in _employeeRepository.GetAll())
                Employees.Add(ToRow(employee));
        }

        private EmployeeRow ToRow(Employee employee)
        {
            var departmentName = Departments.FirstOrDefault(d => d.Id == employee.DepartmentId)?.Name
                ?? $"Department #{employee.DepartmentId}";
            var roleName = Roles.FirstOrDefault(r => r.Id == employee.RoleId)?.Name ?? "Employee";

            return new EmployeeRow
            {
                Employee = employee,
                FullName = employee.FullName,
                Email = employee.Email,
                Phone = string.IsNullOrWhiteSpace(employee.Phone) ? "—" : employee.Phone,
                DepartmentName = departmentName,
                RoleName = roleName,
                Status = employee.Status,
                HireDateDisplay = employee.HireDate.ToString("MMM dd, yyyy")
            };
        }

        private bool FilterEmployee(object obj)
        {
            if (obj is not EmployeeRow row)
                return false;

            if (string.IsNullOrWhiteSpace(SearchText))
                return true;

            var term = SearchText.Trim();

            return row.FullName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || row.Email.Contains(term, StringComparison.OrdinalIgnoreCase);
        }

        //Add / Edit Employee

        private void OpenAddForm()
        {
            _formEmployeeId = 0;
            FormTitle = "Add Employee";
            FormFullName = string.Empty;
            FormEmail = string.Empty;
            FormPhone = string.Empty;
            FormDateOfBirth = DateTime.Today.AddYears(-25);
            FormHireDate = DateTime.Today;
            FormStatus = "Active";
            FormSelectedRole = RoleOptions.FirstOrDefault(r => r.Id == 3); // default: Employee
            FormSelectedDepartment = Departments.FirstOrDefault();
            FormErrorMessage = null;
            IsFormOpen = true;
        }

        private void OpenEditForm(EmployeeRow? row)
        {
            if (row?.Employee == null)
                return;

            var employee = row.Employee;

            _formEmployeeId = employee.EmployeeId;
            FormTitle = "Edit Employee";
            FormFullName = employee.FullName;
            FormEmail = employee.Email;
            FormPhone = employee.Phone ?? string.Empty;
            FormDateOfBirth = employee.DateOfBirth;
            FormHireDate = employee.HireDate;
            FormStatus = employee.Status;
            FormSelectedRole = RoleOptions.FirstOrDefault(r => r.Id == employee.RoleId);
            FormSelectedDepartment = Departments.FirstOrDefault(d => d.Id == employee.DepartmentId);
            FormErrorMessage = null;
            IsFormOpen = true;
        }

        private void CloseForm()
        {
            IsFormOpen = false;
        }

        private void SaveForm()
        {
            if (string.IsNullOrWhiteSpace(FormFullName) || string.IsNullOrWhiteSpace(FormEmail))
            {
                FormErrorMessage = "Full name and email are required.";
                return;
            }

            if (FormSelectedRole == null || FormSelectedDepartment == null)
            {
                FormErrorMessage = "Role and department are required.";
                return;
            }

            var employee = new Employee
            {
                EmployeeId = _formEmployeeId,
                FullName = FormFullName.Trim(),
                Email = FormEmail.Trim(),
                Phone = string.IsNullOrWhiteSpace(FormPhone) ? null : FormPhone.Trim(),
                DateOfBirth = FormDateOfBirth ?? DateTime.Today.AddYears(-25),
                HireDate = FormHireDate ?? DateTime.Today,
                Status = FormStatus,
                RoleId = FormSelectedRole.Id,
                DepartmentId = FormSelectedDepartment.Id
            };

            if (_formEmployeeId == 0)
            {
                var newId = _employeeRepository.Insert(employee);
                employee.EmployeeId = newId;
                Employees.Add(ToRow(employee));
            }
            else
            {
                _employeeRepository.Update(employee);
                var existing = Employees.FirstOrDefault(e => e.Employee.EmployeeId == _formEmployeeId);
                if (existing != null)
                {
                    var index = Employees.IndexOf(existing);
                    Employees[index] = ToRow(employee);
                }
            }

            IsFormOpen = false;
        }

        //Delete Employee

        private void RequestDelete(EmployeeRow? row)
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

            _employeeRepository.Delete(PendingDelete.Employee.EmployeeId);
            Employees.Remove(PendingDelete);

            PendingDelete = null;
            IsDeleteConfirmOpen = false;
        }

        private void CancelDelete()
        {
            PendingDelete = null;
            IsDeleteConfirmOpen = false;
        }
    }


    public class IdNamePair
    {
        public IdNamePair(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; }
        public string Name { get; }
    }

    public class EmployeeRow
    {
        public Employee Employee { get; set; } = null!;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string HireDateDisplay { get; set; } = string.Empty;
    }
}
