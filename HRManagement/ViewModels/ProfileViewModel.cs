using HRManagement.Models;
using HRManagement.Services;
using HRManagement.Services.Interfaces;
using HRManagement.Utilities;
using System.Collections.ObjectModel;

namespace HRManagement.ViewModels
{
    public class ProfileViewModel : PageViewModel
    {
        private readonly IProfileService _profileService;
        private readonly SessionManager _sessionManager;

        private ProfileData? _profile;

        public override string Title => "My Profile";

        public ProfileViewModel(
            IProfileService profileService,
            SessionManager sessionManager)
        {
            _profileService = profileService;
            _sessionManager = sessionManager;

            EditProfileCommand = new RelayCommand(OnEditProfile);
            SaveProfileCommand = new RelayCommand(_ => SaveProfile());
            CancelEditCommand = new RelayCommand(_ => IsEditOpen = false);

            LoadCurrentUser();
        }

        public RelayCommand EditProfileCommand { get; }
        public RelayCommand SaveProfileCommand { get; }
        public RelayCommand CancelEditCommand { get; }

        //Header

        public string SubHeading => "Your personal and employment information";

        //Personal info 

        private string _fullName = string.Empty;
        public string FullName
        {
            get => _fullName;
            set => SetProperty(ref _fullName, value);
        }

        private string _roleLabel = string.Empty;
        public string RoleLabel
        {
            get => _roleLabel;
            set => SetProperty(ref _roleLabel, value);
        }

        private string _avatarPath = string.Empty;
        public string AvatarPath
        {
            get => _avatarPath;
            set => SetProperty(ref _avatarPath, value);
        }

        private string _email = string.Empty;
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        private string _phone = string.Empty;
        public string Phone
        {
            get => _phone;
            set => SetProperty(ref _phone, value);
        }

        private string _dateOfBirthDisplay = string.Empty;
        public string DateOfBirthDisplay
        {
            get => _dateOfBirthDisplay;
            set => SetProperty(ref _dateOfBirthDisplay, value);
        }

        private string _employeeIdDisplay = string.Empty;
        public string EmployeeIdDisplay
        {
            get => _employeeIdDisplay;
            set => SetProperty(ref _employeeIdDisplay, value);
        }

        private string _department = string.Empty;
        public string Department
        {
            get => _department;
            set => SetProperty(ref _department, value);
        }

        private string _status = string.Empty;
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private string _hireDateDisplay = string.Empty;
        public string HireDateDisplay
        {
            get => _hireDateDisplay;
            set => SetProperty(ref _hireDateDisplay, value);
        }

        private string _lastUpdatedDisplay = string.Empty;
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

        private string _latestEvaluationAmountDisplay = string.Empty;
        public string LatestEvaluationAmountDisplay
        {
            get => _latestEvaluationAmountDisplay;
            set => SetProperty(ref _latestEvaluationAmountDisplay, value);
        }

        private string _latestEvaluationTypeDisplay = string.Empty;
        public string LatestEvaluationTypeDisplay
        {
            get => _latestEvaluationTypeDisplay;
            set => SetProperty(ref _latestEvaluationTypeDisplay, value);
        }

        private string _totalBonusThisYearDisplay = string.Empty;
        public string TotalBonusThisYearDisplay
        {
            get => _totalBonusThisYearDisplay;
            set => SetProperty(ref _totalBonusThisYearDisplay, value);
        }

        // Employment details list

        public ObservableCollection<KeyValueItem> EmploymentDetails { get; } = [];

        //Quick info

        private string _latestPayslipDate = string.Empty;
        public string LatestPayslipDate
        {
            get => _latestPayslipDate;
            set => SetProperty(ref _latestPayslipDate, value);
        }

        private string _latestPayslipAmount = string.Empty;
        public string LatestPayslipAmount
        {
            get => _latestPayslipAmount;
            set => SetProperty(ref _latestPayslipAmount, value);
        }

        private string _contractTypeDisplay = string.Empty;
        public string ContractTypeDisplay
        {
            get => _contractTypeDisplay;
            set => SetProperty(ref _contractTypeDisplay, value);
        }

        private string _contractStatusDisplay = string.Empty;
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
            var signedInEmployeeId = _sessionManager.CurrentUser?.Employee?.EmployeeId;
            if (signedInEmployeeId == null)
                return;

            var profile = _profileService.GetProfile(signedInEmployeeId.Value);
            if (profile == null)
                return;

            _profile = profile;
            var employee = profile.Employee;

            FullName = employee.FullName;
            RoleLabel = profile.RoleLabel;
            AvatarPath = string.IsNullOrWhiteSpace(employee.Avatar)
                ? "/Resources/Images/DefaultAvatar.png"
                : employee.Avatar;

            Email = employee.Email;
            Phone = employee.Phone ?? "—";
            DateOfBirthDisplay = employee.DateOfBirth.ToString("MMM dd, yyyy");
            EmployeeIdDisplay = $"EMP-{employee.EmployeeId:0000}";
            Status = employee.Status;
            Department = profile.DepartmentName;

            ContractTypeDisplay = profile.ContractTypeDisplay;
            ContractStatusDisplay = profile.ContractStatusDisplay;

            HireDateDisplay = employee.HireDate.ToString("MMM dd, yyyy");
            LastUpdatedDisplay = $"Last updated on {DateTime.Now:MMM dd, yyyy 'at' h:mmtt}";

            EmploymentDetails.Clear();
            foreach (var item in profile.EmploymentDetails)
                EmploymentDetails.Add(item);

            AttendanceRate = profile.AttendanceRate;
            AttendanceSummary = profile.AttendanceSummary;

            RequestApprovalRate = profile.RequestApprovalRate;
            PendingRequestsCount = profile.PendingRequestsCount;
            RequestsSummary = profile.RequestsSummary;

            LatestEvaluationAmountDisplay = profile.LatestEvaluationAmountDisplay;
            LatestEvaluationTypeDisplay = profile.LatestEvaluationTypeDisplay;
            TotalBonusThisYearDisplay = profile.TotalBonusThisYearDisplay;

            LatestPayslipDate = profile.LatestPayslipDate;
            LatestPayslipAmount = profile.LatestPayslipAmount;
        }

        private void OnEditProfile(object? parameter)
        {
            if (_profile == null)
                return;

            var employee = _profile.Employee;

            // Pre-fill the form with the current DB values
            FormFullName = employee.FullName;
            FormEmail = employee.Email;
            FormPhone = employee.Phone ?? string.Empty;
            FormDateOfBirth = employee.DateOfBirth;
            FormErrorMessage = null;
            IsEditOpen = true;
        }

        private void SaveProfile()
        {
            var signedInEmployeeId = _sessionManager.CurrentUser?.Employee?.EmployeeId;
            if (signedInEmployeeId == null)
                return;

            var input = new ProfileUpdateInput
            {
                FullName = FormFullName,
                Email = FormEmail,
                Phone = FormPhone,
                DateOfBirth = FormDateOfBirth
            };

            var result = _profileService.UpdateProfile(signedInEmployeeId.Value, input);

            if (!result.Success)
            {
                FormErrorMessage = result.ErrorMessage;
                return;
            }

            IsEditOpen = false;

            // Re-read so the page reflects exactly what was saved.
            LoadCurrentUser();
        }
    }

    public class KeyValueItem(string label, string value)
    {
        public string Label { get; } = label;
        public string Value { get; } = value;
    }
}
