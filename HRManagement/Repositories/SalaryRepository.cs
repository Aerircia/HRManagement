using HRManagement.Models;
using HRManagement.Repositories.Interfaces;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace HRManagement.Repositories
{
    public class SalaryRepository : RepositoryBase, ISalaryRepository
    {
        public Employee? GetEmployee(int employeeId)
        {
            ValidateEmployeeId(employeeId);

            using var connection = Db.CreateConnection();
            connection.Open();

            const string sql = """
            SELECT
                EmployeeID,
                FullName,
                Date_of_birth,
                Phone,
                Email,
                Role_ID,
                Department_ID,
                HireDate,
                Status,
                Avatar
            FROM Employee
            WHERE EmployeeID = @EmployeeId;
            """;

            using var command = new SqlCommand(sql, connection);

            command.Parameters.Add(
                "@EmployeeId",
                SqlDbType.Int).Value = employeeId;

            using var reader = command.ExecuteReader();

            if (!reader.Read())
                return null;

            return MapEmployee(reader);
        }

        public Contract? GetContractForPeriod(
            int employeeId,
            int month,
            int year)
        {
            ValidateEmployeeId(employeeId);

            var (periodStart, periodEnd) =
                CreateSalaryPeriod(month, year);

            using var connection = Db.CreateConnection();
            connection.Open();

            const string sql = """
            SELECT TOP (1)
                Contract_ID,
                Employee_ID,
                Role_ID,
                ContractType,
                StartDate,
                EndDate,
                Status,
                BaseSalary
            FROM Contract
            WHERE Employee_ID = @EmployeeId
              AND StartDate < @PeriodEnd
              AND (
                    EndDate IS NULL
                    OR EndDate >= @PeriodStart
                  )
            ORDER BY
                StartDate DESC,
                Contract_ID DESC;
            """;

            using var command = new SqlCommand(sql, connection);

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

            if (!reader.Read())
                return null;

            return MapContract(reader);
        }

        public Role? GetRole(int roleId)
        {
            if (roleId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(roleId),
                    "Role ID phải lớn hơn 0.");
            }

            using var connection = Db.CreateConnection();
            connection.Open();

            const string sql = """
            SELECT
                Role_ID,
                RoleName,
                PayRate
            FROM Role
            WHERE Role_ID = @RoleId;
            """;

            using var command = new SqlCommand(sql, connection);

            command.Parameters.Add(
                "@RoleId",
                SqlDbType.Int).Value = roleId;

            using var reader = command.ExecuteReader();

            if (!reader.Read())
                return null;

            return MapRole(reader);
        }

        public string GetDepartmentName(int departmentId)
        {
            if (departmentId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(departmentId),
                    "Department ID must be > 0.");
            }

            using var connection = Db.CreateConnection();
            connection.Open();

            const string sql = """
            SELECT DepartmentName
            FROM Department
            WHERE Department_ID = @DepartmentId;
            """;

            using var command = new SqlCommand(sql, connection);

            command.Parameters.Add(
                "@DepartmentId",
                SqlDbType.Int).Value = departmentId;

            var result = command.ExecuteScalar();

            return result == null || result == DBNull.Value
                ? string.Empty
                : Convert.ToString(result) ?? string.Empty;
        }

        public IReadOnlyList<Attendance> GetAttendances(
            int employeeId,
            int month,
            int year)
        {
            ValidateEmployeeId(employeeId);

            var (periodStart, periodEnd) =
                CreateSalaryPeriod(month, year);

            var attendances = new List<Attendance>();

            using var connection = Db.CreateConnection();
            connection.Open();

            const string sql = """
            SELECT
                Attendance_ID,
                Employee_ID,
                Check_in,
                Check_out,
                Status
            FROM Attendance
            WHERE Employee_ID = @EmployeeId
              AND Check_in >= @PeriodStart
              AND Check_in < @PeriodEnd
            ORDER BY
                Check_in ASC,
                Attendance_ID ASC;
            """;

            using var command = new SqlCommand(sql, connection);

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
                attendances.Add(MapAttendance(reader));
            }

            return attendances;
        }

        public IReadOnlyList<EmployeeEvaluation> GetEvaluations(
            int employeeId,
            int month,
            int year)
        {
            ValidateEmployeeId(employeeId);

            var (periodStart, periodEnd) =
                CreateSalaryPeriod(month, year);

            var evaluations = new List<EmployeeEvaluation>();

            using var connection = Db.CreateConnection();
            connection.Open();

            const string sql = """
            SELECT
                Evaluation_ID,
                Employee_ID,
                EvaluationType,
                BonusType,
                Amount,
                Bonus_Date
            FROM EmployeeEvaluation
            WHERE Employee_ID = @EmployeeId
              AND Bonus_Date >= @PeriodStart
              AND Bonus_Date < @PeriodEnd
            ORDER BY
                Bonus_Date ASC,
                Evaluation_ID ASC;
            """;

            using var command = new SqlCommand(sql, connection);

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
                evaluations.Add(MapEvaluation(reader));
            }

            return evaluations;
        }

        public int? GetPayrollId(int employeeId,int month,int year)
        {
            ValidateEmployeeId(employeeId);
            ValidateSalaryPeriod(month, year);

            using var connection = Db.CreateConnection();
            connection.Open();

            const string sql = """
            SELECT TOP (1) Payroll_ID
            FROM Payroll
            WHERE Employee_ID = @EmployeeId
              AND [Month] = @Month
              AND [Year] = @Year
            ORDER BY Payroll_ID DESC;
            """;

            using var command = new SqlCommand(sql, connection);

            command.Parameters.Add(
                "@EmployeeId",
                SqlDbType.Int).Value = employeeId;

            command.Parameters.Add(
                "@Month",
                SqlDbType.Int).Value = month;

            command.Parameters.Add(
                "@Year",
                SqlDbType.Int).Value = year;

            var result = command.ExecuteScalar();

            if (result == null || result == DBNull.Value)
                return null;

            return Convert.ToInt32(result);
        }

        public bool PayrollExists(int employeeId,int month,int year)
        {
            ValidateEmployeeId(employeeId);
            ValidateSalaryPeriod(month, year);

            using var connection = Db.CreateConnection();
            connection.Open();

            const string sql = """
            SELECT CASE
                WHEN EXISTS
                (
                    SELECT 1
                    FROM Payroll
                    WHERE Employee_ID = @EmployeeId
                      AND [Month] = @Month
                      AND [Year] = @Year
                )
                THEN CAST(1 AS BIT)
                ELSE CAST(0 AS BIT)
            END;
            """;

            using var command = new SqlCommand(sql, connection);

            command.Parameters.Add(
                "@EmployeeId",
                SqlDbType.Int).Value = employeeId;

            command.Parameters.Add(
                "@Month",
                SqlDbType.Int).Value = month;

            command.Parameters.Add(
                "@Year",
                SqlDbType.Int).Value = year;

            var result = command.ExecuteScalar();

            return result != null
                   && result != DBNull.Value
                   && Convert.ToBoolean(result);
        }

        private static Employee MapEmployee(SqlDataReader reader)
        {
            return new Employee
            {
                EmployeeId = reader.GetInt32(
                    reader.GetOrdinal("EmployeeID")),

                FullName = reader.GetString(
                    reader.GetOrdinal("FullName")),

                DateOfBirth = reader.GetDateTime(
                    reader.GetOrdinal("Date_of_birth")),

                Phone = GetNullableString(
                    reader,
                    "Phone"),

                Email = reader.GetString(
                    reader.GetOrdinal("Email")),

                RoleId = reader.GetInt32(
                    reader.GetOrdinal("Role_ID")),

                DepartmentId = reader.GetInt32(
                    reader.GetOrdinal("Department_ID")),

                HireDate = reader.GetDateTime(
                    reader.GetOrdinal("HireDate")),

                Status = reader.GetString(
                    reader.GetOrdinal("Status")),

                Avatar = GetNullableString(
                    reader,
                    "Avatar")
            };
        }

        private static Contract MapContract(SqlDataReader reader)
        {
            return new Contract
            {
                ContractId = reader.GetInt32(
                    reader.GetOrdinal("Contract_ID")),

                EmployeeId = reader.GetInt32(
                    reader.GetOrdinal("Employee_ID")),

                RoleId = reader.GetInt32(
                    reader.GetOrdinal("Role_ID")),

                ContractType = reader.GetString(
                    reader.GetOrdinal("ContractType")),

                StartDate = reader.GetDateTime(
                    reader.GetOrdinal("StartDate")),

                EndDate = GetNullableDateTime(
                    reader,
                    "EndDate"),

                Status = reader.GetString(
                    reader.GetOrdinal("Status")),

                BaseSalary = reader.GetDecimal(
                    reader.GetOrdinal("BaseSalary"))
            };
        }

        private static Role MapRole(SqlDataReader reader)
        {
            return new Role
            {
                RoleId = reader.GetInt32(
                    reader.GetOrdinal("Role_ID")),

                RoleName = reader.GetString(
                    reader.GetOrdinal("RoleName")),

                PayRate = reader.GetDecimal(
                    reader.GetOrdinal("PayRate"))
            };
        }

        private static Attendance MapAttendance(SqlDataReader reader)
        {
            return new Attendance
            {
                AttendanceId = reader.GetInt32(
                    reader.GetOrdinal("Attendance_ID")),

                EmployeeId = reader.GetInt32(
                    reader.GetOrdinal("Employee_ID")),

                CheckIn = GetNullableDateTime(
                    reader,
                    "Check_in"),

                CheckOut = GetNullableDateTime(
                    reader,
                    "Check_out"),

                Status = reader.GetString(
                    reader.GetOrdinal("Status"))
            };
        }

        private static EmployeeEvaluation MapEvaluation(SqlDataReader reader)
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
                    reader.GetOrdinal("Bonus_Date"))
            };
        }

        private static string? GetNullableString(
            SqlDataReader reader,
            string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);

            return reader.IsDBNull(ordinal)
                ? null
                : reader.GetString(ordinal);
        }

        private static DateTime? GetNullableDateTime(
            SqlDataReader reader,
            string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);

            return reader.IsDBNull(ordinal)
                ? null
                : reader.GetDateTime(ordinal);
        }

        private static (
            DateTime PeriodStart,
            DateTime PeriodEnd)
            CreateSalaryPeriod(int month, int year)
        {
            ValidateSalaryPeriod(month, year);

            var periodStart = new DateTime(
                year,
                month,
                1);

            var periodEnd = periodStart.AddMonths(1);

            return (periodStart, periodEnd);
        }

        private static void ValidateEmployeeId(int employeeId)
        {
            if (employeeId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(employeeId),
                    "Employee ID must be > 0.");
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
                    "Month isn't between 1 and 12.");
            }

            if (year < 2000)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(year),
                    "Year must be > 2000.");
            }
        }
    }
}