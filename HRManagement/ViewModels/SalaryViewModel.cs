using HRManagement.Models;
using HRManagement.Repositories;
using HRManagement.Services;
using HRManagement.Services.Interfaces;
using HRManagement.Utilities;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace HRManagement.ViewModels;

public class SalaryViewModel : PageViewModel
{
    private readonly ISalaryRepository _salaryRepository;
    private readonly ISalaryCalculator _salaryCalculator;
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
                return;

            HasSalary = value != null;

            OnPropertyChanged(nameof(RoleSalary));
            OnPropertyChanged(nameof(DailySalary));
            OnPropertyChanged(nameof(AttendanceSalary));
            OnPropertyChanged(nameof(SalaryPeriod));
        }
    }

    public ObservableCollection<int> Months { get; }

    public ObservableCollection<int> Years { get; }

    public int SelectedMonth
    {
        get => _selectedMonth;
        set
        {
            if (SetProperty(ref _selectedMonth, value))
            {
                OnPropertyChanged(nameof(SelectedPeriod));
            }
        }
    }

    public int SelectedYear
    {
        get => _selectedYear;
        set
        {
            if (SetProperty(ref _selectedYear, value))
            {
                OnPropertyChanged(nameof(SelectedPeriod));
            }
        }
    }

    public string SelectedPeriod =>
        $"{SelectedMonth:00}/{SelectedYear}";

    public string SalaryPeriod =>
        Salary == null
            ? SelectedPeriod
            : $"{Salary.Month:00}/{Salary.Year}";

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (!SetProperty(ref _isLoading, value))
                return;

            CommandManager.InvalidateRequerySuggested();
        }
    }

    public bool HasSalary
    {
        get => _hasSalary;
        private set => SetProperty(ref _hasSalary, value);
    }

    public decimal RoleSalary =>
        Salary == null
            ? 0
            : Salary.BaseSalary * Salary.PayRate;

    public decimal DailySalary =>
        RoleSalary / 26m;

    public decimal AttendanceSalary =>
        Salary == null
            ? 0
            : DailySalary * Salary.WorkingDays;

    public ICommand LoadSalaryCommand { get; }

    public SalaryViewModel(
        ISalaryRepository salaryRepository,
        ISalaryCalculator salaryCalculator,
        SessionManager sessionManager)
    {
        _salaryRepository = salaryRepository
            ?? throw new ArgumentNullException(nameof(salaryRepository));

        _salaryCalculator = salaryCalculator
            ?? throw new ArgumentNullException(nameof(salaryCalculator));

        _sessionManager = sessionManager
            ?? throw new ArgumentNullException(nameof(sessionManager));

        var currentDate = DateTime.Today;

        Months = new ObservableCollection<int>(
            Enumerable.Range(1, 12));

        Years = CreateYearCollection(currentDate.Year);

        _selectedMonth = currentDate.Month;
        _selectedYear = currentDate.Year;

        LoadSalaryCommand = new RelayCommand(
            _ => LoadSalary(),
            _ => CanLoadSalary());

        LoadSalary();
    }

    private bool CanLoadSalary()
    {
        return !IsLoading
               && SelectedMonth is >= 1 and <= 12
               && SelectedYear >= 2000;
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
                StatusMessage =
                    "The login session could not be found.";

                return;
            }

            var employeeId =
                currentUser.Employee.EmployeeId;

            if (!_salaryRepository.PayrollExists(
                    employeeId,
                    SelectedMonth,
                    SelectedYear))
            {
                StatusMessage =
                    $"Payroll for {SelectedPeriod} has not been created.";

                return;
            }

            var employee =
                _salaryRepository.GetEmployee(employeeId);

            if (employee == null)
            {
                StatusMessage =
                    "Employee information could not be found.";

                return;
            }

            var contract =
                _salaryRepository.GetContractForPeriod(
                    employeeId,
                    SelectedMonth,
                    SelectedYear);

            if (contract == null)
            {
                StatusMessage =
                    $"No valid contract was found for {SelectedPeriod}.";

                return;
            }

            var role =
                _salaryRepository.GetRole(contract.RoleId);

            if (role == null)
            {
                StatusMessage =
                    "The role associated with the contract could not be found.";

                return;
            }

            var departmentName =
                _salaryRepository.GetDepartmentName(
                    employee.DepartmentId);

            var attendances =
                _salaryRepository.GetAttendances(
                    employeeId,
                    SelectedMonth,
                    SelectedYear);

            var evaluations =
                _salaryRepository.GetEvaluations(
                    employeeId,
                    SelectedMonth,
                    SelectedYear);

            Salary = _salaryCalculator.CalculateSalary(
                employee,
                contract,
                role,
                attendances,
                evaluations,
                departmentName,
                SelectedMonth,
                SelectedYear);

            StatusMessage =
                $"Salary information for {SelectedPeriod} was loaded successfully.";
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

            StatusMessage =
                $"Unable to load salary information. {exception.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static ObservableCollection<int>
        CreateYearCollection(int currentYear)
    {
        const int numberOfPreviousYears = 5;

        var firstYear =
            currentYear - numberOfPreviousYears;

        var years = Enumerable
            .Range(
                firstYear,
                numberOfPreviousYears + 1)
            .OrderByDescending(year => year);

        return new ObservableCollection<int>(years);
    }
}