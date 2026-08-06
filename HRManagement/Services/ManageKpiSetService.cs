using HRManagement.Models;
using HRManagement.Repositories.Interfaces;
using HRManagement.Services.Interfaces;

namespace HRManagement.Services;

/// <summary>
/// Provides CRUD operations for KPI Sets (packs of KPIs), restricted to Admin role.
/// Orchestrates KpiSet and KpiSetDetail operations, logging changes under the current user.
/// </summary>
public class ManageKpiSetService : IManageKpiSetService
{
    // Admin only
    private const int AdminRoleId = 1;

    private readonly IKpiSetRepository _kpiSetRepository;
    private readonly IKpiRepository _kpiRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly SessionManager _sessionManager;
    private readonly ILogService _logService;

    public ManageKpiSetService(
        IKpiSetRepository kpiSetRepository,
        IKpiRepository kpiRepository,
        IDepartmentRepository departmentRepository,
        SessionManager sessionManager,
        ILogService logService)
    {
        _kpiSetRepository = kpiSetRepository
            ?? throw new ArgumentNullException(nameof(kpiSetRepository));
        _kpiRepository = kpiRepository
            ?? throw new ArgumentNullException(nameof(kpiRepository));
        _departmentRepository = departmentRepository
            ?? throw new ArgumentNullException(nameof(departmentRepository));
        _sessionManager = sessionManager
            ?? throw new ArgumentNullException(nameof(sessionManager));
        _logService = logService
            ?? throw new ArgumentNullException(nameof(logService));
    }

    public bool CurrentUserHasAccess()
    {
        var currentRoleId = _sessionManager.CurrentUser?.Employee?.RoleId;
        return currentRoleId.HasValue && currentRoleId.Value == AdminRoleId;
    }

    public List<KpiSet> GetKpiSets()
    {
        return _kpiSetRepository.GetAll();
    }

    public KpiSetWithDetails? GetKpiSetById(int kpiSetId)
    {
        var kpiSet = _kpiSetRepository.GetById(kpiSetId);
        if (kpiSet == null)
            return null;

        var details = _kpiSetRepository.GetDetails(kpiSetId);
        return new KpiSetWithDetails
        {
            KpiSet = kpiSet,
            Details = details
        };
    }

    public List<Kpi> GetAvailableKpis()
    {
        return _kpiRepository.GetAll();
    }

    public List<Department> GetDepartments()
    {
        return _departmentRepository.GetAll();
    }

    public KpiSet SaveKpiSet(KpiSetInput input)
    {
        ValidateInput(input);

        if (input.KpiSetId == 0)
        {
            // Create new KPI Set
            var currentUser = _sessionManager.CurrentUser
                ?? throw new InvalidOperationException(
                    "Your session could not be found. Please log in again.");

            var kpiSet = new KpiSet
            {
                KpiSetName = input.KpiSetName.Trim(),
                Description = input.Description?.Trim(),
                DepartmentId = input.DepartmentId,
                IsActive = input.IsActive,
                CreatedAt = DateTime.Now
            };

            var newId = _kpiSetRepository.Add(kpiSet);
            kpiSet.KpiSetId = newId;

            // Add KPI details
            foreach (var detailInput in input.Details)
            {
                var detail = new KpiSetDetail
                {
                    KpiSetId = newId,
                    KpiId = detailInput.KpiId,
                    TargetValue = detailInput.TargetValue,
                    Weight = detailInput.Weight
                };

                _kpiSetRepository.AddDetail(detail);
            }

            _logService.WriteLog(
                currentUser.Account.AccountId,
                $"Created KPI Set: {kpiSet.KpiSetName} with {input.Details.Count} KPIs");

            return kpiSet;
        }
        else
        {
            // Update existing KPI Set
            var existing = _kpiSetRepository.GetById(input.KpiSetId)
                ?? throw new InvalidOperationException("KPI Set could not be found.");

            existing.KpiSetName = input.KpiSetName.Trim();
            existing.Description = input.Description?.Trim();
            existing.DepartmentId = input.DepartmentId;
            existing.IsActive = input.IsActive;

            _kpiSetRepository.Update(existing);

            // Handle detail updates: delete old ones not in input, add new ones, update existing
            var existingDetails = _kpiSetRepository.GetDetails(input.KpiSetId);

            // Delete details that are no longer in the input
            var detailsToDelete = existingDetails
                .Where(ed => !input.Details.Any(id => id.KpiSetDetailId == ed.KpiSetDetailId))
                .ToList();
            foreach (var detail in detailsToDelete)
            {
                _kpiSetRepository.DeleteDetail(detail.KpiSetDetailId);
            }

            // Add or update details
            foreach (var detailInput in input.Details)
            {
                if (detailInput.KpiSetDetailId == 0)
                {
                    // New detail
                    var newDetail = new KpiSetDetail
                    {
                        KpiSetId = input.KpiSetId,
                        KpiId = detailInput.KpiId,
                        TargetValue = detailInput.TargetValue,
                        Weight = detailInput.Weight
                    };
                    _kpiSetRepository.AddDetail(newDetail);
                }
                else
                {
                    // Update existing detail
                    var detailToUpdate = new KpiSetDetail
                    {
                        KpiSetDetailId = detailInput.KpiSetDetailId,
                        KpiSetId = input.KpiSetId,
                        KpiId = detailInput.KpiId,
                        TargetValue = detailInput.TargetValue,
                        Weight = detailInput.Weight
                    };
                    _kpiSetRepository.UpdateDetail(detailToUpdate);
                }
            }

            _logService.WriteLog(
                CurrentAccountId(),
                $"Updated KPI Set: {existing.KpiSetName}");

            return existing;
        }
    }

    public void DeleteKpiSet(int kpiSetId, string kpiSetName)
    {
        // First delete all details associated with this set
        var details = _kpiSetRepository.GetDetails(kpiSetId);
        foreach (var detail in details)
        {
            _kpiSetRepository.DeleteDetail(detail.KpiSetDetailId);
        }

        // Then delete the KPI Set itself
        _kpiSetRepository.Delete(kpiSetId);

        _logService.WriteLog(
            CurrentAccountId(),
            $"Deleted KPI Set: {kpiSetName}");
    }

    private void ValidateInput(KpiSetInput input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        if (string.IsNullOrWhiteSpace(input.KpiSetName))
            throw new ArgumentException("KPI Set name is required.", nameof(input.KpiSetName));

        if (input.KpiSetName.Length > 200)
            throw new ArgumentException("KPI Set name cannot exceed 200 characters.", nameof(input.KpiSetName));

        if (input.Description?.Length > 500)
            throw new ArgumentException("Description cannot exceed 500 characters.", nameof(input.Description));

        if (input.Details.Count == 0)
            throw new ArgumentException("At least one KPI must be added to the set.", nameof(input.Details));

        // Validate each detail
        foreach (var detail in input.Details)
        {
            if (detail.KpiId <= 0)
                throw new ArgumentException("Each KPI detail must reference a valid KPI.", nameof(input.Details));

            if (detail.TargetValue < 0)
                throw new ArgumentException("Target value cannot be negative.", nameof(input.Details));

            if (detail.Weight < 0)
                throw new ArgumentException("Weight cannot be negative.", nameof(input.Details));
        }

        // Optional: validate that total weight sums to expected value (e.g., 100)
        // For now, we allow flexible weighting
    }

    private int CurrentAccountId() =>
        _sessionManager.CurrentUser?.Account?.AccountId
        ?? throw new InvalidOperationException("Current user account not found.");
}
