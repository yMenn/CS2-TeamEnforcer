using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace TeamEnforcer;

// I took this code from CS2-SimpleAdmin @ https://github.com/daffyyyy/CS2-SimpleAdmin
public class Database(string dbConnectionString, ILogger logger)
{
    private readonly ILogger _logger = logger;
    public MySqlConnection GetConnection()
    {
        try
        {
            var connection = new MySqlConnection(dbConnectionString);
            connection.Open();
            return connection;
        }
        catch (Exception ex)
        {
            _logger.LogCritical("Unable to connect to database: {message}", ex.Message);
            throw;
        }
    }

    public async Task<MySqlConnection> GetConnectionAsync()
    {
        try
        {
            var connection = new MySqlConnection(dbConnectionString);
            await connection.OpenAsync();
            return connection;
        }
        catch (Exception ex)
        {
            _logger.LogCritical("Unable to connect to database: {message}", ex.Message);
            throw;
        }
    }

    public bool CheckDatabaseConnection()
    {
        using var connection = GetConnection();

        try
        {
            return connection.Ping();
        }
        catch
        {
            return false;
        }
    }
}