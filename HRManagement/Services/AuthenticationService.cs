using HRManagement.Models;
using HRManagement.Repositories.Interfaces;
using HRManagement.Services.Interfaces;

namespace HRManagement.Services;

public class AuthenticationService(
    IAccountRepository accountRepository,
    IEmployeeRepository employeeRepository,
    IRoleRepository roleRepository,
    SessionManager sessionManager) : IAuthenticationService
{
    private readonly IAccountRepository _accountRepository = accountRepository;
    private readonly IEmployeeRepository _employeeRepository = employeeRepository;
    private readonly IRoleRepository _roleRepository = roleRepository;
    private readonly SessionManager _sessionManager = sessionManager;

    public CurrentUser? Login(string username, string password)
    {
        var account = _accountRepository.Login(username, password);

        if (account == null)
            return null;

        var employee = _employeeRepository.GetById(account.EmployeeId);

        if (employee == null)
            return null;

        var role = _roleRepository.GetById(employee.RoleId);

        if (role == null)
            return null;

        var currentUser = new CurrentUser
        {
            Account = account,
            Employee = employee,
            Role = role
        };

        _sessionManager.Login(currentUser);

        return currentUser;
    }

    public void Logout()
    {
        _sessionManager.Logout();
    }
}