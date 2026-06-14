using Zootify_Backend.Persistance;

namespace Zootify_Backend.Utils.Extensions;

/// <summary>
/// Extension methods for <see cref="WebApplication"/> to simplify startup tasks.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Resolves the <see cref="MigrationManager"/> from the DI container and runs all pending migrations.
    /// </summary>
    /// <param name="webApplication">The running web application whose DI container is used to resolve the migrator.</param>
    public static void Migrate(this WebApplication webApplication)
    {
        using var scope = webApplication.Services.CreateScope();
        var migrator = scope.ServiceProvider.GetRequiredService<MigrationManager>();
        migrator.Migrate();
    }
}
