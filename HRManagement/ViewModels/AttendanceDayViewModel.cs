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
    // Raw Status string ("Present", "OT", "Day Off", etc.) is never renamed -
    // it's still what's stored/compared everywhere else in the app. This map
    // only controls the *label* shown to the user in the calendar cell.
    private static readonly Dictionary<string, string> StatusWordMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Present"] = "Ontime",
        ["Late"] = "Late",
        ["Absent"] = "Absent",
        ["OT"] = "OT",
        ["Day Off"] = "Day Off",
        ["Weekend"] = "Weekend",
        ["Before Hire Date"] = "Before Hire Date"
    };

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

    // The calendar cell's Background binds directly to Status through
    // AttendanceStatusToBrushConverter, which resolves the brush live via
    // Application.Current.FindResource - but WPF only re-runs a converter
    // when the bound source value itself changes, not when a resource the
    // converter happens to look up changes underneath it. After a theme
    // swap the Status value hasn't changed, so without this the cell would
    // stay painted with whatever brush instance the last theme resolved to
    // until something else (e.g. navigating away and back) re-touches
    // Status. This re-raises PropertyChanged for Status only, forcing the
    // binding - and therefore the converter - to re-evaluate against the
    // now-current theme dictionary.
    public void RefreshThemeDependentDisplay() => OnPropertyChanged(nameof(Status));

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
    public DateTime? CheckIn
    {
        get => _checkIn;
        private set
        {
            if (SetProperty(ref _checkIn, value))
            {
                OnPropertyChanged(nameof(WorkedHoursDisplay));
                OnPropertyChanged(nameof(ShouldShowWorkedHours));
            }
        }
    }

    private DateTime? _checkOut;
    public DateTime? CheckOut
    {
        get => _checkOut;
        private set
        {
            if (SetProperty(ref _checkOut, value))
            {
                OnPropertyChanged(nameof(WorkedHoursDisplay));
                OnPropertyChanged(nameof(ShouldShowWorkedHours));
            }
        }
    }

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
                OnPropertyChanged(nameof(DisplayStatusWord));
                OnPropertyChanged(nameof(ShouldShowWorkedHours));
                OnPropertyChanged(nameof(IsWeekendOrBeforeHire));
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

    // Display-only relabeling of Status (e.g. "Present" -> "Ontime"). The
    // underlying Status string is never changed - this is purely a label
    // mapping so converters/comparisons elsewhere keep working unmodified.
    public string DisplayStatusWord =>
        HasStatus && StatusWordMap.TryGetValue(Status, out var word) ? word : Status;

    // Convenience flag so the calendar cell template doesn't need two
    // separate OR-style DataTriggers for "weekend with no OT worked" vs
    // "before hire date" - both cases just show date + status word, no
    // hours/times.
    public bool IsWeekendOrBeforeHire =>
        IsBeforeHireDate || (IsWeekend && string.Equals(Status, "Weekend", StringComparison.OrdinalIgnoreCase));

    // "7h 42m" style duration for the day, or empty when not applicable.
    public string WorkedHoursDisplay
    {
        get
        {
            if (!ShouldShowWorkedHours || !CheckIn.HasValue || !CheckOut.HasValue)
                return string.Empty;

            var span = CheckOut.Value - CheckIn.Value;
            if (span.Ticks <= 0)
                return string.Empty;

            var hours = (int)span.TotalHours;
            var minutes = span.Minutes;
            return $"{hours}h {minutes}m";
        }
    }

    // True only when CheckIn and CheckOut are both present and the day
    // isn't a plain weekend/before-hire/future-empty day - mirrors
    // ShouldShowTimes but specifically gates the new dominant hours-worked
    // text (weekend/before-hire cells should show only date + status word).
    public bool ShouldShowWorkedHours =>
        !IsWeekendOrBeforeHire
        && CheckIn.HasValue
        && CheckOut.HasValue
        && CheckOut.Value > CheckIn.Value;

    public bool ShouldShowTimes
    {
        get
        {
            if (!IsCurrentMonth || IsBeforeHireDate)
                return false;

            // Hide times for Day Off
            if (string.Equals(Status, "Day Off", StringComparison.OrdinalIgnoreCase))
                return false;

            var isOt = string.Equals(Status, "OT", StringComparison.OrdinalIgnoreCase);
            var hasRecord = CheckIn.HasValue || CheckOut.HasValue;

            if (IsWeekend || IsFuture)
                return isOt || hasRecord;

            return true;
        }
    }
}
