using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Zootify_Backend.Configuration;

namespace Zootify_Backend.Models.DataAccess.DBContext;

/// <summary>
/// The main EF Core database context for the Zootify application.
/// </summary>
public class ZootifyDbContext : DbContext
{
    /// <summary>
    /// Parameterless constructor used by EF Core tooling (e.g., migrations).
    /// </summary>
    public ZootifyDbContext() { }

    /// <summary>
    /// Initializes the context with the provided EF Core options (used in production and tests).
    /// </summary>
    public ZootifyDbContext(DbContextOptions<ZootifyDbContext> options) : base(options) { }

    /// <summary>
    /// Configures the SQL Server connection when no options have been supplied externally.
    /// Falls back to the override connection string if the default is not set.
    /// </summary>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
        {
            return;
        }

        var connectionString = ZootifyConfiguration.DatabaseDefaultConnectionString;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = ZootifyConfiguration.DatabaseOverrideConnectionString;
        }

        optionsBuilder.UseSqlServer(
            connectionString,
            o =>
            {
                o.CommandTimeout(120);
            });
    }

    /// <summary>
    /// Discovers and applies all <see cref="IEntityTypeConfiguration{TEntity}"/> classes
    /// found in the executing assembly.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
