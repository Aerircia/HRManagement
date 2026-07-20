using Microsoft.Data.SqlClient;

namespace HRManagement.Data;

public class DatabaseContext
{
    private readonly string _connectionString =
        @"Server=(localdb)\MSSQLLocalDB;
          Database=HRManagement;
          Trusted_Connection=True;
          TrustServerCertificate=True;";

    public SqlConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }
}