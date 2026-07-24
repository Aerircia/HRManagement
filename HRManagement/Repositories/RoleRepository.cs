using HRManagement.Models;
using HRManagement.Repositories.Interfaces;
using Microsoft.Data.SqlClient;

namespace HRManagement.Repositories;

public class RoleRepository : RepositoryBase, IRoleRepository
{
    public Role? GetById(int id)
    {
        using var connection = Db.CreateConnection();

        connection.Open();

        const string sql = """
            SELECT *
            FROM Role
            WHERE Role_ID = @Id
            """;

        using var command = new SqlCommand(sql, connection);

        command.Parameters.AddWithValue("@Id", id);

        using var reader = command.ExecuteReader();

        if (!reader.Read())
            return null;

        return new Role
        {
            RoleId = (int)reader["Role_ID"],
            RoleName = reader["RoleName"].ToString()!,
            PayRate = (decimal)reader["PayRate"]
        };
    }
}