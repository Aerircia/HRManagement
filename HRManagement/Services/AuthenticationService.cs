using HRManagement.Models;
using HRManagement.Repositories.Interfaces;
using HRManagement.Services.Interfaces;

namespace HRManagement.Services;

public class AuthenticationService(
    IAccountRepository accountRepository,
    IEmployeeRepository employeeRepository,
    IRoleRepository roleRepository,
    SessionManager sessionManager,
    ILogService logService) : IAuthenticationService
{
    private readonly IAccountRepository _accountRepository = accountRepository;
    private readonly IEmployeeRepository _employeeRepository = employeeRepository;
    private readonly IRoleRepository _roleRepository = roleRepository;
    private readonly SessionManager _sessionManager = sessionManager;
    private readonly ILogService _logService = logService;

    public CurrentUser? Login(string username, string password)
    {
        var account = _accountRepository.Login(username, password);

        if (account == null)
        {
            _logService.WriteLog(0, $"Failed login attempt for username: {username}");
            return null;
        }

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

        _logService.WriteLog(account.AccountId, $"User logged in: {employee.FullName}");

        return currentUser;
    }

    public void Logout()
    {
        var currentUser = _sessionManager.CurrentUser;

        _sessionManager.Logout();

        if (currentUser != null)
        {
            _logService.WriteLog(currentUser.Account.AccountId, $"User logged out: {currentUser.Employee.FullName}");
        }
    }
}
