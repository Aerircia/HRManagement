using System;
using System.Collections.Generic;
using System.Text;
using HRManagement.Repositories;
using HRManagement.Repositories.Interfaces;
using HRManagement.Services.Interfaces;

// Model aliases
using AttendanceModel = HRManagement.Models.Attendance;
using ContractModel = HRManagement.Models.Contract;
using EmployeeModel = HRManagement.Models.Employee;
using EvaluationModel = HRManagement.Models.EmployeeEvaluation;
using ManageSalaryModel = HRManagement.Models.ManageSalariesItemModel;
using PayrollModel = HRManagement.Models.Payroll;
using RoleModel = HRManagement.Models.Role;
using SalaryDetailModel = HRManagement.Models.SalaryDetailModel;
using SalaryAttendanceSummaryModel = HRManagement.Models.SalaryAttendanceSummary;

namespace HRManagement.Services
{
    public class ManageSalariesService
    : IManageSalariesService
    {
        private readonly ISalaryRepository _salaryRepository;

        private readonly IManageSalariesRepository
            _manageSalariesRepository;

        private readonly ISalaryCalculator
            _salaryCalculator;

        private readonly IAttendanceService
            _attendanceService;

        private readonly IPaidTimeOffService
            _paidTimeOffService;

        public ManageSalariesService(
            ISalaryRepository salaryRepository,
            IManageSalariesRepository manageSalariesRepository,
            ISalaryCalculator salaryCalculator,
            IAttendanceService attendanceService,
            IPaidTimeOffService paidTimeOffService)
        {
            _salaryRepository = salaryRepository
                ?? throw new ArgumentNullException(
                    nameof(salaryRepository));

            _manageSalariesRepository =
                manageSalariesRepository
                ?? throw new ArgumentNullException(
                    nameof(manageSalariesRepository));

            _salaryCalculator = salaryCalculator
                ?? throw new ArgumentNullException(
                    nameof(salaryCalculator));

            _attendanceService = attendanceService
                ?? throw new ArgumentNullException(
                    nameof(attendanceService));

            _paidTimeOffService = paidTimeOffService
                ?? throw new ArgumentNullException(
                    nameof(paidTimeOffService));
        }

        // =========================================================
        // Salary information
        // =========================================================

        public IReadOnlyList<ManageSalaryModel>
            GetMonthlySalaries(
                int month,
                int year)
        {
            ValidateSalaryPeriod(month, year);

            IReadOnlyList<EmployeeModel> employees =
                _manageSalariesRepository
                    .GetEmployeesForPeriod(
                        month,
                        year);

            var salaryItems =
                new List<ManageSalaryModel>();

            foreach (EmployeeModel employee in employees)
            {
                ManageSalaryModel item =
                    BuildSalaryItem(
                        employee,
                        month,
                        year);

                salaryItems.Add(item);
            }

            return salaryItems
                .OrderBy(item => item.FullName)
                .ThenBy(item => item.EmployeeId)
                .ToList();
        }

        public ManageSalaryModel?
            GetEmployeeSalary(
                int employeeId,
                int month,
                int year)
        {
            ValidateEmployeeId(employeeId);
            ValidateSalaryPeriod(month, year);

            EmployeeModel? employee =
                _salaryRepository.GetEmployee(
                    employeeId);

            if (employee == null)
                return null;

            return BuildSalaryItem(
                employee,
                month,
                year);
        }

        // =========================================================
        // Salary components
        // =========================================================

        public void UpdateBaseSalary(
            int contractId,
            decimal baseSalary)
        {
            ValidateContractId(contractId);

            ValidateNonNegativeAmount(
                baseSalary,
                nameof(baseSalary),
                "Base salary");

            _manageSalariesRepository
                .UpdateContractBaseSalary(
                    contractId,
                    baseSalary);
        }

        public void UpdatePayRate(
            int roleId,
            decimal payRate)
        {
            ValidateRoleId(roleId);

            ValidatePositiveAmount(
                payRate,
                nameof(payRate),
                "Pay rate");

            _manageSalariesRepository
                .UpdateRolePayRate(
                    roleId,
                    payRate);
        }

        public void UpdateSalaryComponents(
            int contractId,
            decimal baseSalary,
            int roleId,
            decimal payRate)
        {
            ValidateContractId(contractId);
            ValidateRoleId(roleId);

            ValidateNonNegativeAmount(
                baseSalary,
                nameof(baseSalary),
                "Base salary");

            ValidatePositiveAmount(
                payRate,
                nameof(payRate),
                "Pay rate");

            _manageSalariesRepository
                .UpdateContractBaseSalary(
                    contractId,
                    baseSalary);

            _manageSalariesRepository
                .UpdateRolePayRate(
                    roleId,
                    payRate);
        }

        // =========================================================
        // Attendance
        // =========================================================

        public int AddAttendance(
            AttendanceModel attendance)
        {
            if (attendance == null)
            {
                throw new ArgumentNullException(
                    nameof(attendance));
            }

            ValidateEmployeeId(
                attendance.EmployeeId);

            ValidateAttendance(attendance);

            return _manageSalariesRepository
                .AddAttendance(attendance);
        }

        public void UpdateAttendance(
            AttendanceModel attendance)
        {
            if (attendance == null)
            {
                throw new ArgumentNullException(
                    nameof(attendance));
            }

            ValidateAttendanceId(
                attendance.AttendanceId);

            ValidateEmployeeId(
                attendance.EmployeeId);

            ValidateAttendance(attendance);

            _manageSalariesRepository
                .UpdateAttendance(attendance);
        }

        public void DeleteAttendance(
            int attendanceId)
        {
            ValidateAttendanceId(attendanceId);

            _manageSalariesRepository
                .DeleteAttendance(attendanceId);
        }

        // =========================================================
        // Evaluation
        // =========================================================

        public int AddEvaluation(
            EvaluationModel evaluation)
        {
            if (evaluation == null)
            {
                throw new ArgumentNullException(
                    nameof(evaluation));
            }

            ValidateEmployeeId(
                evaluation.EmployeeId);

            ValidateEvaluation(evaluation);

            return _manageSalariesRepository
                .AddEvaluation(evaluation);
        }

        public void UpdateEvaluation(
            EvaluationModel evaluation)
        {
            if (evaluation == null)
            {
                throw new ArgumentNullException(
                    nameof(evaluation));
            }

            ValidateEvaluationId(
                evaluation.EvaluationId);

            ValidateEmployeeId(
                evaluation.EmployeeId);

            ValidateEvaluation(evaluation);

            _manageSalariesRepository
                .UpdateEvaluation(evaluation);
        }

        public void DeleteEvaluation(
            int evaluationId)
        {
            ValidateEvaluationId(evaluationId);

            _manageSalariesRepository
                .DeleteEvaluation(evaluationId);
        }

        // =========================================================
        // Payroll
        // =========================================================

        public int CreateEmployeePayroll(
            int employeeId,
            int month,
            int year)
        {
            ValidateEmployeeId(employeeId);
            ValidateSalaryPeriod(month, year);

            if (_salaryRepository.PayrollExists(
                    employeeId,
                    month,
                    year))
            {
                throw new InvalidOperationException(
                    $"Payroll for employee {employeeId} " +
                    $"in {month:00}/{year} already exists.");
            }

            EmployeeModel? employee =
                _salaryRepository.GetEmployee(
                    employeeId);

            if (employee == null)
            {
                throw new InvalidOperationException(
                    $"Employee with ID {employeeId} " +
                    "could not be found.");
            }

            ContractModel? contract =
                _salaryRepository
                    .GetContractForPeriod(
                        employeeId,
                        month,
                        year);

            if (contract == null)
            {
                throw new InvalidOperationException(
                    $"Employee {employee.FullName} does not " +
                    $"have a valid contract for " +
                    $"{month:00}/{year}.");
            }

            RoleModel? role =
                _salaryRepository.GetRole(
                    contract.RoleId);

            if (role == null)
            {
                throw new InvalidOperationException(
                    $"Role with ID {contract.RoleId} " +
                    "could not be found.");
            }

            // Kiểm tra dữ liệu lương có thể tính được.
            CalculateSalary(
                employee,
                contract,
                role,
                month,
                year);

            var payroll = new PayrollModel
            {
                EmployeeId = employeeId,
                EvaluationId = null,
                Month = month,
                Year = year
            };

            return _manageSalariesRepository
                .CreatePayroll(payroll);
        }

        public int CreateMonthlyPayroll(
            int month,
            int year)
        {
            ValidateSalaryPeriod(month, year);

            IReadOnlyList<EmployeeModel> employees =
                _manageSalariesRepository
                    .GetEmployeesForPeriod(
                        month,
                        year);

            var payrolls =
                new List<PayrollModel>();

            foreach (EmployeeModel employee in employees)
            {
                bool payrollExists =
                    _salaryRepository.PayrollExists(
                        employee.EmployeeId,
                        month,
                        year);

                if (payrollExists)
                    continue;

                ContractModel? contract =
                    _salaryRepository
                        .GetContractForPeriod(
                            employee.EmployeeId,
                            month,
                            year);

                if (contract == null)
                    continue;

                RoleModel? role =
                    _salaryRepository.GetRole(
                        contract.RoleId);

                if (role == null)
                    continue;

                CalculateSalary(
                    employee,
                    contract,
                    role,
                    month,
                    year);

                payrolls.Add(
                    new PayrollModel
                    {
                        EmployeeId =
                            employee.EmployeeId,

                        EvaluationId = null,

                        Month = month,

                        Year = year
                    });
            }

            return _manageSalariesRepository
                .CreateMonthlyPayroll(payrolls);
        }

        public void DeleteEmployeePayroll(
            int employeeId,
            int month,
            int year)
        {
            ValidateEmployeeId(employeeId);
            ValidateSalaryPeriod(month, year);

            bool payrollExists =
                _salaryRepository.PayrollExists(
                    employeeId,
                    month,
                    year);

            if (!payrollExists)
            {
                throw new InvalidOperationException(
                    $"Payroll for employee {employeeId} " +
                    $"in {month:00}/{year} does not exist.");
            }

            _manageSalariesRepository
                .DeletePayroll(
                    employeeId,
                    month,
                    year);
        }

        // =========================================================
        // Build salary item
        // =========================================================

        private ManageSalaryModel BuildSalaryItem(
            EmployeeModel employee,
            int month,
            int year)
        {
            int? payrollId =
                _salaryRepository.GetPayrollId(
                    employee.EmployeeId,
                    month,
                    year);

            string departmentName =
                GetDepartmentNameSafely(
                    employee.DepartmentId);

            ContractModel? contract =
                _salaryRepository
                    .GetContractForPeriod(
                        employee.EmployeeId,
                        month,
                        year);

            if (contract == null)
            {
                return CreateInvalidSalaryItem(
                    employee,
                    departmentName,
                    payrollId,
                    month,
                    year,
                    hasValidContract: false,
                    hasValidRole: false,
                    validationMessage:
                        $"No valid contract was found for " +
                        $"{month:00}/{year}.");
            }

            RoleModel? role =
                _salaryRepository.GetRole(
                    contract.RoleId);

            if (role == null)
            {
                return CreateInvalidSalaryItem(
                    employee,
                    departmentName,
                    payrollId,
                    month,
                    year,
                    hasValidContract: true,
                    hasValidRole: false,
                    validationMessage:
                        $"Role with ID {contract.RoleId} " +
                        "could not be found.",
                    contract: contract);
            }

            try
            {
                SalaryDetailModel salaryDetail =
                    CalculateSalary(
                        employee,
                        contract,
                        role,
                        month,
                        year);

                return new ManageSalaryModel
                {
                    EmployeeId =
                        salaryDetail.EmployeeId,

                    FullName =
                        salaryDetail.FullName,

                    DepartmentId =
                        employee.DepartmentId,

                    DepartmentName =
                        salaryDetail.DepartmentName,

                    RoleId =
                        role.RoleId,

                    RoleName =
                        salaryDetail.RoleName,

                    PayRate =
                        salaryDetail.PayRate,

                    ContractId =
                        contract.ContractId,

                    BaseSalary =
                        salaryDetail.BaseSalary,

                    CalendarWorkingDays =
                        salaryDetail.CalendarWorkingDays,

                    EffectiveWorkingDays =
                        salaryDetail.EffectiveWorkingDays,

                    PresentDays =
                        salaryDetail.PresentDays,

                    LateDays =
                        salaryDetail.LateDays,

                    LateMinutes =
                        salaryDetail.LateMinutes,

                    WeekdayOtDays =
                        salaryDetail.WeekdayOtDays,

                    WeekendOtDays =
                        salaryDetail.WeekendOtDays,

                    WeekdayOtHours =
                        salaryDetail.WeekdayOtHours,

                    WeekendOtHours =
                        salaryDetail.WeekendOtHours,

                    PaidDayOffDays =
                        salaryDetail.PaidDayOffDays,

                    UnpaidDayOffDays =
                        salaryDetail.UnpaidDayOffDays,

                    WorkingDays =
                        salaryDetail.WorkingDays,

                    AbsentDays =
                        salaryDetail.AbsentDays,

                    RoleSalary =
                        salaryDetail.RoleSalary,

                    DailySalary =
                        salaryDetail.DailySalary,

                    HourlySalary =
                        salaryDetail.HourlySalary,

                    WeekdayOtSalary =
                        salaryDetail.WeekdayOtSalary,

                    WeekendOtSalary =
                        salaryDetail.WeekendOtSalary,

                    Reward =
                        salaryDetail.Reward,

                    Penalty =
                        salaryDetail.Penalty,

                    LatePenalty =
                        salaryDetail.LatePenalty,

                    AbsentDeduction =
                        salaryDetail.AbsentDeduction,

                    UnpaidDayOffDeduction =
                        salaryDetail.UnpaidDayOffDeduction,

                    PayrollId =
                        payrollId,

                    Month =
                        salaryDetail.Month,

                    Year =
                        salaryDetail.Year,

                    TotalSalary =
                        salaryDetail.TotalSalary,

                    IsPayrollCreated =
                        payrollId.HasValue,

                    HasValidContract = true,

                    HasValidRole = true,

                    ValidationMessage =
                        string.Empty
                };
            }
            catch (ArgumentException exception)
            {
                return CreateInvalidSalaryItem(
                    employee,
                    departmentName,
                    payrollId,
                    month,
                    year,
                    hasValidContract: true,
                    hasValidRole: true,
                    validationMessage:
                        exception.Message,
                    contract: contract,
                    role: role);
            }
            catch (InvalidOperationException exception)
            {
                return CreateInvalidSalaryItem(
                    employee,
                    departmentName,
                    payrollId,
                    month,
                    year,
                    hasValidContract: true,
                    hasValidRole: true,
                    validationMessage:
                        exception.Message,
                    contract: contract,
                    role: role);
            }
        }

        private SalaryDetailModel CalculateSalary(
            EmployeeModel employee,
            ContractModel contract,
            RoleModel role,
            int month,
            int year)
        {
            var attendanceSummary =
                _attendanceService
                    .GetMonthSummary(
                        employee.EmployeeId,
                        employee.HireDate,
                        new DateTime(
                            year,
                            month,
                            1));

            var paidTimeOffSummary =
                _paidTimeOffService
                    .GetMonthSummary(
                        employee.EmployeeId,
                        month,
                        year);

            SalaryAttendanceSummaryModel
                salaryAttendanceSummary =
                    SalaryAttendanceSummaryModel
                        .Create(
                            attendanceSummary,
                            paidTimeOffSummary);

            IReadOnlyList<EvaluationModel> evaluations =
                _salaryRepository.GetEvaluations(
                    employee.EmployeeId,
                    month,
                    year);

            string departmentName =
                GetDepartmentNameSafely(
                    employee.DepartmentId);

            return _salaryCalculator
                .CalculateSalary(
                    employee,
                    contract,
                    role,
                    salaryAttendanceSummary,
                    evaluations,
                    departmentName,
                    month,
                    year);
        }

        private static ManageSalaryModel
            CreateInvalidSalaryItem(
                EmployeeModel employee,
                string departmentName,
                int? payrollId,
                int month,
                int year,
                bool hasValidContract,
                bool hasValidRole,
                string validationMessage,
                ContractModel? contract = null,
                RoleModel? role = null)
        {
            return new ManageSalaryModel
            {
                EmployeeId =
                    employee.EmployeeId,

                FullName =
                    employee.FullName,

                DepartmentId =
                    employee.DepartmentId,

                DepartmentName =
                    departmentName,

                RoleId =
                    role?.RoleId
                    ?? contract?.RoleId
                    ?? employee.RoleId,

                RoleName =
                    role?.RoleName
                    ?? string.Empty,

                PayRate =
                    role?.PayRate
                    ?? 0,

                ContractId =
                    contract?.ContractId
                    ?? 0,

                BaseSalary =
                    contract?.BaseSalary
                    ?? 0,

                CalendarWorkingDays = 0,

                EffectiveWorkingDays = 0,

                PresentDays = 0,

                LateDays = 0,

                LateMinutes = 0,

                WeekdayOtDays = 0,

                WeekendOtDays = 0,

                WeekdayOtHours = 0,

                WeekendOtHours = 0,

                PaidDayOffDays = 0,

                UnpaidDayOffDays = 0,

                WorkingDays = 0,

                AbsentDays = 0,

                RoleSalary = 0,

                DailySalary = 0,

                HourlySalary = 0,

                WeekdayOtSalary = 0,

                WeekendOtSalary = 0,

                Reward = 0,

                Penalty = 0,

                LatePenalty = 0,

                AbsentDeduction = 0,

                UnpaidDayOffDeduction = 0,

                PayrollId =
                    payrollId,

                Month = month,

                Year = year,

                TotalSalary = 0,

                IsPayrollCreated =
                    payrollId.HasValue,

                HasValidContract =
                    hasValidContract,

                HasValidRole =
                    hasValidRole,

                ValidationMessage =
                    validationMessage
            };
        }

        private string GetDepartmentNameSafely(
            int departmentId)
        {
            if (departmentId <= 0)
                return string.Empty;

            return _salaryRepository
                .GetDepartmentName(
                    departmentId);
        }

        // =========================================================
        // Validation
        // =========================================================

        private static void ValidateAttendance(
            AttendanceModel attendance)
        {
            if (string.IsNullOrWhiteSpace(
                    attendance.Status))
            {
                throw new ArgumentException(
                    "Attendance status is required.",
                    nameof(attendance));
            }

            if (attendance.CheckIn.HasValue
                && attendance.CheckOut.HasValue
                && attendance.CheckOut.Value
                    < attendance.CheckIn.Value)
            {
                throw new ArgumentException(
                    "Check-out time cannot be earlier " +
                    "than check-in time.",
                    nameof(attendance));
            }
        }

        private static void ValidateEvaluation(
            EvaluationModel evaluation)
        {
            if (evaluation.Amount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(evaluation.Amount),
                    "Evaluation amount cannot be negative.");
            }

            if (evaluation.BonusDate == default)
            {
                throw new ArgumentException(
                    "Bonus date is required.",
                    nameof(evaluation));
            }

            if (string.IsNullOrWhiteSpace(
                    evaluation.BonusType))
            {
                throw new ArgumentException(
                    "Bonus type is required.",
                    nameof(evaluation));
            }

            bool isReward =
                evaluation.BonusType.Equals(
                    "Reward",
                    StringComparison.OrdinalIgnoreCase);

            bool isPenalty =
                evaluation.BonusType.Equals(
                    "Penalty",
                    StringComparison.OrdinalIgnoreCase);

            if (!isReward && !isPenalty)
            {
                throw new ArgumentException(
                    "Bonus type must be Reward or Penalty.",
                    nameof(evaluation));
            }
        }

        private static void ValidateEmployeeId(
            int employeeId)
        {
            if (employeeId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(employeeId),
                    "Employee ID must be greater than 0.");
            }
        }

        private static void ValidateContractId(
            int contractId)
        {
            if (contractId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(contractId),
                    "Contract ID must be greater than 0.");
            }
        }

        private static void ValidateRoleId(
            int roleId)
        {
            if (roleId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(roleId),
                    "Role ID must be greater than 0.");
            }
        }

        private static void ValidateAttendanceId(
            int attendanceId)
        {
            if (attendanceId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(attendanceId),
                    "Attendance ID must be greater than 0.");
            }
        }

        private static void ValidateEvaluationId(
            int evaluationId)
        {
            if (evaluationId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(evaluationId),
                    "Evaluation ID must be greater than 0.");
            }
        }

        private static void ValidateSalaryPeriod(
            int month,
            int year)
        {
            if (month is < 1 or > 12)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(month),
                    "Month must be between 1 and 12.");
            }

            if (year < 2000)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(year),
                    "Year must be greater than or equal to 2000.");
            }
        }

        private static void ValidateNonNegativeAmount(
            decimal value,
            string parameterName,
            string displayName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    $"{displayName} cannot be negative.");
            }
        }

        private static void ValidatePositiveAmount(
            decimal value,
            string parameterName,
            string displayName)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    $"{displayName} must be greater than 0.");
            }
        }
    }
}
