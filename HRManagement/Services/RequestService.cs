using HRManagement.Models;
using HRManagement.Repositories;
using HRManagement.Services.Interfaces;

namespace HRManagement.Services;

public class RequestService : IRequestService
{
    private readonly RequestFormRepository _requestFormRepository;

    public RequestService(RequestFormRepository requestFormRepository)
    {
        _requestFormRepository = requestFormRepository;
    }

    public bool SubmitDayOffRequest(int employeeId, DateTime startDate, DateTime endDate, string reason)
    {
        return Submit(employeeId, "Day Off", $"Reason: {reason}", startDate, endDate);
    }

    public bool SubmitResignationRequest(int employeeId, DateTime lastWorkingDate, string reason)
    {
        return Submit(employeeId, "Resignation", $"Reason: {reason}", null, lastWorkingDate);
    }

    public bool SubmitOtherRequest(int employeeId, string subject, string description)
    {
        var content = $"Subject: {subject}\nDescription: {description}";

        return Submit(employeeId, "Other", content, null, null);
    }

    public List<RequestFormSummary> GetAllRequests()
    {
        return _requestFormRepository.GetAll();
    }

    public bool ApproveRequest(int requestId)
    {
        return _requestFormRepository.UpdateStatus(requestId, "Approved");
    }

    public bool RejectRequest(int requestId)
    {
        return _requestFormRepository.UpdateStatus(requestId, "Rejected");
    }

    private bool Submit(int employeeId, string requestType, string content, DateTime? startDate, DateTime? endDate)
    {
        var form = new RequestForm
        {
            EmployeeId = employeeId,
            RequestType = requestType,
            Content = content,
            StartDate = startDate,
            EndDate = endDate,
            SubmitDate = DateTime.Now,
            Status = "Pending"
        };

        return _requestFormRepository.Insert(form);
    }
}
