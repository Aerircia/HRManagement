namespace HRManagement.Models;

public class Account
{
    public int AccountID { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public int RoleID { get; set; }

    public int EmployeeID { get; set; }
}