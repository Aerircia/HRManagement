namespace HRManagement.Models
{
    // Read-only projection returned by IContractService for display in
    // Manage Contracts. Mirrors EmployeeProfileItemModel's role in Manage
    // Profiles: the ViewModel binds to this, never to the raw Contract
    // entity or a repository.
    public class ContractItemModel
    {
        public Contract Contract { get; set; } = null!;

        public string EmployeeName { get; set; } = string.Empty;

        public string RoleName { get; set; } = string.Empty;

        public string ContractType { get; set; } = string.Empty;

        public string StartDateDisplay { get; set; } = string.Empty;

        public string EndDateDisplay { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string BaseSalaryDisplay { get; set; } = string.Empty;
    }
}
