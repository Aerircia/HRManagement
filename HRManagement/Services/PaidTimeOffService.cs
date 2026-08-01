using HRManagement.Models;
using HRManagement.Repositories.Interfaces;
using HRManagement.Services.Interfaces;

namespace HRManagement.Services;

public class PaidTimeOffService
    : IPaidTimeOffService
{
    private const string DayOffRequestType =
        "Day Off";

    private const string ApprovedStatus =
        "Approved";

    private readonly IRequestFormRepository
        _requestFormRepository;

    public PaidTimeOffService(
        IRequestFormRepository requestFormRepository)
    {
        _requestFormRepository =
            requestFormRepository
            ?? throw new ArgumentNullException(
                nameof(requestFormRepository));
    }

    public PaidTimeOffSummary GetYearSummary(
        int employeeId,
        int year)
    {
        ValidateEmployeeId(employeeId);
        ValidateYear(year);

        var approvedDayOffDates =
            GetApprovedDayOffDates(
                employeeId,
                year);

        var usedPaidDays =
            Math.Min(
                PaidTimeOffSummary
                    .DefaultAnnualAllowance,
                approvedDayOffDates.Count);

        return new PaidTimeOffSummary
        {
            Year = year,

            AnnualAllowance =
                PaidTimeOffSummary
                    .DefaultAnnualAllowance,

            TotalApprovedDayOffDays =
                approvedDayOffDates.Count,

            UsedPaidDays =
                usedPaidDays,

            PaidDayOffDays =
                usedPaidDays,

            UnpaidDayOffDays =
                Math.Max(
                    0,
                    approvedDayOffDates.Count
                    - usedPaidDays)
        };
    }

    public PaidTimeOffSummary GetMonthSummary(
        int employeeId,
        int month,
        int year)
    {
        ValidateEmployeeId(employeeId);
        ValidateMonth(month);
        ValidateYear(year);

        var approvedDayOffDates =
            GetApprovedDayOffDates(
                employeeId,
                year);

        var paidDates =
            approvedDayOffDates
                .Take(
                    PaidTimeOffSummary
                        .DefaultAnnualAllowance)
                .ToHashSet();

        var monthDates =
            approvedDayOffDates
                .Where(
                    date =>
                        date.Month == month)
                .ToList();

        var paidDayOffDays =
            monthDates.Count(
                date =>
                    paidDates.Contains(date));

        var unpaidDayOffDays =
            monthDates.Count
            - paidDayOffDays;

        var usedPaidDays =
            Math.Min(
                PaidTimeOffSummary
                    .DefaultAnnualAllowance,
                approvedDayOffDates.Count(
                    date =>
                        date
                        <= new DateTime(
                            year,
                            month,
                            DateTime.DaysInMonth(
                                year,
                                month))));

        return new PaidTimeOffSummary
        {
            Year = year,

            AnnualAllowance =
                PaidTimeOffSummary
                    .DefaultAnnualAllowance,

            TotalApprovedDayOffDays =
                approvedDayOffDates.Count,

            UsedPaidDays =
                usedPaidDays,

            PaidDayOffDays =
                paidDayOffDays,

            UnpaidDayOffDays =
                unpaidDayOffDays
        };
    }

    public int GetRemainingPaidDays(
        int employeeId,
        int year)
    {
        return GetYearSummary(
            employeeId,
            year)
            .RemainingPaidDays;
    }

    private List<DateTime>
        GetApprovedDayOffDates(
            int employeeId,
            int year)
    {
        var yearStart =
            new DateTime(
                year,
                1,
                1);

        var yearEnd =
            yearStart
                .AddYears(1)
                .AddDays(-1);

        var requests =
            _requestFormRepository
                .GetByEmployee(employeeId)
                .Where(
                    request =>
                        string.Equals(
                            request.RequestType,
                            DayOffRequestType,
                            StringComparison.OrdinalIgnoreCase)
                        &&
                        string.Equals(
                            request.Status,
                            ApprovedStatus,
                            StringComparison.OrdinalIgnoreCase)
                        &&
                        request.StartDate.HasValue
                        &&
                        request.EndDate.HasValue)
                .ToList();

        var dates =
            new HashSet<DateTime>();

        foreach (var request in requests)
        {
            var startDate =
                request.StartDate!.Value.Date;

            var endDate =
                request.EndDate!.Value.Date;

            if (endDate < startDate)
            {
                continue;
            }

            var effectiveStart =
                startDate < yearStart
                    ? yearStart
                    : startDate;

            var effectiveEnd =
                endDate > yearEnd
                    ? yearEnd
                    : endDate;

            if (effectiveEnd < effectiveStart)
            {
                continue;
            }

            for (
                var date = effectiveStart;
                date <= effectiveEnd;
                date = date.AddDays(1))
            {
                if (date.DayOfWeek
                    is DayOfWeek.Saturday
                    or DayOfWeek.Sunday)
                {
                    continue;
                }

                dates.Add(date);
            }
        }

        return dates
            .OrderBy(date => date)
            .ToList();
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

    private static void ValidateMonth(
        int month)
    {
        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(
                nameof(month),
                "Month must be between 1 and 12.");
        }
    }

    private static void ValidateYear(
        int year)
    {
        if (year < 2000)
        {
            throw new ArgumentOutOfRangeException(
                nameof(year),
                "Year must be greater than or equal to 2000.");
        }
    }
}
