using HRManagement.Models;
using HRManagement.Services;
using HRManagement.Services.Interfaces;
using HRManagement.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;

namespace HRManagement.Tests;

[TestClass]
public class RequestsViewModelTests
{
    private Mock<IRequestService> _requestService = null!;
    private SessionManager _sessionManager = null!;

    [TestInitialize]
    public void Setup()
    {
        _requestService = new Mock<IRequestService>();
        _sessionManager = new SessionManager();

        // Constructor calls LoadMyRequests(), which needs a logged-in employee id.
        _sessionManager.Login(new CurrentUser
        {
            Account = new Account { AccountId = 1, EmployeeId = 10, Username = "user" },
            Employee = new Employee { EmployeeId = 10, FullName = "Test User", RoleId = 3 },
            Role = new Role { RoleId = 3, RoleName = "Employee" }
        });

        _requestService.Setup(s => s.GetMyRequests(10)).Returns(new List<RequestFormSummary>());
    }

    private RequestsViewModel CreateSut() => new(_requestService.Object, _sessionManager);

    // ---- LoadMyRequests (constructor) ----

    [TestMethod]
    public void Constructor_LoadsMyRequestsAndSetsIsMyRequestsEmpty()
    {
        // Arrange
        _requestService.Setup(s => s.GetMyRequests(10)).Returns(new List<RequestFormSummary>
        {
            new() { RequestId = 1 }
        });

        // Act
        var sut = CreateSut();

        // Assert
        Assert.AreEqual(1, sut.MyRequests.Count);
        Assert.IsFalse(sut.IsMyRequestsEmpty);
    }

    [TestMethod]
    public void Constructor_NoRequests_SetsIsMyRequestsEmptyTrue()
    {
        // Act
        var sut = CreateSut();

        // Assert
        Assert.AreEqual(0, sut.MyRequests.Count);
        Assert.IsTrue(sut.IsMyRequestsEmpty);
    }

    // ---- SelectCategoryCommand / visibility flags ----

    [TestMethod]
    public void SelectCategoryCommand_ValidCategoryName_UpdatesSelectedCategoryAndVisibilityFlags()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.SelectCategoryCommand.Execute("DayOff");

        // Assert
        Assert.AreEqual(RequestCategory.DayOff, sut.SelectedCategory);
        Assert.IsTrue(sut.IsDayOffFormVisible);
        Assert.IsFalse(sut.IsSelectionVisible);
        Assert.IsTrue(sut.IsFormVisible);
        Assert.IsFalse(sut.IsResignationFormVisible);
    }

    [TestMethod]
    public void SelectCategoryCommand_InvalidCategoryName_LeavesSelectedCategoryUnchanged()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.SelectCategoryCommand.Execute("NotARealCategory");

        // Assert
        Assert.AreEqual(RequestCategory.None, sut.SelectedCategory);
    }

    [TestMethod]
    public void BackCommand_ResetsSelectedCategoryAndClearsStatusMessage()
    {
        // Arrange
        var sut = CreateSut();
        sut.SelectCategoryCommand.Execute("Other");

        // Act
        sut.BackCommand.Execute(null);

        // Assert
        Assert.AreEqual(RequestCategory.None, sut.SelectedCategory);
        Assert.AreEqual(string.Empty, sut.StatusMessage);
    }

    // ---- Send: Day Off validation ----

    [TestMethod]
    public void Send_DayOff_MissingFields_ShowsErrorAndDoesNotCallService()
    {
        // Arrange
        var sut = CreateSut();
        sut.SelectedCategory = RequestCategory.DayOff;
        sut.DayOffStartDate = null; // missing

        // Act
        sut.SendCommand.Execute(null);

        // Assert
        Assert.IsTrue(sut.IsError);
        Assert.AreEqual("Please fill in all fields.", sut.StatusMessage);
        _requestService.Verify(s => s.SubmitDayOffRequest(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public void Send_DayOff_EndDateBeforeStartDate_ShowsError()
    {
        // Arrange
        var sut = CreateSut();
        sut.SelectedCategory = RequestCategory.DayOff;
        sut.DayOffStartDate = new DateTime(2026, 8, 15);
        sut.DayOffEndDate = new DateTime(2026, 8, 10); // before start
        sut.DayOffReason = "reason";

        // Act
        sut.SendCommand.Execute(null);

        // Assert
        Assert.IsTrue(sut.IsError);
        Assert.AreEqual("End date cannot be before start date.", sut.StatusMessage);
    }

    [TestMethod]
    public void Send_DayOff_ValidInput_SubmitsClearsFormAndShowsSuccess()
    {
        // Arrange
        var sut = CreateSut();
        sut.SelectedCategory = RequestCategory.DayOff;
        sut.DayOffStartDate = new DateTime(2026, 8, 10);
        sut.DayOffEndDate = new DateTime(2026, 8, 12);
        sut.DayOffReason = "Family trip";
        _requestService.Setup(s => s.SubmitDayOffRequest(10, sut.DayOffStartDate.Value, sut.DayOffEndDate.Value, "Family trip")).Returns(true);

        // Act
        sut.SendCommand.Execute(null);

        // Assert
        Assert.IsFalse(sut.IsError);
        Assert.AreEqual("Your request has been submitted.", sut.StatusMessage);
        Assert.AreEqual(RequestCategory.None, sut.SelectedCategory);
        Assert.IsNull(sut.DayOffStartDate);
        Assert.AreEqual(string.Empty, sut.DayOffReason);
        _requestService.Verify(s => s.GetMyRequests(10), Times.Exactly(2)); // once in ctor, once after submit
    }

    [TestMethod]
    public void Send_DayOff_ServiceReturnsFalse_ShowsGenericError()
    {
        // Arrange
        var sut = CreateSut();
        sut.SelectedCategory = RequestCategory.DayOff;
        sut.DayOffStartDate = new DateTime(2026, 8, 10);
        sut.DayOffEndDate = new DateTime(2026, 8, 12);
        sut.DayOffReason = "Family trip";
        _requestService.Setup(s => s.SubmitDayOffRequest(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string>())).Returns(false);

        // Act
        sut.SendCommand.Execute(null);

        // Assert
        Assert.IsTrue(sut.IsError);
        Assert.AreEqual("Something went wrong. Please try again.", sut.StatusMessage);
        // Form should NOT be cleared on failure, and category should remain visible.
        Assert.AreEqual(RequestCategory.DayOff, sut.SelectedCategory);
    }

    // ---- Send: Resignation validation ----

    [TestMethod]
    public void Send_Resignation_MissingFields_ShowsError()
    {
        // Arrange
        var sut = CreateSut();
        sut.SelectedCategory = RequestCategory.Resignation;
        sut.ResignationLastWorkingDate = null;

        // Act
        sut.SendCommand.Execute(null);

        // Assert
        Assert.IsTrue(sut.IsError);
        Assert.AreEqual("Please fill in all fields.", sut.StatusMessage);
    }

    [TestMethod]
    public void Send_Resignation_ValidInput_Submits()
    {
        // Arrange
        var sut = CreateSut();
        sut.SelectedCategory = RequestCategory.Resignation;
        sut.ResignationLastWorkingDate = new DateTime(2026, 9, 1);
        sut.ResignationReason = "Moving on";
        _requestService.Setup(s => s.SubmitResignationRequest(10, sut.ResignationLastWorkingDate.Value, "Moving on")).Returns(true);

        // Act
        sut.SendCommand.Execute(null);

        // Assert
        Assert.IsFalse(sut.IsError);
        _requestService.Verify(s => s.SubmitResignationRequest(10, new DateTime(2026, 9, 1), "Moving on"), Times.Once);
    }

    // ---- Send: OT validation (boundary-heavy) ----

    [TestMethod]
    public void Send_Ot_InvalidTimeFormat_ShowsError()
    {
        // Arrange
        var sut = CreateSut();
        sut.SelectedCategory = RequestCategory.OT;
        sut.OtDate = DateTime.Today;
        sut.OtStartTime = "not-a-time";
        sut.OtEndTime = "19:00";
        sut.OtReason = "reason";

        // Act
        sut.SendCommand.Execute(null);

        // Assert
        Assert.IsTrue(sut.IsError);
        Assert.AreEqual("Please enter valid times (e.g. 18:00).", sut.StatusMessage);
    }

    [TestMethod]
    public void Send_Ot_DurationBelowOneHour_ShowsMinimumDurationError()
    {
        // Arrange — boundary just under the 1-hour minimum
        var sut = CreateSut();
        sut.SelectedCategory = RequestCategory.OT;
        sut.OtDate = DateTime.Today;
        sut.OtStartTime = "18:00";
        sut.OtEndTime = "18:30";
        sut.OtReason = "reason";

        // Act
        sut.SendCommand.Execute(null);

        // Assert
        Assert.IsTrue(sut.IsError);
        Assert.AreEqual("Minimum OT duration is 1 hour.", sut.StatusMessage);
    }

    [TestMethod]
    public void Send_Ot_DurationExactlyOneHour_PassesMinimumCheck()
    {
        // Arrange — boundary exactly at 1 hour should NOT trigger the minimum-duration error
        var sut = CreateSut();
        sut.SelectedCategory = RequestCategory.OT;
        sut.OtDate = DateTime.Today;
        sut.OtStartTime = "18:00";
        sut.OtEndTime = "19:00";
        sut.OtReason = "reason";
        _requestService.Setup(s => s.SubmitOtRequest(10, It.IsAny<DateTime>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), "reason")).Returns(true);

        // Act
        sut.SendCommand.Execute(null);

        // Assert
        Assert.IsFalse(sut.IsError);
        Assert.AreNotEqual("Minimum OT duration is 1 hour.", sut.StatusMessage);
    }

    [TestMethod]
    public void Send_Ot_DurationAboveFourHours_ShowsMaximumDurationError()
    {
        // Arrange — boundary just over the 4-hour maximum
        var sut = CreateSut();
        sut.SelectedCategory = RequestCategory.OT;
        sut.OtDate = DateTime.Today;
        sut.OtStartTime = "18:00";
        sut.OtEndTime = "22:30";
        sut.OtReason = "reason";

        // Act
        sut.SendCommand.Execute(null);

        // Assert
        Assert.IsTrue(sut.IsError);
        Assert.AreEqual("Maximum OT duration is 4 hours.", sut.StatusMessage);
    }

    [TestMethod]
    public void Send_Ot_DurationExactlyFourHours_PassesMaximumCheck()
    {
        // Arrange — boundary exactly at 4 hours should NOT trigger the maximum-duration error
        var sut = CreateSut();
        sut.SelectedCategory = RequestCategory.OT;
        sut.OtDate = DateTime.Today;
        sut.OtStartTime = "18:00";
        sut.OtEndTime = "22:00";
        sut.OtReason = "reason";
        _requestService.Setup(s => s.SubmitOtRequest(10, It.IsAny<DateTime>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), "reason")).Returns(true);

        // Act
        sut.SendCommand.Execute(null);

        // Assert
        Assert.IsFalse(sut.IsError);
        Assert.AreNotEqual("Maximum OT duration is 4 hours.", sut.StatusMessage);
    }

    // ---- Send: Other validation ----

    [TestMethod]
    public void Send_Other_MissingFields_ShowsError()
    {
        // Arrange
        var sut = CreateSut();
        sut.SelectedCategory = RequestCategory.Other;
        sut.OtherSubject = "";
        sut.OtherDescription = "";

        // Act
        sut.SendCommand.Execute(null);

        // Assert
        Assert.IsTrue(sut.IsError);
        Assert.AreEqual("Please fill in all fields.", sut.StatusMessage);
    }

    [TestMethod]
    public void Send_Other_ValidInput_Submits()
    {
        // Arrange
        var sut = CreateSut();
        sut.SelectedCategory = RequestCategory.Other;
        sut.OtherSubject = "Equipment";
        sut.OtherDescription = "Need a monitor";
        _requestService.Setup(s => s.SubmitOtherRequest(10, "Equipment", "Need a monitor")).Returns(true);

        // Act
        sut.SendCommand.Execute(null);

        // Assert
        Assert.IsFalse(sut.IsError);
        _requestService.Verify(s => s.SubmitOtherRequest(10, "Equipment", "Need a monitor"), Times.Once);
    }

    // ---- Send: no category selected ----

    [TestMethod]
    public void Send_NoCategorySelected_DoesNothing()
    {
        // Arrange
        var sut = CreateSut();
        sut.SelectedCategory = RequestCategory.None;

        // Act
        sut.SendCommand.Execute(null);

        // Assert
        Assert.AreEqual(string.Empty, sut.StatusMessage);
        Assert.IsFalse(sut.IsError);
    }

    // ---- Not logged in ----

    [TestMethod]
    public void Send_NoLoggedInUser_ShowsLoginRequiredError()
    {
        // Arrange
        var loggedOutSessionManager = new SessionManager(); // never logged in
        _requestService.Setup(s => s.GetMyRequests(It.IsAny<int>())).Returns(new List<RequestFormSummary>());
        var sut = new RequestsViewModel(_requestService.Object, loggedOutSessionManager);
        sut.SelectedCategory = RequestCategory.Other;
        sut.OtherSubject = "Equipment";
        sut.OtherDescription = "Need a monitor";

        // Act
        sut.SendCommand.Execute(null);

        // Assert
        Assert.IsTrue(sut.IsError);
        Assert.AreEqual("You must be logged in to submit a request.", sut.StatusMessage);
        _requestService.Verify(s => s.SubmitOtherRequest(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
