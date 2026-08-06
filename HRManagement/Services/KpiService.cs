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

    public void AssignKpis(AssignKpiRequest request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        if (!TryParseMonth(request.Month, out var startDate, out var endDate))
            throw new ArgumentException($"Invalid month '{request.Month}'. Expected format {MonthFormat}.", nameof(request));

        var accountId = CurrentAccountId();
        var insertedCount = 0;
        var updatedCount = 0;

        foreach (var employeeAssignment in request.EmployeeAssignments)
        {
            // Load once per employee so re-assigning an already-assigned
            // KPI (same employee + KPI_Set_Detail + period) updates the
            // existing row instead of violating
            // UQ_Assignment_EmployeeKPI and crashing.
            var existingForEmployee = _assignmentRepository
                .GetByEmployeeAndPeriod(employeeAssignment.EmployeeId, startDate, endDate)
                .ToDictionary(a => a.KpiSetDetailId);

            foreach (var selection in employeeAssignment.SelectedDetails)
            {
                if (existingForEmployee.TryGetValue(selection.KpiSetDetailId, out var existing))
                {
                    if (existing.IsLocked)
                        continue; // locked assignments are not touched by re-assignment

                    existing.AssignedTarget = selection.AssignedTarget;
                    existing.AssignedWeight = selection.AssignedWeight;

                    _assignmentRepository.Update(existing);
                    updatedCount++;
                }
                else
                {
                    var assignment = new EmployeeKpiAssignment
                    {
                        EmployeeId = employeeAssignment.EmployeeId,
                        KpiSetDetailId = selection.KpiSetDetailId,
                        AssignedTarget = selection.AssignedTarget,
                        AssignedWeight = selection.AssignedWeight,
                        CurrentValue = 0,
                        PendingValue = null,
                        Status = "Not Started",
                        StartDate = startDate,
                        EndDate = endDate,
                        IsLocked = false
                    };

                    _assignmentRepository.Add(assignment);
                    insertedCount++;
                }
            }
        }

        _logService.WriteLog(accountId,
            $"Assigned KPI set {request.KpiSetId} to {request.EmployeeAssignments.Count} employee(s) for {request.Month} " +
            $"({insertedCount} new, {updatedCount} updated).");
    }

    public void UpdateAssignment(UpdateKpiAssignmentRequest request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        var existing = _assignmentRepository.GetById(request.AssignmentId);
        if (existing == null)
            return;

        var changes = new List<string>();

        if (existing.AssignedTarget != request.AssignedTarget)
            changes.Add($"Target {existing.AssignedTarget} -> {request.AssignedTarget}");

        if (existing.AssignedWeight != request.AssignedWeight)
            changes.Add($"Weight {existing.AssignedWeight} -> {request.AssignedWeight}");

        if (existing.CurrentValue != request.CurrentValue)
            changes.Add($"Current value {existing.CurrentValue} -> {request.CurrentValue}");

        existing.AssignedTarget = request.AssignedTarget;
        existing.AssignedWeight = request.AssignedWeight;
        existing.CurrentValue = request.CurrentValue;
        existing.Status = ResolveStatus(existing.AssignedTarget, existing.CurrentValue, existing.Status);

        _assignmentRepository.Update(existing);

        var accountId = CurrentAccountId();
        var changeSummary = changes.Count > 0 ? string.Join("; ", changes) : "no field changes";
        _logService.WriteLog(accountId, $"Updated KPI assignment {existing.AssignmentId} ({changeSummary}).");
    }

    public void DeleteAssignment(int assignmentId)
    {
        var existing = _assignmentRepository.GetById(assignmentId);

        _assignmentRepository.Delete(assignmentId);

        var accountId = CurrentAccountId();
        var description = existing != null
            ? $"Deleted KPI assignment {assignmentId} ({existing.KpiName}) for employee {existing.EmployeeId}."
            : $"Deleted KPI assignment {assignmentId}.";

        _logService.WriteLog(accountId, description);
    }

    // ============================================================
    // Personal KPI page (Employee self-service)
    // ============================================================

    public List<AssignedKpiDto> GetMyAssignedKpis(int employeeId, string month)
    {
        return GetAssignedKpis(employeeId, month);
    }

    public void UpdateMyProgress(int assignmentId, int employeeId, decimal currentValue)
    {
        var existing = _assignmentRepository.GetById(assignmentId);
        if (existing == null)
            return;

        // Enforce server-side that an employee can only update their own
        // assignment - the UI already scopes to the logged-in employee,
        // but that is cosmetic only.
        if (existing.EmployeeId != employeeId)
            return;

        var previousValue = existing.CurrentValue;

        // Only Current_Value may change here - all other fields are left
        // untouched, per the self-service contract.
        existing.CurrentValue = currentValue;
        existing.Status = ResolveStatus(existing.AssignedTarget, currentValue, existing.Status);

        _assignmentRepository.Update(existing);

        var accountId = CurrentAccountId();
        _logService.WriteLog(accountId,
            $"Updated progress on KPI assignment {assignmentId} ({existing.KpiName}): {previousValue} -> {currentValue}.");
    }

    // ============================================================
    // Helpers
    // ============================================================

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
    private int CurrentAccountId() => _sessionManager.CurrentUser?.Account.AccountId ?? 0;
}
