using EvolveDb;
using Microsoft.Data.SqlClient;
using Zootify_Backend.Configuration;

namespace Zootify_Backend.Persistance;

public class MigrationManager
{
    private const int CommandTimeoutInSeconds = 3600;

    private const string Schema = "evolve";
    
    private const string Location = "Persistance/Migrations/";

    private readonly ILogger<MigrationManager> _logger;

    public MigrationManager(ILogger<MigrationManager> logger)
    {
        _logger = logger;
    }

    public void Migrate()
    {
        var connectionString = ZootifyConfiguration.DatabaseDefaultConnectionString;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = ZootifyConfiguration.DatabaseOverrideConnectionString;
        }
        
        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            CommandTimeout = CommandTimeoutInSeconds
        };

        using var connection = new SqlConnection(builder.ConnectionString);

        var evolve = new Evolve(connection)
        {
            Locations = [Location],
            Schemas = [Schema],
            IsEraseDisabled = true,
            OutOfOrder = true
        };

        try
        {
            _logger.LogInformation("Starting database migration.");
            evolve.Migrate();
            _logger.LogInformation("Database migration completed successfully.");
        }
        catch (Exception exception)
        {
            _logger.LogError("Database migration failed. Exception: {Exception}", exception.Message);
            throw new Exception();
        }
    }

}