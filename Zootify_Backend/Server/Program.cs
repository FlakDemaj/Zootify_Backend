using Autofac;
using Autofac.Extensions.DependencyInjection;
using Zootify_Backend.Utils.Extensions;

namespace Zootify_Backend.Server;

public class Program
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
        // WebApplication.CreateBuilder already adds (in order):
        //   1. config.json
        //   2. User Secrets (Development only)
        //   3. Environment variables
        // Initializing here gives ZootifyConfiguration access to all of them.
        var builder = WebApplication.CreateBuilder(args);
        
        builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureContainer<ContainerBuilder>(containerBuilder =>
            {
                var startup = new Startup();
                startup.ConfigureContainer(containerBuilder);
            });

        var startup = new Startup();
        startup.ConfigureServices(builder);

        var app = builder.Build();
        
        if (!app.Environment.IsEnvironment("Testing") && !app.Environment.IsEnvironment("IntegrationTest"))
        {
            app.Migrate();
        }

        startup.ConfigureMiddleware(app);

        await app.RunAsync();
    }
}