using HRManagement.Models;
using HRManagement.Repositories.Interfaces;
using HRManagement.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace HRManagement.Services;

public class KpiService : IKpiService
{
    private readonly IKpiRepository _kpiRepository;
    private readonly IKpiSetRepository _kpiSetRepository;
    private readonly IEmployeeKpiAssignmentRepository _assignmentRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly ILogService _logService;
    private readonly SessionManager _sessionManager;

    private const string MonthFormat = "MM/yyyy";

    private const decimal RequiredTotalWeight = 100m;
    private const decimal WeightTolerance = 0.01m;

    public KpiService(
        IKpiRepository kpiRepository,
        IKpiSetRepository kpiSetRepository,
        IEmployeeKpiAssignmentRepository assignmentRepository,
        IEmployeeRepository employeeRepository,
        IDepartmentRepository departmentRepository,
        ILogService logService,
        SessionManager sessionManager)
    {
        _kpiRepository = kpiRepository ?? throw new ArgumentNullException(nameof(kpiRepository));
        _kpiSetRepository = kpiSetRepository ?? throw new ArgumentNullException(nameof(kpiSetRepository));
        _assignmentRepository = assignmentRepository ?? throw new ArgumentNullException(nameof(assignmentRepository));
        _employeeRepository = employeeRepository ?? throw new ArgumentNullException(nameof(employeeRepository));
        _departmentRepository = departmentRepository ?? throw new ArgumentNullException(nameof(departmentRepository));
        _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
    }

    // ============================================================
    // KPI Assignment page (Admin / Manager)
    // ============================================================

    public List<Department> GetDepartmentOptions()
    {
        var departments = new List<Department>
        {
            new() { DepartmentId = 0, DepartmentName = "All Departments" }
        };

        var currentUser = _sessionManager.CurrentUser;

        if (currentUser != null && IsManager(currentUser))
        {
            // Manager: only their own department, so the dropdown still
            // works without exposing other departments.
            var deptId = currentUser.Employee.DepartmentId;
            var dept = _departmentRepository.GetAll().FirstOrDefault(d => d.DepartmentId == deptId);

            departments.Add(dept ?? new Department { DepartmentId = deptId, DepartmentName = $"Department {deptId}" });
        }
        else
        {
            departments.AddRange(_departmentRepository.GetAll());
        }

        return departments;
    }

    public KpiAssignmentOverviewDto GetAssignmentOverview(int? departmentId, string? employeeFilter, string month)
    {
        var scopedDepartmentId = ResolveScopedDepartmentId(departmentId);

        if (!TryParseMonth(month, out var startDate, out var endDate))
        {
            return new KpiAssignmentOverviewDto();
        }

        var employees = GetScopedEmployees(scopedDepartmentId);

        if (!string.IsNullOrWhiteSpace(employeeFilter))
        {
            employees = employees.Where(e =>
                    (e.FullName != null && e.FullName.Contains(employeeFilter, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        var departmentNamesById = _departmentRepository.GetAll()
            .ToDictionary(d => d.DepartmentId, d => d.DepartmentName);

        var assignments = _assignmentRepository.GetByDepartment(scopedDepartmentId, startDate, endDate);
        var assignmentsByEmployee = assignments.GroupBy(a => a.EmployeeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var rows = new List<KpiEmployeeOverviewDto>();
        var employeesWithoutKpi = 0;
        var atRiskCount = 0;
        var mtdSum = 0.0;
        var mtdCount = 0;

        foreach (var employee in employees)
        {
            assignmentsByEmployee.TryGetValue(employee.EmployeeId, out var employeeAssignments);
            employeeAssignments ??= new List<EmployeeKpiAssignment>();

            var mtdPercent = CalculateMtdPercent(employeeAssignments);

            rows.Add(new KpiEmployeeOverviewDto
            {
                EmployeeId = employee.EmployeeId,
                EmployeeName = employee.FullName,
                EmployeeCode = BuildEmployeeCode(employee.EmployeeId),
                Department = departmentNamesById.TryGetValue(employee.DepartmentId, out var deptName) ? deptName : string.Empty,
                AssignedKpiCount = employeeAssignments.Count,
                KpiMtdPercent = Math.Round(mtdPercent, 1)
            });

            if (employeeAssignments.Count == 0)
            {
                employeesWithoutKpi++;
            }
            else
            {
                mtdSum += mtdPercent;
                mtdCount++;

                if (mtdPercent < 50)
                    atRiskCount++;
            }
        }

        return new KpiAssignmentOverviewDto
        {
            Employees = rows,
            TotalAssignedCount = rows.Count(r => r.AssignedKpiCount > 0),
            EmployeesWithoutKpiCount = employeesWithoutKpi,
            AverageKpiMtdPercent = mtdCount > 0 ? Math.Round(mtdSum / mtdCount, 1) : 0,
            AtRiskCount = atRiskCount
        };
    }

    public List<AssignedKpiDto> GetAssignedKpis(int employeeId, string month)
    {
        if (!TryParseMonth(month, out var startDate, out var endDate))
            return new List<AssignedKpiDto>();

        var assignments = _assignmentRepository.GetByEmployeeAndPeriod(employeeId, startDate, endDate);
        return assignments.Select(MapAssignedKpiDto).ToList();
    }

    public List<string> GetAvailableMonths()
    {
        var months = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var kpiSet in _kpiSetRepository.GetAll())
        {
            months.Add(kpiSet.CreatedAt.ToString(MonthFormat, CultureInfo.InvariantCulture));
        }

        months.Add(DateTime.Now.ToString(MonthFormat, CultureInfo.InvariantCulture));

        return months
            .Select(m => DateTime.ParseExact(m, MonthFormat, CultureInfo.InvariantCulture))
            .OrderByDescending(d => d)
            .Select(d => d.ToString(MonthFormat, CultureInfo.InvariantCulture))
            .ToList();
    }

    public List<KpiSetSummaryDto> GetKpiSetsForMonth(string month, int? departmentId)
    {
        var scopedDepartmentId = ResolveScopedDepartmentId(departmentId);

        var sets = _kpiSetRepository.GetByDepartment(scopedDepartmentId)
            .Where(s => s.IsActive)
            .ToList();

        var departmentNamesById = _departmentRepository.GetAll()
            .ToDictionary(d => d.DepartmentId, d => d.DepartmentName);

        var results = new List<KpiSetSummaryDto>();

        foreach (var set in sets)
        {
            var details = _kpiSetRepository.GetDetails(set.KpiSetId);

            results.Add(new KpiSetSummaryDto
            {
                KpiSetId = set.KpiSetId,
                KpiSetName = set.KpiSetName,
                Description = set.Description,
                Department = set.DepartmentId.HasValue && departmentNamesById.TryGetValue(set.DepartmentId.Value, out var deptName)
                    ? deptName
                    : null,
                Details = details.Select(d => new KpiSetDetailDto
                {
                    KpiSetDetailId = d.KpiSetDetailId,
                    KpiId = d.KpiId,
                    KpiName = d.KpiName ?? string.Empty,
                    MeasurementUnit = d.MeasurementUnit ?? string.Empty,
                    TargetValue = d.TargetValue,
                    Weight = d.Weight
                }).ToList()
            });
        }

        return results;
    }

    public void AssignKpis(
        AssignKpiRequest request)
    {
        EnsureAdmin();

        if (request == null)
        {
            throw new ArgumentNullException(
                nameof(request));
        }

        if (!TryParseMonth(
                request.Month,
                out var startDate,
                out var endDate))
        {
            throw new ArgumentException(
                $"Invalid month '{request.Month}'. " +
                $"Expected format {MonthFormat}.",
                nameof(request));
        }

        if (request.EmployeeAssignments.Count == 0)
        {
            throw new InvalidOperationException(
                "At least one employee must be selected.");
        }

        /*
         * Validate every employee before the first database write.
         * The selected rows represent that employee's complete final KPI
         * set for the period, not an additive merge.
         */
        var replacements =
            new List<PeriodAssignmentReplacement>();

        foreach (var employeeInput
                 in request.EmployeeAssignments)
        {
            if (employeeInput.SelectedDetails.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Employee {employeeInput.EmployeeId} must have " +
                    "at least one selected KPI.");
            }

            var duplicateDetail =
                employeeInput.SelectedDetails
                    .GroupBy(
                        item =>
                            item.KpiSetDetailId)
                    .FirstOrDefault(
                        group =>
                            group.Count() > 1);

            if (duplicateDetail != null)
            {
                throw new InvalidOperationException(
                    $"KPI detail {duplicateDetail.Key} is duplicated " +
                    $"for employee {employeeInput.EmployeeId}.");
            }

            foreach (var selection
                     in employeeInput.SelectedDetails)
            {
                ValidateAssignmentValues(
                    selection.AssignedTarget,
                    selection.AssignedWeight);
            }

            var totalWeight =
                employeeInput.SelectedDetails
                    .Sum(
                        selection =>
                            selection.AssignedWeight);

            ValidateTotalWeight(
                totalWeight,
                employeeInput.EmployeeId);

            var existingAssignments =
                _assignmentRepository
                    .GetByEmployeeAndPeriod(
                        employeeInput.EmployeeId,
                        startDate,
                        endDate);

            if (existingAssignments.Any(
                    HasProgressStarted))
            {
                throw new InvalidOperationException(
                    $"KPI assignments for employee " +
                    $"{employeeInput.EmployeeId} cannot be replaced " +
                    "after progress has started.");
            }

            var assignments =
                employeeInput.SelectedDetails
                    .Select(
                        selection =>
                            new EmployeeKpiAssignment
                            {
                                EmployeeId =
                                    employeeInput.EmployeeId,

                                KpiSetDetailId =
                                    selection.KpiSetDetailId,

                                AssignedTarget =
                                    selection.AssignedTarget,

                                AssignedWeight =
                                    selection.AssignedWeight,

                                CurrentValue = 0,
                                PendingValue = null,
                                Status = "Not Started",
                                StartDate = startDate,
                                EndDate = endDate,
                                IsLocked = false
                            })
                    .ToList();

            replacements.Add(
                new PeriodAssignmentReplacement(
                    employeeInput.EmployeeId,
                    assignments));
        }

        /*
         * Replace each employee period atomically. Existing unselected
         * KPIs are removed; the selected set is inserted as the final
         * 100%-weight assignment set.
         */
        foreach (var replacement
                 in replacements)
        {
            _assignmentRepository
                .ReplacePeriodAssignments(
                    replacement.EmployeeId,
                    startDate,
                    endDate,
                    replacement.Assignments);
        }

        var totalAssignmentCount =
            replacements.Sum(
                replacement =>
                    replacement.Assignments.Count);

        _logService.WriteLog(
            CurrentAccountId(),
            $"Assigned KPI set {request.KpiSetId} to " +
            $"{replacements.Count} employee(s) for {request.Month}. " +
            $"Replaced period assignments with " +
            $"{totalAssignmentCount} KPI row(s).");
    }

    public void UpdateAssignment(
        UpdateKpiAssignmentRequest request)
    {
        EnsureAdmin();

        if (request == null)
            throw new ArgumentNullException(
                nameof(request));

        ValidateAssignmentValues(
            request.AssignedTarget,
            request.AssignedWeight);

        var existing =
            _assignmentRepository.GetById(
                request.AssignmentId)
            ?? throw new InvalidOperationException(
                "KPI assignment was not found.");

        /*
         * A single-row Weight edit would immediately make the period
         * total different from 100%. Weight changes must therefore be
         * submitted as a complete replacement set through Assign KPI.
         */
        if (Math.Abs(
                request.AssignedWeight
                - existing.AssignedWeight)
            > WeightTolerance)
        {
            throw new InvalidOperationException(
                "Weight cannot be changed from the single KPI edit form. " +
                "Use Assign KPI to replace and rebalance the complete " +
                "employee KPI set.");
        }

        if (HasProgressStarted(existing))
        {
            throw new InvalidOperationException(
                "Target and weight can only be edited before " +
                "KPI progress starts.");
        }

        var changes =
            new List<string>();

        if (existing.AssignedTarget
            != request.AssignedTarget)
        {
            changes.Add(
                $"Target {existing.AssignedTarget} " +
                $"-> {request.AssignedTarget}");
        }

        if (existing.AssignedWeight
            != request.AssignedWeight)
        {
            changes.Add(
                $"Weight {existing.AssignedWeight} " +
                $"-> {request.AssignedWeight}");
        }

        /*
         * CurrentValue is intentionally not modified from this dialog.
         * It changes only through Employee Pending -> Manager/Admin
         * approval.
         */
        existing.AssignedTarget =
            request.AssignedTarget;

        existing.AssignedWeight =
            request.AssignedWeight;

        _assignmentRepository.Update(
            existing);

        var changeSummary =
            changes.Count > 0
                ? string.Join(
                    "; ",
                    changes)
                : "no field changes";

        _logService.WriteLog(
            CurrentAccountId(),
            $"Updated KPI assignment " +
            $"{existing.AssignmentId} " +
            $"({changeSummary}).");
    }

    public void DeleteAssignment(
        int assignmentId)
    {
        EnsureAdmin();

        var existing =
            _assignmentRepository.GetById(
                assignmentId)
            ?? throw new InvalidOperationException(
                "KPI assignment was not found.");

        if (HasProgressStarted(existing))
        {
            throw new InvalidOperationException(
                "A KPI assignment cannot be removed after progress starts.");
        }

        /*
         * Removing one weighted KPI from a valid 100% period always
         * leaves an invalid total. Use the replacement wizard so the
         * remaining KPI weights can be rebalanced and saved atomically.
         */
        throw new InvalidOperationException(
            "A single KPI assignment cannot be deleted because the " +
            "remaining total Weight would be below 100%. " +
            "Use Assign KPI to replace the complete employee KPI set.");
    }

    // ============================================================
    // Personal KPI page (Employee self-service)
    // ============================================================

    public List<AssignedKpiDto> GetMyAssignedKpis(int employeeId, string month)
    {
        return GetAssignedKpis(employeeId, month);
    }

    public void UpdateMyProgress(
        int assignmentId,
        int employeeId,
        decimal pendingValue)
    {
        if (pendingValue <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pendingValue),
                "Pending progress must be greater than 0.");
        }

        var existing =
            _assignmentRepository.GetById(assignmentId)
            ?? throw new InvalidOperationException(
                "KPI assignment was not found.");

        if (existing.EmployeeId != employeeId)
        {
            throw new UnauthorizedAccessException(
                "You cannot update another employee's KPI.");
        }

        if (existing.IsLocked)
        {
            throw new InvalidOperationException(
                "This KPI assignment is locked.");
        }

        if (string.Equals(
                existing.Status,
                "Completed",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Completed KPI cannot be updated.");
        }

        if (string.Equals(
                existing.Status,
                "Cancelled",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Cancelled KPI cannot be updated.");
        }

        if (existing.PendingValue.HasValue)
        {
            throw new InvalidOperationException(
                "A progress update is already pending approval.");
        }

        if (DateTime.Today > existing.EndDate.Date)
        {
            throw new InvalidOperationException(
                "The KPI period has ended.");
        }

        /*
         * Employee submits only an increment waiting for approval.
         * CurrentValue remains unchanged until Manager/Admin approves.
         */
        existing.PendingValue = pendingValue;
        existing.Status = "Pending Approval";

        _assignmentRepository.Update(existing);

        _logService.WriteLog(
            CurrentAccountId(),
            $"Submitted pending KPI progress {pendingValue:N2} " +
            $"for assignment {assignmentId} ({existing.KpiName}).");
    }

    public void ApprovePendingProgress(int assignmentId)
    {
        EnsureManagerOrAdmin();

        var assignment =
            _assignmentRepository.GetById(assignmentId)
            ?? throw new InvalidOperationException(
                "KPI assignment was not found.");

        EnsureCanApproveEmployee(
            assignment.EmployeeId);

        if (assignment.IsLocked)
        {
            throw new InvalidOperationException(
                "This KPI assignment is locked.");
        }

        if (!assignment.PendingValue.HasValue)
        {
            throw new InvalidOperationException(
                "There is no pending progress to approve.");
        }

        var approvedValue =
            assignment.PendingValue.Value;

        assignment.CurrentValue +=
            approvedValue;

        assignment.PendingValue = null;

        assignment.Status =
            assignment.AssignedTarget > 0
            && assignment.CurrentValue
                >= assignment.AssignedTarget
                ? "Completed"
                : "In Progress";

        _assignmentRepository.Update(
            assignment);

        _logService.WriteLog(
            CurrentAccountId(),
            $"Approved pending KPI progress {approvedValue:N2} " +
            $"for assignment {assignmentId}. " +
            $"Current value is now {assignment.CurrentValue:N2}.");

        TryFinalizeEmployeePeriod(
            assignment.EmployeeId,
            assignment.StartDate,
            assignment.EndDate);
    }

    public void RejectPendingProgress(int assignmentId)
    {
        EnsureAdmin();

        var assignment =
            _assignmentRepository.GetById(assignmentId)
            ?? throw new InvalidOperationException(
                "KPI assignment was not found.");

        if (assignment.IsLocked)
        {
            throw new InvalidOperationException(
                "This KPI assignment is locked.");
        }

        if (!assignment.PendingValue.HasValue)
        {
            throw new InvalidOperationException(
                "There is no pending progress to reject.");
        }

        var rejectedValue =
            assignment.PendingValue.Value;

        assignment.PendingValue = null;

        assignment.Status =
            assignment.CurrentValue <= 0
                ? "Not Started"
                : "In Progress";

        _assignmentRepository.Update(
            assignment);

        _logService.WriteLog(
            CurrentAccountId(),
            $"Rejected pending KPI progress {rejectedValue:N2} " +
            $"for assignment {assignmentId}.");
    }

    public void SynchronizeExpiredAssignments()
    {
        var expiredAssignments =
            _assignmentRepository.GetExpiredUnlocked(
                DateTime.Today);

        var affectedPeriods =
            expiredAssignments
                .Select(a => new
                {
                    a.EmployeeId,
                    StartDate = a.StartDate.Date,
                    EndDate = a.EndDate.Date
                })
                .Distinct()
                .ToList();

        foreach (var assignment in expiredAssignments)
        {
            assignment.PendingValue = null;
            assignment.Status = "Completed";
            _assignmentRepository.Update(assignment);
        }

        foreach (var period in affectedPeriods)
        {
            TryFinalizeEmployeePeriod(
                period.EmployeeId,
                period.StartDate,
                period.EndDate);
        }

        if (expiredAssignments.Count > 0)
        {
            _logService.WriteLog(
                CurrentAccountId(),
                $"Completed and finalized {expiredAssignments.Count} expired KPI assignment(s).");
        }
    }

    // ============================================================
    // Helpers
    // ============================================================

    private void TryFinalizeEmployeePeriod(
        int employeeId,
        DateTime startDate,
        DateTime endDate)
    {
        var assignments =
            _assignmentRepository.GetByEmployeeAndPeriod(
                employeeId,
                startDate,
                endDate);

        if (assignments.Count == 0
            || assignments.All(a => a.IsLocked)
            || assignments.Any(a => !string.Equals(
                a.Status,
                "Completed",
                StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var totalScore =
            CalculateTotalKpiScore(assignments);

        var result =
            ResolveKpiRewardPenalty(totalScore);

        EmployeeEvaluation? evaluation = null;

        if (result.Amount > 0)
        {
            evaluation = new EmployeeEvaluation
            {
                EmployeeId = employeeId,
                EvaluationType = "KPI",
                BonusType = result.BonusType,
                Amount = result.Amount,
                BonusDate = endDate.Date,
                Comment =
                    $"Automatic KPI evaluation. Total KPI score: {totalScore:N2}."
            };
        }

        _assignmentRepository.FinalizePeriod(
            employeeId,
            startDate,
            endDate,
            evaluation);

        _logService.WriteLog(
            CurrentAccountId(),
            evaluation == null
                ? $"Finalized KPI period for employee {employeeId}. Score {totalScore:N2}; no reward or penalty."
                : $"Finalized KPI period for employee {employeeId}. Score {totalScore:N2}; {result.BonusType} ${result.Amount:N2}.");
    }

    private static decimal CalculateTotalKpiScore(
        IReadOnlyList<EmployeeKpiAssignment> assignments)
    {
        var totalWeight =
            assignments.Sum(a => a.AssignedWeight);

        if (totalWeight <= 0)
            return 0;

        var weightedSum =
            assignments.Sum(
                a => CalculateAchievementRate(a)
                     * a.AssignedWeight);

        return decimal.Round(
            weightedSum / totalWeight,
            2,
            MidpointRounding.AwayFromZero);
    }

    private static decimal CalculateAchievementRate(
        EmployeeKpiAssignment assignment)
    {
        if (assignment.AssignedTarget <= 0)
            return 0;

        decimal achievement;

        if (string.Equals(
                assignment.CalculationMethod,
                "LowerIsBetter",
                StringComparison.OrdinalIgnoreCase))
        {
            achievement =
                assignment.CurrentValue <= 0
                    ? 100m
                    : assignment.AssignedTarget
                      / assignment.CurrentValue
                      * 100m;
        }
        else
        {
            achievement =
                assignment.CurrentValue
                / assignment.AssignedTarget
                * 100m;
        }

        return Math.Clamp(
            achievement,
            0m,
            100m);
    }

    private static KpiEvaluationResult ResolveKpiRewardPenalty(
        decimal score)
    {
        var normalizedScore =
            Math.Clamp(score, 0m, 100m);

        if (normalizedScore >= 90m)
            return new KpiEvaluationResult("Reward", 200m);

        if (normalizedScore >= 80m)
            return new KpiEvaluationResult("Reward", 100m);

        if (normalizedScore >= 60m)
            return new KpiEvaluationResult("None", 0m);

        return new KpiEvaluationResult("Penalty", 100m);
    }

    private sealed record KpiEvaluationResult(
        string BonusType,
        decimal Amount);

    private static void ValidateAssignmentValues(
        decimal target,
        decimal weight)
    {
        if (target <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(target),
                "KPI target must be greater than 0.");
        }

        if (weight <= 0
            || weight > RequiredTotalWeight)
        {
            throw new ArgumentOutOfRangeException(
                nameof(weight),
                "KPI weight must be greater than 0 " +
                "and must not exceed 100%.");
        }
    }

    private static void ValidateTotalWeight(
        decimal totalWeight,
        int employeeId)
    {
        if (Math.Abs(
                totalWeight
                - RequiredTotalWeight)
            <= WeightTolerance)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Total KPI weight for employee {employeeId} " +
            $"must equal 100%. " +
            $"Current total: {totalWeight:N2}%.");
    }

    private static bool HasProgressStarted(
        EmployeeKpiAssignment assignment)
    {
        return assignment.IsLocked
               || assignment.CurrentValue > 0
               || assignment.PendingValue.HasValue
               || !string.Equals(
                   assignment.Status,
                   "Not Started",
                   StringComparison.OrdinalIgnoreCase);
    }

    private sealed record PeriodAssignmentReplacement(
        int EmployeeId,
        List<EmployeeKpiAssignment> Assignments);

    private static bool TryParseMonth(string? month, out DateTime startDate, out DateTime endDate)
    {
        startDate = default;
        endDate = default;

        if (string.IsNullOrWhiteSpace(month))
            return false;

        if (!DateTime.TryParseExact(month, MonthFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return false;

        startDate = new DateTime(parsed.Year, parsed.Month, 1);
        endDate = startDate.AddMonths(1).AddDays(-1);
        return true;
    }

    private static double CalculateMtdPercent(List<EmployeeKpiAssignment> assignments)
    {
        if (assignments.Count == 0)
            return 0;

        // Weighted progress: each KPI contributes its AssignedWeight-share
        // of its own target-completion percentage.
        var totalWeight = assignments.Sum(a => a.AssignedWeight);
        if (totalWeight <= 0)
            return 0;

        var weightedSum = assignments.Sum(a =>
        {
            var progress = a.AssignedTarget <= 0 ? 0 : Math.Min(100, (double)(a.CurrentValue / a.AssignedTarget) * 100);
            return progress * (double)a.AssignedWeight;
        });

        return weightedSum / (double)totalWeight;
    }

    private static string ResolveStatus(decimal target, decimal currentValue, string existingStatus)
    {
        // Don't override a manually-set terminal/administrative status
        // (Pending Approval, Completed, Rejected, Cancelled) via a plain
        // progress update - only move between Not Started / In Progress.
        if (existingStatus is "Pending Approval" or "Completed" or "Rejected" or "Cancelled")
            return existingStatus;

        if (currentValue <= 0)
            return "Not Started";

        if (target > 0 && currentValue >= target)
            return "Completed";

        return "In Progress";
    }

    private static AssignedKpiDto MapAssignedKpiDto(EmployeeKpiAssignment a)
    {
        return new AssignedKpiDto
        {
            AssignmentId = a.AssignmentId,
            KpiSetDetailId = a.KpiSetDetailId,
            KpiId = 0,
            KpiName = a.KpiName ?? string.Empty,
            MeasurementUnit = a.MeasurementUnit ?? string.Empty,
            AssignedTarget = a.AssignedTarget,
            AssignedWeight = a.AssignedWeight,
            CurrentValue = a.CurrentValue,
            PendingValue = a.PendingValue,
            Status = a.Status,
            StartDate = a.StartDate,
            EndDate = a.EndDate,
            IsLocked = a.IsLocked
        };
    }

    private static string BuildEmployeeCode(int employeeId) => $"EMP{employeeId:D4}";

    private List<Employee> GetScopedEmployees(int? scopedDepartmentId)
    {
        if (scopedDepartmentId.HasValue)
            return _employeeRepository.GetByDepartment(scopedDepartmentId.Value).ToList();

        return _employeeRepository.GetAll().ToList();
    }

    /// <summary>
    /// Manager-role users are always scoped to their own department
    /// (Employee.Department_ID) regardless of what was requested from the
    /// UI - this is the real, server-side enforcement of department
    /// scoping described in HRManagement_PROJECT_GUIDE.md �1 / �7.
    /// </summary>
    private int? ResolveScopedDepartmentId(int? requestedDepartmentId)
    {
        var currentUser = _sessionManager.CurrentUser;

        if (currentUser != null && IsManager(currentUser))
            return currentUser.Employee.DepartmentId;

        return requestedDepartmentId is > 0 ? requestedDepartmentId : null;
    }

    private static bool IsManager(CurrentUser currentUser) =>
        currentUser.Role.RoleName.Equals("Manager", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Correct current-account resolution for logging: Account.AccountId,
    /// not Employee.EmployeeId (see the EmployeeId-as-AccountId bug noted
    /// against ManageProfilesService/ContractService - not repeated here).
    /// Falls back to 0 (unauthenticated) when there is no active session.
    /// </summary>
    private void EnsureAdmin()
    {
        var currentUser =
            _sessionManager.CurrentUser
            ?? throw new UnauthorizedAccessException(
                "An authenticated user is required.");

        if (!currentUser.Role.RoleName.Equals(
                "Admin",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException(
                "Only Admin can manage KPI assignments.");
        }
    }

    private void EnsureCanApproveEmployee(
        int employeeId)
    {
        var currentUser =
            _sessionManager.CurrentUser
            ?? throw new UnauthorizedAccessException(
                "An authenticated user is required.");

        if (currentUser.Role.RoleName.Equals(
                "Admin",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!currentUser.Role.RoleName.Equals(
                "Manager",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException(
                "Only Manager or Admin can approve KPI progress.");
        }

        var employee =
            _employeeRepository
                .GetAll()
                .FirstOrDefault(
                    item =>
                        item.EmployeeId == employeeId)
            ?? throw new InvalidOperationException(
                "Employee was not found.");

        if (employee.DepartmentId
            != currentUser.Employee.DepartmentId)
        {
            throw new UnauthorizedAccessException(
                "Manager can approve KPI progress only for employees " +
                "in their department.");
        }
    }

    private void EnsureManagerOrAdmin()
    {
        var currentUser =
            _sessionManager.CurrentUser
            ?? throw new UnauthorizedAccessException(
                "An authenticated user is required.");

        var roleName =
            currentUser.Role.RoleName;

        if (!roleName.Equals(
                "Admin",
                StringComparison.OrdinalIgnoreCase)
            && !roleName.Equals(
                "Manager",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException(
                "Only Manager or Admin can approve KPI progress.");
        }
    }

    private int CurrentAccountId() => _sessionManager.CurrentUser?.Account.AccountId ?? 0;
}
