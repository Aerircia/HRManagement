using System.Collections.Generic;

namespace HRManagement.Models
{
    public class ContractDetailModel
    {
        public Employee Employee { get; set; } = null!;

        public string DepartmentName { get; set; } = string.Empty;

        public bool HasContract { get; set; }

        public Contract? Contract { get; set; }

        public string RoleName { get; set; } = string.Empty;

        public string EmployerName { get; set; } = string.Empty;

        public string EmployerAddress { get; set; } = string.Empty;

        public string EmployeeAddress { get; set; } = string.Empty;

        public string NoticePeriodDays { get; set; } = string.Empty;
    }
}
