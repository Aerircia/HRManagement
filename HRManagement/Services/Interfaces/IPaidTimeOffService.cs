using HRManagement.Models;

namespace HRManagement.Services.Interfaces;

public interface IPaidTimeOffService
{
    PaidTimeOffSummary GetYearSummary(
        int employeeId,
        int year);

    PaidTimeOffSummary GetMonthSummary(
        int employeeId,
        int month,
        int year);

    int GetRemainingPaidDays(
        int employeeId,
        int year);
}
