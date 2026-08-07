using HRManagement.Models;
using HRManagement.Repositories.Interfaces;
using HRManagement.Services;
using HRManagement.Services.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;

namespace HRManagement.Tests;

[TestClass]
public class RequestServiceTests
{
    private Mock<IRequestFormRepository> _requestFormRepository = null!;
    private Mock<IAttendanceService> _attendanceService = null!;
    private Mock<ILogService> _logService = null!;
    private SessionManager _sessionManager = null!;
    private RequestService _sut = null!;

    [TestInitialize]
    public void Setup()
    {
        _requestFormRepository = new Mock<IRequestFormRepository>();
        _attendanceService = new Mock<IAttendanceService>();
        _logService = new Mock<ILogService>();
        _sessionManager = new SessionManager();

        _sut = new RequestService(
            _requestFormRepository.Object,
            _attendanceService.Object,
            _logService.Object,
            _sessionManager);
    }

    private void LogInAs(int accountId, int employeeId)
    {
        _sessionManager.Login(new CurrentUser
        {
            Account = new Account { AccountId = accountId, EmployeeId = employeeId, Username = "user" },
            Employee = new Employee { EmployeeId = employeeId, FullName = "Test User", RoleId = 3 },
            Role = new Role { RoleId = 3, RoleName = "Employee" }
        });
    }

    // ---- SubmitDayOffRequest ----

    [TestMethod]
    public void SubmitDayOffRequest_ValidInput_InsertsFormWithPendingStatusAndLogs()
    {
        // Arrange
        _requestFormRepository.Setup(r => r.Insert(It.IsAny<RequestForm>())).Returns(true);
        var start = new DateTime(2026, 8, 10);
        var end = new DateTime(2026, 8, 12);

        // Act
        var result = _sut.SubmitDayOffRequest(10, start, end, "Family trip");

        // Assert
        Assert.IsTrue(result);
        _logService.Verify(l => l.WriteLog(10, "Day Off Request Submitted"), Times.Once);
        _requestFormRepository.Verify(r => r.Insert(It.Is<RequestForm>(f =>
            f.EmployeeId == 10 &&
            f.RequestType == "Day Off" &&
            f.Content == "Family trip" &&
            f.StartDate == start &&
            f.EndDate == end &&
            f.Status == "Pending")), Times.Once);
    }

    [TestMethod]
    public void SubmitDayOffRequest_RepositoryInsertFails_ReturnsFalse()
    {
        // Arrange
        _requestFormRepository.Setup(r => r.Insert(It.IsAny<RequestForm>())).Returns(false);

        // Act
        var result = _sut.SubmitDayOffRequest(10, DateTime.Today, DateTime.Today.AddDays(1), "reason");

        // Assert
        Assert.IsFalse(result);
        // Note: current code logs "Day Off Request Submitted" BEFORE calling Insert,
        // so the log still fires even though the insert ultimately failed. Documenting
        // this as current behavior, not asserting it's ideal.
        _logService.Verify(l => l.WriteLog(10, "Day Off Request Submitted"), Times.Once);
    }

    // ---- SubmitResignationRequest ----

    [TestMethod]
    public void SubmitResignationRequest_ValidInput_InsertsWithNullStartDate()
    {
        // Arrange
        _requestFormRepository.Setup(r => r.Insert(It.IsAny<RequestForm>())).Returns(true);
        var lastDay = new DateTime(2026, 9, 1);

        // Act
        var result = _sut.SubmitResignationRequest(10, lastDay, "Moving on");

        // Assert
        Assert.IsTrue(result);
        _logService.Verify(l => l.WriteLog(10, "Resignation Request Submitted"), Times.Once);
        _requestFormRepository.Verify(r => r.Insert(It.Is<RequestForm>(f =>
            f.RequestType == "Resignation" &&
            f.StartDate == null &&
            f.EndDate == lastDay)), Times.Once);
    }

    // ---- SubmitOtherRequest ----

    [TestMethod]
    public void SubmitOtherRequest_ValidInput_InsertsWithSubjectAsTypeAndNullDates()
    {
        // Arrange
        _requestFormRepository.Setup(r => r.Insert(It.IsAny<RequestForm>())).Returns(true);

        // Act
        var result = _sut.SubmitOtherRequest(10, "Equipment", "Need a new monitor");

        // Assert
        Assert.IsTrue(result);
        _logService.Verify(l => l.WriteLog(10, "Other Request Submitted"), Times.Once);
        _requestFormRepository.Verify(r => r.Insert(It.Is<RequestForm>(f =>
            f.RequestType == "Equipment" &&
            f.Content == "Need a new monitor" &&
            f.StartDate == null &&
            f.EndDate == null)), Times.Once);
    }

    // ---- SubmitOtRequest ----

    [TestMethod]
    public void SubmitOtRequest_ValidInput_CombinesDateAndTimeIntoStartEnd()
    {
        // Arrange
        _requestFormRepository.Setup(r => r.Insert(It.IsAny<RequestForm>())).Returns(true);
        var date = new DateTime(2026, 8, 10);
        var startTime = new TimeSpan(18, 0, 0);
        var endTime = new TimeSpan(20, 0, 0);

        // Act
        var result = _sut.SubmitOtRequest(10, date, startTime, endTime, "Deadline crunch");

        // Assert
        Assert.IsTrue(result);
        _logService.Verify(l => l.WriteLog(10, "OT Request Submitted"), Times.Once);
        _requestFormRepository.Verify(r => r.Insert(It.Is<RequestForm>(f =>
            f.RequestType == "OT Request" &&
            f.StartDate == date.Date + startTime &&
            f.EndDate == date.Date + endTime)), Times.Once);
    }

    // ---- GetAllRequests / GetRequestsByDepartment / GetMyRequests ----

    [TestMethod]
    public void GetAllRequests_DelegatesToRepositoryGetAll()
    {
        // Arrange
        var expected = new List<RequestFormSummary> { new() { RequestId = 1 } };
        _requestFormRepository.Setup(r => r.GetAll()).Returns(expected);

        // Act
        var result = _sut.GetAllRequests();

        // Assert
        Assert.AreSame(expected, result);
    }

    [TestMethod]
    public void GetRequestsByDepartment_PassesDepartmentIdThroughToRepository()
    {
        // Arrange — the service itself does no filtering; department scoping
        // lives in IRequestFormRepository.GetByDepartment. This test only confirms
        // the correct department id is passed through, not the filtering logic
        // itself (that repository implementation wasn't provided).
        var expected = new List<RequestFormSummary> { new() { RequestId = 2 } };
        _requestFormRepository.Setup(r => r.GetByDepartment(7)).Returns(expected);

        // Act
        var result = _sut.GetRequestsByDepartment(7);

        // Assert
        Assert.AreSame(expected, result);
        _requestFormRepository.Verify(r => r.GetByDepartment(7), Times.Once);
    }

    [TestMethod]
    public void GetMyRequests_PassesEmployeeIdThroughToRepository()
    {
        // Arrange
        var expected = new List<RequestFormSummary> { new() { RequestId = 3 } };
        _requestFormRepository.Setup(r => r.GetByEmployee(10)).Returns(expected);

        // Act
        var result = _sut.GetMyRequests(10);

        // Assert
        Assert.AreSame(expected, result);
    }

    // ---- ApproveRequest ----

    [TestMethod]
    public void ApproveRequest_DayOffRequestType_UpdatesStatusAndSchedulesDayOff()
    {
        // Arrange
        LogInAs(accountId: 99, employeeId: 20);
        var start = new DateTime(2026, 8, 10);
        var end = new DateTime(2026, 8, 12);
        var request = new RequestFormSummary
        {
            RequestId = 2,
            EmployeeId = 10,
            RequestType = "Day Off",
            StartDate = start,
            EndDate = end
        };
        _requestFormRepository.Setup(r => r.UpdateStatus(2, "Approved")).Returns(true);

        // Act
        var result = _sut.ApproveRequest(request);

        // Assert
        Assert.IsTrue(result);
        _attendanceService.Verify(a => a.ScheduleDayOff(10, start.Date, end.Date), Times.Once);
        _logService.Verify(l => l.WriteLog(It.IsAny<int>(), "Day Off Request Approved"), Times.Once);
    }

    [TestMethod]
    public void ApproveRequest_OtherRequestType_UpdatesStatusOnlyNoAttendanceSideEffect()
    {
        // Arrange
        LogInAs(accountId: 99, employeeId: 20);
        var request = new RequestFormSummary
        {
            RequestId = 3,
            EmployeeId = 10,
            RequestType = "Equipment",
            StartDate = null,
            EndDate = null
        };
        _requestFormRepository.Setup(r => r.UpdateStatus(3, "Approved")).Returns(true);

        // Act
        var result = _sut.ApproveRequest(request);

        // Assert
        Assert.IsTrue(result);
        _attendanceService.Verify(a => a.ScheduleOt(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>()), Times.Never);
        _attendanceService.Verify(a => a.ScheduleDayOff(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
    }

    [TestMethod]
    public void ApproveRequest_RepositoryUpdateFails_ReturnsFalseAndDoesNotScheduleOrLog()
    {
        // Arrange
        LogInAs(accountId: 99, employeeId: 20);
        var request = new RequestFormSummary
        {
            RequestId = 4,
            EmployeeId = 10,
            RequestType = "OT Request",
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddHours(2)
        };
        _requestFormRepository.Setup(r => r.UpdateStatus(4, "Approved")).Returns(false);

        // Act
        var result = _sut.ApproveRequest(request);

        // Assert
        Assert.IsFalse(result);
        _attendanceService.Verify(a => a.ScheduleOt(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>()), Times.Never);
        _logService.Verify(l => l.WriteLog(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public void ApproveRequest_OtRequestTypeButMissingDates_UpdatesStatusOnlyDoesNotScheduleOt()
    {
        // Arrange — RequestType is "OT Request" but StartDate/EndDate are null;
        // current code guards on request.StartDate.HasValue && request.EndDate.HasValue
        // before scheduling, so this should update status without a NullReferenceException.
        LogInAs(accountId: 99, employeeId: 20);
        var request = new RequestFormSummary
        {
            RequestId = 5,
            EmployeeId = 10,
            RequestType = "OT Request",
            StartDate = null,
            EndDate = null
        };
        _requestFormRepository.Setup(r => r.UpdateStatus(5, "Approved")).Returns(true);

        // Act
        var result = _sut.ApproveRequest(request);

        // Assert
        Assert.IsTrue(result);
        _attendanceService.Verify(a => a.ScheduleOt(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    // ---- RejectRequest ----

    [TestMethod]
    public void RejectRequest_ValidId_UpdatesStatusToRejectedAndLogs()
    {
        // Arrange
        LogInAs(accountId: 99, employeeId: 20);
        _requestFormRepository.Setup(r => r.UpdateStatus(7, "Rejected")).Returns(true);

        // Act
        var result = _sut.RejectRequest(7);

        // Assert
        Assert.IsTrue(result);
        _logService.Verify(l => l.WriteLog(It.IsAny<int>(), It.Is<string>(m => m.Contains("7") && m.Contains("Rejected"))), Times.Once);
    }

    [TestMethod]
    public void RejectRequest_RepositoryUpdateFails_ReturnsFalse()
    {
        // Arrange
        LogInAs(accountId: 99, employeeId: 20);
        _requestFormRepository.Setup(r => r.UpdateStatus(8, "Rejected")).Returns(false);

        // Act
        var result = _sut.RejectRequest(8);

        // Assert
        Assert.IsFalse(result);
    }

}
