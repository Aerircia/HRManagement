namespace HRManagement.Models
{
    // Plain input DTO passed from ManageContractsViewModel into
    // IContractService for both Add and Update (ContractId == 0 => Add).
    // Mirrors EmployeeProfileInput's role in Manage Profiles.
    public class ContractInput
    {
        public int ContractId { get; set; }

        public int EmployeeId { get; set; }

        public int RoleId { get; set; }

        public string ContractType { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string Status { get; set; } = string.Empty;

        public decimal BaseSalary { get; set; }
    }
}
