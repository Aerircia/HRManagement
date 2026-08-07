using HRManagement.Models;
using HRManagement.Repositories.Interfaces;
using HRManagement.Services;
using HRManagement.Services.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace HRManagement.Tests;

[TestClass]
public class AuthenticationServiceTests
{
    private Mock<IAccountRepository> _accountRepository = null!;
    private Mock<IEmployeeRepository> _employeeRepository = null!;
    private Mock<IRoleRepository> _roleRepository = null!;
    private SessionManager _sessionManager = null!; // concrete singleton per project pattern, not mocked
    private Mock<ILogService> _logService = null!;
    private AuthenticationService _sut = null!;

    [TestInitialize]
    public void Setup()
    {
        _accountRepository = new Mock<IAccountRepository>();
        _employeeRepository = new Mock<IEmployeeRepository>();
        _roleRepository = new Mock<IRoleRepository>();
        _sessionManager = new SessionManager();
        _logService = new Mock<ILogService>();

        _sut = new AuthenticationService(
            _accountRepository.Object,
            _employeeRepository.Object,
            _roleRepository.Object,
            _sessionManager,
            _logService.Object);
    }

    [TestMethod]
    public void Login_ValidCredentials_ReturnsCurrentUserAndLogsSuccess()
    {
        // Arrange
        var account = new Account { AccountId = 1, EmployeeId = 10, Username = "jdoe" };
        var employee = new Employee { EmployeeId = 10, FullName = "Jane Doe", RoleId = 2 };
        var role = new Role { RoleId = 2, RoleName = "Manager" };

        _accountRepository.Setup(r => r.Login("jdoe", "pw123")).Returns(account);
        _employeeRepository.Setup(r => r.GetById(10)).Returns(employee);
        _roleRepository.Setup(r => r.GetById(2)).Returns(role);

        // Act
        var result = _sut.Login("jdoe", "pw123");

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(account, result!.Account);
        Assert.AreEqual(employee, result.Employee);
        Assert.AreEqual(role, result.Role);

        _logService.Verify(l => l.WriteLog(1, It.Is<string>(m => m.Contains("Jane Doe"))), Times.Once);
        _logService.Verify(l => l.WriteLog(0, It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public void Login_InvalidCredentials_ReturnsNullAndLogsFailureWithAccountIdZero()
    {
        // Arrange
        _accountRepository.Setup(r => r.Login("baduser", "wrongpw")).Returns((Account?)null);

        // Act
        var result = _sut.Login("baduser", "wrongpw");

        // Assert
        Assert.IsNull(result);
        _logService.Verify(l => l.WriteLog(0, It.Is<string>(m => m.Contains("baduser"))), Times.Once);
    }

    [TestMethod]
    public void Login_AccountFoundButEmployeeMissing_ReturnsNullWithoutLoggingSuccess()
    {
        // Arrange — data-integrity edge case: Account exists but its Employee record does not.
        var account = new Account { AccountId = 1, EmployeeId = 999, Username = "jdoe" };
        _accountRepository.Setup(r => r.Login("jdoe", "pw123")).Returns(account);
        _employeeRepository.Setup(r => r.GetById(999)).Returns((Employee?)null);

        // Act
        var result = _sut.Login("jdoe", "pw123");

        // Assert
        Assert.IsNull(result);
        // No success log should fire; only the failed-login path logs, and that path
        // isn't hit here since the account WAS found. Current code logs nothing in
        // this branch — this test documents that as current behavior.
        _logService.Verify(l => l.WriteLog(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public void Login_AccountAndEmployeeFoundButRoleMissing_ReturnsNull()
    {
        // Arrange
        var account = new Account { AccountId = 1, EmployeeId = 10, Username = "jdoe" };
        var employee = new Employee { EmployeeId = 10, FullName = "Jane Doe", RoleId = 999 };
        _accountRepository.Setup(r => r.Login("jdoe", "pw123")).Returns(account);
        _employeeRepository.Setup(r => r.GetById(10)).Returns(employee);
        _roleRepository.Setup(r => r.GetById(999)).Returns((Role?)null);

        // Act
        var result = _sut.Login("jdoe", "pw123");

        // Assert
        Assert.IsNull(result);
        _logService.Verify(l => l.WriteLog(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public void Logout_ActiveSession_LogsUsingPreClearCurrentUserSnapshot()
    {
        // Arrange
        var account = new Account { AccountId = 5, EmployeeId = 10, Username = "jdoe" };
        var employee = new Employee { EmployeeId = 10, FullName = "Jane Doe", RoleId = 2 };
        var role = new Role { RoleId = 2, RoleName = "Manager" };
        var currentUser = new CurrentUser { Account = account, Employee = employee, Role = role };
        _sessionManager.Login(currentUser);

        // Act
        _sut.Logout();

        // Assert
        // Confirms the fix documented in project memory: CurrentUser is captured
        // BEFORE SessionManager.Logout() clears it, so the AccountId/FullName used
        // in the log message are the real ones, not null/default.
        _logService.Verify(l => l.WriteLog(5, It.Is<string>(m => m.Contains("Jane Doe"))), Times.Once);
        Assert.IsNull(_sessionManager.CurrentUser);
    }

    [TestMethod]
    public void Logout_NoActiveSession_DoesNotThrowAndDoesNotLog()
    {
        // Arrange — CurrentUser already null (no prior login in this test)

        // Act
        _sut.Logout();

        // Assert
        _logService.Verify(l => l.WriteLog(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }
}
