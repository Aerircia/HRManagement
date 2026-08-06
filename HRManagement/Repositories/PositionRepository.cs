using HRManagement.Models;
using HRManagement.Repositories.Interfaces;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;

namespace HRManagement.Repositories;

public class PositionRepository : RepositoryBase, IPositionRepository
{
    public Position? GetById(int id)
    {
        using var connection = Db.CreateConnection();

        connection.Open();

        const string sql = """
            SELECT *
            FROM Position
            WHERE Position_ID = @Id
            """;

        using var command = new SqlCommand(sql, connection);

        command.Parameters.AddWithValue("@Id", id);

        using var reader = command.ExecuteReader();

        if (!reader.Read())
            return null;

        return MapPosition(reader);
    }

    public List<Position> GetAll()
    {
        var positions = new List<Position>();

        using var connection = Db.CreateConnection();

        connection.Open();

        const string sql = """
            SELECT *
            FROM Position
            ORDER BY PositionName ASC
            """;

        using var command = new SqlCommand(sql, connection);

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            positions.Add(MapPosition(reader));
        }

        return positions;
    }

    private static Position MapPosition(SqlDataReader reader)
    {
        return new Position
        {
            PositionId = (int)reader["Position_ID"],
            PositionName = reader["PositionName"].ToString()!,
            PayRate = (decimal)reader["PayRate"]
        };
    }
}
