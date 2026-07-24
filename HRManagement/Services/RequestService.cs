using HRManagement.Models;
using HRManagement.Repositories;
using HRManagement.Repositories.Interfaces;
using HRManagement.Services.Interfaces;

namespace HRManagement.Services;

public class RequestService(IRequestFormRepository requestFormRepository) : IRequestService
{
    private readonly IRequestFormRepository _requestFormRepository = requestFormRepository;

    public bool SubmitDayOffRequest(int employeeId, DateTime startDate, DateTime endDate, string reason)
    {
        return Submit(employeeId, "Day Off", reason, startDate, endDate);
    }

    public bool SubmitResignationRequest(int employeeId, DateTime lastWorkingDate, string reason)
    {
        return Submit(employeeId, "Resignation", reason, null, lastWorkingDate);
    }

    public bool SubmitOtherRequest(int employeeId, string subject, string description)
    {
        return Submit(employeeId, subject, description, null, null);
    }

    public List<RequestFormSummary> GetAllRequests()
    {
        return _requestFormRepository.GetAll();
    }

    public List<RequestFormSummary> GetRequestsByDepartment(int departmentId)
    {
        return _requestFormRepository.GetByDepartment(departmentId);
    }

    public List<RequestFormSummary> GetMyRequests(int employeeId)
    {
        return _requestFormRepository.GetByEmployee(employeeId);
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
