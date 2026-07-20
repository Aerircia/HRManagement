using HRManagement.Models;
using HRManagement.Repositories;
using HRManagement.Services.Interfaces;

namespace HRManagement.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly AccountRepository _accountRepository;
    private readonly EmployeeRepository _employeeRepository;
    private readonly RoleRepository _roleRepository;
    private readonly SessionManager _sessionManager;

    public AuthenticationService(
        AccountRepository accountRepository,
        EmployeeRepository employeeRepository,
        RoleRepository roleRepository,
        SessionManager sessionManager)
    {
        _accountRepository = accountRepository;
        _employeeRepository = employeeRepository;
        _roleRepository = roleRepository;
        _sessionManager = sessionManager;
    }

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