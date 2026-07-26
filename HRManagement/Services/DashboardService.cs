using HRManagement.Repositories.Interfaces;
using HRManagement.Services.Interfaces;

namespace HRManagement.Services;

// Aggregation layer for the dashboard. Deliberately does not re-derive the
// payout formula: it batches ISalaryCalculator (same formula SalaryView
// uses) across employees/months so the dashboard total can never drift from
// what an individual employee sees on their own Salary page.
public class DashboardService : IDashboardService
{
    private readonly ISalaryRepository _salaryRepository;
    private readonly ISalaryCalculator _salaryCalculator;
    private readonly IAttendanceRepository _attendanceRepository;

    public DashboardService(
        ISalaryRepository salaryRepository,
        ISalaryCalculator salaryCalculator,
        IAttendanceRepository attendanceRepository)
    {
        _salaryRepository = salaryRepository;
        _salaryCalculator = salaryCalculator;
        _attendanceRepository = attendanceRepository;
    }

    public List<MonthlyPayoutPoint> GetMonthlyPayoutTotals(IReadOnlyList<int> employeeIds, int year)
    {
        var points = new List<MonthlyPayoutPoint>(12);

        for (var month = 1; month <= 12; month++)
        {
            decimal monthTotal = 0m;

            foreach (var employeeId in employeeIds)
            {
                if (!_salaryRepository.PayrollExists(employeeId, month, year))
                    continue;

                var employee = _salaryRepository.GetEmployee(employeeId);
                if (employee == null)
                    continue;

                var contract = _salaryRepository.GetContractForPeriod(employeeId, month, year);
                if (contract == null)
                    continue;

                var role = _salaryRepository.GetRole(contract.RoleId);
                if (role == null)
                    continue;

                var attendances = _salaryRepository.GetAttendances(employeeId, month, year);
                var evaluations = _salaryRepository.GetEvaluations(employeeId, month, year);
                var departmentName = _salaryRepository.GetDepartmentName(employee.DepartmentId);

                var detail = _salaryCalculator.CalculateSalary(
                    employee, contract, role, attendances, evaluations, departmentName, month, year);

                monthTotal += detail.TotalSalary;
            }

            points.Add(new MonthlyPayoutPoint { Month = month, Total = monthTotal });
        }

        return points;
    }

    public TodayAttendanceStat GetTodayAttendanceStat(IReadOnlyList<int> employeeIds)
    {
        var today = DateTime.Now.Date;
        var checkedIn = 0;

        foreach (var employeeId in employeeIds)
        {
            var hasCheckedIn = _attendanceRepository
                .GetAttendancesForEmployeeMonth(employeeId, today.Year, today.Month)
                .Any(a => (a.CheckIn ?? a.CheckOut)?.Date == today && a.CheckIn.HasValue);

            if (hasCheckedIn)
                checkedIn++;
        }

        return new TodayAttendanceStat
        {
            CheckedIn = checkedIn,
            TotalEmployees = employeeIds.Count
        };
    }
}
