using HRManagement.Models;
using HRManagement.Repositories.Interfaces;
using HRManagement.Services.Interfaces;
using HRManagement.ViewModels;

namespace HRManagement.Services;

public class ProfileService : IProfileService
{
    private const decimal RetirementContributionRate = 0.07m;

    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IContractRepository _contractRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IEmployeeEvaluationRepository _evaluationRepository;
    private readonly IPayrollRepository _payrollRepository;
    private readonly IRequestFormRepository _requestFormRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ISalaryRepository _salaryRepository;
    private readonly ISalaryCalculator _salaryCalculator;
    private readonly IPaidTimeOffService _paidTimeOffService;
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
        ISalaryRepository salaryRepository,
        ISalaryCalculator salaryCalculator,
        IPaidTimeOffService paidTimeOffService,
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
        _salaryRepository = salaryRepository;
        _salaryCalculator = salaryCalculator;
        _paidTimeOffService = paidTimeOffService;
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
            RoleLabel = _roleRepository.GetById(employee.RoleId)?.RoleName ?? "Employee",
            Address = employee.Address
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
        LoadGlobalStats(employee, currentContract, data);
        LoadJobHistory(employee, data);
        LoadCompensationAndBenefits(employee, currentContract, data);

        return data;
    }

    private void LoadDashboardStats(Employee employee, ProfileData data)
    {
        var now = DateTime.Now;

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

    private void LoadGlobalStats(Employee employee, Contract? currentContract, ProfileData data)
    {
        var now = DateTime.Now;

        // Reuse IPaidTimeOffService - the same PTO source of record used by
        // the salary/payroll pipeline (weekday-only day-off counting,
        // capped at the annual allowance) - so this figure can never drift
        // from what's actually paid out. The previous implementation here
        // summed raw calendar days (including weekends) per approved
        // request and never capped at AnnualPtoAllowanceDays, so it could
        // both overcount and disagree with the real PTO balance.
        var ptoSummary = _paidTimeOffService.GetYearSummary(employee.EmployeeId, now.Year);

        data.PtoDaysUsed = ptoSummary.UsedPaidDays;
        data.PtoDaysRemaining = ptoSummary.RemainingPaidDays;

        var (years, months) = CalculateTenure(employee.HireDate, now);
        data.TenureYears = years;
        data.TenureMonths = months;
        data.TenureDisplay = $"{years}y {months}m";

        if (currentContract != null && years > 0)
        {
            var annualSalary = currentContract.BaseSalary * 12m;
            data.RetirementSavings = annualSalary * RetirementContributionRate * years;
        }
        else
        {
            data.RetirementSavings = 0m;
        }

        var rewardCount = 0;
        var penaltyCount = 0;

        var cursorYear = Math.Max(employee.HireDate.Year, 2000);
        var cursorMonth = employee.HireDate.Year >= 2000 ? employee.HireDate.Month : 1;
        var cursor = new DateTime(cursorYear, cursorMonth, 1);
        var evaluationScanEnd = new DateTime(now.Year, now.Month, 1);

        while (cursor <= evaluationScanEnd)
        {
            var monthlyEvaluations = _evaluationRepository.GetEvaluationsByEmployee(
                employee.EmployeeId, cursor.Month, cursor.Year);

            foreach (var evaluation in monthlyEvaluations)
            {
                if (evaluation.IsReward)
                    rewardCount++;
                else if (evaluation.IsPenalty)
                    penaltyCount++;
            }

            cursor = cursor.AddMonths(1);
        }

        data.TotalRewardCount = rewardCount;
        data.TotalPenaltyCount = penaltyCount;

        var totalEvaluations = rewardCount + penaltyCount;
        data.PerformanceRewardRatio = totalEvaluations > 0
            ? (double)rewardCount / totalEvaluations
            : 0;

        data.PerformanceSummaryDisplay = totalEvaluations > 0
            ? $"{rewardCount} reward{(rewardCount == 1 ? "" : "s")} / {penaltyCount} penalt{(penaltyCount == 1 ? "y" : "ies")}"
            : "No evaluations on file";
    }

    private static (int Years, int Months) CalculateTenure(DateTime hireDate, DateTime asOf)
    {
        var hire = hireDate.Date;
        var today = asOf.Date;

        if (today < hire)
            return (0, 0);

        var years = today.Year - hire.Year;
        var months = today.Month - hire.Month;

        if (today.Day < hire.Day)
            months--;

        if (months < 0)
        {
            years--;
            months += 12;
        }

        return (Math.Max(0, years), Math.Max(0, months));
    }

    private void LoadJobHistory(Employee employee, ProfileData data)
    {
        var contracts = _contractRepository.GetAllByEmployeeId(employee.EmployeeId);

        data.JobHistory = contracts
            .OrderByDescending(c => c.StartDate)
            .Select(c => new JobHistoryEntry
            {
                ContractType = c.ContractType,
                Status = c.Status,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                IsCurrent = string.Equals(c.Status, "Active", StringComparison.OrdinalIgnoreCase),
                DateRangeDisplay = c.EndDate.HasValue
                    ? $"{c.StartDate:MMM yyyy} - {c.EndDate.Value:MMM yyyy}"
                    : $"{c.StartDate:MMM yyyy} - Present"
            })
            .ToList();
    }

    private void LoadCompensationAndBenefits(Employee employee, Contract? currentContract, ProfileData data)
    {
        if (currentContract == null)
        {
            data.CurrentBaseSalaryDisplay = "—";
            data.CurrentPayRateDisplay = "—";
            data.CurrentNetSalaryDisplay = "—";
            data.CurrentSalaryPeriodDisplay = "—";
            data.Benefits = [];
            return;
        }

        var role = _roleRepository.GetById(currentContract.RoleId);

        data.CurrentBaseSalaryDisplay = currentContract.BaseSalary.ToString("C0");
        data.CurrentPayRateDisplay = role != null ? role.PayRate.ToString("N2") : "—";

        // Net salary for the current month, computed the same way
        // SalaryView/ManageSalariesView do (ISalaryCalculator fed by
        // ISalaryRepository), so this figure can never drift from what
        // those pages would show for the same employee/period.
        //
        // Unlike SalaryView, this is NOT gated on PayrollExists() - the
        // Profile page's Compensation card is meant to be a live estimate
        // of the current month's earnings-to-date, not a record of an
        // already-finalized payslip. Gating on PayrollExists would leave
        // this card blank for most of every month (payroll is typically
        // created at month end), which isn't useful here. The Quick Info
        // card elsewhere on this same page already shows the actual last
        // *finalized* payslip (LatestPayslipAmount/LatestPayslipDate) via
        // IPayrollRepository - that one IS a record of a real created
        // payroll row, so the two figures serve different purposes and can
        // legitimately differ.
        var now = DateTime.Now;

        if (role != null)
        {
            var departmentName = _salaryRepository.GetDepartmentName(employee.DepartmentId);
            var attendances = _salaryRepository.GetAttendances(employee.EmployeeId, now.Month, now.Year);
            var evaluations = _salaryRepository.GetEvaluations(employee.EmployeeId, now.Month, now.Year);

            var detail = _salaryCalculator.CalculateSalary(
                employee,
                currentContract,
                role,
                attendances,
                evaluations,
                departmentName,
                now.Month,
                now.Year);

            data.CurrentNetSalaryDisplay = detail.TotalSalary.ToString("C0");
            data.CurrentSalaryPeriodDisplay = $"{now.Month:00}/{now.Year}";
        }
        else
        {
            data.CurrentNetSalaryDisplay = "—";
            data.CurrentSalaryPeriodDisplay = "—";
        }

        data.Benefits = ResolveBenefits(currentContract.ContractType);
    }

    private static List<string> ResolveBenefits(string contractType)
    {
        var normalized = contractType.Trim();

        if (string.Equals(normalized, "Full-time", StringComparison.OrdinalIgnoreCase))
        {
            return
            [
                "Health insurance (full coverage)",
                "Paid time off accrual",
                "Retirement contribution plan"
            ];
        }

        if (string.Equals(normalized, "Part-time", StringComparison.OrdinalIgnoreCase))
        {
            return
            [
                "Health insurance (partial coverage)",
                "Prorated paid time off"
            ];
        }

        if (string.Equals(normalized, "Internship", StringComparison.OrdinalIgnoreCase))
        {
            return
            [
                "Mentorship program",
                "Stipend allowance"
            ];
        }

        if (string.Equals(normalized, "Seasonal", StringComparison.OrdinalIgnoreCase))
        {
            return ["Seasonal allowance"];
        }

        return ["Standard employment benefits"];
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
        var newAddress = string.IsNullOrWhiteSpace(input.Address) ? null : input.Address.Trim();

        if (employee.FullName != newFullName) changes.Add("Full Name");
        if (employee.Email != newEmail) changes.Add("Email");
        if ((employee.Phone ?? "") != (newPhone ?? "")) changes.Add("Phone");
        if (employee.DateOfBirth != newDateOfBirth) changes.Add("Date of Birth");
        if ((employee.Address ?? "") != (newAddress ?? "")) changes.Add("Address");

        employee.FullName = newFullName;
        employee.Email = newEmail;
        employee.Phone = newPhone;
        employee.DateOfBirth = newDateOfBirth;
        employee.Address = newAddress;

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
