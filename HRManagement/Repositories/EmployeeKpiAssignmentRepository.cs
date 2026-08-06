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
                   k.[KPI_Name], k.[Measurement_Unit]
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
                   k.[KPI_Name], k.[Measurement_Unit]
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
                   k.[KPI_Name], k.[Measurement_Unit]
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
                   k.[KPI_Name], k.[Measurement_Unit]
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

    public void Delete(int assignmentId)
    {
        using var conn = Db.CreateConnection();
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM [dbo].[Employee_KPI_Assignment] WHERE [Assignment_ID] = @assignmentId";
        cmd.Parameters.Add(new SqlParameter("@assignmentId", SqlDbType.Int) { Value = assignmentId });

        cmd.ExecuteNonQuery();
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
            MeasurementUnit = reader.IsDBNull(reader.GetOrdinal("Measurement_Unit")) ? null : reader.GetString(reader.GetOrdinal("Measurement_Unit"))
        };
    }
}
