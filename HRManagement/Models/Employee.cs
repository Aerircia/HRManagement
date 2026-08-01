namespace HRManagement.Models;

public class Employee
{
    public int EmployeeId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public DateTime DateOfBirth { get; set; }

    public string? Phone { get; set; }

    public string Email { get; set; } = string.Empty;

    public int RoleId { get; set; }

    public int DepartmentId { get; set; }

    public DateTime HireDate { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? Avatar { get; set; }

    // Added for the Profile page's Personal Details card. Requires a
    // matching nullable NVARCHAR(255) "Address" column on the Employee
    // table - see the SQL note at the top of EmployeeRepository.cs.
    public string? Address { get; set; }
}
