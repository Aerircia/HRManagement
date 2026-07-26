using System;
using System.Collections.Generic;
using System.Text;
using HRManagement.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace HRManagement.Repositories
{
    public class ManageSalariesRepository
    : RepositoryBase, IManageSalariesRepository
    {
        // =========================================================
        // Employee
        // =========================================================

        public IReadOnlyList<Employee> GetEmployeesForPeriod(
            int month,
            int year)
        {
            var (periodStart, periodEnd) =
                CreateSalaryPeriod(month, year);

            var employees = new List<Employee>();

            using var connection = Db.CreateConnection();
            connection.Open();

            const string sql = """
        SELECT DISTINCT
            e.EmployeeID,
            e.FullName,
            e.Date_of_birth,
            e.Phone,
            e.Email,
            e.Role_ID,
            e.Department_ID,
            e.HireDate,
            e.Status,
            e.Avatar
        FROM Employee AS e
        INNER JOIN Contract AS c
            ON c.Employee_ID = e.EmployeeID
        WHERE c.StartDate < @PeriodEnd
          AND
          (
              c.EndDate IS NULL
              OR c.EndDate >= @PeriodStart
          )
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

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                employees.Add(MapEmployee(reader));
            }

            return employees;
        }

        // =========================================================
        // Contract
        // =========================================================

        public void UpdateContractBaseSalary(
            int contractId,
            decimal baseSalary)
        {
            ValidateContractId(contractId);
            ValidateNonNegativeAmount(
                baseSalary,
                nameof(baseSalary),
                "Base salary");

            using var connection = Db.CreateConnection();
            connection.Open();

            const string sql = """
        UPDATE Contract
        SET BaseSalary = @BaseSalary
        WHERE Contract_ID = @ContractId;
        """;

            using var command =
                new SqlCommand(sql, connection);

            command.Parameters.Add(
                "@BaseSalary",
                SqlDbType.Decimal).Value = baseSalary;

            command.Parameters[
                "@BaseSalary"].Precision = 18;

            command.Parameters[
                "@BaseSalary"].Scale = 2;

            command.Parameters.Add(
                "@ContractId",
                SqlDbType.Int).Value = contractId;

            var affectedRows =
                command.ExecuteNonQuery();

            EnsureRecordUpdated(
                affectedRows,
                $"Contract with ID {contractId} could not be found.");
        }

        // =========================================================
        // Role
        // =========================================================

        public void UpdateRolePayRate(
            int roleId,
            decimal payRate)
        {
            ValidateRoleId(roleId);
            ValidateNonNegativeAmount(
                payRate,
                nameof(payRate),
                "Pay rate");

            using var connection = Db.CreateConnection();
            connection.Open();

            const string sql = """
        UPDATE Role
        SET PayRate = @PayRate
        WHERE Role_ID = @RoleId;
        """;

            using var command =
                new SqlCommand(sql, connection);

            command.Parameters.Add(
                "@PayRate",
                SqlDbType.Decimal).Value = payRate;

            command.Parameters[
                "@PayRate"].Precision = 18;

            command.Parameters[
                "@PayRate"].Scale = 2;

            command.Parameters.Add(
                "@RoleId",
                SqlDbType.Int).Value = roleId;

            var affectedRows =
                command.ExecuteNonQuery();

            EnsureRecordUpdated(
                affectedRows,
                $"Role with ID {roleId} could not be found.");
        }

        // =========================================================
        // Attendance
        // =========================================================

        public int AddAttendance(
            Attendance attendance)
        {
            ArgumentNullException.ThrowIfNull(attendance);

            ValidateEmployeeId(
                attendance.EmployeeId);

            ValidateAttendance(attendance);

            using var connection = Db.CreateConnection();
            connection.Open();

            const string sql = """
        INSERT INTO Attendance
        (
            Employee_ID,
            Check_in,
            Check_out,
            Status
        )
        VALUES
        (
            @EmployeeId,
            @CheckIn,
            @CheckOut,
            @Status
        );

        SELECT CAST(SCOPE_IDENTITY() AS INT);
        """;

            using var command =
                new SqlCommand(sql, connection);

            AddAttendanceParameters(
                command,
                attendance,
                includeAttendanceId: false);

            var result =
                command.ExecuteScalar();

            if (result == null
                || result == DBNull.Value)
            {
                throw new InvalidOperationException(
                    "Attendance could not be created.");
            }

            return Convert.ToInt32(result);
        }

        public void UpdateAttendance(
            Attendance attendance)
        {
            ArgumentNullException.ThrowIfNull(attendance);

            ValidateAttendanceId(
                attendance.AttendanceId);

            ValidateEmployeeId(
                attendance.EmployeeId);

            ValidateAttendance(attendance);

            using var connection = Db.CreateConnection();
            connection.Open();

            const string sql = """
        UPDATE Attendance
        SET
            Employee_ID = @EmployeeId,
            Check_in = @CheckIn,
            Check_out = @CheckOut,
            Status = @Status
        WHERE Attendance_ID = @AttendanceId;
        """;

            using var command =
                new SqlCommand(sql, connection);

            AddAttendanceParameters(
                command,
                attendance,
                includeAttendanceId: true);

            var affectedRows =
                command.ExecuteNonQuery();

            EnsureRecordUpdated(
                affectedRows,
                $"Attendance with ID " +
                $"{attendance.AttendanceId} could not be found.");
        }

        public void DeleteAttendance(
            int attendanceId)
        {
            ValidateAttendanceId(attendanceId);

            using var connection = Db.CreateConnection();
            connection.Open();

            const string sql = """
        DELETE FROM Attendance
        WHERE Attendance_ID = @AttendanceId;
        """;

            using var command =
                new SqlCommand(sql, connection);

            command.Parameters.Add(
                "@AttendanceId",
                SqlDbType.Int).Value = attendanceId;

            var affectedRows =
                command.ExecuteNonQuery();

            EnsureRecordUpdated(
                affectedRows,
                $"Attendance with ID {attendanceId} could not be found.");
        }

        // =========================================================
        // Employee Evaluation
        // =========================================================

        public int AddEvaluation(
            EmployeeEvaluation evaluation)
        {
            ArgumentNullException.ThrowIfNull(evaluation);

            ValidateEmployeeId(
                evaluation.EmployeeId);

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
            Bonus_Date
        )
        VALUES
        (
            @EmployeeId,
            @EvaluationType,
            @BonusType,
            @Amount,
            @BonusDate
        );

        SELECT CAST(SCOPE_IDENTITY() AS INT);
        """;

            using var command =
                new SqlCommand(sql, connection);

            AddEvaluationParameters(
                command,
                evaluation,
                includeEvaluationId: false);

            var result =
                command.ExecuteScalar();

            if (result == null
                || result == DBNull.Value)
            {
                throw new InvalidOperationException(
                    "Employee evaluation could not be created.");
            }

            return Convert.ToInt32(result);
        }

        public void UpdateEvaluation(
            EmployeeEvaluation evaluation)
        {
            ArgumentNullException.ThrowIfNull(evaluation);

            ValidateEvaluationId(
                evaluation.EvaluationId);

            ValidateEmployeeId(
                evaluation.EmployeeId);

            ValidateEvaluation(evaluation);

            using var connection = Db.CreateConnection();
            connection.Open();

            const string sql = """
        UPDATE EmployeeEvaluation
        SET
            Employee_ID = @EmployeeId,
            EvaluationType = @EvaluationType,
            BonusType = @BonusType,
            Amount = @Amount,
            Bonus_Date = @BonusDate
        WHERE Evaluation_ID = @EvaluationId;
        """;

            using var command =
                new SqlCommand(sql, connection);

            AddEvaluationParameters(
                command,
                evaluation,
                includeEvaluationId: true);

            var affectedRows =
                command.ExecuteNonQuery();

            EnsureRecordUpdated(
                affectedRows,
                $"Evaluation with ID " +
                $"{evaluation.EvaluationId} could not be found.");
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

            var affectedRows =
                command.ExecuteNonQuery();

            EnsureRecordUpdated(
                affectedRows,
                $"Evaluation with ID {evaluationId} could not be found.");
        }

        // =========================================================
        // Payroll
        // =========================================================

        public int CreatePayroll(
            Payroll payroll)
        {
            ArgumentNullException.ThrowIfNull(payroll);

            ValidatePayroll(payroll);

            using var connection = Db.CreateConnection();
            connection.Open();

            using var transaction =
                connection.BeginTransaction();

            try
            {
                if (PayrollExists(
                        connection,
                        transaction,
                        payroll.EmployeeId,
                        payroll.Month,
                        payroll.Year))
                {
                    throw new InvalidOperationException(
                        $"Payroll for employee " +
                        $"{payroll.EmployeeId} in " +
                        $"{payroll.Month:00}/{payroll.Year} " +
                        $"already exists.");
                }

                var payrollId = InsertPayroll(
                    connection,
                    transaction,
                    payroll);

                transaction.Commit();

                return payrollId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public int CreateMonthlyPayroll(
            IReadOnlyList<Payroll> payrolls)
        {
            ArgumentNullException.ThrowIfNull(payrolls);

            if (payrolls.Count == 0)
                return 0;

            using var connection = Db.CreateConnection();
            connection.Open();

            using var transaction =
                connection.BeginTransaction();

            try
            {
                var createdCount = 0;

                foreach (var payroll in payrolls)
                {
                    if (payroll == null)
                        continue;

                    ValidatePayroll(payroll);

                    var exists = PayrollExists(
                        connection,
                        transaction,
                        payroll.EmployeeId,
                        payroll.Month,
                        payroll.Year);

                    if (exists)
                        continue;

                    InsertPayroll(
                        connection,
                        transaction,
                        payroll);

                    createdCount++;
                }

                transaction.Commit();

                return createdCount;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public void DeletePayroll(
            int employeeId,
            int month,
            int year)
        {
            ValidateEmployeeId(employeeId);
            ValidateSalaryPeriod(month, year);

            using var connection = Db.CreateConnection();
            connection.Open();

            const string sql = """
        DELETE FROM Payroll
        WHERE Employee_ID = @EmployeeId
          AND [Month] = @Month
          AND [Year] = @Year;
        """;

            using var command =
                new SqlCommand(sql, connection);

            command.Parameters.Add(
                "@EmployeeId",
                SqlDbType.Int).Value = employeeId;

            command.Parameters.Add(
                "@Month",
                SqlDbType.Int).Value = month;

            command.Parameters.Add(
                "@Year",
                SqlDbType.Int).Value = year;

            var affectedRows =
                command.ExecuteNonQuery();

            EnsureRecordUpdated(
                affectedRows,
                $"Payroll for employee {employeeId} " +
                $"in {month:00}/{year} could not be found.");
        }

        // =========================================================
        // Payroll helpers
        // =========================================================

        private static int InsertPayroll(
            SqlConnection connection,
            SqlTransaction transaction,
            Payroll payroll)
        {
            const string sql = """
        INSERT INTO Payroll
        (
            Employee_ID,
            Evaluation_ID,
            [Month],
            [Year]
        )
        VALUES
        (
            @EmployeeId,
            @EvaluationId,
            @Month,
            @Year
        );

        SELECT CAST(SCOPE_IDENTITY() AS INT);
        """;

            using var command =
                new SqlCommand(
                    sql,
                    connection,
                    transaction);

            command.Parameters.Add(
                "@EmployeeId",
                SqlDbType.Int).Value =
                    payroll.EmployeeId;

            command.Parameters.Add(
                "@EvaluationId",
                SqlDbType.Int).Value =
                    payroll.EvaluationId.HasValue
                        ? payroll.EvaluationId.Value
                        : DBNull.Value;

            command.Parameters.Add(
                "@Month",
                SqlDbType.Int).Value =
                    payroll.Month;

            command.Parameters.Add(
                "@Year",
                SqlDbType.Int).Value =
                    payroll.Year;

            var result =
                command.ExecuteScalar();

            if (result == null
                || result == DBNull.Value)
            {
                throw new InvalidOperationException(
                    "Payroll could not be created.");
            }

            return Convert.ToInt32(result);
        }

        private static bool PayrollExists(
            SqlConnection connection,
            SqlTransaction transaction,
            int employeeId,
            int month,
            int year)
        {
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

            using var command =
                new SqlCommand(
                    sql,
                    connection,
                    transaction);

            command.Parameters.Add(
                "@EmployeeId",
                SqlDbType.Int).Value = employeeId;

            command.Parameters.Add(
                "@Month",
                SqlDbType.Int).Value = month;

            command.Parameters.Add(
                "@Year",
                SqlDbType.Int).Value = year;

            var result =
                command.ExecuteScalar();

            return result != null
                   && result != DBNull.Value
                   && Convert.ToBoolean(result);
        }

        // =========================================================
        // Parameter helpers
        // =========================================================

        private static void AddAttendanceParameters(
            SqlCommand command,
            Attendance attendance,
            bool includeAttendanceId)
        {
            if (includeAttendanceId)
            {
                command.Parameters.Add(
                    "@AttendanceId",
                    SqlDbType.Int).Value =
                        attendance.AttendanceId;
            }

            command.Parameters.Add(
                "@EmployeeId",
                SqlDbType.Int).Value =
                    attendance.EmployeeId;

            command.Parameters.Add(
                "@CheckIn",
                SqlDbType.DateTime2).Value =
                    attendance.CheckIn.HasValue
                        ? attendance.CheckIn.Value
                        : DBNull.Value;

            command.Parameters.Add(
                "@CheckOut",
                SqlDbType.DateTime2).Value =
                    attendance.CheckOut.HasValue
                        ? attendance.CheckOut.Value
                        : DBNull.Value;

            command.Parameters.Add(
                "@Status",
                SqlDbType.NVarChar,
                50).Value = attendance.Status.Trim();
        }

        private static void AddEvaluationParameters(
            SqlCommand command,
            EmployeeEvaluation evaluation,
            bool includeEvaluationId)
        {
            if (includeEvaluationId)
            {
                command.Parameters.Add(
                    "@EvaluationId",
                    SqlDbType.Int).Value =
                        evaluation.EvaluationId;
            }

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

            command.Parameters[
                "@Amount"].Precision = 18;

            command.Parameters[
                "@Amount"].Scale = 2;

            command.Parameters.Add(
                "@BonusDate",
                SqlDbType.DateTime2).Value =
                    evaluation.BonusDate;
        }

        // =========================================================
        // Mapping
        // =========================================================

        private static Employee MapEmployee(
            SqlDataReader reader)
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

        // =========================================================
        // Validation
        // =========================================================

        private static void ValidateAttendance(
            Attendance attendance)
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
            EmployeeEvaluation evaluation)
        {
            ValidateNonNegativeAmount(
                evaluation.Amount,
                nameof(evaluation.Amount),
                "Evaluation amount");

            if (evaluation.BonusDate == default)
            {
                throw new ArgumentException(
                    "Bonus date is required.",
                    nameof(evaluation));
            }

            if (!string.IsNullOrWhiteSpace(
                    evaluation.BonusType)
                && !evaluation.BonusType.Equals(
                    "Reward",
                    StringComparison.OrdinalIgnoreCase)
                && !evaluation.BonusType.Equals(
                    "Penalty",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Bonus type must be Reward or Penalty.",
                    nameof(evaluation));
            }
        }

        private static void ValidatePayroll(
            Payroll payroll)
        {
            ValidateEmployeeId(
                payroll.EmployeeId);

            ValidateSalaryPeriod(
                payroll.Month,
                payroll.Year);

            if (payroll.EvaluationId.HasValue
                && payroll.EvaluationId.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(payroll.EvaluationId),
                    "Evaluation ID must be greater than 0.");
            }
        }

        private static (
            DateTime PeriodStart,
            DateTime PeriodEnd)
            CreateSalaryPeriod(
                int month,
                int year)
        {
            ValidateSalaryPeriod(month, year);

            var periodStart =
                new DateTime(year, month, 1);

            var periodEnd =
                periodStart.AddMonths(1);

            return (periodStart, periodEnd);
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

        private static void EnsureRecordUpdated(
            int affectedRows,
            string errorMessage)
        {
            if (affectedRows == 0)
            {
                throw new InvalidOperationException(
                    errorMessage);
            }
        }
    }
}
