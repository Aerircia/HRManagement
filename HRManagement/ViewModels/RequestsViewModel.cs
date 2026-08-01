using System.Collections.ObjectModel;
using System.Windows.Input;
using HRManagement.Models;
using HRManagement.Services;
using HRManagement.Services.Interfaces;
using HRManagement.Utilities;

namespace HRManagement.ViewModels;

public enum RequestCategory
{
    None,
    DayOff,
    Resignation,
    OT,
    Other
}

public class RequestsViewModel : PageViewModel
{
    private readonly IRequestService _requestService;
    private readonly SessionManager _sessionManager;

    public override string Title => "Requests";

    public RequestsViewModel(
        IRequestService requestService,
        SessionManager sessionManager)
    {
        _requestService = requestService;
        _sessionManager = sessionManager;

        SelectCategoryCommand = new RelayCommand(SelectCategory);
        BackCommand = new RelayCommand(_ => GoBack());
        SendCommand = new RelayCommand(Send);

        LoadMyRequests();
    }

    public ObservableCollection<RequestFormSummary> MyRequests { get; } = new();

    private bool _isMyRequestsEmpty;
    public bool IsMyRequestsEmpty
    {
        get => _isMyRequestsEmpty;
        set => SetProperty(ref _isMyRequestsEmpty, value);
    }

    private void LoadMyRequests()
    {
        var employeeId = _sessionManager.CurrentUser?.Employee.EmployeeId;

        if (employeeId == null)
            return;

        var requests = _requestService.GetMyRequests(employeeId.Value);

        MyRequests.Clear();

        foreach (var request in requests)
            MyRequests.Add(request);

        IsMyRequestsEmpty = MyRequests.Count == 0;
    }

    private RequestCategory _selectedCategory = RequestCategory.None;
    public RequestCategory SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                OnPropertyChanged(nameof(IsSelectionVisible));
                OnPropertyChanged(nameof(IsFormVisible));
                OnPropertyChanged(nameof(IsDayOffFormVisible));
                OnPropertyChanged(nameof(IsResignationFormVisible));
                OnPropertyChanged(nameof(IsOtFormVisible));
                OnPropertyChanged(nameof(IsOtherFormVisible));
            }
        }
    }

    public bool IsSelectionVisible => SelectedCategory == RequestCategory.None;
    public bool IsFormVisible => SelectedCategory != RequestCategory.None;
    public bool IsDayOffFormVisible => SelectedCategory == RequestCategory.DayOff;
    public bool IsResignationFormVisible => SelectedCategory == RequestCategory.Resignation;
    public bool IsOtFormVisible => SelectedCategory == RequestCategory.OT;
    public bool IsOtherFormVisible => SelectedCategory == RequestCategory.Other;

    // Day Off fields

    private DateTime? _dayOffStartDate;
    public DateTime? DayOffStartDate
    {
        get => _dayOffStartDate;
        set => SetProperty(ref _dayOffStartDate, value);
    }

    private DateTime? _dayOffEndDate;
    public DateTime? DayOffEndDate
    {
        get => _dayOffEndDate;
        set => SetProperty(ref _dayOffEndDate, value);
    }

    private string _dayOffReason = string.Empty;
    public string DayOffReason
    {
        get => _dayOffReason;
        set => SetProperty(ref _dayOffReason, value);
    }

    // Resignation fields

    private DateTime? _resignationLastWorkingDate;
    public DateTime? ResignationLastWorkingDate
    {
        get => _resignationLastWorkingDate;
        set => SetProperty(ref _resignationLastWorkingDate, value);
    }

    private string _resignationReason = string.Empty;
    public string ResignationReason
    {
        get => _resignationReason;
        set => SetProperty(ref _resignationReason, value);
    }

    // OT fields

    private DateTime? _otDate;
    public DateTime? OtDate
    {
        get => _otDate;
        set => SetProperty(ref _otDate, value);
    }
    public ObservableCollection<string> OtTimeOptions { get; } = new()
    {
        "18:00",
        "19:00",
        "20:00",
        "21:00",
        "22:00"
    };

    private string _otStartTime = "18:00";
    public string OtStartTime
    {
        get => _otStartTime;
        set => SetProperty(ref _otStartTime, value);
    }

    private string _otEndTime = "19:00";
    public string OtEndTime
    {
        get => _otEndTime;
        set => SetProperty(ref _otEndTime, value);
    }

    private string _otReason = string.Empty;
    public string OtReason
    {
        get => _otReason;
        set => SetProperty(ref _otReason, value);
    }

    // Other fields

    private string _otherSubject = string.Empty;
    public string OtherSubject
    {
        get => _otherSubject;
        set => SetProperty(ref _otherSubject, value);
    }

    private string _otherDescription = string.Empty;
    public string OtherDescription
    {
        get => _otherDescription;
        set => SetProperty(ref _otherDescription, value);
    }

    // Status feedback

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (SetProperty(ref _statusMessage, value))
            {
                OnPropertyChanged(nameof(ShowErrorMessage));
                OnPropertyChanged(nameof(ShowSuccessMessage));
            }
        }
    }

    private bool _isError;
    public bool IsError
    {
        get => _isError;
        set
        {
            if (SetProperty(ref _isError, value))
            {
                OnPropertyChanged(nameof(ShowErrorMessage));
                OnPropertyChanged(nameof(ShowSuccessMessage));
            }
        }
    }

    public bool ShowErrorMessage => IsError && !string.IsNullOrEmpty(StatusMessage);
    public bool ShowSuccessMessage => !IsError && !string.IsNullOrEmpty(StatusMessage);

    public ICommand SelectCategoryCommand { get; }
    public ICommand BackCommand { get; }
    public ICommand SendCommand { get; }

    private void SelectCategory(object? parameter)
    {
        if (parameter is string categoryName && Enum.TryParse<RequestCategory>(categoryName, out var category))
        {
            StatusMessage = string.Empty;
            SelectedCategory = category;
        }
    }

    private void GoBack()
    {
        StatusMessage = string.Empty;
        SelectedCategory = RequestCategory.None;
    }

    private void Send(object? parameter)
    {
        var employeeId = _sessionManager.CurrentUser?.Employee.EmployeeId;

        if (employeeId == null)
        {
            ShowError("You must be logged in to submit a request.");
            return;
        }

        bool success;

        switch (SelectedCategory)
        {
            case RequestCategory.DayOff:

                if (DayOffStartDate == null || DayOffEndDate == null || string.IsNullOrWhiteSpace(DayOffReason))
                {
                    ShowError("Please fill in all fields.");
                    return;
                }

                if (DayOffEndDate < DayOffStartDate)
                {
                    ShowError("End date cannot be before start date.");
                    return;
                }

                success = _requestService.SubmitDayOffRequest(
                    employeeId.Value,
                    DayOffStartDate.Value,
                    DayOffEndDate.Value,
                    DayOffReason.Trim());

                break;

            case RequestCategory.Resignation:

                if (ResignationLastWorkingDate == null || string.IsNullOrWhiteSpace(ResignationReason))
                {
                    ShowError("Please fill in all fields.");
                    return;
                }

                success = _requestService.SubmitResignationRequest(
                    employeeId.Value,
                    ResignationLastWorkingDate.Value,
                    ResignationReason.Trim());

                break;

            case RequestCategory.OT:

                if (OtDate == null || string.IsNullOrWhiteSpace(OtStartTime) ||
                    string.IsNullOrWhiteSpace(OtEndTime) || string.IsNullOrWhiteSpace(OtReason))
                {
                    ShowError("Please fill in all fields.");
                    return;
                }

                if (!TimeSpan.TryParse(OtStartTime, out var otStart) ||
                    !TimeSpan.TryParse(OtEndTime, out var otEnd))
                {
                    ShowError("Please enter valid times (e.g. 18:00).");
                    return;
                }

                var duration = otEnd - otStart;

                if (duration.TotalHours < 1)
                {
                    ShowError("Minimum OT duration is 1 hour.");
                    return;
                }

                if (duration.TotalHours > 4)
                {
                    ShowError("Maximum OT duration is 4 hours.");
                    return;
                }

                success = _requestService.SubmitOtRequest(
                    employeeId.Value,
                    OtDate.Value,
                    otStart,
                    otEnd,
                    OtReason.Trim());

                break;

            case RequestCategory.Other:

                if (string.IsNullOrWhiteSpace(OtherSubject) || string.IsNullOrWhiteSpace(OtherDescription))
                {
                    ShowError("Please fill in all fields.");
                    return;
                }

                success = _requestService.SubmitOtherRequest(
                    employeeId.Value,
                    OtherSubject.Trim(),
                    OtherDescription.Trim());

                break;

            default:
                return;
        }

        if (!success)
        {
            ShowError("Something went wrong. Please try again.");
            return;
        }

        ClearForm();

        SelectedCategory = RequestCategory.None;
        IsError = false;
        StatusMessage = "Your request has been submitted.";

        LoadMyRequests();
    }

    private void ShowError(string message)
    {
        IsError = true;
        StatusMessage = message;
    }

    private void ClearForm()
    {
        DayOffStartDate = null;
        DayOffEndDate = null;
        DayOffReason = string.Empty;

        ResignationLastWorkingDate = null;
        ResignationReason = string.Empty;

        OtDate = null;
        OtStartTime = "18:00";
        OtEndTime = "19:00";
        OtReason = string.Empty;

        OtherSubject = string.Empty;
        OtherDescription = string.Empty;
    }
}
