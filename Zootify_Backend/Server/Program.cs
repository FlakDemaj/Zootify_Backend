using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration.Json;
using Zootify_Backend.Configuration;
using Zootify_Backend.Utils.Extensions;

namespace Zootify_Backend.Server;

public abstract class Program
{
    /// <summary>
    /// Prevents external instantiation of the Program class.
    /// </summary>
    protected Program()
    {
    }

    /// <summary>
    /// Application entry point. Configures and runs the web application.
    /// </summary>
    /// <param name="args">Command-line arguments</param>
    public static async Task Main(string[] args)
    {
        // Configuration priority:
        // config.json →
        // appsettings →
        // User Secrets →
        // Env vars →
        // CLI args
        var builder = WebApplication.CreateBuilder(args);

        // Insert config.json at position 0 (lowest priority) so that user secrets,
        // environment variables, and command-line args all override its values.
        builder.Configuration.Sources.Insert(
            0,
            new JsonConfigurationSource
            {
                Path = "config.json", 
                Optional = true, 
                ReloadOnChange = true
            });
        
        ZootifyConfiguration.InitializeFromHost(
            builder.Configuration,
            builder.Logging.Services
                .BuildServiceProvider()
                .GetRequiredService<ILogger<BaseConfiguration>>());

        var startup = new Startup();

        startup.ConfigureServices(builder);

        builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureContainer<ContainerBuilder>(startup.ConfigureContainer);

        var app = builder.Build();

        if (!app.Environment.IsEnvironment("Testing") &&
            !app.Environment.IsEnvironment("IntegrationTest"))
        {
            app.Migrate();
        }

        startup.ConfigureMiddleware(app);

        await app.RunAsync();
    }
}