using Zootify_Backend.Persistance;

namespace Zootify_Backend.Utils.Extensions;

public static class WebApplicationExtensions
{
    public static void Migrate(this WebApplication webApplication)
    {
        using var scope = webApplication.Services.CreateScope();
        var migrator = scope.ServiceProvider.GetRequiredService<MigrationManager>();
        migrator.Migrate();
    }
}