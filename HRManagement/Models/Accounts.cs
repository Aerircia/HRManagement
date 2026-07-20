namespace HRManagement.Models;

public class Account
{
    public int AccountId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public int RoleId { get; set; }

    public int EmployeeId { get; set; }
}