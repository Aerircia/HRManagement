using System.Windows.Input;
using HRManagement.Services;
using HRManagement.Services.Interfaces;
using HRManagement.Utilities;

namespace HRManagement.ViewModels;

public enum RequestCategory
{
    None,
    DayOff,
    Resignation,
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
                OnPropertyChanged(nameof(IsOtherFormVisible));
            }
        }
    }

    public bool IsSelectionVisible => SelectedCategory == RequestCategory.None;
    public bool IsFormVisible => SelectedCategory != RequestCategory.None;
    public bool IsDayOffFormVisible => SelectedCategory == RequestCategory.DayOff;
    public bool IsResignationFormVisible => SelectedCategory == RequestCategory.Resignation;
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

        OtherSubject = string.Empty;
        OtherDescription = string.Empty;
    }
}
