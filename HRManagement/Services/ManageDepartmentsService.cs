using HRManagement.Models;
using HRManagement.Repositories.Interfaces;
using HRManagement.Services.Interfaces;

namespace HRManagement.Services;

public class ManageDepartmentsService : IManageDepartmentsService
{
    // Admin only
    private const int AdminRoleId = 1;

    private readonly IDepartmentRepository _departmentRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly SessionManager _sessionManager;
    private readonly ILogService _logService;

    public ManageDepartmentsService(
        IDepartmentRepository departmentRepository,
        IEmployeeRepository employeeRepository,
        SessionManager sessionManager,
        ILogService logService)
    {
        _departmentRepository = departmentRepository
            ?? throw new ArgumentNullException(nameof(departmentRepository));

        _employeeRepository = employeeRepository
            ?? throw new ArgumentNullException(nameof(employeeRepository));

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

    public List<DepartmentRow> GetDepartments()
    {
        return _departmentRepository.GetAll()
            .Select(ToRow)
            .ToList();
    }

    public DepartmentRow SaveDepartment(DepartmentInput input)
    {
        ValidateInput(input);

        if (input.DepartmentId == 0)
        {
            var department = new Department
            {
                DepartmentName = input.DepartmentName.Trim()
            };

            var newId = _departmentRepository.Insert(department);
            department.DepartmentId = newId;

            _logService.WriteLog(
                CurrentAccountId(),
                $"Created department: {department.DepartmentName}");

            return ToRow(department);
        }
        else
        {
            var existing = _departmentRepository.GetById(input.DepartmentId)
                ?? throw new InvalidOperationException("Department could not be found.");

            existing.DepartmentName = input.DepartmentName.Trim();

            _departmentRepository.Update(existing);

            _logService.WriteLog(
                CurrentAccountId(),
                $"Updated department: {existing.DepartmentName}");

            return ToRow(existing);
        }
    }

    public void DeleteDepartment(int departmentId, string departmentName)
    {
        var employeeCount = _employeeRepository.GetByDepartment(departmentId).Count();

        if (employeeCount > 0)
        {
            throw new InvalidOperationException(
                $"{departmentName} still has {employeeCount} employee(s) assigned to it. " +
                "Reassign them to another department before deleting.");
        }

        _departmentRepository.Delete(departmentId);

        _logService.WriteLog(
            CurrentAccountId(),
            $"Deleted department: {departmentName}");
    }

    private DepartmentRow ToRow(Department department)
    {
        return new DepartmentRow
        {
            Department = department,
            DepartmentName = department.DepartmentName,
            EmployeeCount = _employeeRepository.GetByDepartment(department.DepartmentId).Count()
        };
    }

    private int CurrentAccountId() =>
        _sessionManager.CurrentUser!.Account.AccountId;

    private static void ValidateInput(DepartmentInput input)
    {
        if (string.IsNullOrWhiteSpace(input.DepartmentName))
        {
            throw new ArgumentException("Department name is required.");
        }
    }
}
