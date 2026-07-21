using HRManagement.Models;

namespace HRManagement.Services.Interfaces;

public interface IRequestService
{
    bool SubmitDayOffRequest(int employeeId, DateTime startDate, DateTime endDate, string reason);

    bool SubmitResignationRequest(int employeeId, DateTime lastWorkingDate, string reason);

    bool SubmitOtherRequest(int employeeId, string subject, string description);

    List<RequestFormSummary> GetAllRequests();

    bool ApproveRequest(int requestId);

    bool RejectRequest(int requestId);
}
