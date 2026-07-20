namespace HRManagement.Models;

public class CurrentUser
{
    public Account Account { get; init; } = null!;

    public Employee Employee { get; init; } = null!;

    public Role Role { get; init; } = null!;
}