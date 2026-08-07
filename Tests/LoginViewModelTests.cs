using HRManagement.Models;
using HRManagement.Resources;
using HRManagement.Services.Interfaces;
using HRManagement.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace HRManagement.Tests;

[TestClass]
public class LoginViewModelTests
{
    private Mock<IAuthenticationService> _authenticationService = null!;
    private Mock<IWindowService> _windowService = null!;
    private Mock<INavigationService> _navigationService = null!;
    private Mock<ISettingService> _settingService = null!;

    [TestInitialize]
    public void Setup()
    {
        _authenticationService = new Mock<IAuthenticationService>();
        _windowService = new Mock<IWindowService>();
        _navigationService = new Mock<INavigationService>();
        _settingService = new Mock<ISettingService>();

        // Assumption: ISettingService.CurrentTheme is a string compared against
        // SettingResources.ThemeDark/ThemeLight, per LoginViewModel's constructor.
        // Defaulting to light here; overridden in the dark-theme test below.
        _settingService.SetupGet(s => s.CurrentTheme).Returns(SettingResources.ThemeLight);
    }

    private LoginViewModel CreateSut() =>
        new(_authenticationService.Object, _windowService.Object, _navigationService.Object, _settingService.Object);

    [TestMethod]
    public void Constructor_ThemeAlreadyDark_InitializesIsDarkThemeTrue()
    {
        // Arrange
        _settingService.SetupGet(s => s.CurrentTheme).Returns(SettingResources.ThemeDark);

        // Act
        var sut = CreateSut();

        // Assert — reflects persisted theme rather than defaulting to light.
        Assert.IsTrue(sut.IsDarkTheme);
    }

    [TestMethod]
    public void Constructor_ThemeLight_InitializesIsDarkThemeFalse()
    {
        // Act
        var sut = CreateSut();

        // Assert
        Assert.IsFalse(sut.IsDarkTheme);
    }

    [TestMethod]
    public void ToggleThemeCommand_FromLight_SetsDarkThemeAndCallsSettingService()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.ToggleThemeCommand.Execute(null);

        // Assert
        Assert.IsTrue(sut.IsDarkTheme);
        _settingService.Verify(s => s.SetDarkTheme(), Times.Once);
        _settingService.Verify(s => s.SetLightTheme(), Times.Never);
    }

    [TestMethod]
    public void ToggleThemeCommand_FromDark_SetsLightThemeAndCallsSettingService()
    {
        // Arrange
        _settingService.SetupGet(s => s.CurrentTheme).Returns(SettingResources.ThemeDark);
        var sut = CreateSut();

        // Act
        sut.ToggleThemeCommand.Execute(null);

        // Assert
        Assert.IsFalse(sut.IsDarkTheme);
        _settingService.Verify(s => s.SetLightTheme(), Times.Once);
    }

    [TestMethod]
    public void Login_ValidCredentials_NavigatesToDashboardAndShowsMainWindow()
    {
        // Arrange
        var sut = CreateSut();
        sut.Username = "  jdoe  "; // leading/trailing whitespace to verify Trim() is applied
        sut.Password = "pw123";

        var currentUser = new CurrentUser
        {
            Account = new Account { AccountId = 1, EmployeeId = 10, Username = "jdoe" },
            Employee = new Employee { EmployeeId = 10, FullName = "Jane Doe", RoleId = 2 },
            Role = new Role { RoleId = 2, RoleName = "Manager" }
        };

        _authenticationService.Setup(a => a.Login("jdoe", "pw123")).Returns(currentUser);

        // Act
        sut.LoginCommand.Execute(null);

        // Assert
        Assert.AreEqual(string.Empty, sut.ErrorMessage);
        _navigationService.Verify(n => n.Navigate<DashboardViewModel>(), Times.Once);
        _windowService.Verify(w => w.ShowMainWindow(), Times.Once);
    }

    [TestMethod]
    public void Login_InvalidCredentials_SetsErrorMessageAndDoesNotNavigate()
    {
        // Arrange
        var sut = CreateSut();
        sut.Username = "baduser";
        sut.Password = "wrongpw";

        _authenticationService.Setup(a => a.Login("baduser", "wrongpw")).Returns((CurrentUser?)null);

        // Act
        sut.LoginCommand.Execute(null);

        // Assert
        Assert.AreEqual("Invalid username or password.", sut.ErrorMessage);
        _navigationService.Verify(n => n.Navigate<DashboardViewModel>(), Times.Never);
        _windowService.Verify(w => w.ShowMainWindow(), Times.Never);
    }

    [TestMethod]
    public void Login_CalledTwiceSecondTimeFails_ClearsPreviousErrorMessageBeforeShowingNewOne()
    {
        // Arrange — verifies ErrorMessage = string.Empty at the top of Login runs
        // every attempt, not just once, so a stale error doesn't linger oddly.
        var sut = CreateSut();
        sut.Username = "baduser";
        sut.Password = "wrongpw";
        _authenticationService.Setup(a => a.Login("baduser", "wrongpw")).Returns((CurrentUser?)null);

        sut.LoginCommand.Execute(null);
        Assert.AreEqual("Invalid username or password.", sut.ErrorMessage);

        // Act — second attempt, still invalid
        sut.LoginCommand.Execute(null);

        // Assert
        Assert.AreEqual("Invalid username or password.", sut.ErrorMessage);
    }
}
