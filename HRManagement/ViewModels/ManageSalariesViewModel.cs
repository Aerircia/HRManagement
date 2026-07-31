using HRManagement.Models;
using HRManagement.Services;
using HRManagement.Services.Interfaces;
using HRManagement.Utilities;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace HRManagement.ViewModels;

public class ManageSalariesViewModel : PageViewModel
{
    private readonly IManageSalariesService
        _manageSalariesService;
    private readonly ILogService _logService;
    private readonly SessionManager _sessionManager;

    private readonly List<ManageSalariesItemModel>
        _allSalaries = new();

    private ManageSalariesItemModel?
        _selectedSalary;

    private int _selectedMonth;

    private int _selectedYear;

    private string _searchText =
        string.Empty;

    private string _statusMessage =
        string.Empty;

    private bool _isLoading;

    private decimal _editingBaseSalary;

    private decimal _editingPayRate;

    public override string Title =>
        "Manage Salaries";

    // =========================================================
    // Collections
    // =========================================================

    public ObservableCollection<
        ManageSalariesItemModel> Salaries
    {
        get;
    }

    public ObservableCollection<int> Months
    {
        get;
    }

    public ObservableCollection<int> Years
    {
        get;
    }

    // =========================================================
    // Selection
    // =========================================================

    public ManageSalariesItemModel?
        SelectedSalary
    {
        get => _selectedSalary;

        set
        {
            if (!SetProperty(
                    ref _selectedSalary,
                    value))
            {
                return;
            }

            LoadSelectedSalaryValues();

            OnPropertyChanged(
                nameof(HasSelectedSalary));

            OnPropertyChanged(
                nameof(SelectedEmployeeName));

            OnPropertyChanged(
                nameof(SelectedPayrollStatus));

            OnPropertyChanged(
                nameof(CanEditSelectedSalary));

            CommandManager
                .InvalidateRequerySuggested();
        }
    }

    public bool HasSelectedSalary =>
        SelectedSalary != null;

    public string SelectedEmployeeName =>
        SelectedSalary?.FullName
        ?? "No employee selected";

    public string SelectedPayrollStatus =>
        SelectedSalary?.PayrollStatus
        ?? string.Empty;

    public bool CanEditSelectedSalary =>
        SelectedSalary != null
        && SelectedSalary.HasValidContract
        && SelectedSalary.HasValidRole
        && !IsLoading;

    // =========================================================
    // Salary period
    // =========================================================

    public int SelectedMonth
    {
        get => _selectedMonth;

        set
        {
            if (!SetProperty(
                    ref _selectedMonth,
                    value))
            {
                return;
            }

            OnPropertyChanged(
                nameof(SelectedPeriod));

            CommandManager
                .InvalidateRequerySuggested();
        }
    }

    public int SelectedYear
    {
        get => _selectedYear;

        set
        {
            if (!SetProperty(
                    ref _selectedYear,
                    value))
            {
                return;
            }

            OnPropertyChanged(
                nameof(SelectedPeriod));

            CommandManager
                .InvalidateRequerySuggested();
        }
    }

    public string SelectedPeriod =>
        $"{SelectedMonth:00}/{SelectedYear}";

    // =========================================================
    // Search
    // =========================================================

    public string SearchText
    {
        get => _searchText;

        set
        {
            if (!SetProperty(
                    ref _searchText,
                    value))
            {
                return;
            }

            ApplySearchFilter();
        }
    }

    // =========================================================
    // Editing
    // =========================================================

    public decimal EditingBaseSalary
    {
        get => _editingBaseSalary;

        set
        {
            if (!SetProperty(
                    ref _editingBaseSalary,
                    value))
            {
                return;
            }

            OnPropertyChanged(
                nameof(PreviewRoleSalary));

            OnPropertyChanged(
                nameof(PreviewDailySalary));

            OnPropertyChanged(
                nameof(PreviewAttendanceSalary));

            OnPropertyChanged(
                nameof(PreviewTotalSalary));

            CommandManager
                .InvalidateRequerySuggested();
        }
    }

    public decimal EditingPayRate
    {
        get => _editingPayRate;

        set
        {
            if (!SetProperty(
                    ref _editingPayRate,
                    value))
            {
                return;
            }

            OnPropertyChanged(
                nameof(PreviewRoleSalary));

            OnPropertyChanged(
                nameof(PreviewDailySalary));

            OnPropertyChanged(
                nameof(PreviewAttendanceSalary));

            OnPropertyChanged(
                nameof(PreviewTotalSalary));

            CommandManager
                .InvalidateRequerySuggested();
        }
    }

    public decimal PreviewRoleSalary =>
        EditingBaseSalary
        * EditingPayRate;

    public decimal PreviewDailySalary =>
        PreviewRoleSalary / 26m;

    public decimal PreviewAttendanceSalary =>
        SelectedSalary == null
            ? 0
            : PreviewDailySalary
              * SelectedSalary.WorkingDays;

    public decimal PreviewTotalSalary =>
        SelectedSalary == null
            ? 0
            : decimal.Round(
                PreviewAttendanceSalary
                + SelectedSalary.Reward
                - SelectedSalary.Penalty,
                0,
                MidpointRounding.AwayFromZero);

    // =========================================================
    // UI states
    // =========================================================

    public string StatusMessage
    {
        get => _statusMessage;

        private set => SetProperty(
            ref _statusMessage,
            value);
    }

    public bool IsLoading
    {
        get => _isLoading;

        private set
        {
            if (!SetProperty(
                    ref _isLoading,
                    value))
            {
                return;
            }

            OnPropertyChanged(
                nameof(IsNotLoading));

            OnPropertyChanged(
                nameof(CanEditSelectedSalary));

            CommandManager
                .InvalidateRequerySuggested();
        }
    }

    public bool IsNotLoading =>
        !IsLoading;

    public bool HasData =>
        Salaries.Count > 0;

    public int TotalEmployees =>
        Salaries.Count;

    public int CreatedPayrollCount =>
        Salaries.Count(
            item => item.IsPayrollCreated);

    public int PendingPayrollCount =>
        Salaries.Count(
            item => !item.IsPayrollCreated);

    public decimal TotalMonthlySalary =>
        Salaries.Sum(
            item => item.TotalSalary);

    // =========================================================
    // Commands
    // =========================================================

    public ICommand LoadSalariesCommand
    {
        get;
    }

    public ICommand RefreshCommand
    {
        get;
    }

    public ICommand ClearSearchCommand
    {
        get;
    }

    public ICommand SaveSalaryComponentsCommand
    {
        get;
    }

    public ICommand ResetSalaryComponentsCommand
    {
        get;
    }

    public ICommand CreatePayrollCommand
    {
        get;
    }

    public ICommand CreateMonthlyPayrollCommand
    {
        get;
    }

    public ICommand DeletePayrollCommand
    {
        get;
    }

    // =========================================================
    // Constructor
    // =========================================================

    public ManageSalariesViewModel(
        IManageSalariesService
            manageSalariesService,
        SessionManager sessionManager,
        ILogService logService)
    {
        _sessionManager = sessionManager;
        _logService = logService;
        _manageSalariesService =
            manageSalariesService
            ?? throw new ArgumentNullException(
                nameof(manageSalariesService));

        Salaries =
            new ObservableCollection<
                ManageSalariesItemModel>();

        Months =
            new ObservableCollection<int>(
                Enumerable.Range(1, 12));

        var currentDate =
            DateTime.Today;

        Years =
            CreateYearCollection(
                currentDate.Year);

        _selectedMonth =
            currentDate.Month;

        _selectedYear =
            currentDate.Year;

        LoadSalariesCommand =
            new RelayCommand(
                _ => LoadSalaries(),
                _ => CanLoadSalaries());

        RefreshCommand =
            new RelayCommand(
                _ => RefreshSalaries(),
                _ => !IsLoading);

        ClearSearchCommand =
            new RelayCommand(
                _ => ClearSearch(),
                _ => !string.IsNullOrWhiteSpace(
                    SearchText));

        SaveSalaryComponentsCommand =
            new RelayCommand(
                _ => SaveSalaryComponents(),
                _ => CanSaveSalaryComponents());

        ResetSalaryComponentsCommand =
            new RelayCommand(
                _ => ResetSalaryComponents(),
                _ => CanEditSelectedSalary);

        CreatePayrollCommand =
            new RelayCommand(
                _ => CreateSelectedPayroll(),
                _ => CanCreateSelectedPayroll());

        CreateMonthlyPayrollCommand =
            new RelayCommand(
                _ => CreateMonthlyPayroll(),
                _ => CanCreateMonthlyPayroll());

        DeletePayrollCommand =
            new RelayCommand(
                _ => DeleteSelectedPayroll(),
                _ => CanDeleteSelectedPayroll());

        LoadSalaries();
    }

    // =========================================================
    // Load salaries
    // =========================================================

    private bool CanLoadSalaries()
    {
        return !IsLoading
               && SelectedMonth is >= 1 and <= 12
               && SelectedYear >= 2000;
    }

    private void LoadSalaries()
    {
        if (!CanLoadSalaries())
            return;

        IsLoading = true;
        StatusMessage = string.Empty;

        try
        {
            IReadOnlyList<
                ManageSalariesItemModel> salaries =
                _manageSalariesService
                    .GetMonthlySalaries(
                        SelectedMonth,
                        SelectedYear);

            _allSalaries.Clear();
            _allSalaries.AddRange(salaries);

            ApplySearchFilter();

            StatusMessage =
                salaries.Count == 0
                    ? $"No employees were found for " +
                      $"{SelectedPeriod}."
                    : $"Loaded {salaries.Count} employee " +
                      $"salary records for " +
                      $"{SelectedPeriod}.";
        }
        catch (ArgumentException exception)
        {
            ClearSalaryData();

            StatusMessage =
                exception.Message;
        }
        catch (InvalidOperationException exception)
        {
            ClearSalaryData();

            StatusMessage =
                exception.Message;
        }
        catch (Exception exception)
        {
            ClearSalaryData();

            StatusMessage =
                $"Unable to load salary data. " +
                $"{exception.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void RefreshSalaries()
    {
        LoadSalaries();
    }

    // =========================================================
    // Search
    // =========================================================

    private void ApplySearchFilter()
    {
        int? selectedEmployeeId =
            SelectedSalary?.EmployeeId;

        string keyword =
            SearchText.Trim();

        IEnumerable<
            ManageSalariesItemModel> filteredItems =
                _allSalaries;

        if (!string.IsNullOrWhiteSpace(
                keyword))
        {
            filteredItems =
                _allSalaries.Where(
                    item =>
                        ContainsIgnoreCase(
                            item.EmployeeId
                                .ToString(),
                            keyword)
                        || ContainsIgnoreCase(
                            item.FullName,
                            keyword)
                        || ContainsIgnoreCase(
                            item.DepartmentName,
                            keyword)
                        || ContainsIgnoreCase(
                            item.RoleName,
                            keyword)
                        || ContainsIgnoreCase(
                            item.PayrollStatus,
                            keyword));
        }

        Salaries.Clear();

        foreach (var item in filteredItems)
        {
            Salaries.Add(item);
        }

        SelectedSalary =
            selectedEmployeeId.HasValue
                ? Salaries.FirstOrDefault(
                    item =>
                        item.EmployeeId
                        == selectedEmployeeId.Value)
                : null;

        NotifySummaryProperties();
    }

    private void ClearSearch()
    {
        SearchText = string.Empty;
    }

    // =========================================================
    // Selected salary
    // =========================================================

    private void LoadSelectedSalaryValues()
    {
        if (SelectedSalary == null)
        {
            EditingBaseSalary = 0;
            EditingPayRate = 0;

            return;
        }

        EditingBaseSalary =
            SelectedSalary.BaseSalary;

        EditingPayRate =
            SelectedSalary.PayRate;
    }

    private void ResetSalaryComponents()
    {
        LoadSelectedSalaryValues();

        StatusMessage =
            "Salary component changes were reset.";
    }

    // =========================================================
    // Update salary components
    // =========================================================

    private bool CanSaveSalaryComponents()
    {
        return CanEditSelectedSalary
               && EditingBaseSalary >= 0
               && EditingPayRate > 0;
    }

    private void SaveSalaryComponents()
    {
        if (!CanSaveSalaryComponents()
            || SelectedSalary == null)
        {
            return;
        }

        IsLoading = true;
        StatusMessage = string.Empty;

        try
        {
            int employeeId =
                SelectedSalary.EmployeeId;

            _manageSalariesService
                .UpdateSalaryComponents(
                    SelectedSalary.ContractId,
                    EditingBaseSalary,
                    SelectedSalary.RoleId,
                    EditingPayRate);

            ReloadAndSelectEmployee(
                employeeId);

            StatusMessage =
                $"Salary components for " +
                $"{SelectedEmployeeName} " +
                $"were updated successfully.";
        }
        catch (ArgumentException exception)
        {
            StatusMessage =
                exception.Message;
        }
        catch (InvalidOperationException exception)
        {
            StatusMessage =
                exception.Message;
        }
        catch (Exception exception)
        {
            StatusMessage =
                $"Unable to update salary components. " +
                $"{exception.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // =========================================================
    // Create payroll
    // =========================================================

    private bool CanCreateSelectedPayroll()
    {
        return !IsLoading
               && SelectedSalary != null
               && SelectedSalary.CanCreatePayroll;
    }

    private void CreateSelectedPayroll()
    {
        if (!CanCreateSelectedPayroll()
            || SelectedSalary == null)
        {
            return;
        }

        IsLoading = true;
        StatusMessage = string.Empty;

        try
        {
            int employeeId =
                SelectedSalary.EmployeeId;

            string employeeName =
                SelectedSalary.FullName;

            _manageSalariesService
                .CreateEmployeePayroll(
                    employeeId,
                    SelectedMonth,
                    SelectedYear);

            ReloadAndSelectEmployee(
                employeeId);

            StatusMessage =
                $"Payroll for {employeeName} in " +
                $"{SelectedPeriod} was created successfully.";
            _logService.WriteLog(_sessionManager.CurrentUser!.Employee.EmployeeId, $"Created payroll for {employeeName} ({SelectedPeriod})");
        }
        catch (ArgumentException exception)
        {
            StatusMessage =
                exception.Message;
        }
        catch (InvalidOperationException exception)
        {
            StatusMessage =
                exception.Message;
        }
        catch (Exception exception)
        {
            StatusMessage =
                $"Unable to create payroll. " +
                $"{exception.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanCreateMonthlyPayroll()
    {
        return !IsLoading
               && _allSalaries.Any(
                   item =>
                       item.CanCreatePayroll);
    }

    private void CreateMonthlyPayroll()
    {
        if (!CanCreateMonthlyPayroll())
            return;

        IsLoading = true;
        StatusMessage = string.Empty;

        try
        {
            int createdCount =
                _manageSalariesService
                    .CreateMonthlyPayroll(
                        SelectedMonth,
                        SelectedYear);

            ReloadSalaryData();
            _logService.WriteLog(_sessionManager.CurrentUser!.Employee.EmployeeId, $"Created {createdCount} payroll(s) for {SelectedPeriod}");
            StatusMessage =
                createdCount == 0
                    ? $"No new payroll records were " +
                      $"created for {SelectedPeriod}."
                    : $"{createdCount} payroll records " +
                      $"were created successfully for " +
                      $"{SelectedPeriod}.";
        }
        catch (ArgumentException exception)
        {
            StatusMessage =
                exception.Message;
        }
        catch (InvalidOperationException exception)
        {
            StatusMessage =
                exception.Message;
        }
        catch (Exception exception)
        {
            StatusMessage =
                $"Unable to create monthly payroll. " +
                $"{exception.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // =========================================================
    // Delete payroll
    // =========================================================

    private bool CanDeleteSelectedPayroll()
    {
        return !IsLoading
               && SelectedSalary != null
               && SelectedSalary.IsPayrollCreated;
    }

    private void DeleteSelectedPayroll()
    {
        if (!CanDeleteSelectedPayroll()
            || SelectedSalary == null)
        {
            return;
        }

        IsLoading = true;
        StatusMessage = string.Empty;

        try
        {
            int employeeId =
                SelectedSalary.EmployeeId;

            string employeeName =
                SelectedSalary.FullName;

            _manageSalariesService
                .DeleteEmployeePayroll(
                    employeeId,
                    SelectedMonth,
                    SelectedYear);

            ReloadAndSelectEmployee(
                employeeId);
            _logService.WriteLog(_sessionManager.CurrentUser!.Employee.EmployeeId, $"Deleted payroll for {employeeName} ({SelectedPeriod})");
            StatusMessage =
                $"Payroll for {employeeName} in " +
                $"{SelectedPeriod} was deleted successfully.";
        }
        catch (ArgumentException exception)
        {
            StatusMessage =
                exception.Message;
        }
        catch (InvalidOperationException exception)
        {
            StatusMessage =
                exception.Message;
        }
        catch (Exception exception)
        {
            StatusMessage =
                $"Unable to delete payroll. " +
                $"{exception.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // =========================================================
    // Reload helpers
    // =========================================================

    private void ReloadAndSelectEmployee(
        int employeeId)
    {
        ReloadSalaryData();

        SelectedSalary =
            Salaries.FirstOrDefault(
                item =>
                    item.EmployeeId
                    == employeeId);
    }

    private void ReloadSalaryData()
    {
        IReadOnlyList<
            ManageSalariesItemModel> salaries =
                _manageSalariesService
                    .GetMonthlySalaries(
                        SelectedMonth,
                        SelectedYear);

        _allSalaries.Clear();
        _allSalaries.AddRange(salaries);

        ApplySearchFilter();
    }

    private void ClearSalaryData()
    {
        _allSalaries.Clear();

        Salaries.Clear();

        SelectedSalary = null;

        NotifySummaryProperties();
    }

    // =========================================================
    // Property notifications
    // =========================================================

    private void NotifySummaryProperties()
    {
        OnPropertyChanged(
            nameof(HasData));

        OnPropertyChanged(
            nameof(TotalEmployees));

        OnPropertyChanged(
            nameof(CreatedPayrollCount));

        OnPropertyChanged(
            nameof(PendingPayrollCount));

        OnPropertyChanged(
            nameof(TotalMonthlySalary));

        CommandManager
            .InvalidateRequerySuggested();
    }

    private static bool ContainsIgnoreCase(
        string? source,
        string keyword)
    {
        if (string.IsNullOrWhiteSpace(source))
            return false;

        return source.Contains(
            keyword,
            StringComparison.OrdinalIgnoreCase);
    }

    private static ObservableCollection<int>
        CreateYearCollection(
            int currentYear)
    {
        const int previousYears = 5;

        const int followingYears = 1;

        int firstYear =
            currentYear - previousYears;

        int numberOfYears =
            previousYears
            + followingYears
            + 1;

        IEnumerable<int> years =
            Enumerable
                .Range(
                    firstYear,
                    numberOfYears)
                .OrderByDescending(
                    year => year);

        return new ObservableCollection<int>(
            years);
    }
}