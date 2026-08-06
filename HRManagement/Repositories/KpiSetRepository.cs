using HRManagement.Data;
using HRManagement.Models;
using HRManagement.Repositories.Interfaces;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace HRManagement.Repositories;

/// <summary>
/// KPI packs (KPI_Set table) and their line items (KPI_Set_Detail).
/// See KPI_Set.sql / KPI_Set_Detail.sql.
/// </summary>
public class KpiSetRepository : RepositoryBase, IKpiSetRepository
{
    public List<KpiSet> GetAll()
    {
        var results = new List<KpiSet>();

        using var conn = Db.CreateConnection();
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT [KPI_Set_ID], [KPI_Set_Name], [Description], [Department_ID], [Is_Active], [Created_At]
            FROM [dbo].[KPI_Set]
            ORDER BY [Created_At] DESC
            """;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(MapKpiSet(reader));
        }

        return results;
    }

    public List<KpiSet> GetByDepartment(int? departmentId)
    {
        var results = new List<KpiSet>();

        using var conn = Db.CreateConnection();
        conn.Open();

        using var cmd = conn.CreateCommand();

        // NULL departmentId = org-wide packs not scoped to a specific
        // department; packs are returned if they match the requested
        // department OR have no department restriction at all.
        if (departmentId.HasValue)
        {
            cmd.CommandText = """
                SELECT [KPI_Set_ID], [KPI_Set_Name], [Description], [Department_ID], [Is_Active], [Created_At]
                FROM [dbo].[KPI_Set]
                WHERE [Department_ID] = @departmentId OR [Department_ID] IS NULL
                ORDER BY [Created_At] DESC
                """;
            cmd.Parameters.Add(new SqlParameter("@departmentId", SqlDbType.Int) { Value = departmentId.Value });
        }
        else
        {
            cmd.CommandText = """
                SELECT [KPI_Set_ID], [KPI_Set_Name], [Description], [Department_ID], [Is_Active], [Created_At]
                FROM [dbo].[KPI_Set]
                ORDER BY [Created_At] DESC
                """;
        }

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(MapKpiSet(reader));
        }

        return results;
    }

    public KpiSet? GetById(int kpiSetId)
    {
        using var conn = Db.CreateConnection();
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT [KPI_Set_ID], [KPI_Set_Name], [Description], [Department_ID], [Is_Active], [Created_At]
            FROM [dbo].[KPI_Set]
            WHERE [KPI_Set_ID] = @kpiSetId
            """;
        cmd.Parameters.Add(new SqlParameter("@kpiSetId", SqlDbType.Int) { Value = kpiSetId });

        using var reader = cmd.ExecuteReader();
        return reader.Read() ? MapKpiSet(reader) : null;
    }

    public int Add(KpiSet kpiSet)
    {
        if (kpiSet == null) throw new ArgumentNullException(nameof(kpiSet));

        using var conn = Db.CreateConnection();
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO [dbo].[KPI_Set] ([KPI_Set_Name], [Description], [Department_ID], [Is_Active])
            VALUES (@name, @description, @departmentId, @isActive);
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;
        cmd.Parameters.Add(new SqlParameter("@name", SqlDbType.NVarChar, 200) { Value = kpiSet.KpiSetName ?? string.Empty });
        cmd.Parameters.Add(new SqlParameter("@description", SqlDbType.NVarChar, 500) { Value = (object?)kpiSet.Description ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@departmentId", SqlDbType.Int) { Value = (object?)kpiSet.DepartmentId ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@isActive", SqlDbType.Bit) { Value = kpiSet.IsActive });

        var idObj = cmd.ExecuteScalar();
        var newId = idObj != null && idObj != DBNull.Value ? Convert.ToInt32(idObj) : 0;
        kpiSet.KpiSetId = newId;
        return newId;
    }

    public void Update(KpiSet kpiSet)
    {
        if (kpiSet == null) throw new ArgumentNullException(nameof(kpiSet));

        using var conn = Db.CreateConnection();
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE [dbo].[KPI_Set]
            SET [KPI_Set_Name] = @name,
                [Description] = @description,
                [Department_ID] = @departmentId,
                [Is_Active] = @isActive
            WHERE [KPI_Set_ID] = @kpiSetId
            """;
        cmd.Parameters.Add(new SqlParameter("@name", SqlDbType.NVarChar, 200) { Value = kpiSet.KpiSetName ?? string.Empty });
        cmd.Parameters.Add(new SqlParameter("@description", SqlDbType.NVarChar, 500) { Value = (object?)kpiSet.Description ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@departmentId", SqlDbType.Int) { Value = (object?)kpiSet.DepartmentId ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@isActive", SqlDbType.Bit) { Value = kpiSet.IsActive });
        cmd.Parameters.Add(new SqlParameter("@kpiSetId", SqlDbType.Int) { Value = kpiSet.KpiSetId });

        cmd.ExecuteNonQuery();
    }

    public void Delete(int kpiSetId)
    {
        using var conn = Db.CreateConnection();
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM [dbo].[KPI_Set] WHERE [KPI_Set_ID] = @kpiSetId";
        cmd.Parameters.Add(new SqlParameter("@kpiSetId", SqlDbType.Int) { Value = kpiSetId });

        cmd.ExecuteNonQuery();
    }

    public List<KpiSetDetail> GetDetails(int kpiSetId)
    {
        var results = new List<KpiSetDetail>();

        using var conn = Db.CreateConnection();
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT d.[KPI_Set_Detail_ID], d.[KPI_Set_ID], d.[KPI_ID], d.[Target_Value], d.[Weight],
                   k.[KPI_Name], k.[Measurement_Unit]
            FROM [dbo].[KPI_Set_Detail] d
            INNER JOIN [dbo].[KPI] k ON k.[KPI_ID] = d.[KPI_ID]
            WHERE d.[KPI_Set_ID] = @kpiSetId
            ORDER BY k.[KPI_Name]
            """;
        cmd.Parameters.Add(new SqlParameter("@kpiSetId", SqlDbType.Int) { Value = kpiSetId });

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new KpiSetDetail
            {
                KpiSetDetailId = reader.GetInt32(reader.GetOrdinal("KPI_Set_Detail_ID")),
                KpiSetId = reader.GetInt32(reader.GetOrdinal("KPI_Set_ID")),
                KpiId = reader.GetInt32(reader.GetOrdinal("KPI_ID")),
                TargetValue = reader.IsDBNull(reader.GetOrdinal("Target_Value")) ? 0m : reader.GetDecimal(reader.GetOrdinal("Target_Value")),
                Weight = reader.IsDBNull(reader.GetOrdinal("Weight")) ? 0m : reader.GetDecimal(reader.GetOrdinal("Weight")),
                KpiName = reader.IsDBNull(reader.GetOrdinal("KPI_Name")) ? null : reader.GetString(reader.GetOrdinal("KPI_Name")),
                MeasurementUnit = reader.IsDBNull(reader.GetOrdinal("Measurement_Unit")) ? null : reader.GetString(reader.GetOrdinal("Measurement_Unit"))
            });
        }

        return results;
    }

    public int AddDetail(KpiSetDetail detail)
    {
        if (detail == null) throw new ArgumentNullException(nameof(detail));

        using var conn = Db.CreateConnection();
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO [dbo].[KPI_Set_Detail] ([KPI_Set_ID], [KPI_ID], [Target_Value], [Weight])
            VALUES (@kpiSetId, @kpiId, @target, @weight);
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;
        cmd.Parameters.Add(new SqlParameter("@kpiSetId", SqlDbType.Int) { Value = detail.KpiSetId });
        cmd.Parameters.Add(new SqlParameter("@kpiId", SqlDbType.Int) { Value = detail.KpiId });
        cmd.Parameters.Add(new SqlParameter("@target", SqlDbType.Decimal) { Value = detail.TargetValue });
        cmd.Parameters.Add(new SqlParameter("@weight", SqlDbType.Decimal) { Value = detail.Weight });

        var idObj = cmd.ExecuteScalar();
        var newId = idObj != null && idObj != DBNull.Value ? Convert.ToInt32(idObj) : 0;
        detail.KpiSetDetailId = newId;
        return newId;
    }

    public void UpdateDetail(KpiSetDetail detail)
    {
        if (detail == null) throw new ArgumentNullException(nameof(detail));

        using var conn = Db.CreateConnection();
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE [dbo].[KPI_Set_Detail]
            SET [Target_Value] = @target,
                [Weight] = @weight
            WHERE [KPI_Set_Detail_ID] = @kpiSetDetailId
            """;
        cmd.Parameters.Add(new SqlParameter("@target", SqlDbType.Decimal) { Value = detail.TargetValue });
        cmd.Parameters.Add(new SqlParameter("@weight", SqlDbType.Decimal) { Value = detail.Weight });
        cmd.Parameters.Add(new SqlParameter("@kpiSetDetailId", SqlDbType.Int) { Value = detail.KpiSetDetailId });

        cmd.ExecuteNonQuery();
    }

    public void DeleteDetail(int kpiSetDetailId)
    {
        using var conn = Db.CreateConnection();
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM [dbo].[KPI_Set_Detail] WHERE [KPI_Set_Detail_ID] = @kpiSetDetailId";
        cmd.Parameters.Add(new SqlParameter("@kpiSetDetailId", SqlDbType.Int) { Value = kpiSetDetailId });

        cmd.ExecuteNonQuery();
    }

    private static KpiSet MapKpiSet(SqlDataReader reader)
    {
        var deptOrdinal = reader.GetOrdinal("Department_ID");

        return new KpiSet
        {
            KpiSetId = reader.GetInt32(reader.GetOrdinal("KPI_Set_ID")),
            KpiSetName = reader.IsDBNull(reader.GetOrdinal("KPI_Set_Name")) ? string.Empty : reader.GetString(reader.GetOrdinal("KPI_Set_Name")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
            DepartmentId = reader.IsDBNull(deptOrdinal) ? null : reader.GetInt32(deptOrdinal),
            IsActive = !reader.IsDBNull(reader.GetOrdinal("Is_Active")) && reader.GetBoolean(reader.GetOrdinal("Is_Active")),
            CreatedAt = reader.IsDBNull(reader.GetOrdinal("Created_At")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("Created_At"))
        };
    }
}
