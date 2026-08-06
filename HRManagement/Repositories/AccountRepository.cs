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

        return Map(reader);
    }

    public Account? GetByEmployeeId(int employeeId)
    {
        using var connection = Db.CreateConnection();

        connection.Open();

        const string sql = """
            SELECT *
            FROM Account
            WHERE Employee_ID = @EmployeeId
            """;

        using var command = new SqlCommand(sql, connection);

        command.Parameters.AddWithValue("@EmployeeId", employeeId);

        using var reader = command.ExecuteReader();

        if (!reader.Read())
            return null;

        return Map(reader);
    }

    public bool UsernameExists(string username)
    {
        using var connection = Db.CreateConnection();

        connection.Open();

        const string sql = """
            SELECT CASE WHEN EXISTS (
                SELECT 1 FROM Account WHERE Username = @Username
            ) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END
            """;

        using var command = new SqlCommand(sql, connection);

        command.Parameters.AddWithValue("@Username", username);

        var result = command.ExecuteScalar();

        return result != null && result != DBNull.Value && (bool)result;
    }

    public int Insert(Account account, string plainTextPassword)
    {
        using var connection = Db.CreateConnection();

        connection.Open();

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(plainTextPassword);

        const string sql = """
            INSERT INTO Account (Username, Password, Role_ID, Employee_ID)
            OUTPUT INSERTED.Account_ID
            VALUES (@Username, @Password, @RoleId, @EmployeeId)
            """;

        using var command = new SqlCommand(sql, connection);

        command.Parameters.AddWithValue("@Username", account.Username);
        command.Parameters.AddWithValue("@Password", passwordHash);
        command.Parameters.AddWithValue("@RoleId", account.RoleId);
        command.Parameters.AddWithValue("@EmployeeId", account.EmployeeId);

        return (int)command.ExecuteScalar();
    }

    public void DeleteByEmployeeId(int employeeId)
    {
        using var connection = Db.CreateConnection();

        connection.Open();

        const string sql = """
            DELETE FROM Account
            WHERE Employee_ID = @EmployeeId
            """;

        using var command = new SqlCommand(sql, connection);

        command.Parameters.AddWithValue("@EmployeeId", employeeId);

        command.ExecuteNonQuery();
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

            account = Map(reader);
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

    public bool SetPassword(int accountId, string newPassword)
    {
        using var connection = Db.CreateConnection();

        connection.Open();

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

    private static Account Map(SqlDataReader reader)
    {
        return new Account
        {
            AccountId = (int)reader["Account_ID"],
            Username = reader["Username"].ToString()!,
            Password = reader["Password"].ToString()!,
            RoleId = (int)reader["Role_ID"],
            EmployeeId = (int)reader["Employee_ID"]
        };
    }
}
