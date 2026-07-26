using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using HRManagement.Models;
using HRManagement.Services;
using HRManagement.Services.Interfaces;
using HRManagement.Utilities;

namespace HRManagement.ViewModels;

public class ManageRequestsViewModel : PageViewModel
{
    private readonly IRequestService _requestService;
    private readonly IAuthorizationService _authorizationService;
    private readonly SessionManager _sessionManager;

    private List<RequestFormSummary> _allRequests = new();

    public override string Title => "Manage Requests";

    public ManageRequestsViewModel(
        IRequestService requestService,
        IAuthorizationService authorizationService,
        SessionManager sessionManager)
    {
        _requestService = requestService;
        _authorizationService = authorizationService;
        _sessionManager = sessionManager;

        ApproveCommand = new RelayCommand(p => UpdateStatus(p, "Approved"));
        RejectCommand = new RelayCommand(p => UpdateStatus(p, "Rejected"));
        RefreshCommand = new RelayCommand(_ => Load());
        SetStatusFilterCommand = new RelayCommand(p => StatusFilter = p as string ?? "All");

        Load();
    }

    public ObservableCollection<RequestFormSummary> Requests { get; } = new();

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                ApplyFilter();
        }
    }

    private string _statusFilter = "All";
    public string StatusFilter
    {
        get => _statusFilter;
        set
        {
            if (SetProperty(ref _statusFilter, value))
                ApplyFilter();
        }
    }

    private bool _isEmpty;
    public bool IsEmpty
    {
        get => _isEmpty;
        set => SetProperty(ref _isEmpty, value);
    }

    private int _totalCount;
    public int TotalCount
    {
        get => _totalCount;
        set => SetProperty(ref _totalCount, value);
    }

    private int _pendingCount;
    public int PendingCount
    {
        get => _pendingCount;
        set => SetProperty(ref _pendingCount, value);
    }

    private int _approvedCount;
    public int ApprovedCount
    {
        get => _approvedCount;
        set => SetProperty(ref _approvedCount, value);
    }

    private int _rejectedCount;
    public int RejectedCount
    {
        get => _rejectedCount;
        set => SetProperty(ref _rejectedCount, value);
    }

    public ICommand ApproveCommand { get; }
    public ICommand RejectCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand SetStatusFilterCommand { get; }

    private void Load()
    {
        _allRequests = _authorizationService.IsAdmin
            ? _requestService.GetAllRequests()
            : _requestService.GetRequestsByDepartment(_sessionManager.CurrentUser!.Employee.DepartmentId);

        TotalCount = _allRequests.Count;
        PendingCount = _allRequests.Count(r => r.Status == "Pending");
        ApprovedCount = _allRequests.Count(r => r.Status == "Approved");
        RejectedCount = _allRequests.Count(r => r.Status == "Rejected");

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var filtered = _allRequests.AsEnumerable();

        if (StatusFilter != "All")
            filtered = filtered.Where(r => r.Status == StatusFilter);

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.Trim();

            filtered = filtered.Where(r =>
                r.EmployeeName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                r.RequestType.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        Requests.Clear();

        foreach (var request in filtered)
            Requests.Add(request);

        IsEmpty = Requests.Count == 0;
    }

    private void UpdateStatus(object? parameter, string status)
    {
        if (parameter is not RequestFormSummary request)
            return;

        var success = status == "Approved"
            ? _requestService.ApproveRequest(request)
            : _requestService.RejectRequest(request.RequestId);

        if (success)
            Load();
    }
}
