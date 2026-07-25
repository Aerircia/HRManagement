using HRManagement.Models;
using HRManagement.Utilities;

namespace HRManagement.ViewModels;

// Thin, bindable wrapper around AttendanceDayModel. Deliberately holds no
// WPF types (Brush, Visibility, etc.) - color/visibility is resolved in
// XAML via converters (AttendanceStatusToBrushConverter, BoolToVisibility)
// keyed off Status, so there is exactly one place that maps
// status -> appearance.
public class AttendanceDayViewModel : ViewModelBase
{
    public AttendanceDayViewModel(AttendanceDayModel model)
    {
        UpdateFrom(model);
    }

    public void UpdateFrom(AttendanceDayModel model)
    {
        Date = model.Date;
        IsCurrentMonth = model.IsCurrentMonth;
        IsWeekend = model.IsWeekend;
        IsBeforeHireDate = model.IsBeforeHireDate;
        IsFuture = model.IsFuture;
        CheckIn = model.CheckIn;
        CheckOut = model.CheckOut;
        Status = model.Status;
        LatenessMinutes = model.LatenessMinutes;
    }

    private DateTime _date;
    public DateTime Date { get => _date; private set => SetProperty(ref _date, value); }

    private bool _isCurrentMonth;
    public bool IsCurrentMonth { get => _isCurrentMonth; private set => SetProperty(ref _isCurrentMonth, value); }

    private bool _isWeekend;
    public bool IsWeekend { get => _isWeekend; private set => SetProperty(ref _isWeekend, value); }

    private bool _isBeforeHireDate;
    public bool IsBeforeHireDate { get => _isBeforeHireDate; private set => SetProperty(ref _isBeforeHireDate, value); }

    private bool _isFuture;
    public bool IsFuture { get => _isFuture; private set => SetProperty(ref _isFuture, value); }

    private DateTime? _checkIn;
    public DateTime? CheckIn { get => _checkIn; private set => SetProperty(ref _checkIn, value); }

    private DateTime? _checkOut;
    public DateTime? CheckOut { get => _checkOut; private set => SetProperty(ref _checkOut, value); }

    private string _status = string.Empty;
    public string Status
    {
        get => _status;
        private set
        {
            if (SetProperty(ref _status, value))
            {
                OnPropertyChanged(nameof(DisplayStatus));
                OnPropertyChanged(nameof(HasStatus));
            }
        }
    }

    private int? _latenessMinutes;
    public int? LatenessMinutes
    {
        get => _latenessMinutes;
        private set
        {
            if (SetProperty(ref _latenessMinutes, value))
                OnPropertyChanged(nameof(DisplayStatus));
        }
    }

    // True for leading/trailing filler cells belonging to the previous/next month.
    public bool IsFillerCell => !IsCurrentMonth;

    public bool HasStatus => !string.IsNullOrEmpty(Status);

    public string DisplayStatus
    {
        get
        {
            if (string.Equals(Status, "Late", StringComparison.OrdinalIgnoreCase) && LatenessMinutes is > 0)
                return $"Late: {LatenessMinutes.Value} min";

            return Status;
        }
    }

    public bool ShouldShowTimes
    {
        get
        {
            if (!IsCurrentMonth || IsBeforeHireDate)
                return false;

            var isOt = string.Equals(Status, "OT", StringComparison.OrdinalIgnoreCase);
            var hasRecord = CheckIn.HasValue || CheckOut.HasValue;

            if (IsWeekend || IsFuture)
                return isOt || hasRecord;

            return true;
        }
    }
}
