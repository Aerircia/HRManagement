using HRManagement.Repositories.Interfaces;
using HRManagement.Services.Interfaces;

namespace HRManagement.Services;

public class ContractService : IContractService
{
    private readonly IContractRepository _contractRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IRoleRepository _roleRepository;

    public ContractService(
        IContractRepository contractRepository,
        IEmployeeRepository employeeRepository,
        IDepartmentRepository departmentRepository,
        IRoleRepository roleRepository)
    {
        _contractRepository = contractRepository;
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _roleRepository = roleRepository;
    }

    public ContractData? GetContract(int employeeId)
    {
        var employee = _employeeRepository.GetById(employeeId);
        if (employee == null)
            return null;

        var department = _departmentRepository.GetById(employee.DepartmentId);

        var data = new ContractData
        {
            Employee = employee,
            DepartmentName = department?.DepartmentName ?? $"Department #{employee.DepartmentId}"
        };

        var contract = _contractRepository.GetCurrentByEmployeeId(employeeId);
        data.Contract = contract;

        if (contract == null)
            return data;

        data.RoleName = _roleRepository.GetById(contract.RoleId)?.RoleName ?? "—";

        // Static placeholder company/legal fields used to render the
        // printable agreement text (ContractView.xaml). Not employee- or
        // contract-specific data, so they live here rather than on a model.
        data.EmployerName = "My Company Inc.";
        data.EmployerAddress = "123 Business Rd, Suite 400, Tech City, ST 12345";
        data.EmployeeAddress = "987 Residential Ave, Apt 2B, Home City, ST 54321";
        data.NoticePeriodDays = "14";

        return data;
    }
}
