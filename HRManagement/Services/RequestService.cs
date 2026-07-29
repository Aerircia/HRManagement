using HRManagement.Models;
using HRManagement.Repositories;
using HRManagement.Repositories.Interfaces;
using HRManagement.Services.Interfaces;

namespace HRManagement.Services;

public class RequestService(IRequestFormRepository requestFormRepository, 
                            IAttendanceService attendanceService,
                            ILogService logService,
                            SessionManager sessionManager) : IRequestService
{
    private readonly IRequestFormRepository _requestFormRepository = requestFormRepository;
    private readonly IAttendanceService _attendanceService = attendanceService;
    private readonly ILogService _logService = logService;
    private readonly SessionManager _sessionManager = sessionManager;

    public bool SubmitDayOffRequest(int employeeId, DateTime startDate, DateTime endDate, string reason)
    {
        _logService.WriteLog(employeeId, "Day Off Request Submitted");
        return Submit(employeeId, "Day Off", reason, startDate, endDate);
    }

    public bool SubmitResignationRequest(int employeeId, DateTime lastWorkingDate, string reason)
    {
        _logService.WriteLog(employeeId, "Resignation Request Submitted");
        return Submit(employeeId, "Resignation", reason, null, lastWorkingDate);
    }

    public bool SubmitOtherRequest(int employeeId, string subject, string description)
    {
        _logService.WriteLog(employeeId, "Other Request Submitted");
        return Submit(employeeId, subject, description, null, null);
    }

    public bool SubmitOtRequest(int employeeId, DateTime date, TimeSpan startTime, TimeSpan endTime, string reason)
    {
        _logService.WriteLog(employeeId, "OT Request Submitted");
        // Reuses the existing StartDate/EndDate columns to carry the OT
        // window: StartDate = date + start time, EndDate = date + end time.
        // ManageRequestsViewModel reads these back on approval to actually
        // schedule the OT attendance record via IAttendanceService.
        var start = date.Date + startTime;
        var end = date.Date + endTime;

        return Submit(employeeId, "OT Request", reason, start, end);
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

    public bool ApproveRequest(RequestFormSummary request)
    {
        var success = _requestFormRepository.UpdateStatus(request.RequestId, "Approved");

        if (!success)
            return false;

        // Approving an OT Request doesn't just flip the status - it also
        // creates the actual OT attendance record for that employee/day,
        // so it shows up on their Attendance calendar (single source of
        // truth for attendance stays IAttendanceService/AttendanceRepository).
        if (string.Equals(request.RequestType, "OT Request", StringComparison.OrdinalIgnoreCase)
            && request.StartDate.HasValue && request.EndDate.HasValue)
        {
            _attendanceService.ScheduleOt(
                request.EmployeeId,
                request.StartDate.Value.Date,
                request.StartDate.Value.TimeOfDay,
                request.EndDate.Value.TimeOfDay);
        }
        // Handle Day Off approval
        else if (string.Equals(request.RequestType, "Day Off", StringComparison.OrdinalIgnoreCase)
                 && request.StartDate.HasValue && request.EndDate.HasValue)
        {
            _attendanceService.ScheduleDayOff(
                request.EmployeeId,
                request.StartDate.Value.Date,
                request.EndDate.Value.Date);
        }

        _logService.WriteLog(_sessionManager.CurrentUser!.Account.EmployeeId, $"{request.RequestType} Request Approved");
        return true;
    }

    public bool RejectRequest(int requestId)
    {
        _logService.WriteLog(_sessionManager.CurrentUser!.Account.EmployeeId, $"Request ID:{requestId} Rejected");
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
