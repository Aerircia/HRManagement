using HRManagement.Models;
using HRManagement.Repositories.Interfaces;
using Microsoft.Data.SqlClient;

namespace HRManagement.Repositories;

public class AccountRepository : RepositoryBase, IAccountRepository
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

    public bool ChangePassword(int accountId, string currentPassword, string newPassword)
    {
        using var connection = Db.CreateConnection();

        connection.Open();

        const string selectSql = """
            SELECT *
            FROM Account
            WHERE Account_ID = @AccountId
            """;

        Account account;

        using (var selectCommand = new SqlCommand(selectSql, connection))
        {
            selectCommand.Parameters.AddWithValue("@AccountId", accountId);

            using var reader = selectCommand.ExecuteReader();

            if (!reader.Read())
                return false;

            account = new Account
            {
                AccountId = (int)reader["Account_ID"],
                Username = reader["Username"].ToString()!,
                Password = reader["Password"].ToString()!,
                RoleId = (int)reader["Role_ID"],
                EmployeeId = (int)reader["Employee_ID"]
            };
        }

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, account.Password))
            return false;

        var newHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

        const string updateSql = """
            UPDATE Account
            SET Password = @Password
            WHERE Account_ID = @AccountId
            """;

        using var updateCommand = new SqlCommand(updateSql, connection);

        updateCommand.Parameters.AddWithValue("@Password", newHash);
        updateCommand.Parameters.AddWithValue("@AccountId", accountId);

        var rowsAffected = updateCommand.ExecuteNonQuery();

        return rowsAffected > 0;
    }
}
