using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Zootify_Backend.Utils.Configuration;

namespace Zootify_Backend.Models.DataAccess.DBContext;

public class ZootifyDbContext : DbContext
{
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
