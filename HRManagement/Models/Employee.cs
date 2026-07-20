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
}