using HRManagement.Models;
using HRManagement.Services;
using HRManagement.Services.Interfaces;
using HRManagement.Utilities;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace HRManagement.ViewModels;

public class SalaryViewModel : PageViewModel
{
    private readonly IManageSalariesService _manageSalariesService;
    private readonly SessionManager _sessionManager;
    private SalaryDetailModel? _salary;

    private int _selectedMonth;
    private int _selectedYear;
    private string _statusMessage = string.Empty;
    private bool _isLoading;
    private bool _hasSalary;
    public override string Title => "Salary";

    public SalaryDetailModel? Salary
    {
        get => _salary;

        private set
        {
            if (!SetProperty(ref _salary, value))
            {
                return;
            }

            HasSalary = value != null;

            OnPropertyChanged(nameof(RoleSalary));
            OnPropertyChanged(nameof(DailySalary));
            OnPropertyChanged(nameof(AttendanceSalary));
            OnPropertyChanged(nameof(SalaryPeriod));
            OnPropertyChanged(nameof(OvertimeSalary));
            OnPropertyChanged(nameof(TotalDeductions));
        }
    }

    public ObservableCollection<int> Months
    {
        get;
    }
    public ObservableCollection<int> Years
    {
        get;
    }

    public int SelectedMonth
    {
        get => _selectedMonth;

        set
        {
            if (!SetProperty(ref _selectedMonth, value))
            {
                return;
            }

            OnPropertyChanged(nameof(SelectedPeriod));
        }
    }

    public int SelectedYear
    {
        get => _selectedYear;

        set
        {
            if (!SetProperty(ref _selectedYear, value))
            {
                return;
            }

            OnPropertyChanged(nameof(SelectedPeriod));
        }
    }

    public string SelectedPeriod => $"{SelectedMonth:00}/{SelectedYear}";

    public string SalaryPeriod => Salary == null ? SelectedPeriod : $"{Salary.Month:00}/{Salary.Year}";

    public string StatusMessage
    {
        get => _statusMessage;

        private set => SetProperty(ref _statusMessage, value ?? string.Empty);
    }

    public bool IsLoading
    {
        get => _isLoading;

        private set
        {
            if (!SetProperty(ref _isLoading, value))
            {
                return;
            }

            CommandManager.InvalidateRequerySuggested();
        }
    }

    public bool HasSalary
    {
        get => _hasSalary;

        private set => SetProperty(ref _hasSalary, value);
    }

    public decimal RoleSalary => Salary?.RoleSalary ?? 0;

    public decimal DailySalary => Salary?.DailySalary ?? 0;

    /*
     * Compatibility property for the current SalaryView.
     *
     * The old View treated AttendanceSalary as a separate amount.
     * Under the new model, the regular role salary is the full base
     * amount before attendance/PTO deductions.
     */
    public decimal AttendanceSalary =>
        Salary == null ? 0 : Salary.RoleSalary - Salary.AbsentDeduction - Salary.UnpaidDayOffDeduction;

    public decimal OvertimeSalary => Salary?.OvertimeSalary ?? 0;

    public decimal TotalDeductions => Salary?.TotalDeductions ?? 0;

    public ICommand LoadSalaryCommand
    {
        get;
    }

    public SalaryViewModel(
        IManageSalariesService manageSalariesService,
        SessionManager sessionManager)
    {
        _manageSalariesService = manageSalariesService ?? throw new ArgumentNullException(nameof(manageSalariesService));
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));

        var currentDate = DateTime.Today;
        Months = new ObservableCollection<int>(Enumerable.Range(1, 12));
        Years = CreateYearCollection(currentDate.Year);

        _selectedMonth = currentDate.Month;
        _selectedYear = currentDate.Year;

        LoadSalaryCommand = new RelayCommand(_ => LoadSalary(), _ => CanLoadSalary());

        LoadSalary();
    }

    private bool CanLoadSalary()
    {
        return !IsLoading && SelectedMonth is >= 1 and <= 12 && SelectedYear >= 2000;
    }

    private void LoadSalary()
    {
        if (!CanLoadSalary())
            return;

        Salary = null;
        StatusMessage = string.Empty;

        IsLoading = true;

        try
        {
            var currentUser = _sessionManager.CurrentUser;
            if (currentUser == null)
            {
                StatusMessage = "The login session could not be found.";

                return;
            }

            var employeeId = currentUser.Employee.EmployeeId;

            var result = _manageSalariesService.GetMySalaryDetail(
                employeeId,
                SelectedMonth,
                SelectedYear);

            Salary = result.Salary;
            StatusMessage = result.StatusMessage;
        }
        catch (ArgumentException exception)
        {
            Salary = null;
            StatusMessage = exception.Message;
        }
        catch (InvalidOperationException exception)
        {
            Salary = null;
            StatusMessage = exception.Message;
        }
        catch (Exception exception)
        {
            Salary = null;
            StatusMessage = $"Unable to load salary information. {exception.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static ObservableCollection<int> CreateYearCollection(int currentYear)
    {
        const int numberOfPreviousYears = 5;
        var firstYear = currentYear - numberOfPreviousYears;
        var years = Enumerable.Range(firstYear, numberOfPreviousYears + 1).OrderByDescending(year => year);

        return new ObservableCollection<int>(years);
    }
}
