using HRManagement.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;


namespace HRManagement.Repositories;

public class EmployeeEvaluationRepository : RepositoryBase, IEmployeeEvaluationRepository
{
    public EmployeeEvaluation? GetLatestByEmployeeId(int employeeId)
    {
        using var connection = Db.CreateConnection();

        connection.Open();

        const string sql = """
            SELECT TOP 1 *
            FROM EmployeeEvaluation
            WHERE Employee_ID = @EmployeeId
            ORDER BY Bonus_Date DESC
            """;

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@EmployeeId", employeeId);

        using var reader = command.ExecuteReader();

        if (!reader.Read())
            return null;

        return Map(reader);
    }

    public EmployeeEvaluation? GetById(int evaluationId)
    {
        using var connection = Db.CreateConnection();

        connection.Open();

        const string sql = """
            SELECT *
            FROM EmployeeEvaluation
            WHERE Evaluation_ID = @Id
            """;

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", evaluationId);

        using var reader = command.ExecuteReader();

        if (!reader.Read())
            return null;

        return Map(reader);
    }

    public decimal GetTotalBonusForYear(int employeeId, int year)
    {
        using var connection = Db.CreateConnection();

        connection.Open();

        const string sql = """
            SELECT ISNULL(SUM(Amount), 0)
            FROM EmployeeEvaluation
            WHERE Employee_ID = @EmployeeId
              AND YEAR(Bonus_Date) = @Year
            """;

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@EmployeeId", employeeId);
        command.Parameters.AddWithValue("@Year", year);

        var result = command.ExecuteScalar();
        return result == null ? 0m : (decimal)result;
    }

    private static EmployeeEvaluation Map(SqlDataReader reader)
    {
        return new EmployeeEvaluation
        {
            EvaluationId = (int)reader["Evaluation_ID"],
            EmployeeId = (int)reader["Employee_ID"],
            EvaluationType = reader["EvaluationType"] as string,
            BonusType = reader["BonusType"] as string,
            Amount = (decimal)reader["Amount"],
            BonusDate = (System.DateTime)reader["Bonus_Date"]
        };
    }
    public IReadOnlyList<EvaluationEmployeeItemModel> GetEmployees(
            int month,
            int year,
            string? searchText = null,
            int? departmentId = null)
    {
        var (periodStart, periodEnd) =
            CreateEvaluationPeriod(month, year);

        if (departmentId.HasValue &&
            departmentId.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(departmentId),
                "Department ID must be greater than 0.");
        }

        var employees =
            new List<EvaluationEmployeeItemModel>();

        using var connection = Db.CreateConnection();
        connection.Open();

        const string sql = """
                SELECT
                    e.EmployeeID,
                    e.FullName,

                    e.Department_ID,
                    ISNULL(d.DepartmentName, '') AS DepartmentName,

                    e.Role_ID,
                    ISNULL(r.RoleName, '') AS RoleName,

                    COUNT(ev.Evaluation_ID) AS EvaluationCount,

                    ISNULL(
                        SUM(
                            CASE
                                WHEN ev.BonusType = 'Reward'
                                THEN ev.Amount
                                ELSE 0
                            END
                        ),
                        0
                    ) AS TotalReward,

                    ISNULL(
                        SUM(
                            CASE
                                WHEN ev.BonusType = 'Penalty'
                                THEN ev.Amount
                                ELSE 0
                            END
                        ),
                        0
                    ) AS TotalPenalty

                FROM Employee e

                LEFT JOIN Department d
                    ON e.Department_ID = d.Department_ID

                LEFT JOIN Role r
                    ON e.Role_ID = r.Role_ID

                LEFT JOIN EmployeeEvaluation ev
                    ON e.EmployeeID = ev.Employee_ID
                    AND ev.Bonus_Date >= @PeriodStart
                    AND ev.Bonus_Date < @PeriodEnd

                WHERE
                    (
                        @DepartmentId IS NULL
                        OR e.Department_ID = @DepartmentId
                    )
                    AND
                    (
                        @SearchText IS NULL
                        OR @SearchText = ''
                        OR e.FullName LIKE '%' + @SearchText + '%'
                    )

                GROUP BY
                    e.EmployeeID,
                    e.FullName,
                    e.Department_ID,
                    d.DepartmentName,
                    e.Role_ID,
                    r.RoleName

                ORDER BY
                    e.FullName ASC,
                    e.EmployeeID ASC;
                """;

        using var command =
            new SqlCommand(sql, connection);

        command.Parameters.Add(
            "@PeriodStart",
            SqlDbType.DateTime2).Value = periodStart;

        command.Parameters.Add(
            "@PeriodEnd",
            SqlDbType.DateTime2).Value = periodEnd;

        command.Parameters.Add(
            "@SearchText",
            SqlDbType.NVarChar,
            200).Value =
            string.IsNullOrWhiteSpace(searchText)
                ? DBNull.Value
                : searchText.Trim();

        command.Parameters.Add(
            "@DepartmentId",
            SqlDbType.Int).Value =
            departmentId.HasValue
                ? departmentId.Value
                : DBNull.Value;

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            employees.Add(
                MapEvaluationEmployeeItem(reader));
        }

        return employees;
    }

    public IReadOnlyList<EmployeeEvaluationItemModel>
        GetEvaluationsByEmployee(
            int employeeId,
            int month,
            int year)
    {
        ValidateEmployeeId(employeeId);

        var (periodStart, periodEnd) =
            CreateEvaluationPeriod(month, year);

        var evaluations =
            new List<EmployeeEvaluationItemModel>();

        using var connection = Db.CreateConnection();
        connection.Open();

        const string sql = """
                SELECT
                    Evaluation_ID,
                    Employee_ID,
                    EvaluationType,
                    BonusType,
                    Amount,
                    Bonus_Date,
                    Comment

                FROM EmployeeEvaluation

                WHERE Employee_ID = @EmployeeId
                  AND Bonus_Date >= @PeriodStart
                  AND Bonus_Date < @PeriodEnd

                ORDER BY
                    Bonus_Date DESC,
                    Evaluation_ID DESC;
                """;

        using var command =
            new SqlCommand(sql, connection);

        command.Parameters.Add(
            "@EmployeeId",
            SqlDbType.Int).Value = employeeId;

        command.Parameters.Add(
            "@PeriodStart",
            SqlDbType.DateTime2).Value = periodStart;

        command.Parameters.Add(
            "@PeriodEnd",
            SqlDbType.DateTime2).Value = periodEnd;

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            evaluations.Add(
                MapEmployeeEvaluationItem(reader));
        }

        return evaluations;
    }

    public EmployeeEvaluation? GetEvaluationById(
        int evaluationId)
    {
        ValidateEvaluationId(evaluationId);

        using var connection = Db.CreateConnection();
        connection.Open();

        const string sql = """
                SELECT
                    Evaluation_ID,
                    Employee_ID,
                    EvaluationType,
                    BonusType,
                    Amount,
                    Bonus_Date,
                    Comment

                FROM EmployeeEvaluation

                WHERE Evaluation_ID = @EvaluationId;
                """;

        using var command =
            new SqlCommand(sql, connection);

        command.Parameters.Add(
            "@EvaluationId",
            SqlDbType.Int).Value = evaluationId;

        using var reader = command.ExecuteReader();

        if (!reader.Read())
            return null;

        return MapEmployeeEvaluation(reader);
    }

    public bool EmployeeExists(
        int employeeId)
    {
        ValidateEmployeeId(employeeId);

        using var connection = Db.CreateConnection();
        connection.Open();

        const string sql = """
                SELECT CASE
                    WHEN EXISTS
                    (
                        SELECT 1
                        FROM Employee
                        WHERE EmployeeID = @EmployeeId
                    )
                    THEN CAST(1 AS BIT)
                    ELSE CAST(0 AS BIT)
                END;
                """;

        using var command =
            new SqlCommand(sql, connection);

        command.Parameters.Add(
            "@EmployeeId",
            SqlDbType.Int).Value = employeeId;

        var result = command.ExecuteScalar();

        return result != null
               && result != DBNull.Value
               && Convert.ToBoolean(result);
    }

    public bool EvaluationExists(
        int evaluationId)
    {
        ValidateEvaluationId(evaluationId);

        using var connection = Db.CreateConnection();
        connection.Open();

        const string sql = """
                SELECT CASE
                    WHEN EXISTS
                    (
                        SELECT 1
                        FROM EmployeeEvaluation
                        WHERE Evaluation_ID = @EvaluationId
                    )
                    THEN CAST(1 AS BIT)
                    ELSE CAST(0 AS BIT)
                END;
                """;

        using var command =
            new SqlCommand(sql, connection);

        command.Parameters.Add(
            "@EvaluationId",
            SqlDbType.Int).Value = evaluationId;

        var result = command.ExecuteScalar();

        return result != null
               && result != DBNull.Value
               && Convert.ToBoolean(result);
    }

    public int AddEvaluation(
        EmployeeEvaluation evaluation)
    {
        ValidateEvaluation(evaluation);

        using var connection = Db.CreateConnection();
        connection.Open();

        const string sql = """
                INSERT INTO EmployeeEvaluation
                (
                    Employee_ID,
                    EvaluationType,
                    BonusType,
                    Amount,
                    Bonus_Date,
                    Comment
                )
                OUTPUT INSERTED.Evaluation_ID
                VALUES
                (
                    @EmployeeId,
                    @EvaluationType,
                    @BonusType,
                    @Amount,
                    @BonusDate,
                    @Comment
                );
                """;

        using var command =
            new SqlCommand(sql, connection);

        AddEvaluationParameters(
            command,
            evaluation);

        var result = command.ExecuteScalar();

        if (result == null ||
            result == DBNull.Value)
        {
            throw new InvalidOperationException(
                "Unable to create the employee evaluation.");
        }

        return Convert.ToInt32(result);
    }

    public void UpdateEvaluation(
        EmployeeEvaluation evaluation)
    {
        ValidateEvaluation(evaluation);
        ValidateEvaluationId(evaluation.EvaluationId);

        using var connection = Db.CreateConnection();
        connection.Open();

        const string sql = """
                UPDATE EmployeeEvaluation
                SET
                    Employee_ID = @EmployeeId,
                    EvaluationType = @EvaluationType,
                    BonusType = @BonusType,
                    Amount = @Amount,
                    Bonus_Date = @BonusDate,
                    Comment = @Comment

                WHERE Evaluation_ID = @EvaluationId;
                """;

        using var command =
            new SqlCommand(sql, connection);

        command.Parameters.Add(
            "@EvaluationId",
            SqlDbType.Int).Value =
            evaluation.EvaluationId;

        AddEvaluationParameters(
            command,
            evaluation);

        var affectedRows = command.ExecuteNonQuery();

        EnsureRecordUpdated(
            affectedRows,
            evaluation.EvaluationId);
    }

    public void DeleteEvaluation(
        int evaluationId)
    {
        ValidateEvaluationId(evaluationId);

        using var connection = Db.CreateConnection();
        connection.Open();

        const string sql = """
                DELETE FROM EmployeeEvaluation
                WHERE Evaluation_ID = @EvaluationId;
                """;

        using var command =
            new SqlCommand(sql, connection);

        command.Parameters.Add(
            "@EvaluationId",
            SqlDbType.Int).Value = evaluationId;

        var affectedRows = command.ExecuteNonQuery();

        EnsureRecordUpdated(
            affectedRows,
            evaluationId);
    }

    private static void AddEvaluationParameters(
        SqlCommand command,
        EmployeeEvaluation evaluation)
    {
        command.Parameters.Add(
            "@EmployeeId",
            SqlDbType.Int).Value =
            evaluation.EmployeeId;

        command.Parameters.Add(
            "@EvaluationType",
            SqlDbType.NVarChar,
            100).Value =
            string.IsNullOrWhiteSpace(
                evaluation.EvaluationType)
                ? DBNull.Value
                : evaluation.EvaluationType.Trim();

        command.Parameters.Add(
            "@BonusType",
            SqlDbType.NVarChar,
            100).Value =
            string.IsNullOrWhiteSpace(
                evaluation.BonusType)
                ? DBNull.Value
                : evaluation.BonusType.Trim();

        command.Parameters.Add(
            "@Amount",
            SqlDbType.Decimal).Value =
            evaluation.Amount;

        command.Parameters["@Amount"].Precision = 18;
        command.Parameters["@Amount"].Scale = 2;

        command.Parameters.Add(
            "@BonusDate",
            SqlDbType.DateTime2).Value =
            evaluation.BonusDate;

        command.Parameters.Add(
            "@Comment",
            SqlDbType.NVarChar,
            500).Value =
            string.IsNullOrWhiteSpace(
                evaluation.Comment)
                ? DBNull.Value
                : evaluation.Comment.Trim();
    }

    private static EvaluationEmployeeItemModel
        MapEvaluationEmployeeItem(
            SqlDataReader reader)
    {
        return new EvaluationEmployeeItemModel
        {
            EmployeeId = reader.GetInt32(
                reader.GetOrdinal("EmployeeID")),

            FullName = GetRequiredString(
                reader,
                "FullName"),

            DepartmentId = reader.GetInt32(
                reader.GetOrdinal("Department_ID")),

            DepartmentName = GetRequiredString(
                reader,
                "DepartmentName"),

            RoleId = reader.GetInt32(
                reader.GetOrdinal("Role_ID")),

            RoleName = GetRequiredString(
                reader,
                "RoleName"),

            EvaluationCount = reader.GetInt32(
                reader.GetOrdinal("EvaluationCount")),

            TotalReward = reader.GetDecimal(
                reader.GetOrdinal("TotalReward")),

            TotalPenalty = reader.GetDecimal(
                reader.GetOrdinal("TotalPenalty"))
        };
    }

    private static EmployeeEvaluationItemModel
        MapEmployeeEvaluationItem(
            SqlDataReader reader)
    {
        return new EmployeeEvaluationItemModel
        {
            EvaluationId = reader.GetInt32(
                reader.GetOrdinal("Evaluation_ID")),

            EmployeeId = reader.GetInt32(
                reader.GetOrdinal("Employee_ID")),

            EvaluationType =
                GetNullableString(
                    reader,
                    "EvaluationType")
                ?? string.Empty,

            BonusType =
                GetNullableString(
                    reader,
                    "BonusType")
                ?? string.Empty,

            Amount = reader.GetDecimal(
                reader.GetOrdinal("Amount")),

            BonusDate = reader.GetDateTime(
                reader.GetOrdinal("Bonus_Date")),

            Comment =
                GetNullableString(
                    reader,
                    "Comment")
                ?? string.Empty
        };
    }

    private static EmployeeEvaluation
        MapEmployeeEvaluation(
            SqlDataReader reader)
    {
        return new EmployeeEvaluation
        {
            EvaluationId = reader.GetInt32(
                reader.GetOrdinal("Evaluation_ID")),

            EmployeeId = reader.GetInt32(
                reader.GetOrdinal("Employee_ID")),

            EvaluationType = GetNullableString(
                reader,
                "EvaluationType"),

            BonusType = GetNullableString(
                reader,
                "BonusType"),

            Amount = reader.GetDecimal(
                reader.GetOrdinal("Amount")),

            BonusDate = reader.GetDateTime(
                reader.GetOrdinal("Bonus_Date")),

            Comment = GetNullableString(
                reader,
                "Comment")
        };
    }

    private static string GetRequiredString(
        SqlDataReader reader,
        string columnName)
    {
        var ordinal =
            reader.GetOrdinal(columnName);

        return reader.IsDBNull(ordinal)
            ? string.Empty
            : reader.GetString(ordinal);
    }

    private static string? GetNullableString(
        SqlDataReader reader,
        string columnName)
    {
        var ordinal =
            reader.GetOrdinal(columnName);

        return reader.IsDBNull(ordinal)
            ? null
            : reader.GetString(ordinal);
    }

    private static (
        DateTime PeriodStart,
        DateTime PeriodEnd)
        CreateEvaluationPeriod(
            int month,
            int year)
    {
        ValidateEvaluationPeriod(
            month,
            year);

        var periodStart =
            new DateTime(
                year,
                month,
                1);

        var periodEnd =
            periodStart.AddMonths(1);

        return (
            periodStart,
            periodEnd);
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

    private static void ValidateEvaluationPeriod(
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

    private static void ValidateEvaluation(
        EmployeeEvaluation evaluation)
    {
        if (evaluation == null)
        {
            throw new ArgumentNullException(
                nameof(evaluation));
        }

        ValidateEmployeeId(
            evaluation.EmployeeId);

        if (evaluation.Amount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(evaluation.Amount),
                "Evaluation amount cannot be negative.");
        }

        if (evaluation.BonusDate == default)
        {
            throw new ArgumentException(
                "Evaluation date is required.",
                nameof(evaluation.BonusDate));
        }

        if (evaluation.Comment?.Length > 500)
        {
            throw new ArgumentException(
                "Comment cannot exceed 500 characters.",
                nameof(evaluation.Comment));
        }
    }

    private static void EnsureRecordUpdated(
        int affectedRows,
        int evaluationId)
    {
        if (affectedRows > 0)
            return;

        throw new InvalidOperationException(
            $"Employee evaluation {evaluationId} was not found.");
    }
}
