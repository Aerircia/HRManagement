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
    public class ManageProfilesViewModel : PageViewModel
    {
        private readonly IManageProfilesService _manageProfilesService;

        public override string Title => "Manage Profiles";

        public ManageProfilesViewModel(IManageProfilesService manageProfilesService)
        {
            _manageProfilesService = manageProfilesService;

            Employees = new ObservableCollection<EmployeeProfileItemModel>();
            Departments = new ObservableCollection<IdNamePair>();
            RoleOptions = _manageProfilesService.GetRoleOptions();
            StatusOptionsList = StatusOptions;

            DepartmentFilterOptions = new ObservableCollection<IdNamePair>();
            RoleFilterOptions = new ObservableCollection<IdNamePair> { AllRolesOption };
            foreach (var role in RoleOptions)
                RoleFilterOptions.Add(role);

            SortOptions = new List<string> { "Name (A-Z)", "Name (Z-A)", "Hire Date (Newest)", "Hire Date (Oldest)" };
            _selectedSort = SortOptions[0];

            EmployeesView = CollectionViewSource.GetDefaultView(Employees);
            EmployeesView.Filter = FilterEmployee;
            ApplySort();

            AddCommand = new RelayCommand(_ => OpenAddForm());
            EditCommand = new RelayCommand(param => OpenEditForm(param as EmployeeProfileItemModel));
            DeleteCommand = new RelayCommand(param => RequestDelete(param as EmployeeProfileItemModel));
            SaveCommand = new RelayCommand(_ => SaveForm());
            CancelCommand = new RelayCommand(_ => CloseForm());
            ConfirmDeleteCommand = new RelayCommand(_ => ConfirmDelete());
            CancelDeleteCommand = new RelayCommand(_ => CancelDelete());

            HasAccess = _manageProfilesService.CurrentUserHasAccess();
            IsAdmin = _manageProfilesService.CurrentUserIsAdmin();

            if (HasAccess)
            {
                LoadDepartments();
                LoadEmployees();
            }
        }

        public static readonly List<string> StatusOptions = new() { "Active", "Inactive", "On Leave" };

        private static readonly IdNamePair AllDepartmentsOption = new(0, "All Departments");
        private static readonly IdNamePair AllRolesOption = new(0, "All Roles");

        //Access control

        public bool HasAccess { get; }
        public bool HasNoAccess => !HasAccess;

        // Admin sees everyone and gets Department/Role filters; Manager is
        // scoped to their own department and doesn't get those filters.
        public bool IsAdmin { get; }

        //List

        public ObservableCollection<EmployeeProfileItemModel> Employees { get; }
        public ICollectionView EmployeesView { get; }
        public ObservableCollection<IdNamePair> Departments { get; }
        public List<IdNamePair> RoleOptions { get; }
        public List<string> StatusOptionsList { get; }

        //Summary stat cards (Total / Active / On Leave / Inactive)

        private int _totalEmployeesCount;
        public int TotalEmployeesCount
        {
            get => _totalEmployeesCount;
            private set => SetProperty(ref _totalEmployeesCount, value);
        }

        private int _activeEmployeesCount;
        public int ActiveEmployeesCount
        {
            get => _activeEmployeesCount;
            private set => SetProperty(ref _activeEmployeesCount, value);
        }

        private int _onLeaveEmployeesCount;
        public int OnLeaveEmployeesCount
        {
            get => _onLeaveEmployeesCount;
            private set => SetProperty(ref _onLeaveEmployeesCount, value);
        }

        private int _inactiveEmployeesCount;
        public int InactiveEmployeesCount
        {
            get => _inactiveEmployeesCount;
            private set => SetProperty(ref _inactiveEmployeesCount, value);
        }

        // Filter dropdown sources (Department/Role) - separate from the
        // Add/Edit form's Departments/RoleOptions since these need an
        // "All" entry the form lists shouldn't have.
        public ObservableCollection<IdNamePair> DepartmentFilterOptions { get; }
        public ObservableCollection<IdNamePair> RoleFilterOptions { get; }

        public List<string> SortOptions { get; }

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

        private IdNamePair _selectedDepartmentFilter;
        public IdNamePair SelectedDepartmentFilter
        {
            get => _selectedDepartmentFilter;
            set
            {
                if (SetProperty(ref _selectedDepartmentFilter, value))
                    EmployeesView.Refresh();
            }
        }

        private IdNamePair _selectedRoleFilter;
        public IdNamePair SelectedRoleFilter
        {
            get => _selectedRoleFilter;
            set
            {
                if (SetProperty(ref _selectedRoleFilter, value))
                    EmployeesView.Refresh();
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

        // Account provisioning (Add Employee form, and Edit form for an
        // existing employee who doesn't have a login account yet)

        private bool _isNewEmployee = true;

        private bool _formHasExistingAccount;
        public bool FormHasExistingAccount
        {
            get => _formHasExistingAccount;
            set
            {
                if (SetProperty(ref _formHasExistingAccount, value))
                    OnPropertyChanged(nameof(CanCreateAccount));
            }
        }

        public bool CanCreateAccount => _isNewEmployee || !FormHasExistingAccount;

        private bool _formCreateAccount = true;
        public bool FormCreateAccount
        {
            get => _formCreateAccount;
            set => SetProperty(ref _formCreateAccount, value);
        }

        private string _formAccountUsername = string.Empty;
        public string FormAccountUsername
        {
            get => _formAccountUsername;
            set => SetProperty(ref _formAccountUsername, value);
        }

        private string _formAccountPassword = string.Empty;
        public string FormAccountPassword
        {
            get => _formAccountPassword;
            set => SetProperty(ref _formAccountPassword, value);
        }

        // Admin/manager-initiated password reset (Edit form only, for an
        // employee who already has a login account). Independent of the
        // account-provisioning fields above, which only apply when the
        // employee doesn't have an account yet.

        public bool CanResetPassword => !_isNewEmployee && FormHasExistingAccount;

        private bool _formResetPassword;
        public bool FormResetPassword
        {
            get => _formResetPassword;
            set => SetProperty(ref _formResetPassword, value);
        }

        private string _formNewPassword = string.Empty;
        public string FormNewPassword
        {
            get => _formNewPassword;
            set => SetProperty(ref _formNewPassword, value);
        }

        private string _formConfirmNewPassword = string.Empty;
        public string FormConfirmNewPassword
        {
            get => _formConfirmNewPassword;
            set => SetProperty(ref _formConfirmNewPassword, value);
        }

        //Delete confirmation overlay 

        private bool _isDeleteConfirmOpen;
        public bool IsDeleteConfirmOpen
        {
            get => _isDeleteConfirmOpen;
            set => SetProperty(ref _isDeleteConfirmOpen, value);
        }

        private EmployeeProfileItemModel? _pendingDelete;
        public EmployeeProfileItemModel? PendingDelete
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
            foreach (var department in _manageProfilesService.GetDepartments())
                Departments.Add(department);

            DepartmentFilterOptions.Clear();
            DepartmentFilterOptions.Add(AllDepartmentsOption);
            foreach (var department in Departments)
                DepartmentFilterOptions.Add(department);

            SelectedDepartmentFilter ??= AllDepartmentsOption;
            SelectedRoleFilter ??= AllRolesOption;
        }

        private void LoadEmployees()
        {
            Employees.Clear();
            foreach (var employee in _manageProfilesService.GetEmployees())
                Employees.Add(employee);

            UpdateStats();
        }

        private void UpdateStats()
        {
            TotalEmployeesCount = Employees.Count;
            ActiveEmployeesCount = Employees.Count(e => string.Equals(e.Status, "Active", StringComparison.OrdinalIgnoreCase));
            OnLeaveEmployeesCount = Employees.Count(e => string.Equals(e.Status, "On Leave", StringComparison.OrdinalIgnoreCase));
            InactiveEmployeesCount = Employees.Count(e => string.Equals(e.Status, "Inactive", StringComparison.OrdinalIgnoreCase));
        }

        private bool FilterEmployee(object obj)
        {
            if (obj is not EmployeeProfileItemModel row)
                return false;

            if (SelectedDepartmentFilter != null
                && SelectedDepartmentFilter.Id != 0
                && row.Employee.DepartmentId != SelectedDepartmentFilter.Id)
            {
                return false;
            }

            if (SelectedRoleFilter != null
                && SelectedRoleFilter.Id != 0
                && row.Employee.RoleId != SelectedRoleFilter.Id)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(SearchText))
                return true;

            var term = SearchText.Trim();

            return row.FullName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || row.Email.Contains(term, StringComparison.OrdinalIgnoreCase);
        }

        private void ApplySort()
        {
            EmployeesView.SortDescriptions.Clear();

            switch (SelectedSort)
            {
                case "Name (Z-A)":
                    EmployeesView.SortDescriptions.Add(new SortDescription(nameof(EmployeeProfileItemModel.FullName), ListSortDirection.Descending));
                    break;

                case "Hire Date (Newest)":
                    EmployeesView.SortDescriptions.Add(new SortDescription($"{nameof(EmployeeProfileItemModel.Employee)}.{nameof(Models.Employee.HireDate)}", ListSortDirection.Descending));
                    break;

                case "Hire Date (Oldest)":
                    EmployeesView.SortDescriptions.Add(new SortDescription($"{nameof(EmployeeProfileItemModel.Employee)}.{nameof(Models.Employee.HireDate)}", ListSortDirection.Ascending));
                    break;

                default: // "Name (A-Z)"
                    EmployeesView.SortDescriptions.Add(new SortDescription(nameof(EmployeeProfileItemModel.FullName), ListSortDirection.Ascending));
                    break;
            }
        }

        //Add / Edit Employee

        private void OpenAddForm()
        {
            _formEmployeeId = 0;
            _isNewEmployee = true;
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

            FormHasExistingAccount = false;
            FormCreateAccount = true;
            FormAccountUsername = string.Empty;
            FormAccountPassword = string.Empty;
            OnPropertyChanged(nameof(CanCreateAccount));

            FormResetPassword = false;
            FormNewPassword = string.Empty;
            FormConfirmNewPassword = string.Empty;
            OnPropertyChanged(nameof(CanResetPassword));

            IsFormOpen = true;
        }

        private void OpenEditForm(EmployeeProfileItemModel? row)
        {
            if (row?.Employee == null)
                return;

            var employee = row.Employee;

            _formEmployeeId = employee.EmployeeId;
            _isNewEmployee = false;
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

            FormHasExistingAccount = row.HasAccount;
            FormCreateAccount = !row.HasAccount;
            FormAccountUsername = string.Empty;
            FormAccountPassword = string.Empty;
            OnPropertyChanged(nameof(CanCreateAccount));

            FormResetPassword = false;
            FormNewPassword = string.Empty;
            FormConfirmNewPassword = string.Empty;
            OnPropertyChanged(nameof(CanResetPassword));

            IsFormOpen = true;
        }

        private void CloseForm()
        {
            IsFormOpen = false;
        }

        /// <summary>
        /// Client-side validation for instant feedback. The service layer
        /// re-validates everything regardless - this is a convenience layer
        /// only, never the real guard.
        /// </summary>
        private bool TryValidateForm(out string? errorMessage)
        {
            if (string.IsNullOrWhiteSpace(FormFullName) || string.IsNullOrWhiteSpace(FormEmail))
            {
                errorMessage = "Full name and email are required.";
                return false;
            }

            if (!ValidationRules.IsValidEmail(FormEmail))
            {
                errorMessage = ValidationRules.EmailErrorMessage;
                return false;
            }

            if (!ValidationRules.IsValidPhone(FormPhone))
            {
                errorMessage = ValidationRules.PhoneErrorMessage;
                return false;
            }

            if (!ValidationRules.IsValidHireDate(FormHireDate))
            {
                errorMessage = ValidationRules.HireDateErrorMessage;
                return false;
            }

            if (!ValidationRules.IsValidBirthDate(FormDateOfBirth))
            {
                errorMessage = ValidationRules.BirthDateErrorMessage;
                return false;
            }

            if (FormSelectedRole == null || FormSelectedDepartment == null)
            {
                errorMessage = "Role and department are required.";
                return false;
            }

            if (CanCreateAccount && FormCreateAccount)
            {
                if (string.IsNullOrWhiteSpace(FormAccountUsername))
                {
                    errorMessage = "Username is required to create an account.";
                    return false;
                }

                if (!ValidationRules.IsValidPassword(FormAccountPassword))
                {
                    errorMessage = ValidationRules.PasswordErrorMessage;
                    return false;
                }
            }

            if (CanResetPassword && FormResetPassword)
            {
                if (!ValidationRules.IsValidPassword(FormNewPassword))
                {
                    errorMessage = ValidationRules.PasswordErrorMessage;
                    return false;
                }

                if (FormNewPassword != FormConfirmNewPassword)
                {
                    errorMessage = "New password and confirmation do not match.";
                    return false;
                }
            }

            errorMessage = null;
            return true;
        }

        private void SaveForm()
        {
            if (!TryValidateForm(out var validationError))
            {
                FormErrorMessage = validationError;
                return;
            }

            var input = new EmployeeProfileInput
            {
                EmployeeId = _formEmployeeId,
                FullName = FormFullName,
                Email = FormEmail,
                Phone = FormPhone,
                DateOfBirth = FormDateOfBirth,
                HireDate = FormHireDate,
                Status = FormStatus,
                RoleId = FormSelectedRole?.Id ?? 0,
                DepartmentId = FormSelectedDepartment?.Id ?? 0,
                CreateAccount = CanCreateAccount && FormCreateAccount,
                AccountUsername = FormAccountUsername,
                AccountPassword = FormAccountPassword,
                ResetPassword = CanResetPassword && FormResetPassword,
                NewPassword = FormNewPassword
            };

            try
            {
                if (_formEmployeeId == 0)
                {
                    var saved = _manageProfilesService.AddEmployee(input);
                    Employees.Add(saved);
                }
                else
                {
                    var saved = _manageProfilesService.UpdateEmployee(input);
                    var existing = Employees.FirstOrDefault(e => e.Employee.EmployeeId == _formEmployeeId);
                    if (existing != null)
                    {
                        var index = Employees.IndexOf(existing);
                        Employees[index] = saved;
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

        //Delete Employee

        private void RequestDelete(EmployeeProfileItemModel? row)
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
                _manageProfilesService.DeleteEmployee(PendingDelete.Employee.EmployeeId);
                Employees.Remove(PendingDelete);
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
