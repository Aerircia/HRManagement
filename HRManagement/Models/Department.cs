namespace HRManagement.Models;

public class Department
{
    public int DepartmentId { get; set; }

    public string DepartmentName { get; set; } = string.Empty;
}

public class DepartmentRow
{
    public Department Department { get; set; } = null!;
    public string DepartmentName { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
}