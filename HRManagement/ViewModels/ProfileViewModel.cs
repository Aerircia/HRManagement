using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using HRManagement.Models;
using HRManagement.Repositories;
using HRManagement.Services;
using HRManagement.Utilities;

namespace HRManagement.ViewModels
{

    public class ProfileViewModel : PageViewModel
    {

        private static readonly Dictionary<int, string> RoleNames = new()
        {
            [1] = "Admin",
            [2] = "Manager",
            [3] = "Employee"
        };

        private readonly EmployeeRepository _employeeRepository;
        private readonly DepartmentRepository _departmentRepository;
        private readonly ContractRepository _contractRepository;
        private readonly AttendanceRepository _attendanceRepository;
        private readonly EmployeeEvaluationRepository _evaluationRepository;
        private readonly PayrollRepository _payrollRepository;
        private readonly RequestFormRepository _requestFormRepository;
        private readonly SessionManager _sessionManager;

        private Employee _employee;
        private Contract? _currentContract;

        public override string Title => "My Profile";

        public ProfileViewModel(
            EmployeeRepository employeeRepository,
            DepartmentRepository departmentRepository,
            ContractRepository contractRepository,
            AttendanceRepository attendanceRepository,
            EmployeeEvaluationRepository evaluationRepository,
            PayrollRepository payrollRepository,
            RequestFormRepository requestFormRepository,
            SessionManager sessionManager)
        {
            _employeeRepository = employeeRepository;
            _departmentRepository = departmentRepository;
            _contractRepository = contractRepository;
            _attendanceRepository = attendanceRepository;
            _evaluationRepository = evaluationRepository;
            _payrollRepository = payrollRepository;
            _requestFormRepository = requestFormRepository;
            _sessionManager = sessionManager;

            EditProfileCommand = new RelayCommand(OnEditProfile);
            SaveProfileCommand = new RelayCommand(_ => SaveProfile());
            CancelEditCommand = new RelayCommand(_ => IsEditOpen = false);

            EmploymentDetails = new ObservableCollection<KeyValueItem>();

            LoadCurrentUser();
        }

        public RelayCommand EditProfileCommand { get; }
        public RelayCommand SaveProfileCommand { get; }
        public RelayCommand CancelEditCommand { get; }

        //Header

        public string SubHeading => "Your personal and employment information";

        //Personal info 

        private string _fullName;
        public string FullName
        {
            get => _fullName;
            set => SetProperty(ref _fullName, value);
        }

        private string _roleLabel;
        public string RoleLabel
        {
            get => _roleLabel;
            set => SetProperty(ref _roleLabel, value);
        }

        private string _avatarPath;
        public string AvatarPath
        {
            get => _avatarPath;
            set => SetProperty(ref _avatarPath, value);
        }

        private string _email;
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        private string _phone;
        public string Phone
        {
            get => _phone;
            set => SetProperty(ref _phone, value);
        }

        private string _dateOfBirthDisplay;
        public string DateOfBirthDisplay
        {
            get => _dateOfBirthDisplay;
            set => SetProperty(ref _dateOfBirthDisplay, value);
        }

        private string _employeeIdDisplay;
        public string EmployeeIdDisplay
        {
            get => _employeeIdDisplay;
            set => SetProperty(ref _employeeIdDisplay, value);
        }

        private string _department;
        public string Department
        {
            get => _department;
            set => SetProperty(ref _department, value);
        }

        private string _status;
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private string _hireDateDisplay;
        public string HireDateDisplay
        {
            get => _hireDateDisplay;
            set => SetProperty(ref _hireDateDisplay, value);
        }

        private string _lastUpdatedDisplay;
        public string LastUpdatedDisplay
        {
            get => _lastUpdatedDisplay;
            set => SetProperty(ref _lastUpdatedDisplay, value);
        }

        //Dashboard: Attendance 

        private double _attendanceRate;
        public double AttendanceRate
        {
            get => _attendanceRate;
            set
            {
                if (SetProperty(ref _attendanceRate, value))
                    OnPropertyChanged(nameof(AttendanceRateDisplay));
            }
        }

        public string AttendanceRateDisplay => $"{AttendanceRate:0}%";

        private string _attendanceSummary;
        public string AttendanceSummary
        {
            get => _attendanceSummary;
            set => SetProperty(ref _attendanceSummary, value);
        }

        //Dashboard: Requests

        private double _requestApprovalRate;
        public double RequestApprovalRate
        {
            get => _requestApprovalRate;
            set => SetProperty(ref _requestApprovalRate, value);
        }

        private string _requestsSummary;
        public string RequestsSummary
        {
            get => _requestsSummary;
            set => SetProperty(ref _requestsSummary, value);
        }

        // Dashboard: Latest evaluation / bonus 

        private string _latestEvaluationAmountDisplay;
        public string LatestEvaluationAmountDisplay
        {
            get => _latestEvaluationAmountDisplay;
            set => SetProperty(ref _latestEvaluationAmountDisplay, value);
        }

        private string _latestEvaluationTypeDisplay;
        public string LatestEvaluationTypeDisplay
        {
            get => _latestEvaluationTypeDisplay;
            set => SetProperty(ref _latestEvaluationTypeDisplay, value);
        }

        private string _totalBonusThisYearDisplay;
        public string TotalBonusThisYearDisplay
        {
            get => _totalBonusThisYearDisplay;
            set => SetProperty(ref _totalBonusThisYearDisplay, value);
        }

        // Employment details list

        public ObservableCollection<KeyValueItem> EmploymentDetails { get; }

        //Quick info

        private string _latestPayslipDate;
        public string LatestPayslipDate
        {
            get => _latestPayslipDate;
            set => SetProperty(ref _latestPayslipDate, value);
        }

        private string _latestPayslipAmount;
        public string LatestPayslipAmount
        {
            get => _latestPayslipAmount;
            set => SetProperty(ref _latestPayslipAmount, value);
        }

        private string _contractTypeDisplay;
        public string ContractTypeDisplay
        {
            get => _contractTypeDisplay;
            set => SetProperty(ref _contractTypeDisplay, value);
        }

        private string _contractStatusDisplay;
        public string ContractStatusDisplay
        {
            get => _contractStatusDisplay;
            set => SetProperty(ref _contractStatusDisplay, value);
        }

        private int _pendingRequestsCount;
        public int PendingRequestsCount
        {
            get => _pendingRequestsCount;
            set => SetProperty(ref _pendingRequestsCount, value);
        }

        //Edit Profile overlay

        private bool _isEditOpen;
        public bool IsEditOpen
        {
            get => _isEditOpen;
            set => SetProperty(ref _isEditOpen, value);
        }

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

        private DateTime? _formDateOfBirth;
        public DateTime? FormDateOfBirth
        {
            get => _formDateOfBirth;
            set => SetProperty(ref _formDateOfBirth, value);
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

        // Loading

        private void LoadCurrentUser()
        {
            var signedInEmployee = _sessionManager.CurrentUser?.Employee;
            if (signedInEmployee == null)
                return;

            _employee = _employeeRepository.GetById(signedInEmployee.EmployeeId);
            if (_employee == null)
                return;

            FullName = _employee.FullName;
            RoleLabel = RoleNames.TryGetValue(_employee.RoleId, out var roleName) ? roleName : "Employee";
            AvatarPath = string.IsNullOrWhiteSpace(_employee.Avatar)
                ? "/Resources/Images/DefaultAvatar.png"
                : _employee.Avatar;

            Email = _employee.Email;
            Phone = _employee.Phone ?? "—";
            DateOfBirthDisplay = _employee.DateOfBirth.ToString("MMM dd, yyyy");
            EmployeeIdDisplay = $"EMP-{_employee.EmployeeId:0000}";
            Status = _employee.Status;

            var department = _departmentRepository.GetById(_employee.DepartmentId);
            Department = department?.DepartmentName ?? $"Department #{_employee.DepartmentId}";

            _currentContract = _contractRepository.GetCurrentByEmployeeId(_employee.EmployeeId);
            ContractTypeDisplay = _currentContract?.ContractType ?? "No contract on file";
            ContractStatusDisplay = _currentContract?.Status ?? "—";

            HireDateDisplay = _employee.HireDate.ToString("MMM dd, yyyy");
            LastUpdatedDisplay = $"Last updated on {DateTime.Now:MMM dd, yyyy 'at' h:mmtt}";

            EmploymentDetails.Clear();
            EmploymentDetails.Add(new KeyValueItem("Department", Department));
            EmploymentDetails.Add(new KeyValueItem("Status", _employee.Status));
            EmploymentDetails.Add(new KeyValueItem("Hire Date", HireDateDisplay));
            EmploymentDetails.Add(new KeyValueItem("Date of Birth", DateOfBirthDisplay));
            EmploymentDetails.Add(new KeyValueItem("Employee ID", EmployeeIdDisplay));

            if (_currentContract != null)
            {
                EmploymentDetails.Add(new KeyValueItem("Contract Type", _currentContract.ContractType));
                EmploymentDetails.Add(new KeyValueItem("Contract Status", _currentContract.Status));
                EmploymentDetails.Add(new KeyValueItem("Base Salary", $"{_currentContract.BaseSalary:C0}"));
                EmploymentDetails.Add(new KeyValueItem("Contract Start", _currentContract.StartDate.ToString("MMM dd, yyyy")));
                if (_currentContract.EndDate.HasValue)
                    EmploymentDetails.Add(new KeyValueItem("Contract End", _currentContract.EndDate.Value.ToString("MMM dd, yyyy")));
            }

            LoadDashboardStats();
        }

        private void LoadDashboardStats()
        {
            var now = DateTime.Now;

            // Attendance
           
            var attendanceRecords = _attendanceRepository.GetByEmployeeForMonth(_employee.EmployeeId, now.Year, now.Month);
            var presentCount = attendanceRecords.Count(a => string.Equals(a.Status, "Present", StringComparison.OrdinalIgnoreCase));
            var totalAttendanceRecords = attendanceRecords.Count;

            AttendanceRate = totalAttendanceRecords > 0
                ? (double)presentCount / totalAttendanceRecords * 100
                : 0;
            AttendanceSummary = totalAttendanceRecords > 0
                ? $"{presentCount} present / {totalAttendanceRecords - presentCount} other this month"
                : "No attendance records this month";

            //Requests
            var thisYearRequests = _requestFormRepository.GetByEmployeeId(_employee.EmployeeId)
                .Where(r => r.SubmitDate.Year == now.Year)
                .ToList();

            var approvedCount = thisYearRequests.Count(r => string.Equals(r.Status, "Approved", StringComparison.OrdinalIgnoreCase));
            var pendingCount = thisYearRequests.Count(r => string.Equals(r.Status, "Pending", StringComparison.OrdinalIgnoreCase));
            var totalRequests = thisYearRequests.Count;

            RequestApprovalRate = totalRequests > 0 ? (double)approvedCount / totalRequests * 100 : 0;
            PendingRequestsCount = pendingCount;
            RequestsSummary = totalRequests > 0
                ? $"{approvedCount} approved / {totalRequests} total this year"
                : "No requests submitted this year";

            //Latest evaluation / bonus 
            var latestEvaluation = _evaluationRepository.GetLatestByEmployeeId(_employee.EmployeeId);
            if (latestEvaluation != null)
            {
                LatestEvaluationAmountDisplay = latestEvaluation.Amount.ToString("C0");
                var type = latestEvaluation.EvaluationType ?? latestEvaluation.BonusType ?? "Evaluation";
                LatestEvaluationTypeDisplay = $"{type} · {latestEvaluation.BonusDate:MMM dd, yyyy}";
            }
            else
            {
                LatestEvaluationAmountDisplay = "—";
                LatestEvaluationTypeDisplay = "No evaluations on file";
            }

            TotalBonusThisYearDisplay = _evaluationRepository.GetTotalBonusForYear(_employee.EmployeeId, now.Year).ToString("C0");

            // Latest payslip
            var latestPayroll = _payrollRepository.GetLatestByEmployeeId(_employee.EmployeeId);
            if (latestPayroll != null)
            {
                LatestPayslipDate = new DateTime(latestPayroll.Year, latestPayroll.Month, 1).ToString("MMM yyyy");

                var bonusAmount = latestPayroll.EvaluationId.HasValue
                    ? _evaluationRepository.GetById(latestPayroll.EvaluationId.Value)?.Amount ?? 0m
                    : 0m;
                var payAmount = (_currentContract?.BaseSalary ?? 0m) + bonusAmount;
                LatestPayslipAmount = payAmount.ToString("C0");
            }
            else
            {
                LatestPayslipDate = "No payroll yet";
                LatestPayslipAmount = "—";
            }
        }

        private void OnEditProfile(object? parameter)
        {
            if (_employee == null)
                return;

            // Pre-fill the form with the current DB values
            FormFullName = _employee.FullName;
            FormEmail = _employee.Email;
            FormPhone = _employee.Phone ?? string.Empty;
            FormDateOfBirth = _employee.DateOfBirth;
            FormErrorMessage = null;
            IsEditOpen = true;
        }

        private void SaveProfile()
        {
            if (_employee == null)
                return;

            if (string.IsNullOrWhiteSpace(FormFullName) || string.IsNullOrWhiteSpace(FormEmail))
            {
                FormErrorMessage = "Full name and email are required.";
                return;
            }


            _employee.FullName = FormFullName.Trim();
            _employee.Email = FormEmail.Trim();
            _employee.Phone = string.IsNullOrWhiteSpace(FormPhone) ? null : FormPhone.Trim();
            _employee.DateOfBirth = FormDateOfBirth ?? _employee.DateOfBirth;

            _employeeRepository.Update(_employee);

            IsEditOpen = false;

            // Re-read from the database so the page reflects exactly what was saved.
            LoadCurrentUser();
        }
    }

  
    public class KeyValueItem
    {
        public KeyValueItem(string label, string value)
        {
            Label = label;
            Value = value;
        }

        public string Label { get; }
        public string Value { get; }
    }
}
