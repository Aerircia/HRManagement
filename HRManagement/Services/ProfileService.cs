using HRManagement.Models;
using HRManagement.Repositories.Interfaces;
using HRManagement.Services.Interfaces;
using HRManagement.ViewModels;

namespace HRManagement.Services;

/// <summary>
/// Backing service for ProfileViewModel ("My Profile" page). Owns loading
/// the signed-in employee's personal/employment info and dashboard stats,
/// and saving edits to the editable profile fields. Pulled out of the
/// ViewModel so the view -> viewmodel -> service -> repository flow matches
/// the rest of the app (see ManageProfilesService, EmployeeEvaluationService).
/// </summary>
public class ProfileService : IProfileService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IContractRepository _contractRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IEmployeeEvaluationRepository _evaluationRepository;
    private readonly IPayrollRepository _payrollRepository;
    private readonly IRequestFormRepository _requestFormRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ILogService _logService;

    public ProfileService(
        IEmployeeRepository employeeRepository,
        IDepartmentRepository departmentRepository,
        IContractRepository contractRepository,
        IAttendanceRepository attendanceRepository,
        IEmployeeEvaluationRepository evaluationRepository,
        IPayrollRepository payrollRepository,
        IRequestFormRepository requestFormRepository,
        IRoleRepository roleRepository,
        ILogService logService)
    {
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _contractRepository = contractRepository;
        _attendanceRepository = attendanceRepository;
        _evaluationRepository = evaluationRepository;
        _payrollRepository = payrollRepository;
        _requestFormRepository = requestFormRepository;
        _roleRepository = roleRepository;
        _logService = logService;
    }

    public ProfileData? GetProfile(int employeeId)
    {
        var employee = _employeeRepository.GetById(employeeId);
        if (employee == null)
            return null;

        var data = new ProfileData
        {
            Employee = employee,
            RoleLabel = _roleRepository.GetById(employee.RoleId)?.RoleName ?? "Employee"
        };

        var department = _departmentRepository.GetById(employee.DepartmentId);
        data.DepartmentName = department?.DepartmentName ?? $"Department #{employee.DepartmentId}";

        var currentContract = _contractRepository.GetCurrentByEmployeeId(employee.EmployeeId);
        data.CurrentContract = currentContract;
        data.ContractTypeDisplay = currentContract?.ContractType ?? "No contract on file";
        data.ContractStatusDisplay = currentContract?.Status ?? "—";

        data.EmploymentDetails.Add(new KeyValueItem("Department", data.DepartmentName));
        data.EmploymentDetails.Add(new KeyValueItem("Status", employee.Status));
        data.EmploymentDetails.Add(new KeyValueItem("Hire Date", employee.HireDate.ToString("MMM dd, yyyy")));
        data.EmploymentDetails.Add(new KeyValueItem("Date of Birth", employee.DateOfBirth.ToString("MMM dd, yyyy")));
        data.EmploymentDetails.Add(new KeyValueItem("Employee ID", $"EMP-{employee.EmployeeId:0000}"));

        if (currentContract != null)
        {
            data.EmploymentDetails.Add(new KeyValueItem("Contract Type", currentContract.ContractType));
            data.EmploymentDetails.Add(new KeyValueItem("Contract Status", currentContract.Status));
            data.EmploymentDetails.Add(new KeyValueItem("Base Salary", $"{currentContract.BaseSalary:C0}"));
            data.EmploymentDetails.Add(new KeyValueItem("Contract Start", currentContract.StartDate.ToString("MMM dd, yyyy")));
            if (currentContract.EndDate.HasValue)
                data.EmploymentDetails.Add(new KeyValueItem("Contract End", currentContract.EndDate.Value.ToString("MMM dd, yyyy")));
        }

        LoadDashboardStats(employee, data);

        return data;
    }

    private void LoadDashboardStats(Employee employee, ProfileData data)
    {
        var now = DateTime.Now;

        // Attendance
        var attendanceRecords = _attendanceRepository
            .GetAttendancesForEmployeeMonth(employee.EmployeeId, now.Year, now.Month)
            .ToList();

        var presentCount = attendanceRecords.Count(a => string.Equals(a.Status, "Present", StringComparison.OrdinalIgnoreCase));
        var totalAttendanceRecords = attendanceRecords.Count;

        data.AttendanceRate = totalAttendanceRecords > 0
            ? (double)presentCount / totalAttendanceRecords * 100
            : 0;
        data.AttendanceSummary = totalAttendanceRecords > 0
            ? $"{presentCount} present / {totalAttendanceRecords - presentCount} other this month"
            : "No attendance records this month";

        // Requests
        var thisYearRequests = _requestFormRepository.GetByEmployeeId(employee.EmployeeId)
            .Where(r => r.SubmitDate.Year == now.Year)
            .ToList();

        var approvedCount = thisYearRequests.Count(r => string.Equals(r.Status, "Approved", StringComparison.OrdinalIgnoreCase));
        var pendingCount = thisYearRequests.Count(r => string.Equals(r.Status, "Pending", StringComparison.OrdinalIgnoreCase));
        var totalRequests = thisYearRequests.Count;

        data.RequestApprovalRate = totalRequests > 0 ? (double)approvedCount / totalRequests * 100 : 0;
        data.PendingRequestsCount = pendingCount;
        data.RequestsSummary = totalRequests > 0
            ? $"{approvedCount} approved / {totalRequests} total this year"
            : "No requests submitted this year";

        // Latest evaluation / bonus
        var latestEvaluation = _evaluationRepository.GetLatestByEmployeeId(employee.EmployeeId);
        if (latestEvaluation != null)
        {
            data.LatestEvaluationAmountDisplay = latestEvaluation.Amount.ToString("C0");
            var type = latestEvaluation.EvaluationType ?? latestEvaluation.BonusType ?? "Evaluation";
            data.LatestEvaluationTypeDisplay = $"{type} · {latestEvaluation.BonusDate:MMM dd, yyyy}";
        }
        else
        {
            data.LatestEvaluationAmountDisplay = "—";
            data.LatestEvaluationTypeDisplay = "No evaluations on file";
        }

        data.TotalBonusThisYearDisplay = _evaluationRepository
            .GetTotalBonusForYear(employee.EmployeeId, now.Year)
            .ToString("C0");

        // Latest payslip
        var latestPayroll = _payrollRepository.GetLatestByEmployeeId(employee.EmployeeId);
        if (latestPayroll != null)
        {
            data.LatestPayslipDate = new DateTime(latestPayroll.Year, latestPayroll.Month, 1).ToString("MMM yyyy");

            var bonusAmount = latestPayroll.EvaluationId.HasValue
                ? _evaluationRepository.GetById(latestPayroll.EvaluationId.Value)?.Amount ?? 0m
                : 0m;
            var payAmount = (data.CurrentContract?.BaseSalary ?? 0m) + bonusAmount;
            data.LatestPayslipAmount = payAmount.ToString("C0");
        }
        else
        {
            data.LatestPayslipDate = "No payroll yet";
            data.LatestPayslipAmount = "—";
        }
    }

    public ProfileUpdateResult UpdateProfile(int employeeId, ProfileUpdateInput input)
    {
        var employee = _employeeRepository.GetById(employeeId);
        if (employee == null)
        {
            return new ProfileUpdateResult
            {
                Success = false,
                ErrorMessage = "Employee could not be found."
            };
        }

        if (string.IsNullOrWhiteSpace(input.FullName) || string.IsNullOrWhiteSpace(input.Email))
        {
            return new ProfileUpdateResult
            {
                Success = false,
                ErrorMessage = "Full name and email are required."
            };
        }

        var changes = new List<string>();

        var newFullName = input.FullName.Trim();
        var newEmail = input.Email.Trim();
        var newPhone = string.IsNullOrWhiteSpace(input.Phone) ? null : input.Phone.Trim();
        var newDateOfBirth = input.DateOfBirth ?? employee.DateOfBirth;

        if (employee.FullName != newFullName) changes.Add("Full Name");
        if (employee.Email != newEmail) changes.Add("Email");
        if ((employee.Phone ?? "") != (newPhone ?? "")) changes.Add("Phone");
        if (employee.DateOfBirth != newDateOfBirth) changes.Add("Date of Birth");

        employee.FullName = newFullName;
        employee.Email = newEmail;
        employee.Phone = newPhone;
        employee.DateOfBirth = newDateOfBirth;

        _employeeRepository.Update(employee);

        var message = changes.Count > 0 ? "Updated: " + string.Join(", ", changes) : "Profile updated";
        _logService.WriteLog(employeeId, message);

        return new ProfileUpdateResult
        {
            Success = true,
            Changes = changes
        };
    }
}
