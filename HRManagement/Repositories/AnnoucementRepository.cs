using HRManagement.Models;
using HRManagement.Repositories.Interfaces;
using Microsoft.Data.SqlClient;

namespace HRManagement.Repositories;

public class AnnouncementRepository : RepositoryBase, IAnnouncementRepository
{
    public List<Announcement> GetActive(int take = 10)
    {
        var results = new List<Announcement>();

        using var connection = Db.CreateConnection();
        connection.Open();

        const string sql = """
            SELECT TOP (@Take) a.Announcement_ID, a.Title, a.Content, a.PostedBy,
                   e.FullName AS PostedByName, a.PostedDate, a.IsActive
            FROM Announcement a
            JOIN Account acc ON acc.Account_ID = a.PostedBy
            JOIN Employee e ON e.EmployeeID = acc.Employee_ID
            WHERE a.IsActive = 1
            ORDER BY a.PostedDate DESC
            """;

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Take", take);

        using var reader = command.ExecuteReader();

        while (reader.Read())
            results.Add(Map(reader));

        return results;
    }

    public List<Announcement> GetAll()
    {
        var results = new List<Announcement>();

        using var connection = Db.CreateConnection();
        connection.Open();

        const string sql = """
            SELECT a.Announcement_ID, a.Title, a.Content, a.PostedBy,
                   e.FullName AS PostedByName, a.PostedDate, a.IsActive
            FROM Announcement a
            JOIN Account acc ON acc.Account_ID = a.PostedBy
            JOIN Employee e ON e.EmployeeID = acc.Employee_ID
            ORDER BY a.PostedDate DESC
            """;

        using var command = new SqlCommand(sql, connection);

        using var reader = command.ExecuteReader();

        while (reader.Read())
            results.Add(Map(reader));

        return results;
    }

    public int Insert(Announcement announcement)
    {
        using var connection = Db.CreateConnection();
        connection.Open();

        const string sql = """
            INSERT INTO Announcement (Title, Content, PostedBy, PostedDate, IsActive)
            OUTPUT INSERTED.Announcement_ID
            VALUES (@Title, @Content, @PostedBy, @PostedDate, @IsActive)
            """;

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Title", announcement.Title);
        command.Parameters.AddWithValue("@Content", announcement.Content);
        command.Parameters.AddWithValue("@PostedBy", announcement.PostedBy);
        command.Parameters.AddWithValue("@PostedDate", announcement.PostedDate);
        command.Parameters.AddWithValue("@IsActive", announcement.IsActive);

        return (int)command.ExecuteScalar();
    }

    public void Update(Announcement announcement)
    {
        using var connection = Db.CreateConnection();
        connection.Open();

        const string sql = """
            UPDATE Announcement
            SET Title = @Title,
                Content = @Content,
                IsActive = @IsActive
            WHERE Announcement_ID = @Id
            """;

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Title", announcement.Title);
        command.Parameters.AddWithValue("@Content", announcement.Content);
        command.Parameters.AddWithValue("@IsActive", announcement.IsActive);
        command.Parameters.AddWithValue("@Id", announcement.AnnouncementId);

        command.ExecuteNonQuery();
    }

    public void Delete(int id)
    {
        using var connection = Db.CreateConnection();
        connection.Open();

        const string sql = """
            DELETE FROM Announcement
            WHERE Announcement_ID = @Id
            """;

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);

        command.ExecuteNonQuery();
    }

    private static Announcement Map(SqlDataReader reader)
    {
        return new Announcement
        {
            AnnouncementId = (int)reader["Announcement_ID"],
            Title = reader["Title"].ToString()!,
            Content = reader["Content"].ToString()!,
            PostedBy = (int)reader["PostedBy"],
            PostedByName = reader["PostedByName"].ToString()!,
            PostedDate = (DateTime)reader["PostedDate"],
            IsActive = (bool)reader["IsActive"]
        };
    }
}
