using HRManagement.Models;
using HRManagement.Services;
using HRManagement.Services.Interfaces;
using HRManagement.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace HRManagement.ViewModels;

public class EmployeeEvaluationViewModel : PageViewModel
{
    public override string Title => "Employee Evaluations";
    private readonly IEmployeeEvaluationService
        _evaluationService;

    private readonly List<EvaluationEmployeeItemModel>
        _departmentSource = new();

    private string _searchText = string.Empty;

    private int _selectedMonth;

    private int _selectedYear;

    private Department? _selectedDepartment;

    private EvaluationEmployeeItemModel?
        _selectedEmployee;

    private EmployeeEvaluationItemModel?
        _selectedEvaluation;

    private string _selectedBonusType = "Reward";

    private string _evaluationType = string.Empty;

    private decimal _amount;

    private DateTime _bonusDate;

    private string _comment = string.Empty;

    private string _statusMessage = string.Empty;

    private bool _isLoading;

    public EmployeeEvaluationViewModel(
        IEmployeeEvaluationService evaluationService)
    {
        _evaluationService =
            evaluationService
            ?? throw new ArgumentNullException(
                nameof(evaluationService));

        Employees =
            new ObservableCollection<
                EvaluationEmployeeItemModel>();

        Evaluations =
            new ObservableCollection<
                EmployeeEvaluationItemModel>();

        Departments =
            new ObservableCollection<Department>();

        Months =
            new ObservableCollection<int>(
                Enumerable.Range(1, 12));

        Years =
            new ObservableCollection<int>(
                CreateYearRange());

        BonusTypes =
            new ObservableCollection<string>
            {
                    "Reward",
                    "Penalty"
            };

        EvaluationTypes =
            new ObservableCollection<string>
            {
                    "KPI",
                    "Attendance",
                    "Performance",
                    "Project",
                    "Discipline",
                    "Work Quality"
            };

        var today = DateTime.Today;

        _selectedMonth = today.Month;
        _selectedYear = today.Year;
        _bonusDate = today;

        LoadEmployeesCommand =
            new RelayCommand(
                _ => LoadEmployees(),
                _ => !IsLoading);

        RefreshCommand =
            new RelayCommand(
                _ => RefreshData(),
                _ => !IsLoading);

        SearchCommand =
            new RelayCommand(
                _ => LoadEmployees(),
                _ => !IsLoading);

        ClearSearchCommand =
            new RelayCommand(
                _ => ClearSearch(),
                _ =>
                    !IsLoading &&
                    !string.IsNullOrWhiteSpace(
                        SearchText));

        SaveEvaluationCommand =
            new RelayCommand(
                _ => SaveEvaluation(),
                _ => CanSaveEvaluation());

        DeleteEvaluationCommand =
            new RelayCommand(
                _ => DeleteSelectedEvaluation(),
                _ => CanDeleteEvaluation());

        ResetFormCommand =
            new RelayCommand(
                _ => ResetEvaluationForm(),
                _ => !IsLoading);

        SelectEvaluationCommand =
            new RelayCommand(
                parameter =>
                    SelectEvaluation(
                        parameter as
                            EmployeeEvaluationItemModel),
                _ => !IsLoading);
    }

    #region Collections

    public ObservableCollection<
        EvaluationEmployeeItemModel>
        Employees
    { get; }

    public ObservableCollection<
        EmployeeEvaluationItemModel>
        Evaluations
    { get; }

    public ObservableCollection<Department>
        Departments
    { get; }

    public ObservableCollection<int>
        Months
    { get; }

    public ObservableCollection<int>
        Years
    { get; }

    public ObservableCollection<string>
        BonusTypes
    { get; }

    public ObservableCollection<string>
        EvaluationTypes
    { get; }

    #endregion

    #region Filter properties

    public string SearchText
    {
        get => _searchText;

        set
        {
            if (!SetProperty(
                    ref _searchText,
                    value ?? string.Empty))
            {
                return;
            }

            CommandManager
                .InvalidateRequerySuggested();
        }
    }

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

            SetDefaultEvaluationDate();
            LoadEmployees();
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

            SetDefaultEvaluationDate();
            LoadEmployees();
        }
    }

    public string SelectedPeriod =>
        $"{SelectedMonth:00}/{SelectedYear}";

    public Department? SelectedDepartment
    {
        get => _selectedDepartment;

        set
        {
            if (!SetProperty(
                    ref _selectedDepartment,
                    value))
            {
                return;
            }

            LoadEmployees();
        }
    }

    #endregion

    #region Selection properties

    public EvaluationEmployeeItemModel?
        SelectedEmployee
    {
        get => _selectedEmployee;

        set
        {
            if (!SetProperty(
                    ref _selectedEmployee,
                    value))
            {
                return;
            }

            OnPropertyChanged(
                nameof(HasSelectedEmployee));

            OnPropertyChanged(
                nameof(SelectedEmployeeName));

            OnPropertyChanged(
                nameof(SelectedEmployeeDepartment));

            OnPropertyChanged(
                nameof(SelectedEmployeeRole));

            OnPropertyChanged(
                nameof(TotalReward));

            OnPropertyChanged(
                nameof(TotalPenalty));

            OnPropertyChanged(
                nameof(NetAdjustment));

            OnPropertyChanged(
                nameof(EvaluationCount));

            ResetEvaluationForm();
            LoadSelectedEmployeeEvaluations();

            CommandManager
                .InvalidateRequerySuggested();
        }
    }

    public EmployeeEvaluationItemModel?
        SelectedEvaluation
    {
        get => _selectedEvaluation;

        set
        {
            if (!SetProperty(
                    ref _selectedEvaluation,
                    value))
            {
                return;
            }

            OnPropertyChanged(
                nameof(IsEditMode));

            OnPropertyChanged(
                nameof(FormTitle));

            OnPropertyChanged(
                nameof(SaveButtonText));

            LoadSelectedEvaluationIntoForm();

            CommandManager
                .InvalidateRequerySuggested();
        }
    }

    public bool HasSelectedEmployee =>
        SelectedEmployee != null;

    public bool IsEditMode =>
        SelectedEvaluation != null;

    public string SelectedEmployeeName =>
        SelectedEmployee?.FullName
        ?? "No employee selected";

    public string SelectedEmployeeDepartment =>
        SelectedEmployee?.DepartmentName
        ?? string.Empty;

    public string SelectedEmployeeRole =>
        SelectedEmployee?.RoleName
        ?? string.Empty;

    #endregion

    #region Evaluation form properties

    public string SelectedBonusType
    {
        get => _selectedBonusType;

        set
        {
            if (!SetProperty(
                    ref _selectedBonusType,
                    value ?? string.Empty))
            {
                return;
            }

            OnPropertyChanged(
                nameof(IsRewardSelected));

            OnPropertyChanged(
                nameof(IsPenaltySelected));

            CommandManager
                .InvalidateRequerySuggested();
        }
    }

    public string EvaluationType
    {
        get => _evaluationType;

        set
        {
            if (!SetProperty(
                    ref _evaluationType,
                    value ?? string.Empty))
            {
                return;
            }

            CommandManager
                .InvalidateRequerySuggested();
        }
    }

    public decimal Amount
    {
        get => _amount;

        set
        {
            if (!SetProperty(
                    ref _amount,
                    value))
            {
                return;
            }

            CommandManager
                .InvalidateRequerySuggested();
        }
    }

    public DateTime BonusDate
    {
        get => _bonusDate;

        set
        {
            if (!SetProperty(
                    ref _bonusDate,
                    value))
            {
                return;
            }

            CommandManager
                .InvalidateRequerySuggested();
        }
    }

    public string Comment
    {
        get => _comment;

        set
        {
            if (!SetProperty(
                    ref _comment,
                    value ?? string.Empty))
            {
                return;
            }

            OnPropertyChanged(
                nameof(CommentLength));

            CommandManager
                .InvalidateRequerySuggested();
        }
    }

    public int CommentLength =>
        Comment?.Length ?? 0;

    public bool IsRewardSelected =>
        string.Equals(
            SelectedBonusType,
            "Reward",
            StringComparison.OrdinalIgnoreCase);

    public bool IsPenaltySelected =>
        string.Equals(
            SelectedBonusType,
            "Penalty",
            StringComparison.OrdinalIgnoreCase);

    public string FormTitle =>
        IsEditMode
            ? "Edit Evaluation"
            : "New Evaluation";

    public string SaveButtonText =>
        IsEditMode
            ? "Update Evaluation"
            : "Add Evaluation";

    #endregion

    #region Summary properties

    public decimal TotalReward =>
        SelectedEmployee?.TotalReward ?? 0;

    public decimal TotalPenalty =>
        SelectedEmployee?.TotalPenalty ?? 0;

    public decimal NetAdjustment =>
        SelectedEmployee?.NetAdjustment ?? 0;

    public int EvaluationCount =>
        SelectedEmployee?.EvaluationCount ?? 0;

    public int TotalEmployees =>
        Employees.Count;

    public bool HasEmployees =>
        Employees.Count > 0;

    public bool HasEvaluations =>
        Evaluations.Count > 0;

    #endregion

    #region State properties

    public string StatusMessage
    {
        get => _statusMessage;

        private set => SetProperty(
            ref _statusMessage,
            value ?? string.Empty);
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

            CommandManager
                .InvalidateRequerySuggested();
        }
    }

    #endregion

    #region Commands

    public ICommand LoadEmployeesCommand
    {
        get;
    }

    public ICommand RefreshCommand
    {
        get;
    }

    public ICommand SearchCommand
    {
        get;
    }

    public ICommand ClearSearchCommand
    {
        get;
    }

    public ICommand SaveEvaluationCommand
    {
        get;
    }

    public ICommand DeleteEvaluationCommand
    {
        get;
    }

    public ICommand ResetFormCommand
    {
        get;
    }

    public ICommand SelectEvaluationCommand
    {
        get;
    }

    

    #endregion

    #region Employee loading

    private void LoadEmployees()
    {
        if (IsLoading)
            return;

        var selectedEmployeeId =
            SelectedEmployee?.EmployeeId;

        try
        {
            IsLoading = true;
            StatusMessage = string.Empty;

            var employees =
                _evaluationService.GetEmployees(
                    SelectedMonth,
                    SelectedYear,
                    SearchText,
                    SelectedDepartment?.DepartmentId);

            ReplaceCollection(
                Employees,
                employees);

            OnPropertyChanged(
                nameof(TotalEmployees));

            OnPropertyChanged(
                nameof(HasEmployees));

            if (selectedEmployeeId.HasValue)
            {
                SelectedEmployee =
                    Employees.FirstOrDefault(
                        employee =>
                            employee.EmployeeId
                            == selectedEmployeeId.Value);
            }

            if (SelectedEmployee == null)
            {
                Evaluations.Clear();

                OnPropertyChanged(
                    nameof(HasEvaluations));
            }

            StatusMessage =
                Employees.Count > 0
                    ? $"Loaded {Employees.Count} employees."
                    : "No employees were found.";
        }
        catch (UnauthorizedAccessException ex)
        {
            ClearAllData();
            StatusMessage = ex.Message;
        }
        catch (Exception ex)
        {
            ClearAllData();

            StatusMessage =
                $"Unable to load employees. {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void LoadDepartmentOptions()
    {
        try
        {
            var employees =
                _evaluationService.GetEmployees(
                    SelectedMonth,
                    SelectedYear);

            _departmentSource.Clear();
            _departmentSource.AddRange(employees);

            var currentDepartmentId =
                SelectedDepartment?.DepartmentId;

            var departments =
                employees
                    .Where(
                        employee =>
                            employee.DepartmentId > 0)
                    .GroupBy(
                        employee =>
                            employee.DepartmentId)
                    .Select(
                        group =>
                            new Department
                            {
                                DepartmentId =
                                    group.Key,

                                DepartmentName =
                                    group
                                        .First()
                                        .DepartmentName
                            })
                    .OrderBy(
                        department =>
                            department.DepartmentName)
                    .ToList();

            ReplaceCollection(
                Departments,
                departments);

            if (currentDepartmentId.HasValue)
            {
                _selectedDepartment =
                    Departments.FirstOrDefault(
                        department =>
                            department.DepartmentId
                            == currentDepartmentId.Value);

                OnPropertyChanged(
                    nameof(SelectedDepartment));
            }
        }
        catch
        {
            Departments.Clear();
            _departmentSource.Clear();
        }
    }

    private void LoadSelectedEmployeeEvaluations()
    {
        Evaluations.Clear();

        OnPropertyChanged(
            nameof(HasEvaluations));

        if (SelectedEmployee == null)
            return;

        try
        {
            IsLoading = true;
            StatusMessage = string.Empty;

            var evaluations =
                _evaluationService
                    .GetEmployeeEvaluations(
                        SelectedEmployee.EmployeeId,
                        SelectedMonth,
                        SelectedYear);

            ReplaceCollection(
                Evaluations,
                evaluations);

            OnPropertyChanged(
                nameof(HasEvaluations));

            StatusMessage =
                Evaluations.Count > 0
                    ? $"Loaded {Evaluations.Count} evaluations for " +
                      $"{SelectedEmployee.FullName}."
                    : $"{SelectedEmployee.FullName} has no evaluations " +
                      $"for {SelectedPeriod}.";
        }
        catch (Exception ex)
        {
            Evaluations.Clear();

            OnPropertyChanged(
                nameof(HasEvaluations));

            StatusMessage =
                $"Unable to load evaluations. {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Evaluation operations

    private bool CanSaveEvaluation()
    {
        return !IsLoading
               && SelectedEmployee != null
               && !string.IsNullOrWhiteSpace(
                   SelectedBonusType)
               && !string.IsNullOrWhiteSpace(
                   EvaluationType)
               && Amount > 0
               && BonusDate != default
               && CommentLength <= 500;
    }

    private void SaveEvaluation()
    {
        if (SelectedEmployee == null)
        {
            StatusMessage =
                "Please select an employee.";
            return;
        }

        if (!IsDateInSelectedPeriod(
                BonusDate))
        {
            StatusMessage =
                $"Evaluation date must belong to " +
                $"{SelectedPeriod}.";
            return;
        }

        var employeeId =
            SelectedEmployee.EmployeeId;

        try
        {
            IsLoading = true;
            StatusMessage = string.Empty;

            if (SelectedEvaluation == null)
            {
                _evaluationService
                    .CreateEvaluation(
                        employeeId,
                        SelectedBonusType,
                        EvaluationType,
                        Amount,
                        BonusDate,
                        Comment);

                StatusMessage =
                    "Employee evaluation was added successfully.";
            }
            else
            {
                _evaluationService
                    .UpdateEvaluation(
                        SelectedEvaluation.EvaluationId,
                        SelectedBonusType,
                        EvaluationType,
                        Amount,
                        BonusDate,
                        Comment);

                StatusMessage =
                    "Employee evaluation was updated successfully.";
            }

            ResetEvaluationForm();

            ReloadEmployeeData(
                employeeId);
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanDeleteEvaluation()
    {
        return !IsLoading
               && SelectedEvaluation != null;
    }

    private void DeleteSelectedEvaluation()
    {
        if (SelectedEvaluation == null)
        {
            StatusMessage =
                "Please select an evaluation to delete.";
            return;
        }

        var evaluationId =
            SelectedEvaluation.EvaluationId;

        var employeeId =
            SelectedEvaluation.EmployeeId;

        try
        {
            IsLoading = true;
            StatusMessage = string.Empty;

            _evaluationService
                .DeleteEvaluation(
                    evaluationId);

            ResetEvaluationForm();

            ReloadEmployeeData(
                employeeId);

            StatusMessage =
                "Employee evaluation was deleted successfully.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SelectEvaluation(
        EmployeeEvaluationItemModel?
            evaluation)
    {
        if (evaluation == null)
            return;

        SelectedEvaluation = evaluation;
    }

    private void LoadSelectedEvaluationIntoForm()
    {
        if (SelectedEvaluation == null)
            return;

        SelectedBonusType =
            SelectedEvaluation.BonusType;

        EvaluationType =
            SelectedEvaluation.EvaluationType;

        Amount =
            SelectedEvaluation.Amount;

        BonusDate =
            SelectedEvaluation.BonusDate;

        Comment =
            SelectedEvaluation.Comment;
    }

    private void ResetEvaluationForm()
    {
        _selectedEvaluation = null;

        OnPropertyChanged(
            nameof(SelectedEvaluation));

        SelectedBonusType = "Reward";
        EvaluationType = string.Empty;
        Amount = 0;
        Comment = string.Empty;

        SetDefaultEvaluationDate();

        OnPropertyChanged(
            nameof(IsEditMode));

        OnPropertyChanged(
            nameof(FormTitle));

        OnPropertyChanged(
            nameof(SaveButtonText));

        CommandManager
            .InvalidateRequerySuggested();
    }

    #endregion

    #region Refresh and search

    private void RefreshData()
    {
        var selectedEmployeeId =
            SelectedEmployee?.EmployeeId;

        LoadDepartmentOptions();
        LoadEmployees();

        if (!selectedEmployeeId.HasValue)
            return;

        SelectedEmployee =
            Employees.FirstOrDefault(
                employee =>
                    employee.EmployeeId
                    == selectedEmployeeId.Value);
    }

    private void ClearSearch()
    {
        SearchText = string.Empty;
        SelectedDepartment = null;

        LoadEmployees();
    }

    private void ReloadEmployeeData(
        int employeeId)
    {
        /*
         * Reload danh sách để cập nhật:
         * - EvaluationCount
         * - TotalReward
         * - TotalPenalty
         * - NetAdjustment
         */
        var employees =
            _evaluationService.GetEmployees(
                SelectedMonth,
                SelectedYear,
                SearchText,
                SelectedDepartment?.DepartmentId);

        ReplaceCollection(
            Employees,
            employees);

        SelectedEmployee =
            Employees.FirstOrDefault(
                employee =>
                    employee.EmployeeId
                    == employeeId);

        OnPropertyChanged(
            nameof(TotalEmployees));

        OnPropertyChanged(
            nameof(HasEmployees));
    }

    #endregion

    #region Helpers

    private void SetDefaultEvaluationDate()
    {
        var currentDate =
            DateTime.Today;

        if (SelectedMonth == currentDate.Month
            && SelectedYear == currentDate.Year)
        {
            BonusDate = currentDate;
            return;
        }

        BonusDate =
            new DateTime(
                SelectedYear,
                SelectedMonth,
                1);
    }

    private bool IsDateInSelectedPeriod(
        DateTime date)
    {
        return date.Month == SelectedMonth
               && date.Year == SelectedYear;
    }

    private void ClearAllData()
    {
        Employees.Clear();
        Evaluations.Clear();

        _selectedEmployee = null;
        _selectedEvaluation = null;

        OnPropertyChanged(
            nameof(SelectedEmployee));

        OnPropertyChanged(
            nameof(SelectedEvaluation));

        OnPropertyChanged(
            nameof(HasSelectedEmployee));

        OnPropertyChanged(
            nameof(HasEmployees));

        OnPropertyChanged(
            nameof(HasEvaluations));

        OnPropertyChanged(
            nameof(TotalEmployees));

        OnPropertyChanged(
            nameof(TotalReward));

        OnPropertyChanged(
            nameof(TotalPenalty));

        OnPropertyChanged(
            nameof(NetAdjustment));

        OnPropertyChanged(
            nameof(EvaluationCount));
    }

    private static void ReplaceCollection<T>(
        ObservableCollection<T> target,
        IEnumerable<T> source)
    {
        target.Clear();

        foreach (var item in source)
        {
            target.Add(item);
        }
    }

    private static IEnumerable<int>
        CreateYearRange()
    {
        var currentYear =
            DateTime.Today.Year;

        return Enumerable.Range(
            currentYear - 5,
            11);
    }

    #endregion
}