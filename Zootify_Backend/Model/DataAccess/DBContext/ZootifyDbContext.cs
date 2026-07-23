using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Zootify_Backend.Configuration;
using Zootify_Backend.Model.DataAccess.Converters;

namespace Zootify_Backend.Model.DataAccess.DBContext;

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
    /// <param name="options">The EF Core options containing the database provider configuration.</param>
    public ZootifyDbContext(DbContextOptions<ZootifyDbContext> options) : base(options) { }

    /// <summary>
    /// Configures the SQL Server connection when no options have been supplied externally.
    /// Falls back to the override connection string if the default is not set.
    /// </summary>
    /// <param name="optionsBuilder">The builder used to configure EF Core options.</param>
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
    /// Applies the UTC converters to every <see cref="DateTime"/> property so values survive the
    /// round trip through the offset-less <c>DATETIME2</c> columns with their kind intact.
    /// </summary>
    /// <param name="configurationBuilder">The builder used to configure model-wide conventions.</param>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<NullableUtcDateTimeConverter>();
    }

    /// <summary>
    /// Discovers and applies all <see cref="IEntityTypeConfiguration{TEntity}"/> classes
    /// found in the executing assembly.
    /// </summary>
    /// <param name="modelBuilder">The builder used to construct the model for the database context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
