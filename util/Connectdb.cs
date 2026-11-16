using Oracle.ManagedDataAccess.Client;

namespace HuanBank.Api.Util;

public sealed class Database
{
    private readonly string _connectionString;

    public Database(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Oracle")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:Oracle configuration.");

        if (string.IsNullOrWhiteSpace(_connectionString))
            throw new InvalidOperationException("ConnectionStrings:Oracle must not be empty.");
    }

    public OracleConnection GetConnection()
    {
        OracleConnection conn = new(_connectionString);
        conn.Open();
        return conn;
    }
}
