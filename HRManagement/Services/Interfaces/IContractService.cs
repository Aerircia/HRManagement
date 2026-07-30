using HRManagement.Models;

namespace HRManagement.Services.Interfaces;

/// <summary>
/// Read model for the "Contract" page - the signed-in employee's own
/// contract document, plus the display-only employer/employee fields
/// used to render the printable agreement text.
/// </summary>
public class ContractData
{
    public Employee Employee { get; set; } = null!;
    public string DepartmentName { get; set; } = string.Empty;
    public Contract? Contract { get; set; }
    public string RoleName { get; set; } = string.Empty;

    public string EmployerName { get; set; } = string.Empty;
    public string EmployerAddress { get; set; } = string.Empty;
    public string EmployeeAddress { get; set; } = string.Empty;
    public string NoticePeriodDays { get; set; } = string.Empty;

    public bool HasContract => Contract != null;
}

public interface IContractService
{
    /// <summary>
    /// Loads the given employee's current contract document data. Returns
    /// null if the employee cannot be found (a found employee with no
    /// contract still returns a ContractData with HasContract == false).
    /// </summary>
    ContractData? GetContract(int employeeId);
}
