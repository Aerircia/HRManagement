using HRManagement.Models;
using HRManagement.Repositories;
using HRManagement.Repositories.Interfaces;
using HRManagement.Services;
using HRManagement.Services.Interfaces;
using HRManagement.Utilities;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace HRManagement.ViewModels
{
    public class AttendanceViewModel : PageViewModel
    {
        private int? _displayEmployeeId;
        private DateTime? _displayHireDate;

        public void SetDisplayedEmployee(int? employeeId, DateTime? hireDate)
        {
            _displayEmployeeId = employeeId;
            _displayHireDate = hireDate;
            UpdateHireAndRebuild();
        }

        private void UpdateHireAndRebuild()
        {
            _hireDate = _displayHireDate ?? _sessionManager.CurrentUser?.Employee.HireDate.Date;
            if (_hireDate.HasValue)
            {
                var hireMonth = new DateTime(_hireDate.Value.Year, _hireDate.Value.Month, 1);
                if (CurrentMonth < hireMonth) CurrentMonth = hireMonth;
            }
            BuildMonth();
        }

        private DateTime? _hireDate;
        public int LateGraceMinutes { get; set; } = 5;
        public TimeSpan ShiftStart => TimeSpan.FromHours(8);

        private readonly IAttendanceRepository _attendanceRepository;
        private readonly IAttendanceService _attendanceService;
        private readonly SessionManager _sessionManager;

        public AttendanceViewModel(SessionManager sessionManager, IAttendanceRepository attendanceRepository, IAttendanceService? attendanceService = null)
        {
            _sessionManager = sessionManager;
            _attendanceRepository = attendanceRepository;
            _attendanceService = attendanceService ?? new AttendanceService(attendanceRepository); // adjust as appropriate
            _attendanceRepository.OnAttendanceChanged += AttendanceRepository_OnAttendanceChanged;


            if (_sessionManager.CurrentUser != null)
            {
                _hireDate = _sessionManager.CurrentUser.Employee.HireDate.Date;
                CurrentMonth = new DateTime(_hireDate.Value.Year, _hireDate.Value.Month, 1);
            }
            else
            {
                CurrentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            }

            Days = [];

            _prevCommand = new RelayCommand(_ => ChangeMonth(-1));
            _nextCommand = new RelayCommand(_ => ChangeMonth(1));
            _checkInCommand = new RelayCommand(_ => CheckIn(), _ => CanCheckInLogic());
            _checkOutCommand = new RelayCommand(_ => CheckOut(), _ => CanCheckOutLogic());

            _sessionManager.OnUserChanged += SessionManager_OnUserChanged;

            BuildMonth();

            // subscribe to attendance changes so calendar updates when OT assigned
            _attendanceRepository.OnAttendanceChanged += AttendanceRepository_OnAttendanceChanged;
        }

        public override string Title => "Attendance";

        private DateTime _currentMonth;
        public DateTime CurrentMonth { get => _currentMonth; set => SetProperty(ref _currentMonth, value); }

        public ObservableCollection<AttendanceDayViewModel> Days { get; }

        private readonly RelayCommand _prevCommand;
        private readonly RelayCommand _nextCommand;
        private readonly RelayCommand _checkInCommand;
        private readonly RelayCommand _checkOutCommand;

        public ICommand PrevMonthCommand => _prevCommand;
        public ICommand NextMonthCommand => _nextCommand;
        public ICommand CheckInCommand => _checkInCommand;
        public ICommand CheckOutCommand => _checkOutCommand;

        private void ChangeMonth(int delta)
        {
            var candidate = CurrentMonth.AddMonths(delta);
            if (_hireDate.HasValue)
            {
                var hireMonth = new DateTime(_hireDate.Value.Year, _hireDate.Value.Month, 1);
                if (candidate < hireMonth) { CurrentMonth = hireMonth; BuildMonth(); return; }
            }
            CurrentMonth = candidate;
            BuildMonth();
        }

        private void BuildMonth()
        {
            Days.Clear();
            var first = new DateTime(CurrentMonth.Year, CurrentMonth.Month, 1);
            var daysInMonth = DateTime.DaysInMonth(CurrentMonth.Year, CurrentMonth.Month);
            var start = first;
            while (start.DayOfWeek != System.DayOfWeek.Monday) start = start.AddDays(-1);

            var totalCells = ((int)Math.Ceiling((start.AddDays(daysInMonth + (first - start).Days) - start).TotalDays));
            while (totalCells % 7 != 0) totalCells++;

            for (int i = 0; i < totalCells; i++)
            {
                var date = start.AddDays(i);
                var cell = new AttendanceDayViewModel(date) { IsCurrentMonth = date.Month == CurrentMonth.Month };
                if (_hireDate.HasValue && date.Date < _hireDate.Value) cell.IsBeforeHireDate = true;
                if (date.Date > DateTime.Now.Date && !cell.IsWeekend)
                {
                    cell.Background = System.Windows.Media.Brushes.White; cell.Status = string.Empty; cell.LatenessMinutes = null;
                    Days.Add(cell); continue;
                }
                Days.Add(cell);
            }

            // choose which employee to display: override if provided, otherwise session user
            int? empIdToUse = _displayEmployeeId ?? _sessionManager.CurrentUser?.Employee.EmployeeId;
            if (empIdToUse.HasValue)
            {
                var list = _attendanceRepository.GetAttendancesForEmployeeMonth(empIdToUse.Value, CurrentMonth.Year, CurrentMonth.Month);
                foreach (var att in list)
                {
                    var dateKey = (att.CheckIn ?? att.CheckOut ?? DateTime.Now).Date;
                    var day = Days.FirstOrDefault(d => d.Date.Date == dateKey);
                    if (day != null)
                    {
                        day.CheckIn = att.CheckIn; day.CheckOut = att.CheckOut;
                        if (!string.IsNullOrEmpty(att.Status) && att.Status.Equals("OT", StringComparison.OrdinalIgnoreCase))
                        {
                            day.Status = "OT";
                            day.LatenessMinutes = null;
                        }
                        else if (day.CheckIn.HasValue && !day.CheckOut.HasValue)
                        {
                            // in-progress
                            day.Status = "Working";
                            day.LatenessMinutes = null;
                        }
                        else if (day.CheckIn.HasValue && day.CheckOut.HasValue)
                        {
                            var shiftStart = day.Date.Add(ShiftStart);
                            int minutesLate = (int)Math.Round((day.CheckIn.Value - shiftStart).TotalMinutes);
                            if (minutesLate <= LateGraceMinutes)
                            {
                                day.Status = "OnTime";
                                day.LatenessMinutes = 0;
                            }
                            else
                            {
                                day.Status = "Late";
                                day.LatenessMinutes = minutesLate;
                            }
                        }
                        else { day.LatenessMinutes = null; day.Status = att.Status; }
                    }
                }
            }

            foreach (var d in Days) d.UpdateColor();
            _checkInCommand?.RaiseCanExecuteChanged(); _checkOutCommand?.RaiseCanExecuteChanged();
            UpdateSummary();
        }
        public void RefreshIfEmployee(int employeeId, DateTime date)
        {
            if (_sessionManager.CurrentUser != null && _sessionManager.CurrentUser.Employee.EmployeeId == employeeId)
            {
                if (date.Year == CurrentMonth.Year && date.Month == CurrentMonth.Month)
                    BuildMonth();
            }
        }

        private bool CanCheckInLogic()
        {
            var today = DateTime.Now.Date;
            // allow check-in on weekends only when there's a scheduled OT or existing attendance for today
            if (_hireDate.HasValue && today < _hireDate.Value) return false;
            var cell = Days.FirstOrDefault(d => d.Date.Date == today);
            if (cell == null) return false;
            if (cell.CheckIn.HasValue) return false;
            if (today.DayOfWeek == System.DayOfWeek.Saturday || today.DayOfWeek == System.DayOfWeek.Sunday)
            {
                // on weekends, only allow if the day is marked as OT or there is already an attendance record
                if (string.IsNullOrEmpty(cell.Status) || !cell.Status.Equals("OT", StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            return true;
        }

        private bool CanCheckOutLogic()
        {
            var today = DateTime.Now.Date;
            if (_hireDate.HasValue && today < _hireDate.Value) return false;
            var cell = Days.FirstOrDefault(d => d.Date.Date == today);
            if (cell == null) return false;
            if (!cell.CheckIn.HasValue) return false;
            if (cell.CheckOut.HasValue) return false;
            if (today.DayOfWeek == System.DayOfWeek.Saturday || today.DayOfWeek == System.DayOfWeek.Sunday)
            {
                // on weekends, only allow check-out when the day is OT
                if (string.IsNullOrEmpty(cell.Status) || !cell.Status.Equals("OT", StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            return true;
        }

        private void CheckIn()
        {
            var today = DateTime.Now.Date; var cell = Days.FirstOrDefault(d => d.Date.Date == today); if (cell == null) return; if (cell.CheckIn.HasValue) return;
            cell.CheckIn = DateTime.Now; var shiftThreshold = today.Add(ShiftStart).AddMinutes(LateGraceMinutes);
            if (DateTime.Now <= shiftThreshold) { cell.Status = "OnTime"; cell.LatenessMinutes = 0; }
            else { cell.Status = "Late"; cell.LatenessMinutes = (int)Math.Round((DateTime.Now - today.Add(ShiftStart)).TotalMinutes); }
            cell.UpdateColor(); UpdateSummary();
            if (_sessionManager.CurrentUser != null)
            {
                var att = new Attendance { EmployeeId = _sessionManager.CurrentUser.Employee.EmployeeId, CheckIn = cell.CheckIn, CheckOut = cell.CheckOut, Status = cell.Status };
                _attendanceRepository.UpsertAttendance(att);
            }
            _checkInCommand?.RaiseCanExecuteChanged(); _checkOutCommand?.RaiseCanExecuteChanged();
        }

        private void CheckOut()
        {
            var today = DateTime.Now.Date; var cell = Days.FirstOrDefault(d => d.Date.Date == today); if (cell == null) return; if (cell.CheckOut.HasValue) return;
            cell.CheckOut = DateTime.Now; if (string.IsNullOrEmpty(cell.Status)) cell.Status = "Present"; cell.UpdateColor(); UpdateSummary();
            if (_sessionManager.CurrentUser != null)
            {
                var att = new Attendance { EmployeeId = _sessionManager.CurrentUser.Employee.EmployeeId, CheckIn = cell.CheckIn, CheckOut = cell.CheckOut, Status = cell.Status };
                _attendanceRepository.UpsertAttendance(att);
            }
            _checkInCommand?.RaiseCanExecuteChanged(); _checkOutCommand?.RaiseCanExecuteChanged();
        }

        private void SessionManager_OnUserChanged(object? sender, EventArgs e)
        {
            if (_sessionManager.CurrentUser != null) { _hireDate = _sessionManager.CurrentUser.Employee.HireDate.Date; CurrentMonth = new DateTime(_hireDate.Value.Year, _hireDate.Value.Month, 1); }
            else { _hireDate = null; CurrentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1); }
            BuildMonth(); _checkInCommand?.RaiseCanExecuteChanged(); _checkOutCommand?.RaiseCanExecuteChanged();
        }

        private void AttendanceRepository_OnAttendanceChanged(object? sender, HRManagement.Models.AttendanceChangedEventArgs e)
        {
            // if this calendar belongs to the employee whose attendance changed, rebuild month
            if (_sessionManager.CurrentUser != null && _sessionManager.CurrentUser.Employee.EmployeeId == e.EmployeeId)
            {
                // if changed date is in current month, rebuild
                if (e.Date.Year == CurrentMonth.Year && e.Date.Month == CurrentMonth.Month)
                    BuildMonth();
            }
        }

        private void UpdateSummary()
        {
            // Build cumulative summary from hire date up to the end of the viewed month (or last available attendance month)
            if (_sessionManager.CurrentUser == null)
            {
                // fallback to previous behavior when no current user
                TotalDaysWorked = Days.Count(d => d.IsCurrentMonth && d.CheckIn.HasValue && (!_hireDate.HasValue || d.Date.Date >= _hireDate.Value) && d.Date.Date <= DateTime.Now.Date);
                TotalLate = Days.Count(d => d.IsCurrentMonth && d.LatenessMinutes.HasValue && d.LatenessMinutes > 0 && (!_hireDate.HasValue || d.Date.Date >= _hireDate.Value) && d.Date.Date <= DateTime.Now.Date);
                TotalLateMinutes = Days.Where(d => d.IsCurrentMonth && d.LatenessMinutes.HasValue && (!_hireDate.HasValue || d.Date.Date >= _hireDate.Value) && d.Date.Date <= DateTime.Now.Date).Sum(d => d.LatenessMinutes ?? 0);
                TotalAbsent = Days.Count(d => d.IsCurrentMonth && !d.CheckIn.HasValue && d.IsWorkday() && (!_hireDate.HasValue || d.Date.Date >= _hireDate.Value) && d.Date.Date <= DateTime.Now.Date);
                TotalOnTime = Days.Count(d => d.IsCurrentMonth && d.CheckIn.HasValue && d.LatenessMinutes.HasValue && d.LatenessMinutes.Value == 0 && (!_hireDate.HasValue || d.Date.Date >= _hireDate.Value) && d.Date.Date <= DateTime.Now.Date);
                return;
            }

            var empId = _sessionManager.CurrentUser.Employee.EmployeeId;
            var hire = _hireDate ?? _sessionManager.CurrentUser.Employee.HireDate.Date;

            // gather attendance records from hire month up to now
            var allAtts = new System.Collections.Generic.List<Attendance>();
            var m = new DateTime(hire.Year, hire.Month, 1);
            var searchEnd = DateTime.Now.Date;
            while (m <= searchEnd)
            {
                allAtts.AddRange(_attendanceRepository.GetAttendancesForEmployeeMonth(empId, m.Year, m.Month));
                m = m.AddMonths(1);
            }

            // determine last month that actually has attendance/OT data
            DateTime lastDataDate = allAtts.Select(a => (a.CheckIn ?? a.CheckOut ?? DateTime.Now).Date).DefaultIfEmpty(hire).Max();
            var lastDataMonthStart = new DateTime(lastDataDate.Year, lastDataDate.Month, 1);

            // decide which month we should accumulate up to: if viewing an earlier month use that month; if viewing beyond lastDataMonth use lastDataMonth
            var viewMonthStart = new DateTime(CurrentMonth.Year, CurrentMonth.Month, 1);
            DateTime accumulateUpToMonthStart = viewMonthStart <= lastDataMonthStart ? viewMonthStart : lastDataMonthStart;
            var accumulateEndDate = accumulateUpToMonthStart.AddMonths(1).AddDays(-1);
            if (accumulateEndDate > DateTime.Now.Date) accumulateEndDate = DateTime.Now.Date;

            // collect attendances up to accumulateEndDate
            var attUpTo = allAtts.Where(a => ((a.CheckIn ?? a.CheckOut ?? DateTime.Now).Date) <= accumulateEndDate).ToList();

            // build per-day grouping using earliest CheckIn when available
            var grouped = attUpTo.GroupBy(a => (a.CheckIn ?? a.CheckOut ?? DateTime.Now).Date)
                                 .Select(g => new
                                 {
                                     Date = g.Key,
                                     EarliestCheckIn = g.Where(x => x.CheckIn.HasValue).Select(x => x.CheckIn!.Value).OrderBy(x => x).FirstOrDefault() as DateTime?,
                                     HasCheckIn = g.Any(x => x.CheckIn.HasValue),
                                     Status = g.Select(x => x.Status).FirstOrDefault(s => !string.IsNullOrEmpty(s)) ?? string.Empty
                                 }).ToList();

            // total working days from hire up to accumulateEndDate (excluding weekends and before hire)
            var totalWorkingDays = 0;
            for (var d = hire.Date; d <= accumulateEndDate; d = d.AddDays(1))
            {
                if (d.DayOfWeek == System.DayOfWeek.Saturday || d.DayOfWeek == System.DayOfWeek.Sunday) continue;
                totalWorkingDays++;
            }

            var daysWorked = grouped.Count(g => g.HasCheckIn && g.Date >= hire.Date);
            var onTime = 0; var late = 0; var totalLateMinutes = 0;
            foreach (var g in grouped)
            {
                if (!g.HasCheckIn) continue;
                var shiftStart = g.Date.Add(ShiftStart);
                if (g.EarliestCheckIn.HasValue)
                {
                    var minutesLate = (int)Math.Round((g.EarliestCheckIn.Value - shiftStart).TotalMinutes);
                    if (minutesLate <= LateGraceMinutes) onTime++;
                    else { late++; totalLateMinutes += Math.Max(0, minutesLate); }
                }
            }

            var absent = Math.Max(0, totalWorkingDays - daysWorked);

            TotalDaysWorked = daysWorked;
            TotalLate = late;
            TotalLateMinutes = totalLateMinutes;
            TotalAbsent = absent;
            TotalOnTime = onTime;
        }

        private int _totalDaysWorked; public int TotalDaysWorked { get => _totalDaysWorked; set => SetProperty(ref _totalDaysWorked, value); }
        private int _totalLate; public int TotalLate { get => _totalLate; set => SetProperty(ref _totalLate, value); }
        private int _totalLateMinutes; public int TotalLateMinutes { get => _totalLateMinutes; set => SetProperty(ref _totalLateMinutes, value); }
        private int _totalAbsent; public int TotalAbsent { get => _totalAbsent; set => SetProperty(ref _totalAbsent, value); }
        private int _totalOnTime; public int TotalOnTime { get => _totalOnTime; set => SetProperty(ref _totalOnTime, value); }

        public class AttendanceDayViewModel : ViewModelBase
        {
            public AttendanceDayViewModel(DateTime date) { Date = date; IsWeekend = Date.DayOfWeek == System.DayOfWeek.Saturday || Date.DayOfWeek == System.DayOfWeek.Sunday; UpdateColor(); }
            public DateTime Date { get; }
            public bool IsCurrentMonth { get; set; }
            private DateTime? _checkIn; public DateTime? CheckIn { get => _checkIn; set => SetProperty(ref _checkIn, value); }
            private DateTime? _checkOut; public DateTime? CheckOut { get => _checkOut; set => SetProperty(ref _checkOut, value); }
            private string _status = string.Empty; public string Status { get => _status; set { if (SetProperty(ref _status, value)) OnPropertyChanged(nameof(DisplayStatus)); } }
            private System.Windows.Media.Brush _background = System.Windows.Media.Brushes.Transparent; public System.Windows.Media.Brush Background { get => _background; set => SetProperty(ref _background, value); }
            private bool _isBeforeHireDate; public bool IsBeforeHireDate { get => _isBeforeHireDate; set => SetProperty(ref _isBeforeHireDate, value); }
            private int? _latenessMinutes; public int? LatenessMinutes { get => _latenessMinutes; set { if (SetProperty(ref _latenessMinutes, value)) OnPropertyChanged(nameof(DisplayStatus)); } }
            private bool _isWeekend; public bool IsWeekend { get => _isWeekend; set => SetProperty(ref _isWeekend, value); }
            // optional scheduled times that may be populated from attendance records
            private DateTime? _scheduledStart; public DateTime? ScheduledStart { get => _scheduledStart; set => SetProperty(ref _scheduledStart, value); }
            private DateTime? _scheduledEnd; public DateTime? ScheduledEnd { get => _scheduledEnd; set => SetProperty(ref _scheduledEnd, value); }
            public string DisplayStatus { get { if (!string.IsNullOrEmpty(Status) && Status.Equals("Late", StringComparison.OrdinalIgnoreCase) && LatenessMinutes.HasValue && LatenessMinutes.Value > 0) return $"Late: {LatenessMinutes.Value} min"; if (!string.IsNullOrEmpty(Status)) return Status; return string.Empty; } }

            public bool ShouldShowTimes
            {
                get
                {
                    if (IsBeforeHireDate) return false;
                    if (IsWeekend)
                    {
                        // only show times on weekends when OT or explicit check-in/out exists
                        return !string.IsNullOrEmpty(Status) && Status.Equals("OT", StringComparison.OrdinalIgnoreCase)
                               || CheckIn.HasValue
                               || CheckOut.HasValue;
                    }

                    // weekdays: show times (may be empty) unless before hire
                    // For future dates, still show times if an OT/attendance record exists
                    if (Date.Date > DateTime.Now.Date)
                    {
                        return (!string.IsNullOrEmpty(Status) && Status.Equals("OT", StringComparison.OrdinalIgnoreCase)) || CheckIn.HasValue || CheckOut.HasValue;
                    }

                    return true;
                }
            }

            public void UpdateColor()
            {
                if (!IsCurrentMonth) { Background = System.Windows.Media.Brushes.Transparent; return; }
                // OT should always render as purple regardless of weekend/weekday or lateness
                if (!string.IsNullOrEmpty(Status) && Status.Equals("OT", StringComparison.OrdinalIgnoreCase))
                {
                    Background = System.Windows.Media.Brushes.MediumPurple;
                    LatenessMinutes = null;
                    return;
                }
                if (IsWeekend)
                {
                    // Nếu cuối tuần nhưng có dữ liệu chấm công (OT) thì hiển thị như ngày làm
                    // treat weekend as workday when there's an OT record (Status=="OT") or explicit check times
                    if ((!string.IsNullOrEmpty(Status) && Status.Equals("OT", StringComparison.OrdinalIgnoreCase)) || CheckIn.HasValue || CheckOut.HasValue)
                    {
                        if (LatenessMinutes.HasValue && LatenessMinutes.Value > 0)
                        {
                            Background = System.Windows.Media.Brushes.Orange;
                        }
                        else
                        {
                            Background = System.Windows.Media.Brushes.MediumPurple;   // hoặc LightGreen
                        }

                        if (string.IsNullOrWhiteSpace(Status))
                            Status = "OT";

                        return;
                    }

                    Background = System.Windows.Media.Brushes.LightSteelBlue;
                    Status = "Weekend";
                    LatenessMinutes = null;
                    return;
                }
                if (Date.Date > DateTime.Now.Date)
                {
                    // For future dates, preserve OT or explicit attendance so scheduled OT shows on calendar;
                    // otherwise render as neutral/empty.
                    if ((!string.IsNullOrEmpty(Status) && Status.Equals("OT", StringComparison.OrdinalIgnoreCase)) || CheckIn.HasValue || CheckOut.HasValue)
                    {
                        // allow normal coloring logic to run below
                    }
                    else
                    {
                        Background = System.Windows.Media.Brushes.White; Status = string.Empty; LatenessMinutes = null; return;
                    }
                }
                if (IsBeforeHireDate) { Background = System.Windows.Media.Brushes.LightGray; Status = "Before Hire Date"; LatenessMinutes = null; return; }
                if (!CheckIn.HasValue) { Background = System.Windows.Media.Brushes.LightCoral; Status = "Absent"; LatenessMinutes = null; return; }
                if (LatenessMinutes.HasValue && LatenessMinutes.Value > 0) { Background = System.Windows.Media.Brushes.Orange; return; }
                Background = System.Windows.Media.Brushes.LightGreen;
            }

            public bool IsWorkday() { return Date.DayOfWeek != System.DayOfWeek.Saturday && Date.DayOfWeek != System.DayOfWeek.Sunday; }
        }
    }
}