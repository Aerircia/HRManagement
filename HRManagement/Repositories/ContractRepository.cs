using HRManagement.Models;
using HRManagement.Repositories.Interfaces;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;

namespace HRManagement.Repositories;

public class ContractRepository : RepositoryBase, IContractRepository
{
    /// <summary>
    /// Returns the employee's current contract: prefers one with Status = 'Active',
    /// falling back to the most recently started contract if none is marked active.
    /// </summary>
    public Contract? GetCurrentByEmployeeId(int employeeId)
    {
        using var connection = Db.CreateConnection();

        connection.Open();

        const string sql = """
            SELECT TOP 1 *
            FROM Contract
            WHERE Employee_ID = @EmployeeId
            ORDER BY
                CASE WHEN Status = 'Active' THEN 0 ELSE 1 END,
                StartDate DESC
            """;

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@EmployeeId", employeeId);

        using var reader = command.ExecuteReader();

        if (!reader.Read())
            return null;

        return Map(reader);
    }

    public List<Contract> GetAllByEmployeeId(int employeeId)
    {
        using var connection = Db.CreateConnection();

        connection.Open();

        const string sql = """
            SELECT *
            FROM Contract
            WHERE Employee_ID = @EmployeeId
            ORDER BY StartDate DESC
            """;

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@EmployeeId", employeeId);

        using var reader = command.ExecuteReader();

        var results = new List<Contract>();

        while (reader.Read())
            results.Add(Map(reader));

        return results;
    }

    public List<Contract> GetAll()
    {
        using var connection = Db.CreateConnection();

        connection.Open();

        const string sql = """
            SELECT *
            FROM Contract
            ORDER BY StartDate DESC
            """;

        using var command = new SqlCommand(sql, connection);

        using var reader = command.ExecuteReader();

        var results = new List<Contract>();

        while (reader.Read())
            results.Add(Map(reader));

        return results;
    }

    public int Insert(Contract contract)
    {
        using var connection = Db.CreateConnection();

        connection.Open();

        const string sql = """
            INSERT INTO Contract (Employee_ID, Role_ID, ContractType, StartDate, EndDate, Status, BaseSalary)
            OUTPUT INSERTED.Contract_ID
            VALUES (@EmployeeId, @RoleId, @ContractType, @StartDate, @EndDate, @Status, @BaseSalary)
            """;

        using var command = new SqlCommand(sql, connection);
        AddContractParameters(command, contract);

        return (int)command.ExecuteScalar();
    }

    public void Update(Contract contract)
    {
        using var connection = Db.CreateConnection();

        connection.Open();

        const string sql = """
            UPDATE Contract
            SET Employee_ID = @EmployeeId,
                Role_ID = @RoleId,
                ContractType = @ContractType,
                StartDate = @StartDate,
                EndDate = @EndDate,
                Status = @Status,
                BaseSalary = @BaseSalary
            WHERE Contract_ID = @Id
            """;

        using var command = new SqlCommand(sql, connection);
        AddContractParameters(command, contract);
        command.Parameters.AddWithValue("@Id", contract.ContractId);

        command.ExecuteNonQuery();
    }

    public void Delete(int id)
    {
        using var connection = Db.CreateConnection();

        connection.Open();

        const string sql = """
            DELETE FROM Contract
            WHERE Contract_ID = @Id
            """;

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);

        command.ExecuteNonQuery();
    }

    private static void AddContractParameters(SqlCommand command, Contract contract)
    {
        command.Parameters.AddWithValue("@EmployeeId", contract.EmployeeId);
        command.Parameters.AddWithValue("@RoleId", contract.RoleId);
        command.Parameters.AddWithValue("@ContractType", contract.ContractType);
        command.Parameters.AddWithValue("@StartDate", contract.StartDate);
        command.Parameters.AddWithValue("@EndDate", (object?)contract.EndDate ?? System.DBNull.Value);
        command.Parameters.AddWithValue("@Status", contract.Status);
        command.Parameters.AddWithValue("@BaseSalary", contract.BaseSalary);
    }

    private static Contract Map(SqlDataReader reader)
    {
        return new Contract
        {
            ContractId = (int)reader["Contract_ID"],
            EmployeeId = (int)reader["Employee_ID"],
            RoleId = (int)reader["Role_ID"],
            ContractType = reader["ContractType"].ToString()!,
            StartDate = (System.DateTime)reader["StartDate"],
            EndDate = reader["EndDate"] as System.DateTime?,
            Status = reader["Status"].ToString()!,
            BaseSalary = (decimal)reader["BaseSalary"]
        };
    }
}
