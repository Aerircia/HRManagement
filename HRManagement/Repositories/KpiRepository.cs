using HRManagement.Data;
using HRManagement.Models;
using HRManagement.Repositories.Interfaces;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace HRManagement.Repositories;

/// <summary>
/// Master KPI definitions (KPI table). See KPI.sql.
/// </summary>
public class KpiRepository : RepositoryBase, IKpiRepository
{
    public List<Kpi> GetAll()
    {
        var results = new List<Kpi>();

        using var conn = Db.CreateConnection();
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT [KPI_ID], [KPI_Name], [Description], [KPI_Type],
                   [Measurement_Unit], [Default_Target], [Calculation_Method],
                   [Is_Active], [Created_At]
            FROM [dbo].[KPI]
            ORDER BY [KPI_Name]
            """;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(MapKpi(reader));
        }

        return results;
    }

    public Kpi? GetById(int kpiId)
    {
        using var conn = Db.CreateConnection();
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT [KPI_ID], [KPI_Name], [Description], [KPI_Type],
                   [Measurement_Unit], [Default_Target], [Calculation_Method],
                   [Is_Active], [Created_At]
            FROM [dbo].[KPI]
            WHERE [KPI_ID] = @kpiId
            """;
        cmd.Parameters.Add(new SqlParameter("@kpiId", SqlDbType.Int) { Value = kpiId });

        using var reader = cmd.ExecuteReader();
        return reader.Read() ? MapKpi(reader) : null;
    }

    public int Add(Kpi kpi)
    {
        if (kpi == null) throw new ArgumentNullException(nameof(kpi));

        using var conn = Db.CreateConnection();
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO [dbo].[KPI]
                ([KPI_Name], [Description], [KPI_Type], [Measurement_Unit],
                 [Default_Target], [Calculation_Method], [Is_Active])
            VALUES
                (@name, @description, @type, @unit, @target, @calcMethod, @isActive);
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;
        cmd.Parameters.Add(new SqlParameter("@name", SqlDbType.NVarChar, 200) { Value = kpi.KpiName ?? string.Empty });
        cmd.Parameters.Add(new SqlParameter("@description", SqlDbType.NVarChar, 1000) { Value = (object?)kpi.Description ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@type", SqlDbType.NVarChar, 30) { Value = kpi.KpiType ?? string.Empty });
        cmd.Parameters.Add(new SqlParameter("@unit", SqlDbType.NVarChar, 30) { Value = kpi.MeasurementUnit ?? string.Empty });
        cmd.Parameters.Add(new SqlParameter("@target", SqlDbType.Decimal) { Value = kpi.DefaultTarget });
        cmd.Parameters.Add(new SqlParameter("@calcMethod", SqlDbType.NVarChar, 30) { Value = kpi.CalculationMethod ?? "HigherIsBetter" });
        cmd.Parameters.Add(new SqlParameter("@isActive", SqlDbType.Bit) { Value = kpi.IsActive });

        var idObj = cmd.ExecuteScalar();
        var newId = idObj != null && idObj != DBNull.Value ? Convert.ToInt32(idObj) : 0;
        kpi.KpiId = newId;
        return newId;
    }

    public void Update(Kpi kpi)
    {
        if (kpi == null) throw new ArgumentNullException(nameof(kpi));

        using var conn = Db.CreateConnection();
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE [dbo].[KPI]
            SET [KPI_Name] = @name,
                [Description] = @description,
                [KPI_Type] = @type,
                [Measurement_Unit] = @unit,
                [Default_Target] = @target,
                [Calculation_Method] = @calcMethod,
                [Is_Active] = @isActive
            WHERE [KPI_ID] = @kpiId
            """;
        cmd.Parameters.Add(new SqlParameter("@name", SqlDbType.NVarChar, 200) { Value = kpi.KpiName ?? string.Empty });
        cmd.Parameters.Add(new SqlParameter("@description", SqlDbType.NVarChar, 1000) { Value = (object?)kpi.Description ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@type", SqlDbType.NVarChar, 30) { Value = kpi.KpiType ?? string.Empty });
        cmd.Parameters.Add(new SqlParameter("@unit", SqlDbType.NVarChar, 30) { Value = kpi.MeasurementUnit ?? string.Empty });
        cmd.Parameters.Add(new SqlParameter("@target", SqlDbType.Decimal) { Value = kpi.DefaultTarget });
        cmd.Parameters.Add(new SqlParameter("@calcMethod", SqlDbType.NVarChar, 30) { Value = kpi.CalculationMethod ?? "HigherIsBetter" });
        cmd.Parameters.Add(new SqlParameter("@isActive", SqlDbType.Bit) { Value = kpi.IsActive });
        cmd.Parameters.Add(new SqlParameter("@kpiId", SqlDbType.Int) { Value = kpi.KpiId });

        cmd.ExecuteNonQuery();
    }

    public void Delete(int kpiId)
    {
        using var conn = Db.CreateConnection();
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM [dbo].[KPI] WHERE [KPI_ID] = @kpiId";
        cmd.Parameters.Add(new SqlParameter("@kpiId", SqlDbType.Int) { Value = kpiId });

        cmd.ExecuteNonQuery();
    }

    private static Kpi MapKpi(SqlDataReader reader)
    {
        return new Kpi
        {
            KpiId = reader.GetInt32(reader.GetOrdinal("KPI_ID")),
            KpiName = reader.IsDBNull(reader.GetOrdinal("KPI_Name")) ? string.Empty : reader.GetString(reader.GetOrdinal("KPI_Name")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
            KpiType = reader.IsDBNull(reader.GetOrdinal("KPI_Type")) ? string.Empty : reader.GetString(reader.GetOrdinal("KPI_Type")),
            MeasurementUnit = reader.IsDBNull(reader.GetOrdinal("Measurement_Unit")) ? string.Empty : reader.GetString(reader.GetOrdinal("Measurement_Unit")),
            DefaultTarget = reader.IsDBNull(reader.GetOrdinal("Default_Target")) ? 0m : reader.GetDecimal(reader.GetOrdinal("Default_Target")),
            CalculationMethod = reader.IsDBNull(reader.GetOrdinal("Calculation_Method")) ? "HigherIsBetter" : reader.GetString(reader.GetOrdinal("Calculation_Method")),
            IsActive = !reader.IsDBNull(reader.GetOrdinal("Is_Active")) && reader.GetBoolean(reader.GetOrdinal("Is_Active")),
            CreatedAt = reader.IsDBNull(reader.GetOrdinal("Created_At")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("Created_At"))
        };
    }
}
