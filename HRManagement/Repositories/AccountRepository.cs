using HRManagement.Models;
using Microsoft.Data.SqlClient;

namespace HRManagement.Repositories;

public class AccountRepository : RepositoryBase
{
    public Account? GetByUsername(string username)
    {
        using var connection = Db.CreateConnection();

        connection.Open();

        const string sql = """
            SELECT *
            FROM Account
            WHERE Username = @Username
            """;

        using var command = new SqlCommand(sql, connection);

        command.Parameters.AddWithValue("@Username", username);

        using var reader = command.ExecuteReader();

        if (!reader.Read())
            return null;

        return new Account
        {
            AccountId = (int)reader["Account_ID"],
            Username = reader["Username"].ToString()!,
            Password = reader["Password"].ToString()!,
            RoleId = (int)reader["Role_ID"],
            EmployeeId = (int)reader["Employee_ID"]
        };
    }

    public bool VerifyPassword(Account account, string password)
    {
        return BCrypt.Net.BCrypt.Verify(password, account.Password);
    }

    public Account? Login(string username, string password)
    {
        var account = GetByUsername(username);

        if (account == null)
            return null;

        if (!BCrypt.Net.BCrypt.Verify(password, account.Password))
            return null;

        return account;
    }
}