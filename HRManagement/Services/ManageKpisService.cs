using HRManagement.Models;
using HRManagement.Repositories.Interfaces;
using HRManagement.Services.Interfaces;

namespace HRManagement.Services;

public class ManageKpisService : IManageKpisService
{
    // Admin only
    private const int AdminRoleId = 1;

    private static readonly List<string> KpiTypeOptions = ["Financial", "Quality", "Productivity"];

    private static readonly List<string> MeasurementUnitOptions =
        ["USD", "Percent", "Review", "Candidate", "Ticket", "Task"];

    private static readonly List<string> CalculationMethodOptions = ["HigherIsBetter", "LowerIsBetter"];

    private readonly IKpiRepository _kpiRepository;
    private readonly IKpiSetRepository _kpiSetRepository;
    private readonly SessionManager _sessionManager;
    private readonly ILogService _logService;

    public ManageKpisService(
        IKpiRepository kpiRepository,
        IKpiSetRepository kpiSetRepository,
        SessionManager sessionManager,
        ILogService logService)
    {
        _kpiRepository = kpiRepository
            ?? throw new ArgumentNullException(nameof(kpiRepository));

        _kpiSetRepository = kpiSetRepository
            ?? throw new ArgumentNullException(nameof(kpiSetRepository));

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

    public List<KpiRow> GetKpis()
    {
        return _kpiRepository.GetAll()
            .Select(ToRow)
            .ToList();
    }

    public List<string> GetKpiTypeOptions() => KpiTypeOptions;
    public List<string> GetMeasurementUnitOptions() => MeasurementUnitOptions;
    public List<string> GetCalculationMethodOptions() => CalculationMethodOptions;

    public KpiRow SaveKpi(KpiInput input)
    {
        ValidateInput(input);

        if (input.KpiId == 0)
        {
            var kpi = new Kpi
            {
                KpiName = input.KpiName.Trim(),
                Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim(),
                KpiType = input.KpiType,
                MeasurementUnit = input.MeasurementUnit,
                DefaultTarget = input.DefaultTarget,
                CalculationMethod = input.CalculationMethod,
                IsActive = input.IsActive
            };

            _kpiRepository.Add(kpi);

            _logService.WriteLog(
                CurrentAccountId(),
                $"Created KPI: {kpi.KpiName}");

            return ToRow(kpi);
        }
        else
        {
            var existing = _kpiRepository.GetById(input.KpiId)
                ?? throw new InvalidOperationException("KPI could not be found.");

            existing.KpiName = input.KpiName.Trim();
            existing.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
            existing.KpiType = input.KpiType;
            existing.MeasurementUnit = input.MeasurementUnit;
            existing.DefaultTarget = input.DefaultTarget;
            existing.CalculationMethod = input.CalculationMethod;
            existing.IsActive = input.IsActive;

            _kpiRepository.Update(existing);

            _logService.WriteLog(
                CurrentAccountId(),
                $"Updated KPI: {existing.KpiName}");

            return ToRow(existing);
        }
    }

    public void DeleteKpi(int kpiId, string kpiName)
    {
        if (IsUsedInAnyKpiSet(kpiId))
        {
            throw new InvalidOperationException(
                $"{kpiName} is still used in one or more KPI packs. " +
                "Remove it from those packs before deleting.");
        }

        _kpiRepository.Delete(kpiId);

        _logService.WriteLog(
            CurrentAccountId(),
            $"Deleted KPI: {kpiName}");
    }

    private bool IsUsedInAnyKpiSet(int kpiId)
    {
        return _kpiSetRepository.GetAll()
            .SelectMany(set => _kpiSetRepository.GetDetails(set.KpiSetId))
            .Any(detail => detail.KpiId == kpiId);
    }

    private static KpiRow ToRow(Kpi kpi)
    {
        return new KpiRow
        {
            Kpi = kpi,
            KpiName = kpi.KpiName,
            Description = kpi.Description,
            KpiType = kpi.KpiType,
            MeasurementUnit = kpi.MeasurementUnit,
            DefaultTarget = kpi.DefaultTarget,
            CalculationMethod = kpi.CalculationMethod,
            IsActive = kpi.IsActive
        };
    }

    private int CurrentAccountId() =>
        _sessionManager.CurrentUser!.Account.AccountId;

    private static void ValidateInput(KpiInput input)
    {
        if (string.IsNullOrWhiteSpace(input.KpiName))
            throw new ArgumentException("KPI name is required.");

        if (string.IsNullOrWhiteSpace(input.KpiType))
            throw new ArgumentException("KPI type is required.");

        if (string.IsNullOrWhiteSpace(input.MeasurementUnit))
            throw new ArgumentException("Measurement unit is required.");

        if (string.IsNullOrWhiteSpace(input.CalculationMethod))
            throw new ArgumentException("Calculation method is required.");

        if (input.DefaultTarget < 0)
            throw new ArgumentException("Default target cannot be negative.");
    }
}
