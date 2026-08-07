using HRManagement.Models;
using HRManagement.Services;
using HRManagement.Services.Interfaces;
using HRManagement.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;

namespace HRManagement.Tests;

[TestClass]
public class ManageRequestsViewModelTests
{
    private Mock<IRequestService> _requestService = null!;
    private Mock<IAuthorizationService> _authorizationService = null!;
    private SessionManager _sessionManager = null!;

    [TestInitialize]
    public void Setup()
    {
        _requestService = new Mock<IRequestService>();
        _authorizationService = new Mock<IAuthorizationService>();
        _sessionManager = new SessionManager();

        _sessionManager.Login(new CurrentUser
        {
            Account = new Account { AccountId = 1, EmployeeId = 10, Username = "manager1" },
            Employee = new Employee { EmployeeId = 10, FullName = "Manager One", RoleId = 2, DepartmentId = 3 },
            Role = new Role { RoleId = 2, RoleName = "Manager" }
        });
    }

    private ManageRequestsViewModel CreateSut() =>
        new(_requestService.Object, _authorizationService.Object, _sessionManager);

    private static RequestFormSummary Req(int id, string status, string type = "Day Off", string employeeName = "Employee")
        => new() { RequestId = id, Status = status, RequestType = type, EmployeeName = employeeName };

    // ---- Load: Admin vs non-Admin branch (department scoping entry point) ----

    [TestMethod]
    public void Constructor_UserIsAdmin_CallsGetAllRequestsNotDepartmentFiltered()
    {
        // Arrange
        _authorizationService.SetupGet(a => a.IsAdmin).Returns(true);
        _requestService.Setup(s => s.GetAllRequests()).Returns(new List<RequestFormSummary>());

        // Act
        var sut = CreateSut();

        // Assert
        _requestService.Verify(s => s.GetAllRequests(), Times.Once);
        _requestService.Verify(s => s.GetRequestsByDepartment(It.IsAny<int>()), Times.Never);
    }

    [TestMethod]
    public void Constructor_UserIsNotAdmin_CallsGetRequestsByDepartmentWithCurrentUserDepartment()
    {
        // Arrange — this is the department-scoping entry point: a non-Admin
        // (e.g. Manager) must only ever be loaded via GetRequestsByDepartment,
        // and specifically with THEIR OWN department id, never an arbitrary one.
        _authorizationService.SetupGet(a => a.IsAdmin).Returns(false);
        _requestService.Setup(s => s.GetRequestsByDepartment(3)).Returns(new List<RequestFormSummary>());

        // Act
        var sut = CreateSut();

        // Assert
        _requestService.Verify(s => s.GetRequestsByDepartment(3), Times.Once);
        _requestService.Verify(s => s.GetAllRequests(), Times.Never);
    }

    // ---- Load: counts ----

    [TestMethod]
    public void Constructor_LoadsRequests_ComputesTotalPendingApprovedRejectedCounts()
    {
        // Arrange
        _authorizationService.SetupGet(a => a.IsAdmin).Returns(true);
        _requestService.Setup(s => s.GetAllRequests()).Returns(new List<RequestFormSummary>
        {
            Req(1, "Pending"),
            Req(2, "Pending"),
            Req(3, "Approved"),
            Req(4, "Rejected"),
        });

        // Act
        var sut = CreateSut();

        // Assert
        Assert.AreEqual(4, sut.TotalCount);
        Assert.AreEqual(2, sut.PendingCount);
        Assert.AreEqual(1, sut.ApprovedCount);
        Assert.AreEqual(1, sut.RejectedCount);
        Assert.IsFalse(sut.IsEmpty);
    }

    [TestMethod]
    public void Constructor_NoRequests_SetsIsEmptyTrueAndZeroCounts()
    {
        // Arrange
        _authorizationService.SetupGet(a => a.IsAdmin).Returns(true);
        _requestService.Setup(s => s.GetAllRequests()).Returns(new List<RequestFormSummary>());

        // Act
        var sut = CreateSut();

        // Assert
        Assert.IsTrue(sut.IsEmpty);
        Assert.AreEqual(0, sut.TotalCount);
    }

    // ---- Filtering: status filter ----

    [TestMethod]
    public void StatusFilter_SetToPending_ShowsOnlyPendingRequests()
    {
        // Arrange
        _authorizationService.SetupGet(a => a.IsAdmin).Returns(true);
        _requestService.Setup(s => s.GetAllRequests()).Returns(new List<RequestFormSummary>
        {
            Req(1, "Pending"),
            Req(2, "Approved"),
        });
        var sut = CreateSut();

        // Act
        sut.StatusFilter = "Pending";

        // Assert
        Assert.AreEqual(1, sut.Requests.Count);
        Assert.AreEqual(1, sut.Requests[0].RequestId);
    }

    [TestMethod]
    public void SetStatusFilterCommand_NullParameter_DefaultsToAll()
    {
        // Arrange
        _authorizationService.SetupGet(a => a.IsAdmin).Returns(true);
        _requestService.Setup(s => s.GetAllRequests()).Returns(new List<RequestFormSummary>
        {
            Req(1, "Pending"),
            Req(2, "Approved"),
        });
        var sut = CreateSut();
        sut.StatusFilter = "Pending"; // narrow first

        // Act
        sut.SetStatusFilterCommand.Execute(null);

        // Assert
        Assert.AreEqual("All", sut.StatusFilter);
        Assert.AreEqual(2, sut.Requests.Count);
    }

    // ---- Filtering: search text ----

    [TestMethod]
    public void SearchText_MatchesEmployeeNameCaseInsensitive_FiltersResults()
    {
        // Arrange
        _authorizationService.SetupGet(a => a.IsAdmin).Returns(true);
        _requestService.Setup(s => s.GetAllRequests()).Returns(new List<RequestFormSummary>
        {
            Req(1, "Pending", employeeName: "Jane Doe"),
            Req(2, "Pending", employeeName: "John Smith"),
        });
        var sut = CreateSut();

        // Act
        sut.SearchText = "jane";

        // Assert
        Assert.AreEqual(1, sut.Requests.Count);
        Assert.AreEqual("Jane Doe", sut.Requests[0].EmployeeName);
    }

    [TestMethod]
    public void SearchText_MatchesRequestType_FiltersResults()
    {
        // Arrange
        _authorizationService.SetupGet(a => a.IsAdmin).Returns(true);
        _requestService.Setup(s => s.GetAllRequests()).Returns(new List<RequestFormSummary>
        {
            Req(1, "Pending", type: "Day Off", employeeName: "Jane Doe"),
            Req(2, "Pending", type: "OT Request", employeeName: "John Smith"),
        });
        var sut = CreateSut();

        // Act
        sut.SearchText = "OT";

        // Assert
        Assert.AreEqual(1, sut.Requests.Count);
        Assert.AreEqual("OT Request", sut.Requests[0].RequestType);
    }

    [TestMethod]
    public void SearchText_And_StatusFilter_CombineWithAndLogic()
    {
        // Arrange
        _authorizationService.SetupGet(a => a.IsAdmin).Returns(true);
        _requestService.Setup(s => s.GetAllRequests()).Returns(new List<RequestFormSummary>
        {
            Req(1, "Pending", employeeName: "Jane Doe"),
            Req(2, "Approved", employeeName: "Jane Doe"),
        });
        var sut = CreateSut();

        // Act
        sut.StatusFilter = "Pending";
        sut.SearchText = "Jane";

        // Assert — only the Pending one matching "Jane" should remain
        Assert.AreEqual(1, sut.Requests.Count);
        Assert.AreEqual(1, sut.Requests[0].RequestId);
    }

    // ---- Approve / Reject ----

    [TestMethod]
    public void ApproveCommand_ValidRequest_CallsApproveRequestAndReloads()
    {
        // Arrange
        _authorizationService.SetupGet(a => a.IsAdmin).Returns(true);
        var request = Req(1, "Pending");
        _requestService.SetupSequence(s => s.GetAllRequests())
            .Returns(new List<RequestFormSummary> { request })   // initial Load() in constructor
            .Returns(new List<RequestFormSummary> { request });  // reload after approve
        _requestService.Setup(s => s.ApproveRequest(request)).Returns(true);
        var sut = CreateSut();

        // Act
        sut.ApproveCommand.Execute(request);

        // Assert
        _requestService.Verify(s => s.ApproveRequest(request), Times.Once);
        _requestService.Verify(s => s.GetAllRequests(), Times.Exactly(2)); // confirms reload happened
    }

    [TestMethod]
    public void ApproveCommand_ServiceReturnsFalse_DoesNotReload()
    {
        // Arrange
        _authorizationService.SetupGet(a => a.IsAdmin).Returns(true);
        var request = Req(1, "Pending");
        _requestService.Setup(s => s.GetAllRequests()).Returns(new List<RequestFormSummary> { request });
        _requestService.Setup(s => s.ApproveRequest(request)).Returns(false);
        var sut = CreateSut();

        // Act
        sut.ApproveCommand.Execute(request);

        // Assert — Load() only called once (constructor); no reload on failure
        _requestService.Verify(s => s.GetAllRequests(), Times.Once);
    }

    [TestMethod]
    public void ApproveCommand_NullParameter_DoesNothing()
    {
        // Arrange
        _authorizationService.SetupGet(a => a.IsAdmin).Returns(true);
        _requestService.Setup(s => s.GetAllRequests()).Returns(new List<RequestFormSummary>());
        var sut = CreateSut();

        // Act
        sut.ApproveCommand.Execute(null);

        // Assert
        _requestService.Verify(s => s.ApproveRequest(It.IsAny<RequestFormSummary>()), Times.Never);
    }

    [TestMethod]
    public void RejectCommand_ValidRequest_CallsRejectRequestByIdAndReloads()
    {
        // Arrange
        _authorizationService.SetupGet(a => a.IsAdmin).Returns(true);
        var request = Req(5, "Pending");
        _requestService.Setup(s => s.GetAllRequests()).Returns(new List<RequestFormSummary> { request });
        _requestService.Setup(s => s.RejectRequest(5)).Returns(true);
        var sut = CreateSut();

        // Act
        sut.RejectCommand.Execute(request);

        // Assert
        _requestService.Verify(s => s.RejectRequest(5), Times.Once);
        _requestService.Verify(s => s.GetAllRequests(), Times.Exactly(2));
    }

    [TestMethod]
    public void RefreshCommand_ReloadsRequests()
    {
        // Arrange
        _authorizationService.SetupGet(a => a.IsAdmin).Returns(true);
        _requestService.Setup(s => s.GetAllRequests()).Returns(new List<RequestFormSummary>());

        var sut = CreateSut();

        // Act
        sut.RefreshCommand.Execute(null);

        // Assert
        _requestService.Verify(s => s.GetAllRequests(), Times.Exactly(2)); // constructor + refresh
    }

    // ---- Department-scoping regression guard ----

    [TestMethod]
    public void Manager_NeverTriggersGetAllRequests_RegardlessOfFilterOrSearchActions()
    {
        // Arrange — regression guard: filtering/searching happen client-side over
        // _allRequests already scoped by GetRequestsByDepartment; make sure no
        // filter/search/refresh path accidentally calls the unscoped GetAllRequests
        // for a non-Admin user.
        _authorizationService.SetupGet(a => a.IsAdmin).Returns(false);
        _requestService.Setup(s => s.GetRequestsByDepartment(3)).Returns(new List<RequestFormSummary>
        {
            Req(1, "Pending", employeeName: "Jane Doe"),
        });
        var sut = CreateSut();

        // Act
        sut.SearchText = "Jane";
        sut.StatusFilter = "Pending";
        sut.RefreshCommand.Execute(null);

        // Assert
        _requestService.Verify(s => s.GetAllRequests(), Times.Never);
        _requestService.Verify(s => s.GetRequestsByDepartment(3), Times.AtLeastOnce);
    }
}
