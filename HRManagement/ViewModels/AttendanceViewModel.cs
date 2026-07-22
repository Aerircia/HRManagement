using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using HRManagement.Models;
using HRManagement.Repositories;
using HRManagement.Services;
using HRManagement.Utilities;

namespace HRManagement.ViewModels
{
    public class AttendanceViewModel : PageViewModel
    {
        private DateTime? _hireDate;
        public int LateGraceMinutes { get; set; } = 5;
        public TimeSpan ShiftStart => TimeSpan.FromHours(8);

        private readonly AttendanceRepository _attendanceRepository;
        private readonly SessionManager _sessionManager;

        public AttendanceViewModel(SessionManager sessionManager, AttendanceRepository attendanceRepository)
        {
            _sessionManager = sessionManager;
            _attendanceRepository = attendanceRepository;

            if (_sessionManager.CurrentUser != null)
            {
                _hireDate = _sessionManager.CurrentUser.Employee.HireDate.Date;
                CurrentMonth = new DateTime(_hireDate.Value.Year, _hireDate.Value.Month, 1);
            }
            else
            {
                CurrentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            }

            Days = new ObservableCollection<AttendanceDayViewModel>();

            _prevCommand = new RelayCommand(_ => ChangeMonth(-1));
            _nextCommand = new RelayCommand(_ => ChangeMonth(1));
            _checkInCommand = new RelayCommand(_ => CheckIn(), _ => CanCheckInLogic());
            _checkOutCommand = new RelayCommand(_ => CheckOut(), _ => CanCheckOutLogic());

            _sessionManager.OnUserChanged += SessionManager_OnUserChanged;

            BuildMonth();
        }

        public override string Title => "Attendance";

        private DateTime _currentMonth;
        public DateTime CurrentMonth { get => _currentMonth; set => SetProperty(ref _currentMonth, value); }

        public ObservableCollection<AttendanceDayViewModel> Days { get; }

        private RelayCommand? _prevCommand;
        private RelayCommand? _nextCommand;
        private RelayCommand? _checkInCommand;
        private RelayCommand? _checkOutCommand;

        public ICommand PrevMonthCommand => _prevCommand!;
        public ICommand NextMonthCommand => _nextCommand!;
        public ICommand CheckInCommand => _checkInCommand!;
        public ICommand CheckOutCommand => _checkOutCommand!;

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

            if (_sessionManager.CurrentUser != null)
            {
                var empId = _sessionManager.CurrentUser.Employee.EmployeeId;
                var list = _attendanceRepository.GetAttendancesForEmployeeMonth(empId, CurrentMonth.Year, CurrentMonth.Month);
                foreach (var att in list)
                {
                    var dateKey = (att.CheckIn ?? att.CheckOut ?? DateTime.Now).Date;
                    var day = Days.FirstOrDefault(d => d.Date.Date == dateKey);
                    if (day != null)
                    {
                        day.CheckIn = att.CheckIn; day.CheckOut = att.CheckOut;
                        if (day.CheckIn.HasValue)
                        {
                            var shiftStart = day.Date.Add(ShiftStart);
                            var minutesLate = (int)Math.Round((day.CheckIn.Value - shiftStart).TotalMinutes);
                            if (minutesLate <= LateGraceMinutes) { day.LatenessMinutes = 0; day.Status = "OnTime"; }
                            else { day.LatenessMinutes = Math.Max(0, minutesLate); day.Status = "Late"; }
                        }
                        else { day.LatenessMinutes = null; day.Status = att.Status; }
                    }
                }
            }

            foreach (var d in Days) d.UpdateColor();
            _checkInCommand?.RaiseCanExecuteChanged(); _checkOutCommand?.RaiseCanExecuteChanged();
            UpdateSummary();
        }

        private bool CanCheckInLogic()
        {
            var today = DateTime.Now.Date;
            if (today.DayOfWeek == System.DayOfWeek.Saturday || today.DayOfWeek == System.DayOfWeek.Sunday) return false;
            if (_hireDate.HasValue && today < _hireDate.Value) return false;
            var cell = Days.FirstOrDefault(d => d.Date.Date == today); if (cell == null) return false;
            if (cell.CheckIn.HasValue) return false; return true;
        }

        private bool CanCheckOutLogic()
        {
            var today = DateTime.Now.Date; if (today.DayOfWeek == System.DayOfWeek.Saturday || today.DayOfWeek == System.DayOfWeek.Sunday) return false;
            if (_hireDate.HasValue && today < _hireDate.Value) return false; var cell = Days.FirstOrDefault(d => d.Date.Date == today); if (cell == null) return false;
            if (!cell.CheckIn.HasValue) return false; if (cell.CheckOut.HasValue) return false; return true;
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

        private void UpdateSummary()
        {
            TotalDaysWorked = Days.Count(d => d.IsCurrentMonth && d.CheckIn.HasValue && (!_hireDate.HasValue || d.Date.Date >= _hireDate.Value) && d.Date.Date <= DateTime.Now.Date);
            TotalLate = Days.Count(d => d.IsCurrentMonth && d.LatenessMinutes.HasValue && d.LatenessMinutes > 0 && (!_hireDate.HasValue || d.Date.Date >= _hireDate.Value) && d.Date.Date <= DateTime.Now.Date);
            TotalLateMinutes = Days.Where(d => d.IsCurrentMonth && d.LatenessMinutes.HasValue && (!_hireDate.HasValue || d.Date.Date >= _hireDate.Value) && d.Date.Date <= DateTime.Now.Date).Sum(d => d.LatenessMinutes ?? 0);
            TotalAbsent = Days.Count(d => d.IsCurrentMonth && !d.CheckIn.HasValue && d.IsWorkday() && (!_hireDate.HasValue || d.Date.Date >= _hireDate.Value) && d.Date.Date <= DateTime.Now.Date);
            TotalOnTime = Days.Count(d => d.IsCurrentMonth && d.CheckIn.HasValue && d.LatenessMinutes.HasValue && d.LatenessMinutes.Value == 0 && (!_hireDate.HasValue || d.Date.Date >= _hireDate.Value) && d.Date.Date <= DateTime.Now.Date);
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
            public string DisplayStatus { get { if (!string.IsNullOrEmpty(Status) && Status.Equals("Late", StringComparison.OrdinalIgnoreCase) && LatenessMinutes.HasValue && LatenessMinutes.Value > 0) return $"Late: {LatenessMinutes.Value} min"; if (!string.IsNullOrEmpty(Status)) return Status; return string.Empty; } }

            public void UpdateColor()
            {
                if (!IsCurrentMonth) { Background = System.Windows.Media.Brushes.Transparent; return; }
                if (IsWeekend) { Background = System.Windows.Media.Brushes.LightSteelBlue; Status = "Weekend"; LatenessMinutes = null; return; }
                if (Date.Date > DateTime.Now.Date) { Background = System.Windows.Media.Brushes.White; Status = string.Empty; LatenessMinutes = null; return; }
                if (IsBeforeHireDate) { Background = System.Windows.Media.Brushes.LightGray; Status = "Before Hire Date"; LatenessMinutes = null; return; }
                if (!CheckIn.HasValue) { Background = System.Windows.Media.Brushes.LightCoral; Status = "Absent"; LatenessMinutes = null; return; }
                if (LatenessMinutes.HasValue && LatenessMinutes.Value > 0) { Background = System.Windows.Media.Brushes.Orange; return; }
                Background = System.Windows.Media.Brushes.LightGreen;
            }

            public bool IsWorkday() { return Date.DayOfWeek != System.DayOfWeek.Saturday && Date.DayOfWeek != System.DayOfWeek.Sunday; }
        }
    }
}