using HRManagement.Data;
using HRManagement.Models;
using HRManagement.Repositories.Interfaces;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace HRManagement.Repositories;

/// <summary>
/// Per-employee KPI assignments (Employee_KPI_Assignment table).
/// See Employee_KPI_Assignment.sql.
/// </summary>
public class EmployeeKpiAssignmentRepository : RepositoryBase, IEmployeeKpiAssignmentRepository
{
    public List<EmployeeKpiAssignment> GetByEmployee(int employeeId)
    {
        var results = new List<EmployeeKpiAssignment>();

        using var conn = Db.CreateConnection();
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT a.[Assignment_ID], a.[EmployeeID], a.[KPI_Set_Detail_ID], a.[Assigned_Target],
                   a.[Assigned_Weight], a.[Current_Value], a.[Pending_Value], a.[Status],
                   a.[Start_Date], a.[End_Date], a.[Created_At], a.[Is_Locked],
                   k.[KPI_Name], k.[Measurement_Unit], k.[Calculation_Method]
            FROM [dbo].[Employee_KPI_Assignment] a
            INNER JOIN [dbo].[KPI_Set_Detail] d ON d.[KPI_Set_Detail_ID] = a.[KPI_Set_Detail_ID]
            INNER JOIN [dbo].[KPI] k ON k.[KPI_ID] = d.[KPI_ID]
            WHERE a.[EmployeeID] = @employeeId
            ORDER BY a.[Start_Date] DESC
            """;
        cmd.Parameters.Add(new SqlParameter("@employeeId", SqlDbType.Int) { Value = employeeId });

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(MapAssignment(reader));
        }

        return results;
    }

    public List<EmployeeKpiAssignment> GetByEmployeeAndPeriod(int employeeId, DateTime startDate, DateTime endDate)
    {
        var results = new List<EmployeeKpiAssignment>();

        using var conn = Db.CreateConnection();
        conn.Open();

        using var cmd = conn.CreateCommand();
        // Overlap match rather than exact equality, so a query for a given
        // month picks up assignments whose period contains that month.
        cmd.CommandText = """
            SELECT a.[Assignment_ID], a.[EmployeeID], a.[KPI_Set_Detail_ID], a.[Assigned_Target],
                   a.[Assigned_Weight], a.[Current_Value], a.[Pending_Value], a.[Status],
                   a.[Start_Date], a.[End_Date], a.[Created_At], a.[Is_Locked],
                   k.[KPI_Name], k.[Measurement_Unit], k.[Calculation_Method]
            FROM [dbo].[Employee_KPI_Assignment] a
            INNER JOIN [dbo].[KPI_Set_Detail] d ON d.[KPI_Set_Detail_ID] = a.[KPI_Set_Detail_ID]
            INNER JOIN [dbo].[KPI] k ON k.[KPI_ID] = d.[KPI_ID]
            WHERE a.[EmployeeID] = @employeeId
              AND a.[Start_Date] <= @endDate
              AND a.[End_Date] >= @startDate
            ORDER BY k.[KPI_Name]
            """;
        cmd.Parameters.Add(new SqlParameter("@employeeId", SqlDbType.Int) { Value = employeeId });
        cmd.Parameters.Add(new SqlParameter("@startDate", SqlDbType.Date) { Value = startDate.Date });
        cmd.Parameters.Add(new SqlParameter("@endDate", SqlDbType.Date) { Value = endDate.Date });

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(MapAssignment(reader));
        }

        return results;
    }

    public List<EmployeeKpiAssignment> GetByDepartment(int? departmentId, DateTime startDate, DateTime endDate)
    {
        var results = new List<EmployeeKpiAssignment>();

        using var conn = Db.CreateConnection();
        conn.Open();

        using var cmd = conn.CreateCommand();

        var sql = """
            SELECT a.[Assignment_ID], a.[EmployeeID], a.[KPI_Set_Detail_ID], a.[Assigned_Target],
                   a.[Assigned_Weight], a.[Current_Value], a.[Pending_Value], a.[Status],
                   a.[Start_Date], a.[End_Date], a.[Created_At], a.[Is_Locked],
                   k.[KPI_Name], k.[Measurement_Unit], k.[Calculation_Method]
            FROM [dbo].[Employee_KPI_Assignment] a
            INNER JOIN [dbo].[KPI_Set_Detail] d ON d.[KPI_Set_Detail_ID] = a.[KPI_Set_Detail_ID]
            INNER JOIN [dbo].[KPI] k ON k.[KPI_ID] = d.[KPI_ID]
            INNER JOIN [dbo].[Employee] e ON e.[EmployeeID] = a.[EmployeeID]
            WHERE a.[Start_Date] <= @endDate
              AND a.[End_Date] >= @startDate
            """;

        if (departmentId.HasValue)
            sql += " AND e.[Department_ID] = @departmentId";

        sql += " ORDER BY a.[EmployeeID], k.[KPI_Name]";

        cmd.CommandText = sql;
        cmd.Parameters.Add(new SqlParameter("@startDate", SqlDbType.Date) { Value = startDate.Date });
        cmd.Parameters.Add(new SqlParameter("@endDate", SqlDbType.Date) { Value = endDate.Date });
        if (departmentId.HasValue)
            cmd.Parameters.Add(new SqlParameter("@departmentId", SqlDbType.Int) { Value = departmentId.Value });

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(MapAssignment(reader));
        }

        return results;
    }

    public EmployeeKpiAssignment? GetById(int assignmentId)
    {
        using var conn = Db.CreateConnection();
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT a.[Assignment_ID], a.[EmployeeID], a.[KPI_Set_Detail_ID], a.[Assigned_Target],
                   a.[Assigned_Weight], a.[Current_Value], a.[Pending_Value], a.[Status],
                   a.[Start_Date], a.[End_Date], a.[Created_At], a.[Is_Locked],
                   k.[KPI_Name], k.[Measurement_Unit], k.[Calculation_Method]
            FROM [dbo].[Employee_KPI_Assignment] a
            INNER JOIN [dbo].[KPI_Set_Detail] d ON d.[KPI_Set_Detail_ID] = a.[KPI_Set_Detail_ID]
            INNER JOIN [dbo].[KPI] k ON k.[KPI_ID] = d.[KPI_ID]
            WHERE a.[Assignment_ID] = @assignmentId
            """;
        cmd.Parameters.Add(new SqlParameter("@assignmentId", SqlDbType.Int) { Value = assignmentId });

        using var reader = cmd.ExecuteReader();
        return reader.Read() ? MapAssignment(reader) : null;
    }

    public int Add(EmployeeKpiAssignment assignment)
    {
        if (assignment == null) throw new ArgumentNullException(nameof(assignment));

        using var conn = Db.CreateConnection();
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO [dbo].[Employee_KPI_Assignment]
                ([EmployeeID], [KPI_Set_Detail_ID], [Assigned_Target], [Assigned_Weight],
                 [Current_Value], [Pending_Value], [Status], [Start_Date], [End_Date], [Is_Locked])
            VALUES
                (@employeeId, @kpiSetDetailId, @target, @weight,
                 @currentValue, @pendingValue, @status, @startDate, @endDate, @isLocked);
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;
        cmd.Parameters.Add(new SqlParameter("@employeeId", SqlDbType.Int) { Value = assignment.EmployeeId });
        cmd.Parameters.Add(new SqlParameter("@kpiSetDetailId", SqlDbType.Int) { Value = assignment.KpiSetDetailId });
        cmd.Parameters.Add(new SqlParameter("@target", SqlDbType.Decimal) { Value = assignment.AssignedTarget });
        cmd.Parameters.Add(new SqlParameter("@weight", SqlDbType.Decimal) { Value = assignment.AssignedWeight });
        cmd.Parameters.Add(new SqlParameter("@currentValue", SqlDbType.Decimal) { Value = assignment.CurrentValue });
        cmd.Parameters.Add(new SqlParameter("@pendingValue", SqlDbType.Decimal) { Value = (object?)assignment.PendingValue ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@status", SqlDbType.NVarChar, 30) { Value = assignment.Status ?? "Not Started" });
        cmd.Parameters.Add(new SqlParameter("@startDate", SqlDbType.Date) { Value = assignment.StartDate.Date });
        cmd.Parameters.Add(new SqlParameter("@endDate", SqlDbType.Date) { Value = assignment.EndDate.Date });
        cmd.Parameters.Add(new SqlParameter("@isLocked", SqlDbType.Bit) { Value = assignment.IsLocked });

        var idObj = cmd.ExecuteScalar();
        var newId = idObj != null && idObj != DBNull.Value ? Convert.ToInt32(idObj) : 0;
        assignment.AssignmentId = newId;
        return newId;
    }

    public void Update(EmployeeKpiAssignment assignment)
    {
        if (assignment == null) throw new ArgumentNullException(nameof(assignment));

        using var conn = Db.CreateConnection();
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE [dbo].[Employee_KPI_Assignment]
            SET [Assigned_Target] = @target,
                [Assigned_Weight] = @weight,
                [Current_Value] = @currentValue,
                [Pending_Value] = @pendingValue,
                [Status] = @status,
                [Is_Locked] = @isLocked
            WHERE [Assignment_ID] = @assignmentId
            """;
        cmd.Parameters.Add(new SqlParameter("@target", SqlDbType.Decimal) { Value = assignment.AssignedTarget });
        cmd.Parameters.Add(new SqlParameter("@weight", SqlDbType.Decimal) { Value = assignment.AssignedWeight });
        cmd.Parameters.Add(new SqlParameter("@currentValue", SqlDbType.Decimal) { Value = assignment.CurrentValue });
        cmd.Parameters.Add(new SqlParameter("@pendingValue", SqlDbType.Decimal) { Value = (object?)assignment.PendingValue ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@status", SqlDbType.NVarChar, 30) { Value = assignment.Status ?? "Not Started" });
        cmd.Parameters.Add(new SqlParameter("@isLocked", SqlDbType.Bit) { Value = assignment.IsLocked });
        cmd.Parameters.Add(new SqlParameter("@assignmentId", SqlDbType.Int) { Value = assignment.AssignmentId });

        cmd.ExecuteNonQuery();
    }

    public void SaveBatch(
        IReadOnlyList<EmployeeKpiAssignment> assignments)
    {
        if (assignments == null)
        {
            throw new ArgumentNullException(
                nameof(assignments));
        }

        if (assignments.Count == 0)
            return;

        using var connection =
            Db.CreateConnection();

        connection.Open();

        using var transaction =
            connection.BeginTransaction();

        try
        {
            foreach (var assignment
                     in assignments)
            {
                if (assignment == null)
                {
                    throw new InvalidOperationException(
                        "Assignment batch contains a null item.");
                }

                if (assignment.AssignmentId > 0)
                {
                    using var updateCommand =
                        connection.CreateCommand();

                    updateCommand.Transaction =
                        transaction;

                    updateCommand.CommandText = """
                        UPDATE [dbo].[Employee_KPI_Assignment]
                        SET [Assigned_Target] = @target,
                            [Assigned_Weight] = @weight
                        WHERE [Assignment_ID] = @assignmentId
                          AND [Is_Locked] = 0
                          AND [Current_Value] = 0
                          AND [Pending_Value] IS NULL
                          AND [Status] = 'Not Started';
                        """;

                    AddDecimalParameter(
                        updateCommand,
                        "@target",
                        assignment.AssignedTarget);

                    AddDecimalParameter(
                        updateCommand,
                        "@weight",
                        assignment.AssignedWeight);

                    updateCommand.Parameters.Add(
                        new SqlParameter(
                            "@assignmentId",
                            SqlDbType.Int)
                        {
                            Value =
                                assignment.AssignmentId
                        });

                    var updatedRows =
                        updateCommand.ExecuteNonQuery();

                    if (updatedRows != 1)
                    {
                        throw new InvalidOperationException(
                            $"KPI assignment " +
                            $"{assignment.AssignmentId} could not be " +
                            "updated because it is locked, missing, or " +
                            "progress has already started.");
                    }
                }
                else
                {
                    using var insertCommand =
                        connection.CreateCommand();

                    insertCommand.Transaction =
                        transaction;

                    insertCommand.CommandText = """
                        INSERT INTO [dbo].[Employee_KPI_Assignment]
                        (
                            [EmployeeID],
                            [KPI_Set_Detail_ID],
                            [Assigned_Target],
                            [Assigned_Weight],
                            [Current_Value],
                            [Pending_Value],
                            [Status],
                            [Start_Date],
                            [End_Date],
                            [Is_Locked]
                        )
                        VALUES
                        (
                            @employeeId,
                            @kpiSetDetailId,
                            @target,
                            @weight,
                            0,
                            NULL,
                            'Not Started',
                            @startDate,
                            @endDate,
                            0
                        );

                        SELECT CAST(
                            SCOPE_IDENTITY()
                            AS INT);
                        """;

                    insertCommand.Parameters.Add(
                        new SqlParameter(
                            "@employeeId",
                            SqlDbType.Int)
                        {
                            Value =
                                assignment.EmployeeId
                        });

                    insertCommand.Parameters.Add(
                        new SqlParameter(
                            "@kpiSetDetailId",
                            SqlDbType.Int)
                        {
                            Value =
                                assignment.KpiSetDetailId
                        });

                    AddDecimalParameter(
                        insertCommand,
                        "@target",
                        assignment.AssignedTarget);

                    AddDecimalParameter(
                        insertCommand,
                        "@weight",
                        assignment.AssignedWeight);

                    insertCommand.Parameters.Add(
                        new SqlParameter(
                            "@startDate",
                            SqlDbType.Date)
                        {
                            Value =
                                assignment.StartDate.Date
                        });

                    insertCommand.Parameters.Add(
                        new SqlParameter(
                            "@endDate",
                            SqlDbType.Date)
                        {
                            Value =
                                assignment.EndDate.Date
                        });

                    var newId =
                        Convert.ToInt32(
                            insertCommand.ExecuteScalar());

                    assignment.AssignmentId =
                        newId;
                }
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public void ReplacePeriodAssignments(
        int employeeId,
        DateTime startDate,
        DateTime endDate,
        IReadOnlyList<EmployeeKpiAssignment> assignments)
    {
        if (assignments == null)
        {
            throw new ArgumentNullException(
                nameof(assignments));
        }

        if (assignments.Count == 0)
        {
            throw new InvalidOperationException(
                "At least one KPI assignment is required.");
        }

        if (assignments.Any(
                assignment =>
                    assignment.EmployeeId != employeeId))
        {
            throw new InvalidOperationException(
                "All assignments must belong to the same employee.");
        }

        using var connection =
            Db.CreateConnection();

        connection.Open();

        using var transaction =
            connection.BeginTransaction();

        try
        {
            /*
             * Server-side concurrency protection. The service already
             * checks this rule, but the repository repeats it inside the
             * transaction so progress cannot start between validation
             * and the database write.
             */
            using (var progressCommand =
                   connection.CreateCommand())
            {
                progressCommand.Transaction =
                    transaction;

                progressCommand.CommandText = """
                    SELECT COUNT(1)
                    FROM [dbo].[Employee_KPI_Assignment]
                    WHERE [EmployeeID] = @employeeId
                      AND [Start_Date] = @startDate
                      AND [End_Date] = @endDate
                      AND
                      (
                          [Is_Locked] = 1
                          OR [Current_Value] > 0
                          OR [Pending_Value] IS NOT NULL
                          OR [Status] <> 'Not Started'
                      );
                    """;

                progressCommand.Parameters.Add(
                    new SqlParameter(
                        "@employeeId",
                        SqlDbType.Int)
                    {
                        Value = employeeId
                    });

                progressCommand.Parameters.Add(
                    new SqlParameter(
                        "@startDate",
                        SqlDbType.Date)
                    {
                        Value = startDate.Date
                    });

                progressCommand.Parameters.Add(
                    new SqlParameter(
                        "@endDate",
                        SqlDbType.Date)
                    {
                        Value = endDate.Date
                    });

                var startedCount =
                    Convert.ToInt32(
                        progressCommand.ExecuteScalar());

                if (startedCount > 0)
                {
                    throw new InvalidOperationException(
                        "KPI assignments cannot be replaced after " +
                        "progress has started.");
                }
            }

            /*
             * Delete the old not-started period set first. All new rows
             * are inserted afterwards in the same transaction.
             */
            using (var deleteCommand =
                   connection.CreateCommand())
            {
                deleteCommand.Transaction =
                    transaction;

                deleteCommand.CommandText = """
                    DELETE FROM [dbo].[Employee_KPI_Assignment]
                    WHERE [EmployeeID] = @employeeId
                      AND [Start_Date] = @startDate
                      AND [End_Date] = @endDate;
                    """;

                deleteCommand.Parameters.Add(
                    new SqlParameter(
                        "@employeeId",
                        SqlDbType.Int)
                    {
                        Value = employeeId
                    });

                deleteCommand.Parameters.Add(
                    new SqlParameter(
                        "@startDate",
                        SqlDbType.Date)
                    {
                        Value = startDate.Date
                    });

                deleteCommand.Parameters.Add(
                    new SqlParameter(
                        "@endDate",
                        SqlDbType.Date)
                    {
                        Value = endDate.Date
                    });

                deleteCommand.ExecuteNonQuery();
            }

            foreach (var assignment
                     in assignments)
            {
                using var insertCommand =
                    connection.CreateCommand();

                insertCommand.Transaction =
                    transaction;

                insertCommand.CommandText = """
                    INSERT INTO [dbo].[Employee_KPI_Assignment]
                    (
                        [EmployeeID],
                        [KPI_Set_Detail_ID],
                        [Assigned_Target],
                        [Assigned_Weight],
                        [Current_Value],
                        [Pending_Value],
                        [Status],
                        [Start_Date],
                        [End_Date],
                        [Is_Locked]
                    )
                    VALUES
                    (
                        @employeeId,
                        @kpiSetDetailId,
                        @target,
                        @weight,
                        0,
                        NULL,
                        'Not Started',
                        @startDate,
                        @endDate,
                        0
                    );

                    SELECT CAST(
                        SCOPE_IDENTITY()
                        AS INT);
                    """;

                insertCommand.Parameters.Add(
                    new SqlParameter(
                        "@employeeId",
                        SqlDbType.Int)
                    {
                        Value = employeeId
                    });

                insertCommand.Parameters.Add(
                    new SqlParameter(
                        "@kpiSetDetailId",
                        SqlDbType.Int)
                    {
                        Value =
                            assignment.KpiSetDetailId
                    });

                AddDecimalParameter(
                    insertCommand,
                    "@target",
                    assignment.AssignedTarget);

                AddDecimalParameter(
                    insertCommand,
                    "@weight",
                    assignment.AssignedWeight);

                insertCommand.Parameters.Add(
                    new SqlParameter(
                        "@startDate",
                        SqlDbType.Date)
                    {
                        Value = startDate.Date
                    });

                insertCommand.Parameters.Add(
                    new SqlParameter(
                        "@endDate",
                        SqlDbType.Date)
                    {
                        Value = endDate.Date
                    });

                assignment.AssignmentId =
                    Convert.ToInt32(
                        insertCommand.ExecuteScalar());
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public List<EmployeeKpiAssignment> GetExpiredUnlocked(
        DateTime today)
    {
        var results =
            new List<EmployeeKpiAssignment>();

        using var conn =
            Db.CreateConnection();

        conn.Open();

        using var cmd =
            conn.CreateCommand();

        cmd.CommandText = """
            SELECT a.[Assignment_ID], a.[EmployeeID],
                   a.[KPI_Set_Detail_ID], a.[Assigned_Target],
                   a.[Assigned_Weight], a.[Current_Value],
                   a.[Pending_Value], a.[Status],
                   a.[Start_Date], a.[End_Date],
                   a.[Created_At], a.[Is_Locked],
                   k.[KPI_Name], k.[Measurement_Unit], k.[Calculation_Method]
            FROM [dbo].[Employee_KPI_Assignment] a
            INNER JOIN [dbo].[KPI_Set_Detail] d
                ON d.[KPI_Set_Detail_ID] =
                   a.[KPI_Set_Detail_ID]
            INNER JOIN [dbo].[KPI] k
                ON k.[KPI_ID] = d.[KPI_ID]
            WHERE a.[End_Date] < @today
              AND a.[Is_Locked] = 0
              AND a.[Status] NOT IN
                  ('Completed', 'Cancelled')
            ORDER BY a.[EmployeeID],
                     a.[Start_Date],
                     a.[Assignment_ID]
            """;

        cmd.Parameters.Add(
            new SqlParameter(
                "@today",
                SqlDbType.Date)
            {
                Value = today.Date
            });

        using var reader =
            cmd.ExecuteReader();

        while (reader.Read())
        {
            results.Add(
                MapAssignment(reader));
        }

        return results;
    }

    public void LockPeriod(
        int employeeId,
        DateTime startDate,
        DateTime endDate)
    {
        using var conn =
            Db.CreateConnection();

        conn.Open();

        using var cmd =
            conn.CreateCommand();

        cmd.CommandText = """
            UPDATE [dbo].[Employee_KPI_Assignment]
            SET [Is_Locked] = 1
            WHERE [EmployeeID] = @employeeId
              AND [Start_Date] = @startDate
              AND [End_Date] = @endDate
            """;

        cmd.Parameters.Add(
            new SqlParameter(
                "@employeeId",
                SqlDbType.Int)
            {
                Value = employeeId
            });

        cmd.Parameters.Add(
            new SqlParameter(
                "@startDate",
                SqlDbType.Date)
            {
                Value = startDate.Date
            });

        cmd.Parameters.Add(
            new SqlParameter(
                "@endDate",
                SqlDbType.Date)
            {
                Value = endDate.Date
            });

        cmd.ExecuteNonQuery();
    }

    public void FinalizePeriod(
        int employeeId,
        DateTime startDate,
        DateTime endDate,
        EmployeeEvaluation? evaluation)
    {
        using var connection = Db.CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();

        try
        {
            using (var lockCommand = connection.CreateCommand())
            {
                lockCommand.Transaction = transaction;
                lockCommand.CommandText = """
                    UPDATE [dbo].[Employee_KPI_Assignment]
                    SET [Is_Locked] = 1
                    WHERE [EmployeeID] = @employeeId
                      AND [Start_Date] = @startDate
                      AND [End_Date] = @endDate
                      AND [Status] = 'Completed';
                    """;

                lockCommand.Parameters.Add(new SqlParameter("@employeeId", SqlDbType.Int) { Value = employeeId });
                lockCommand.Parameters.Add(new SqlParameter("@startDate", SqlDbType.Date) { Value = startDate.Date });
                lockCommand.Parameters.Add(new SqlParameter("@endDate", SqlDbType.Date) { Value = endDate.Date });

                if (lockCommand.ExecuteNonQuery() <= 0)
                    throw new InvalidOperationException("No completed KPI assignments were available to finalize.");
            }

            if (evaluation != null)
            {
                using var existsCommand = connection.CreateCommand();
                existsCommand.Transaction = transaction;
                existsCommand.CommandText = """
                    SELECT CASE WHEN EXISTS
                    (
                        SELECT 1
                        FROM [dbo].[EmployeeEvaluation]
                        WHERE [Employee_ID] = @employeeId
                          AND [EvaluationType] = 'KPI'
                          AND [Bonus_Date] >= @periodStart
                          AND [Bonus_Date] < @periodEnd
                    )
                    THEN CAST(1 AS BIT)
                    ELSE CAST(0 AS BIT)
                    END;
                    """;

                existsCommand.Parameters.Add(new SqlParameter("@employeeId", SqlDbType.Int) { Value = employeeId });
                existsCommand.Parameters.Add(new SqlParameter("@periodStart", SqlDbType.DateTime2) { Value = startDate.Date });
                existsCommand.Parameters.Add(new SqlParameter("@periodEnd", SqlDbType.DateTime2) { Value = endDate.Date.AddDays(1) });

                var exists = Convert.ToBoolean(existsCommand.ExecuteScalar());

                if (!exists)
                {
                    using var insertCommand = connection.CreateCommand();
                    insertCommand.Transaction = transaction;
                    insertCommand.CommandText = """
                        INSERT INTO [dbo].[EmployeeEvaluation]
                        (
                            [Employee_ID],
                            [EvaluationType],
                            [BonusType],
                            [Amount],
                            [Bonus_Date],
                            [Comment]
                        )
                        VALUES
                        (
                            @employeeId,
                            @evaluationType,
                            @bonusType,
                            @amount,
                            @bonusDate,
                            @comment
                        );
                        """;

                    insertCommand.Parameters.Add(new SqlParameter("@employeeId", SqlDbType.Int) { Value = evaluation.EmployeeId });
                    insertCommand.Parameters.Add(new SqlParameter("@evaluationType", SqlDbType.NVarChar, 100) { Value = evaluation.EvaluationType ?? "KPI" });
                    insertCommand.Parameters.Add(new SqlParameter("@bonusType", SqlDbType.NVarChar, 100) { Value = evaluation.BonusType ?? "Reward" });

                    var amount = new SqlParameter("@amount", SqlDbType.Decimal)
                    {
                        Precision = 18,
                        Scale = 2,
                        Value = evaluation.Amount
                    };
                    insertCommand.Parameters.Add(amount);

                    insertCommand.Parameters.Add(new SqlParameter("@bonusDate", SqlDbType.DateTime2) { Value = evaluation.BonusDate.Date });
                    insertCommand.Parameters.Add(new SqlParameter("@comment", SqlDbType.NVarChar, 500)
                    {
                        Value = string.IsNullOrWhiteSpace(evaluation.Comment)
                            ? DBNull.Value
                            : evaluation.Comment.Trim()
                    });

                    insertCommand.ExecuteNonQuery();
                }
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public void Delete(int assignmentId)
    {
        using var conn = Db.CreateConnection();
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM [dbo].[Employee_KPI_Assignment] WHERE [Assignment_ID] = @assignmentId";
        cmd.Parameters.Add(new SqlParameter("@assignmentId", SqlDbType.Int) { Value = assignmentId });

        cmd.ExecuteNonQuery();
    }

    private static void AddDecimalParameter(
        SqlCommand command,
        string name,
        decimal value)
    {
        var parameter =
            new SqlParameter(
                name,
                SqlDbType.Decimal)
            {
                Precision = 18,
                Scale = 2,
                Value = value
            };

        command.Parameters.Add(
            parameter);
    }

    private static EmployeeKpiAssignment MapAssignment(SqlDataReader reader)
    {
        var pendingOrdinal = reader.GetOrdinal("Pending_Value");

        return new EmployeeKpiAssignment
        {
            AssignmentId = reader.GetInt32(reader.GetOrdinal("Assignment_ID")),
            EmployeeId = reader.GetInt32(reader.GetOrdinal("EmployeeID")),
            KpiSetDetailId = reader.GetInt32(reader.GetOrdinal("KPI_Set_Detail_ID")),
            AssignedTarget = reader.IsDBNull(reader.GetOrdinal("Assigned_Target")) ? 0m : reader.GetDecimal(reader.GetOrdinal("Assigned_Target")),
            AssignedWeight = reader.IsDBNull(reader.GetOrdinal("Assigned_Weight")) ? 0m : reader.GetDecimal(reader.GetOrdinal("Assigned_Weight")),
            CurrentValue = reader.IsDBNull(reader.GetOrdinal("Current_Value")) ? 0m : reader.GetDecimal(reader.GetOrdinal("Current_Value")),
            PendingValue = reader.IsDBNull(pendingOrdinal) ? null : reader.GetDecimal(pendingOrdinal),
            Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? "Not Started" : reader.GetString(reader.GetOrdinal("Status")),
            StartDate = reader.IsDBNull(reader.GetOrdinal("Start_Date")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("Start_Date")),
            EndDate = reader.IsDBNull(reader.GetOrdinal("End_Date")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("End_Date")),
            CreatedAt = reader.IsDBNull(reader.GetOrdinal("Created_At")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("Created_At")),
            IsLocked = !reader.IsDBNull(reader.GetOrdinal("Is_Locked")) && reader.GetBoolean(reader.GetOrdinal("Is_Locked")),
            KpiName = reader.IsDBNull(reader.GetOrdinal("KPI_Name")) ? null : reader.GetString(reader.GetOrdinal("KPI_Name")),
            MeasurementUnit = reader.IsDBNull(reader.GetOrdinal("Measurement_Unit")) ? null : reader.GetString(reader.GetOrdinal("Measurement_Unit")),
            CalculationMethod = reader.IsDBNull(reader.GetOrdinal("Calculation_Method"))
                ? "HigherIsBetter"
                : reader.GetString(reader.GetOrdinal("Calculation_Method"))
        };
    }
}
