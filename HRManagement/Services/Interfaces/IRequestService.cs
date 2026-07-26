using HRManagement.Models;

namespace HRManagement.Services.Interfaces;

public interface IRequestService
{
    bool SubmitDayOffRequest(int employeeId, DateTime startDate, DateTime endDate, string reason);

    bool SubmitResignationRequest(int employeeId, DateTime lastWorkingDate, string reason);

    bool SubmitOtherRequest(int employeeId, string subject, string description);

    bool SubmitOtRequest(int employeeId, DateTime date, TimeSpan startTime, TimeSpan endTime, string reason);

    List<RequestFormSummary> GetAllRequests();

    List<RequestFormSummary> GetRequestsByDepartment(int departmentId);

    List<RequestFormSummary> GetMyRequests(int employeeId);

    bool ApproveRequest(RequestFormSummary request);

    bool RejectRequest(int requestId);
}
